import VisitsPage from '@/features/visitors-management/visits-page';

export default function EmployeeVisitorsPage() {
  return (
    <VisitsPage
      title="Visitors"
      description="Review your scheduled visits, expected arrivals, and visitor coordination for the days ahead."
      createTo="/employee/visitors/new"
      editTo="/employee/visitors/$visitId/edit"
      storageKey="fabric.employee-visitors.calendar"
    />
  );
}
