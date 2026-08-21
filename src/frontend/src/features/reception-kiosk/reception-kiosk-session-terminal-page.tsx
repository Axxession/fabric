import { Navigate, useNavigate } from '@tanstack/react-router';
import { AlertCircle, Home } from 'lucide-react';

import { buttonVariants } from '@/shared/components/ui/button';

import { clearActiveCourse, clearComplianceLaunch } from './reception-kiosk-compliance';
import { getReceptionKioskTerminalCopy, useReceptionKioskCurrentSession } from './reception-kiosk-session';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskSessionTerminalPage() {
  const navigate = useNavigate();
  const sessionQuery = useReceptionKioskCurrentSession();

  if (!hasReceptionKioskSettings()) return <Navigate to="/reception-kiosk/setup" replace />;
  if (sessionQuery.isLoading) return null;
  if (sessionQuery.isError || !sessionQuery.data) return <Navigate to="/reception-kiosk" replace />;
  if (sessionQuery.data.status === 'Active') return <Navigate to="/reception-kiosk/session" replace />;

  const copy = getReceptionKioskTerminalCopy(sessionQuery.data.stopReason, sessionQuery.data.stopMessage);

  function goHome() {
    clearActiveCourse();
    clearComplianceLaunch();
    void navigate({ to: '/reception-kiosk' });
  }

  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-8 text-center shadow-sm sm:p-12">
      <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-error-background text-error">
        <AlertCircle className="size-12" aria-hidden="true" />
      </div>
      <p className="mt-8 text-[14px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">Session ended</p>
      <h2 className="mt-3 text-[36px] font-semibold tracking-tight sm:text-[56px]">{copy.title}</h2>
      <p className="mx-auto mt-5 max-w-2xl text-[18px] leading-8 text-muted-foreground sm:text-[22px] sm:leading-9">{copy.message}</p>
      <div className="mt-10 grid gap-4 sm:grid-cols-1">
        <button type="button" className={buttonVariants({ size: 'lg', className: 'h-16 rounded-[1rem] text-[20px]' })} onClick={goHome}>
          <Home className="size-6" aria-hidden="true" />
          Home
        </button>
      </div>
    </section>
  );
}
