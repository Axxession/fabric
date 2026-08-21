import { Navigate, useNavigate } from '@tanstack/react-router';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { CheckCircle2 } from 'lucide-react';

import { Button, buttonVariants } from '@/shared/components/ui/button';

import { finalizeReceptionKioskSession, stopReceptionKioskSession } from './reception-kiosk-api';
import { clearActiveCourse, clearComplianceLaunch } from './reception-kiosk-compliance';
import { receptionKioskCurrentSessionQueryKey, useReceptionKioskCurrentSession } from './reception-kiosk-session';
import { saveReceptionKioskResult } from './reception-kiosk-result';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskSessionOnboardPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const sessionQuery = useReceptionKioskCurrentSession();

  const finalizeMutation = useMutation({
    mutationFn: async () => await finalizeReceptionKioskSession(),
    onSuccess: async () => {
      clearActiveCourse();
      clearComplianceLaunch();
      saveReceptionKioskResult('onboarding-success');
      await queryClient.invalidateQueries({ queryKey: receptionKioskCurrentSessionQueryKey });
      await navigate({ to: '/reception-kiosk/success' });
    },
    onError: async (error) => {
      saveReceptionKioskResult('action-failed', error instanceof Error ? error.message : undefined);
      await navigate({ to: '/reception-kiosk/failed' });
    },
  });

  if (!hasReceptionKioskSettings()) return <Navigate to="/reception-kiosk/setup" replace />;
  if (sessionQuery.isLoading) return null;
  if (sessionQuery.isError || !sessionQuery.data) return <Navigate to="/reception-kiosk" replace />;
  if (sessionQuery.data.status !== 'Active' || sessionQuery.data.currentStep !== 'Onboard') return <Navigate to="/reception-kiosk/session" replace />;

  const arrival = sessionQuery.data.arrival;
  const fullName = `${arrival.firstName} ${arrival.lastName}`.trim();

  async function handleHome() {
    await stopReceptionKioskSession('HomeRedirect', 'User returned home.');
    await queryClient.invalidateQueries({ queryKey: receptionKioskCurrentSessionQueryKey });
    await navigate({ to: '/reception-kiosk/session/terminal' });
  }

  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-8 text-center shadow-sm sm:p-12">
      <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-success-background text-success">
        <CheckCircle2 className="size-12" aria-hidden="true" />
      </div>
      <p className="mt-8 text-[14px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">Ready to onboard</p>
      <h2 className="mt-3 text-[36px] font-semibold tracking-tight sm:text-[56px]">{fullName}</h2>
      <p className="mx-auto mt-5 max-w-2xl text-[18px] leading-8 text-muted-foreground sm:text-[22px] sm:leading-9">All required steps are completed. Continue to finalize onboarding.</p>
      <div className="mt-10 grid gap-4 sm:grid-cols-1">
        <Button className="h-16 rounded-[1rem] text-[20px]" disabled={finalizeMutation.isPending} onClick={() => finalizeMutation.mutate()}>{finalizeMutation.isPending ? 'Finalizing...' : 'Continue'}</Button>
        <button type="button" className={buttonVariants({ variant: 'outline', size: 'lg', className: 'h-16 rounded-[1rem] text-[20px]' })} onClick={() => void handleHome()}>Home</button>
      </div>
    </section>
  );
}
