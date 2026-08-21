import { Navigate, useNavigate } from '@tanstack/react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { BookOpenCheck, ShieldCheck } from 'lucide-react';

import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { advanceReceptionKioskSession, getCurrentReceptionKioskSessionCompliance, launchCurrentReceptionKioskSessionComplianceCourse, markCurrentReceptionKioskSessionNonCompliant, stopReceptionKioskSession, type ReceptionKioskComplianceRequirement } from './reception-kiosk-api';
import { clearActiveCourse, clearComplianceLaunch, saveActiveCourse, saveComplianceLaunch } from './reception-kiosk-compliance';
import { receptionKioskCurrentSessionQueryKey, useReceptionKioskCurrentSession } from './reception-kiosk-session';
import { saveReceptionKioskResult } from './reception-kiosk-result';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskSessionCompliancePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const sessionQuery = useReceptionKioskCurrentSession();
  const complianceQuery = useQuery({
    queryKey: ['reception-kiosk', 'current-session', 'compliance'],
    enabled: sessionQuery.data?.status === 'Active' && sessionQuery.data.currentStep === 'ComplianceCheck',
    queryFn: async () => await getCurrentReceptionKioskSessionCompliance(),
  });

  const launchCourse = useMutation({
    mutationFn: async (requirementDefinitionId: string) => await launchCurrentReceptionKioskSessionComplianceCourse(requirementDefinitionId),
    onSuccess: async (launch) => {
      saveComplianceLaunch(launch);
      if (launch.token) {
        saveActiveCourse({ requirementDefinitionId: launch.requirementDefinitionId, courseId: launch.courseId, courseTitle: launch.courseTitle, token: launch.token });
        await navigate({ to: '/reception-kiosk/session/compliance/course' });
        return;
      }

      await navigate({ to: '/reception-kiosk/session/compliance/language' });
    },
    onError: async (error) => {
      saveReceptionKioskResult('action-failed', error instanceof Error ? error.message : undefined);
      await navigate({ to: '/reception-kiosk/failed' });
    },
  });

  const continueMutation = useMutation({
    mutationFn: async () => {
      if (!complianceQuery.data) throw new Error('Compliance is required.');
      if (complianceQuery.data.status === 'NonCompliant') return await markCurrentReceptionKioskSessionNonCompliant();
      return await advanceReceptionKioskSession();
    },
    onSuccess: async (session) => {
      await queryClient.invalidateQueries({ queryKey: receptionKioskCurrentSessionQueryKey });
      if (session.status === 'Stopped') {
        await navigate({ to: '/reception-kiosk/session/terminal' });
        return;
      }

      await navigate({ to: '/reception-kiosk/session' });
    },
    onError: async (error) => {
      saveReceptionKioskResult('action-failed', error instanceof Error ? error.message : undefined);
      await navigate({ to: '/reception-kiosk/failed' });
    },
  });

  if (!hasReceptionKioskSettings()) return <Navigate to="/reception-kiosk/setup" replace />;
  if (sessionQuery.isLoading || complianceQuery.isLoading) return null;
  if (sessionQuery.isError || !sessionQuery.data) return <Navigate to="/reception-kiosk" replace />;
  if (sessionQuery.data.status !== 'Active' || sessionQuery.data.currentStep !== 'ComplianceCheck') return <Navigate to="/reception-kiosk/session" replace />;
  if (complianceQuery.isError || !complianceQuery.data) return <Navigate to="/reception-kiosk/failed" replace />;

  const arrival = sessionQuery.data.arrival;
  const fullName = `${arrival.firstName} ${arrival.lastName}`.trim();

  async function handleCancel() {
    await stopReceptionKioskSession('HomeRedirect', 'User returned home.');
    await queryClient.invalidateQueries({ queryKey: receptionKioskCurrentSessionQueryKey });
    await navigate({ to: '/reception-kiosk/session/terminal' });
  }

  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-5 shadow-sm sm:p-8 lg:p-10">
      <div className="mt-2 grid gap-6 xl:grid-cols-[0.95fr_1.05fr] xl:items-start">
        <div className="rounded-[2rem] bg-hover-blue p-6 sm:p-8">
          <div className="flex size-18 items-center justify-center rounded-full bg-content text-primary sm:size-20">
            <ShieldCheck className="size-9 sm:size-10" aria-hidden="true" />
          </div>
          <p className="mt-7 text-[13px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">Compliance</p>
          <h2 className="mt-3 text-[34px] font-semibold tracking-tight sm:text-[52px]">{fullName}</h2>
          {arrival.company ? <p className="mt-3 text-[22px] text-muted-foreground sm:text-[24px]">{arrival.company}</p> : null}
          <div className="mt-6"><Badge variant={getComplianceVariant(complianceQuery.data.status)}>{getComplianceLabel(complianceQuery.data.status)}</Badge></div>
          <p className="mt-5 max-w-xl text-[16px] leading-7 text-muted-foreground">Complete any available learning courses. When compliance is still missing, the kiosk will end this session and ask you to contact your contact person.</p>
        </div>
        <div className="rounded-[2rem] border border-border p-6 sm:p-8">
          <div className="flex items-center gap-3 text-primary">
            <BookOpenCheck className="size-7" aria-hidden="true" />
            <h3 className="text-[24px] font-semibold">Requirement overview</h3>
          </div>
          {complianceQuery.data.requirements.length === 0 ? <p className="mt-6 text-[15px] text-muted-foreground">No compliance requirements found.</p> : (
            <div className="mt-6 grid gap-4">
              {complianceQuery.data.requirements.map((requirement) => <RequirementCard key={requirement.requirementDefinitionId} requirement={requirement} busy={launchCourse.isPending} onCompleteCourse={() => launchCourse.mutate(requirement.requirementDefinitionId)} />)}
            </div>
          )}
          <Button className="mt-8 h-14 w-full rounded-[1rem] text-[18px]" disabled={continueMutation.isPending} onClick={() => continueMutation.mutate()}>{continueMutation.isPending ? 'Continuing...' : 'Next'}</Button>
          <button type="button" className={buttonVariants({ variant: 'outline', size: 'lg', className: 'mt-3 h-14 w-full rounded-[1rem] text-[18px]' })} onClick={() => void handleCancel()}>Home</button>
        </div>
      </div>
    </section>
  );
}

function RequirementCard({ requirement, busy, onCompleteCourse }: { readonly requirement: ReceptionKioskComplianceRequirement; readonly busy: boolean; readonly onCompleteCourse: () => void; }) {
  return (
    <article className="rounded-[1.5rem] border border-border bg-content p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <h4 className="text-[18px] font-semibold text-foreground">{requirement.name}</h4>
            <Badge variant={getRequirementVariant(requirement.status)}>{getRequirementLabel(requirement.status)}</Badge>
            {!requirement.isBlocking ? <Badge variant="outline">Non-blocking</Badge> : null}
          </div>
          <p className="mt-2 text-[14px] font-medium text-muted-foreground">{requirement.code}</p>
          <p className="mt-3 text-[15px] leading-7 text-muted-foreground">{requirement.reason}</p>
          {requirement.validUntil ? <p className="mt-3 text-[14px] text-muted-foreground">Valid until {new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(requirement.validUntil))}</p> : null}
          {requirement.course ? <p className="mt-3 text-[14px] text-muted-foreground">Course: {requirement.course.courseTitle}</p> : null}
        </div>
        {requirement.course ? <Button type="button" className="h-11 rounded-[0.9rem] px-5" disabled={busy} onClick={onCompleteCourse}>Complete course</Button> : null}
      </div>
    </article>
  );
}

function getComplianceVariant(status: 'Compliant' | 'TemporarilyCompliant' | 'NonCompliant') { return status === 'Compliant' ? 'success' : status === 'TemporarilyCompliant' ? 'secondary' : 'error'; }
function getComplianceLabel(status: 'Compliant' | 'TemporarilyCompliant' | 'NonCompliant') { return status === 'Compliant' ? 'Compliant' : status === 'TemporarilyCompliant' ? 'Temporarily compliant' : 'Not compliant'; }
function getRequirementVariant(status: 'Fulfilled' | 'Missing' | 'Failed' | 'Expired') { return status === 'Fulfilled' ? 'success' : status === 'Expired' ? 'secondary' : 'error'; }
function getRequirementLabel(status: 'Fulfilled' | 'Missing' | 'Failed' | 'Expired') { return status === 'Fulfilled' ? 'Fulfilled' : status === 'Missing' ? 'Missing' : status === 'Failed' ? 'Failed' : 'Expired'; }
