import { useParams } from '@tanstack/react-router';

import { PrintRunDetailPageContent } from './print-run-detail-page';

export default function PrintRunDetailPageDesfireStudio() {
  const { runId } = useParams({ from: '/desfire-studio/printing/runs/$runId' });

  return <PrintRunDetailPageContent runId={runId} />;
}
