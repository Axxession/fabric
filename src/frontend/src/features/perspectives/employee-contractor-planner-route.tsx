import { Navigate } from '@tanstack/react-router';
import type { ReactNode } from 'react';

import { useCurrentActor } from '@/shared/actors/current-actor';

const contractorEnrollmentRole = 'contractor-enrollment';
const contractorPlanningRole = 'contractor-planning';

export function EmployeeContractorPlannerRoute({ children }: { children: ReactNode }) {
  const actorQuery = useCurrentActor();

  if (actorQuery.isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">Loading contractors workspace...</p>;
  }

  const roles = actorQuery.data?.roles ?? [];

  if (!roles.includes(contractorPlanningRole) && !roles.includes(contractorEnrollmentRole)) {
    return <Navigate to="/employee" replace />;
  }

  return <>{children}</>;
}
