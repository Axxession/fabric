import { useMutation, useQuery } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api, getAccessToken } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { ScormPlayer } from './scorm-player';

type CourseLanguageResponse = components['schemas']['CourseLanguageResponse'];
type EnrollmentResponse = components['schemas']['EnrollmentResponse'];

type StartLaunchSessionResponse = {
  token: string;
};

export default function ScormTestPage() {
  const { enrollmentId } = useParams({ from: '/main/scorm/test/$enrollmentId' });
  const [selectedLanguageId, setSelectedLanguageId] = useState('');
  const [launchToken, setLaunchToken] = useState<string | null>(null);

  const enrollmentQuery = useQuery({
    queryKey: ['learning', 'enrollment', enrollmentId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/learning/enrollments/{id}', { params: { path: { id: enrollmentId } } });
      if (error || !data) throw new Error('Could not load enrollment.');
      return data as EnrollmentResponse;
    },
  });

  const languagesQuery = useQuery({
    queryKey: ['learning', 'course', enrollmentQuery.data?.courseId, 'languages'],
    enabled: Boolean(enrollmentQuery.data?.courseId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/learning/courses/{id}/languages', { params: { path: { id: enrollmentQuery.data!.courseId } } });
      if (error || !data) throw new Error('Could not load languages.');
      return (data as CourseLanguageResponse[]).filter((item) => item.isActive && item.currentVersionId);
    },
  });

  const startSession = useMutation({
    mutationFn: async (languageId: string) => {
      const token = getAccessToken();
      const response = await fetch('/api/learning/runtime/sessions', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({ enrollmentId, languageId, scoId: null }),
      });

      if (!response.ok) throw new Error('Could not start session.');
      return await response.json() as StartLaunchSessionResponse;
    },
    onSuccess: ({ token }) => {
      setLaunchToken(token);
      toast.success('SCORM session started.');
    },
    onError: () => {
      setLaunchToken(null);
      toast.error('Could not start SCORM session.');
    },
  });

  const languages = languagesQuery.data ?? [];

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}><ArrowLeft className="size-4" aria-hidden="true" /></Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">SCORM test player</h2>
          <p className="mt-2 max-w-3xl text-[14px] text-muted-foreground">Select a language version for this enrollment. The page starts a launch session and renders the token-only SCORM player.</p>
        </div>
      </header>

      <Card className="grid gap-4 p-6">
        <div className="grid gap-2">
          <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Launch</p>
          <h3 className="text-[18px] font-semibold tracking-tight text-foreground">Choose language</h3>
          <p className="text-[14px] text-muted-foreground">Active languages with a current SCORM version are available below.</p>
        </div>

        {enrollmentQuery.isLoading || languagesQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading enrollment and language data...</p> : null}
        {enrollmentQuery.error || languagesQuery.error ? <p className="text-[14px] text-error">Could not load SCORM launch data.</p> : null}

        <label className="grid max-w-sm gap-2 text-[14px] font-medium">
          <span>Language</span>
          <select
            className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary"
            value={selectedLanguageId}
            onChange={(event) => {
              const nextLanguageId = event.target.value;
              setSelectedLanguageId(nextLanguageId);
              setLaunchToken(null);
              if (nextLanguageId) startSession.mutate(nextLanguageId);
            }}
            disabled={startSession.isPending || languages.length === 0}
          >
            <option value="">Select language</option>
            {languages.map((item) => <option key={item.id} value={item.id}>{item.displayLabel} ({item.languageCode})</option>)}
          </select>
        </label>

        {languages.length === 0 && !languagesQuery.isLoading ? <p className="text-[14px] text-muted-foreground">No active language with a current version is available for this course.</p> : null}
      </Card>

      {launchToken ? <ScormPlayer key={launchToken} token={launchToken} onExit={() => setLaunchToken(null)} /> : null}
    </div>
  );
}
