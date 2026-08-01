import { useParams } from '@tanstack/react-router';

import { DiversificationStrategyEditPageContent } from './diversification-strategy-form-page';

export default function DiversificationStrategyEditPageDesfireStudio() {
  const { strategyId } = useParams({ from: '/desfire-studio/diversification-strategies/$strategyId/edit' });

  return <DiversificationStrategyEditPageContent strategyId={strategyId} />;
}
