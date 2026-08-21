import { Navigate } from '@tanstack/react-router';

import { hasReceptionKioskSettings } from './reception-kiosk-settings';
import { getReceptionKioskSessionPath, useReceptionKioskCurrentSession } from './reception-kiosk-session';

export default function ReceptionKioskSessionPage() {
  const sessionQuery = useReceptionKioskCurrentSession();

  if (!hasReceptionKioskSettings()) {
    return <Navigate to="/reception-kiosk/setup" replace />;
  }

  if (sessionQuery.isLoading) {
    return (
      <section className="w-full rounded-[2rem] border border-border bg-content p-8 text-center shadow-sm sm:p-12">
        <p className="text-[14px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">Loading</p>
        <h2 className="mt-3 text-[36px] font-semibold tracking-tight sm:text-[56px]">Preparing session</h2>
        <p className="mx-auto mt-5 max-w-2xl text-[18px] leading-8 text-muted-foreground sm:text-[22px] sm:leading-9">Checking the current kiosk step.</p>
      </section>
    );
  }

  if (sessionQuery.isError || !sessionQuery.data) {
    return <Navigate to="/reception-kiosk" replace />;
  }

  return <Navigate to={getReceptionKioskSessionPath(sessionQuery.data)} replace />;
}
