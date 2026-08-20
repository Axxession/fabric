import { useQuery } from '@tanstack/react-query';
import { AlertCircle, LoaderCircle, X } from 'lucide-react';
import { Scorm12API, Scorm2004API } from 'scorm-again';
import { useEffect, useRef, useState } from 'react';

import { apiBaseUrl } from '@/shared/api/client';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

type ScormVersion = 'Scorm12' | 'Scorm2004';

type CourseScoResponse = {
  id: string;
  courseVersionId: string;
  scoIdentifier: string;
  title: string;
  launchUrl: string;
  resourcePath: string;
  manifestOrder: number;
  masteryScore: number | string | null;
};

type LaunchSessionBootstrapResponse = {
  enrollmentId: string;
  courseId: string;
  courseVersionId: string;
  courseLanguageId: string;
  scormVersion: ScormVersion;
  attemptId: string | null;
  activeScoId: string | null;
  contentBaseUrl: string;
  launchPath: string;
  expiresAt: string;
  scos: CourseScoResponse[];
};

type ScormProgressResponse = {
  id: string;
  attemptId: string;
  courseId: string;
  courseVersionId: string;
  scoId: string | null;
  identityId: string;
  scormVersion: ScormVersion;
  completionStatus: string | null;
  successStatus: string | null;
  score: number | null;
  scoreScaled: number | null;
  bookmarkLocation: string | null;
  sessionTime: string | null;
  suspendData: string | null;
  rawCmiData: string;
  lastCommittedAt: string;
};

type RuntimePersistRequest = {
  token: string;
  scoId: string | null;
  completionStatus: string | null;
  successStatus: string | null;
  score: number | null;
  scoreScaled: number | null;
  bookmarkLocation: string | null;
  sessionTime: string | null;
  suspendData: string | null;
  isCompleted: boolean;
  rawCmiData: string;
};

type ScormPlayerProps = {
  token: string;
  onExit?: () => void;
  onComplete?: (result: { completionStatus: string | null; successStatus: string | null; score: number | null }) => void;
};

type ScormApiInstance = Scorm12API | Scorm2004API;

declare global {
  interface Window {
    API?: Scorm12API;
    API_1484_11?: Scorm2004API;
  }
}

export function ScormPlayer({ token, onExit, onComplete }: ScormPlayerProps) {
  const runtimeRef = useRef<ScormApiInstance | null>(null);
  const completionReportedRef = useRef(false);
  const [launchUrl, setLaunchUrl] = useState<string | null>(null);
  const [saveState, setSaveState] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');

  const bootstrapQuery = useQuery({
    queryKey: ['learning', 'runtime', 'session', token],
    queryFn: async () => requestJson<LaunchSessionBootstrapResponse>(`/api/learning/runtime/sessions/${encodeURIComponent(token)}`),
  });

  const progressQuery = useQuery({
    queryKey: ['learning', 'runtime', 'progress', token, bootstrapQuery.data?.activeScoId],
    enabled: Boolean(bootstrapQuery.data),
    queryFn: async () => requestOptionalJson<ScormProgressResponse>(`/api/learning/runtime/progress?token=${encodeURIComponent(token)}&scoId=${bootstrapQuery.data?.activeScoId ?? ''}`),
  });

  useEffect(() => {
    if (!bootstrapQuery.data || progressQuery.isLoading) return;

    completionReportedRef.current = false;
    const bootstrap = bootstrapQuery.data;
    const runtime = createRuntime(bootstrap.scormVersion);
    runtimeRef.current = runtime;

    if (progressQuery.data?.rawCmiData) {
      try {
        const rawState = JSON.parse(progressQuery.data.rawCmiData) as Record<string, unknown>;
        loadRuntimeState(runtime, rawState);
      } catch {
        // Ignore malformed persisted state and start with fresh runtime state.
      }
    }

    const persistProgress = async (isTerminateCommit: boolean) => {
      setSaveState('saving');
      const payload = buildPersistRequest(token, bootstrap, runtime);

      try {
        await requestJson<ScormProgressResponse>('/api/learning/runtime/progress', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        });
        setSaveState('saved');

        if (payload.isCompleted && !completionReportedRef.current) {
          completionReportedRef.current = true;
          onComplete?.({ completionStatus: payload.completionStatus, successStatus: payload.successStatus, score: payload.score });
        }
      } catch {
        setSaveState('error');
      }
    };

    const onCommit = () => { void persistProgress(false); };
    const onTerminate = () => { void persistProgress(true); };

    runtime.on('Commit', onCommit);
    runtime.on('Terminate LMSFinish', onTerminate);

    if (bootstrap.scormVersion === 'Scorm12') {
      window.API = runtime as Scorm12API;
      window.API_1484_11 = undefined;
    } else {
      window.API = undefined;
      window.API_1484_11 = runtime as Scorm2004API;
    }

    setLaunchUrl(resolveRuntimeUrl(`${bootstrap.contentBaseUrl}/${bootstrap.launchPath}`));

    return () => {
      runtime.off('Commit', onCommit);
      runtime.off('Terminate LMSFinish', onTerminate);
      runtimeRef.current = null;
      window.API = undefined;
      window.API_1484_11 = undefined;
    };
  }, [bootstrapQuery.data, onComplete, progressQuery.data, progressQuery.isLoading, token]);

  if (bootstrapQuery.isLoading || progressQuery.isLoading) {
    return <StateCard icon={<LoaderCircle className="size-5 animate-spin" />} title="Preparing SCORM session" detail="Loading session data, learner state, and runtime bridge." />;
  }

  if (bootstrapQuery.error || progressQuery.error || !bootstrapQuery.data || !launchUrl) {
    return <StateCard icon={<AlertCircle className="size-5" />} title="Could not load SCORM player" detail="Session bootstrap or learner progress could not be loaded." tone="error" />;
  }

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-structural border border-border bg-content px-4 py-3">
        <div>
          <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">SCORM session</p>
          <p className="mt-1 text-[14px] text-foreground">{bootstrapQuery.data.scos.find((item) => item.id === bootstrapQuery.data.activeScoId)?.title ?? 'Launch SCO'}</p>
        </div>
        <div className="flex items-center gap-2 text-[13px] text-muted-foreground">
          <span>{saveState === 'saving' ? 'Saving progress...' : saveState === 'saved' ? 'Progress saved' : saveState === 'error' ? 'Progress save failed' : 'Runtime ready'}</span>
          {onExit ? <Button type="button" variant="outline" size="sm" onClick={onExit}><X className="size-4" aria-hidden="true" />Exit</Button> : null}
        </div>
      </div>

      <Card className="overflow-hidden p-0">
        <iframe title="SCORM player" src={launchUrl} className="h-[78vh] w-full border-0 bg-white" allow="fullscreen" />
      </Card>
    </div>
  );
}

function createRuntime(version: ScormVersion): ScormApiInstance {
  const settings = {
    autocommit: true,
    autocommitSeconds: 10,
    lmsCommitUrl: false,
    syncOnInitialize: false,
    syncOnTerminate: false,
  };

  return version === 'Scorm12' ? new Scorm12API(settings) : new Scorm2004API(settings);
}

function buildPersistRequest(token: string, bootstrap: LaunchSessionBootstrapResponse, runtime: ScormApiInstance): RuntimePersistRequest {
  const snapshot = runtime.renderCMIToJSONObject() as Record<string, unknown>;

  if (bootstrap.scormVersion === 'Scorm12') {
    const lessonStatus = readString(snapshot, 'cmi.core.lesson_status');
    const successStatus = lessonStatus === 'passed' ? 'passed' : lessonStatus === 'failed' ? 'failed' : null;
    const completionStatus = lessonStatus === 'completed' || lessonStatus === 'passed' || lessonStatus === 'failed' ? 'completed' : lessonStatus === 'incomplete' ? 'incomplete' : null;
    return {
      token,
      scoId: bootstrap.activeScoId,
      completionStatus,
      successStatus,
      score: readNumber(snapshot, 'cmi.core.score.raw'),
      scoreScaled: null,
      bookmarkLocation: readString(snapshot, 'cmi.core.lesson_location'),
      sessionTime: readString(snapshot, 'cmi.core.session_time'),
      suspendData: readString(snapshot, 'cmi.suspend_data'),
      isCompleted: completionStatus === 'completed' || successStatus === 'passed' || successStatus === 'failed',
      rawCmiData: JSON.stringify(snapshot),
    };
  }

  const completionStatus = readString(snapshot, 'cmi.completion_status');
  const successStatus = readString(snapshot, 'cmi.success_status');
  return {
    token,
    scoId: bootstrap.activeScoId,
    completionStatus,
    successStatus,
    score: readNumber(snapshot, 'cmi.score.raw'),
    scoreScaled: readNumber(snapshot, 'cmi.score.scaled'),
    bookmarkLocation: readString(snapshot, 'cmi.location'),
    sessionTime: readString(snapshot, 'cmi.session_time'),
    suspendData: readString(snapshot, 'cmi.suspend_data'),
    isCompleted: completionStatus === 'completed' || successStatus === 'passed' || successStatus === 'failed',
    rawCmiData: JSON.stringify(snapshot),
  };
}

function readString(snapshot: Record<string, unknown>, key: string) {
  const value = readValue(snapshot, key);
  return typeof value === 'string' && value.trim() !== '' ? value : null;
}

function readNumber(snapshot: Record<string, unknown>, key: string) {
  const value = readValue(snapshot, key);
  if (typeof value === 'number' && Number.isFinite(value)) return value;
  if (typeof value === 'string' && value.trim() !== '') {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  return null;
}

function readValue(snapshot: Record<string, unknown>, key: string) {
  const flatValue = snapshot[key];
  if (flatValue !== undefined) return flatValue;

  const segments = key.split('.');
  let current: unknown = snapshot;
  for (const segment of segments) {
    if (!current || typeof current !== 'object' || !(segment in current)) return undefined;
    current = (current as Record<string, unknown>)[segment];
  }

  return current;
}

function loadRuntimeState(runtime: ScormApiInstance, rawState: Record<string, unknown>) {
  if (isFlattenedCmiState(rawState)) {
    runtime.loadFromFlattenedJSON(rawState);
    return;
  }

  if (isNestedCmiState(rawState)) {
    runtime.loadFromJSON(rawState);
    return;
  }

  runtime.loadFromFlattenedJSON(flattenObject(rawState));
}

function isFlattenedCmiState(rawState: Record<string, unknown>) {
  return Object.keys(rawState).some((key) => key.startsWith('cmi.') || key.startsWith('adl.'));
}

function isNestedCmiState(rawState: Record<string, unknown>) {
  return 'cmi' in rawState || 'adl' in rawState;
}

function flattenObject(value: unknown, prefix = ''): Record<string, unknown> {
  if (!value || typeof value !== 'object') {
    return prefix ? { [prefix]: value } : {};
  }

  const entries = Object.entries(value as Record<string, unknown>);
  if (entries.length === 0) {
    return prefix ? { [prefix]: value } : {};
  }

  return entries.reduce<Record<string, unknown>>((result, [key, child]) => {
    const nextPrefix = prefix ? `${prefix}.${key}` : key;
    if (child && typeof child === 'object' && !Array.isArray(child)) {
      Object.assign(result, flattenObject(child, nextPrefix));
    } else {
      result[nextPrefix] = child;
    }

    return result;
  }, {});
}

function resolveRuntimeUrl(path: string) {
  if (/^https?:\/\//.test(path)) return path;
  return `${apiBaseUrl}${path}`;
}

async function requestOptionalJson<T>(path: string, init?: RequestInit) {
  const response = await fetch(resolveRuntimeUrl(path), init);
  if (response.status === 204) return null;
  if (!response.ok) throw new Error(`Request failed with status ${response.status}`);
  return await response.json() as T;
}

async function requestJson<T>(path: string, init?: RequestInit) {
  const response = await fetch(resolveRuntimeUrl(path), init);
  if (!response.ok) throw new Error(`Request failed with status ${response.status}`);
  return await response.json() as T;
}

function StateCard({ icon, title, detail, tone = 'default' }: { readonly icon: React.ReactNode; readonly title: string; readonly detail: string; readonly tone?: 'default' | 'error'; }) {
  return (
    <Card className={tone === 'error' ? 'border-error/40 p-6' : 'p-6'}>
      <div className="flex items-start gap-3">
        <div className={tone === 'error' ? 'text-error' : 'text-muted-foreground'}>{icon}</div>
        <div>
          <h3 className="text-[18px] font-semibold tracking-tight text-foreground">{title}</h3>
          <p className="mt-2 text-[14px] text-muted-foreground">{detail}</p>
        </div>
      </div>
    </Card>
  );
}
