import { useNavigate } from '@tanstack/react-router';

import { AutomationPageShell } from './automation-page-shell';
import { WorkflowInstanceList } from './elsa-studio-elements';

export default function WorkflowInstancesPage() {
  const navigate = useNavigate();

  return (
    <AutomationPageShell title="Workflow Instances" description="Inspect workflow runs, statuses, execution history, and active automation work.">
      {(props) => (
        <WorkflowInstanceList
          {...props}
          onViewWorkflowInstance={(instanceId) => void navigate({ to: '/administration/automation/workflow-instances/$instanceId', params: { instanceId } })}
        />
      )}
    </AutomationPageShell>
  );
}
