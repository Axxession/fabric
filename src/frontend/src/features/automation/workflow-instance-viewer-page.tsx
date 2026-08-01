import { useQueryClient } from '@tanstack/react-query';
import { useAuth } from 'react-oidc-context';
import { useNavigate, useParams } from '@tanstack/react-router';

import { elsaApiBaseUrl } from './automation-settings';
import { ElsaStudioViewerScreen } from './elsa-studio-fullscreen';
import { workflowDefinitionsQueryKey, workflowHistoryQueryKey } from './workflow-query-keys';

export default function WorkflowInstanceViewerPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { instanceId } = useParams({ from: '/main/administration/automation/workflow-instances/$instanceId' });

  return (
    <ElsaStudioViewerScreen
      instanceId={instanceId}
      runtime={{ remoteEndpoint: elsaApiBaseUrl, accessToken: auth.user?.access_token }}
      onEditWorkflowDefinition={(definitionId) => {
        void queryClient.invalidateQueries({ queryKey: workflowDefinitionsQueryKey });
        void queryClient.invalidateQueries({ queryKey: workflowHistoryQueryKey });
        void navigate({ to: '/administration/automation/workflow-definitions/$definitionId/edit', params: { definitionId } });
      }}
    />
  );
}
