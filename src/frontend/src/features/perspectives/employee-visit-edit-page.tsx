import { useParams } from '@tanstack/react-router';

import { VisitEditPageContent } from '@/features/visitors-management/visit-edit-page';

export default function EmployeeVisitEditPage() {
  const { visitId } = useParams({ from: '/main/employee/visitors/$visitId/edit' });
  return <VisitEditPageContent visitId={visitId} />;
}
