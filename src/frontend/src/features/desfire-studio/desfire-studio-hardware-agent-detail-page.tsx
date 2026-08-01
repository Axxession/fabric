import { useParams } from '@tanstack/react-router';

import { HardwareAgentDetailPageContent } from '@/features/facility/hardware-agent-detail-page';

export default function DesfireStudioHardwareAgentDetailPage() {
  const { agentId } = useParams({ from: '/desfire-studio/hardware-agents/$agentId' });

  return <HardwareAgentDetailPageContent agentId={agentId} backTo="/desfire-studio/hardware-agents" />;
}
