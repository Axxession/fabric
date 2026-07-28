import { useQueryClient } from '@tanstack/react-query';
import { useAuth } from 'react-oidc-context';
import { useNavigate, useParams } from '@tanstack/react-router';

import { elsaApiBaseUrl } from './automation-settings';
import { ElsaStudioEditorScreen } from './elsa-studio-fullscreen';
import { workflowDefinitionsQueryKey, workflowHistoryQueryKey } from './workflow-query-keys';

export default function WorkflowDefinitionEditorPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { definitionId } = useParams({ from: '/main/administration/automation/workflow-definitions/$definitionId/edit' });

  return (
    <ElsaStudioEditorScreen
      definitionId={definitionId}
      runtime={{ remoteEndpoint: elsaApiBaseUrl, accessToken: auth.user?.access_token }}
      onWorkflowDefinitionExecuted={(workflowInstanceId) => {
        void queryClient.invalidateQueries({ queryKey: workflowDefinitionsQueryKey });
        void queryClient.invalidateQueries({ queryKey: workflowHistoryQueryKey });
        void navigate({ to: '/administration/automation/workflow-instances/$instanceId', params: { instanceId: workflowInstanceId } });
      }}
    />
  );
}
