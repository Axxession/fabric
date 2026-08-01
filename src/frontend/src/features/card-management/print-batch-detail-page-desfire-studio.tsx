import { useParams } from '@tanstack/react-router';

import { PrintBatchDetailPageContent } from './print-batch-detail-page';

export default function PrintBatchDetailPageDesfireStudio() {
  const { batchId } = useParams({ from: '/desfire-studio/printing/$batchId' });

  return <PrintBatchDetailPageContent batchId={batchId} />;
}
