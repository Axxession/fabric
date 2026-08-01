import { useLocation, useNavigate } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';

import { ReceptionDeskWorkstationAdminPanel } from '@/features/reception-desk/reception-desk-workstation-admin-panel';
import { HardwareAgentsPanel } from '@/features/facility/hardware-agents-panel';
import { ReceptionKioskAdminPanel } from '@/features/reception-kiosk/reception-kiosk-admin-panel';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

type ClientsTab = 'hardware-agents' | 'reception-desk-kiosk' | 'reception-desk-workstations';

export default function ClientsPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { t } = useTranslation();
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
          <TabsList aria-label={t('administration.clients.tabsAriaLabel')} className="h-auto w-fit max-w-full flex-wrap justify-start gap-1">
            <TabsTrigger value="hardware-agents">{t('administration.clients.hardwareAgents')}</TabsTrigger>
            <TabsTrigger value="reception-desk-kiosk">{t('administration.clients.receptionKiosk')}</TabsTrigger>
            <TabsTrigger value="reception-desk-workstations">{t('administration.clients.receptionDeskWorkstations')}</TabsTrigger>
          </TabsList>

        <TabsContent value="hardware-agents" className="pt-4">
          <HardwareAgentsPanel />
        </TabsContent>

        <TabsContent value="reception-desk-kiosk" className="pt-4">
          <ReceptionKioskAdminPanel />
        </TabsContent>

        <TabsContent value="reception-desk-workstations" className="pt-4">
          <ReceptionDeskWorkstationAdminPanel />
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
