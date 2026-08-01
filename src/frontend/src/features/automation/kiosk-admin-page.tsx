import { AutomationSectionNav } from '@/features/automation/automation-section-nav';
import { WorkflowKiosksPanel } from '@/features/automation/workflow-kiosks-panel';

export default function KioskAdminPage() {
  return (
    <section className="grid gap-4">
      <AutomationSectionNav />

      <WorkflowKiosksPanel />
    </section>
  );
}
