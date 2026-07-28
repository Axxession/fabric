import { Navigate } from '@tanstack/react-router';
import type { ReactNode } from 'react';

import { useCurrentActor } from '@/shared/actors/current-actor';

export function EmployeeHostRoute({ children }: { children: ReactNode }) {
  const actorQuery = useCurrentActor();

  if (actorQuery.isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">Loading visitors workspace...</p>;
  }

  if (!actorQuery.data?.isHost) {
    return <Navigate to="/employee" replace />;
  }

  return <>{children}</>;
}
