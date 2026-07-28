import { useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { type PropsWithChildren, useEffect, useRef, useState } from 'react';

import { WorkflowDefinitionEditor, WorkflowInstanceViewer } from './elsa-studio-elements';
import { type ElsaRuntimeProps } from './automation-page-shell';
import { useElsaStudioAssets } from './elsa-studio-assets';
import { workflowDefinitionsQueryKey, workflowHistoryQueryKey } from './workflow-query-keys';

const fullscreenRouteExpression = /^\/administration\/automation\/(workflow-definitions\/[^/]+\/edit|workflow-instances\/[^/]+)$/;
const editorElements = ['elsa-workflow-definition-editor'] as const;
const viewerElements = ['elsa-workflow-instance-viewer'] as const;

export function isElsaStudioFullscreenRoute(pathname: string) {
  return fullscreenRouteExpression.test(pathname);
}

export function ElsaStudioEditorScreen({ definitionId, runtime, onWorkflowDefinitionExecuted }: { readonly definitionId: string; readonly runtime: ElsaRuntimeProps; readonly onWorkflowDefinitionExecuted?: (workflowInstanceId: string) => void }) {
  const assets = useElsaStudioAssets(editorElements);
  const runtimeKey = useElsaRuntimeKey(runtime.accessToken);
  useElsaStudioFullscreenDocument();

  return (
    <ElsaStudioFullscreenFrame title="Workflow Definition Editor" backTo="/administration/automation/workflow" backLabel="Back to workflow definitions">
      {assets.status === 'ready' ? <WorkflowDefinitionEditor key={`${definitionId}:${runtimeKey}`} {...runtime} definitionId={definitionId} onWorkflowDefinitionExecuted={onWorkflowDefinitionExecuted} className="fabric-elsa-studio-root block h-full w-full" /> : null}
      {assets.status === 'loading' ? <ElsaStudioStatusMessage>Loading Elsa Studio editor...</ElsaStudioStatusMessage> : null}
      {assets.status === 'error' ? <ElsaStudioStatusMessage tone="error">{assets.error ?? 'Could not load Elsa Studio editor.'}</ElsaStudioStatusMessage> : null}
    </ElsaStudioFullscreenFrame>
  );
}

export function ElsaStudioViewerScreen({ instanceId, runtime, onEditWorkflowDefinition }: { readonly instanceId: string; readonly runtime: ElsaRuntimeProps; readonly onEditWorkflowDefinition?: (definitionId: string) => void }) {
  const assets = useElsaStudioAssets(viewerElements);
  const runtimeKey = useElsaRuntimeKey(runtime.accessToken);
  useElsaStudioFullscreenDocument();

  return (
    <ElsaStudioFullscreenFrame title="Workflow Instance Viewer" backTo="/administration/automation/workflow?tab=history" backLabel="Back to workflow instances">
      {assets.status === 'ready' ? <WorkflowInstanceViewer key={`${instanceId}:${runtimeKey}`} {...runtime} instanceId={instanceId} onEditWorkflowDefinition={onEditWorkflowDefinition} className="fabric-elsa-studio-root block h-full w-full" /> : null}
      {assets.status === 'loading' ? <ElsaStudioStatusMessage>Loading Elsa Studio viewer...</ElsaStudioStatusMessage> : null}
      {assets.status === 'error' ? <ElsaStudioStatusMessage tone="error">{assets.error ?? 'Could not load Elsa Studio viewer.'}</ElsaStudioStatusMessage> : null}
    </ElsaStudioFullscreenFrame>
  );
}

function useElsaRuntimeKey(accessToken: string | undefined) {
  const [runtimeKey, setRuntimeKey] = useState(0);
  const previousAccessToken = useRef(accessToken);

  useEffect(() => {
    if (previousAccessToken.current === accessToken)
      return;

    previousAccessToken.current = accessToken;
    setRuntimeKey((current) => current + 1);
  }, [accessToken]);

  return runtimeKey;
}

function ElsaStudioFullscreenFrame({ title, backTo, backLabel, children }: PropsWithChildren<{ readonly title: string; readonly backTo: string; readonly backLabel: string }>) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  function goBack() {
    void queryClient.invalidateQueries({ queryKey: workflowDefinitionsQueryKey });
    void queryClient.invalidateQueries({ queryKey: workflowHistoryQueryKey });
    void navigate({ to: backTo as never });
  }

  return (
    <section className="flex h-screen flex-col overflow-hidden bg-background text-foreground">
      <div className="flex items-center gap-4 border-b border-border bg-content px-4 py-3 sm:px-5">
        <button type="button" onClick={goBack} className="rounded-interactive border border-border px-3 py-2 text-[14px] font-semibold transition hover:bg-hover-blue">
          {backLabel}
        </button>
        <div className="min-w-0">
          <h1 className="truncate text-[18px] font-semibold tracking-tight sm:text-[20px]">{title}</h1>
          <p className="text-[12px] text-muted-foreground sm:text-[13px]">Fullscreen Elsa Studio workspace.</p>
        </div>
      </div>
      <div className="min-h-0 flex-1 overflow-hidden">{children}</div>
    </section>
  );
}

function ElsaStudioStatusMessage({ children, tone = 'muted' }: PropsWithChildren<{ readonly tone?: 'muted' | 'error' }>) {
  return (
    <div className="flex h-full items-center justify-center p-6">
      <p className={tone === 'error' ? 'text-[14px] text-error' : 'text-[14px] text-muted-foreground'}>{children}</p>
    </div>
  );
}

function useElsaStudioFullscreenDocument() {
  useEffect(() => {
    document.documentElement.classList.add('elsa-studio-fullscreen');
    document.body.classList.add('elsa-studio-fullscreen');

    return () => {
      document.documentElement.classList.remove('elsa-studio-fullscreen');
      document.body.classList.remove('elsa-studio-fullscreen');
    };
  }, []);
}
