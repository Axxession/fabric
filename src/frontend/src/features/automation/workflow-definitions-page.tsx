import { useNavigate } from '@tanstack/react-router';

import { AutomationPageShell } from './automation-page-shell';
import { WorkflowDefinitionList } from './elsa-studio-elements';

export default function WorkflowDefinitionsPage() {
  const navigate = useNavigate();

  return (
    <AutomationPageShell title="Workflow Definitions" description="Browse workflow definitions and open the Elsa Studio designer for edits.">
      {(props) => (
        <WorkflowDefinitionList
          {...props}
          onEditWorkflowDefinition={(definitionId) => void navigate({ to: '/administration/automation/workflow-definitions/$definitionId/edit', params: { definitionId } })}
        />
      )}
    </AutomationPageShell>
  );
}
