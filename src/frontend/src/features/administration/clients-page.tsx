import { useLocation, useNavigate } from '@tanstack/react-router';

import { HardwareAgentsPanel } from '@/features/facility/hardware-agents-panel';
import { ReceptionKioskAdminPanel } from '@/features/reception-kiosk/reception-kiosk-admin-panel';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

type ClientsTab = 'hardware-agents' | 'reception-desk-kiosk' | 'reception-desk-workstations';

export default function ClientsPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = getActiveTab(location.searchStr);

  function changeTab(nextTab: string) {
    if (!isClientsTab(nextTab)) {
      return;
    }

    void navigate({ to: '/administration/clients', search: { tab: nextTab } as never, replace: true });
  }

  return (
    <section className="rounded-structural border border-border bg-content p-4 sm:p-6">
        <Tabs value={activeTab} onValueChange={changeTab}>
          <TabsList aria-label="Client sections" className="h-auto w-fit max-w-full flex-wrap justify-start gap-1">
            <TabsTrigger value="hardware-agents">Hardware Agents</TabsTrigger>
            <TabsTrigger value="reception-desk-kiosk">Reception Desk Kiosk</TabsTrigger>
            <TabsTrigger value="reception-desk-workstations">Reception Desk Workstations</TabsTrigger>
          </TabsList>

        <TabsContent value="hardware-agents" className="pt-4">
          <HardwareAgentsPanel />
        </TabsContent>

        <TabsContent value="reception-desk-kiosk" className="pt-4">
          <ReceptionKioskAdminPanel />
        </TabsContent>

        <TabsContent value="reception-desk-workstations" className="pt-4">
          <div className="rounded-structural border border-border p-6">
            <Empty>
              <EmptyHeader>
                <EmptyTitle>Reception desk workstations not available yet</EmptyTitle>
                <EmptyDescription>This section is reserved for future workstation management in the reception desk domain.</EmptyDescription>
              </EmptyHeader>
            </Empty>
          </div>
        </TabsContent>
      </Tabs>
    </section>
  );
}

function getActiveTab(searchStr: string): ClientsTab {
  const tab = new URLSearchParams(searchStr).get('tab');
  return isClientsTab(tab) ? tab : 'hardware-agents';
}

function isClientsTab(value: string | null | undefined): value is ClientsTab {
  return value === 'hardware-agents' || value === 'reception-desk-kiosk' || value === 'reception-desk-workstations';
}
