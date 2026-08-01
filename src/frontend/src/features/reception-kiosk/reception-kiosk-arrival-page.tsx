import { Link, Navigate, useNavigate } from '@tanstack/react-router';
import { useMutation } from '@tanstack/react-query';
import { ArrowLeft, CalendarClock, CheckCircle2, UserRound } from 'lucide-react';
import { Button, buttonVariants } from '@/shared/components/ui/button';

import { clearReceptionKioskArrival, getReceptionKioskArrival, onboardReceptionKioskArrival } from './reception-kiosk-api';
import {
  buildOnboardingRequest,
  clearOnboardingState,
  getCapturePreviewUrl,
  getNextIncompleteOnboardingStep,
  getOnboardingStepPath,
  getRequiredOnboardingSteps,
  getStoredOnboardingState,
} from './reception-kiosk-onboarding';
import { saveReceptionKioskResult } from './reception-kiosk-result';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskArrivalPage() {
  const navigate = useNavigate();
  if (!hasReceptionKioskSettings()) {
    return <Navigate to="/reception-kiosk/setup" replace />;
  }

  const arrival = getReceptionKioskArrival();
  if (!arrival) {
    return <Navigate to="/reception-kiosk" replace />;
  }

  const fullName = `${arrival.firstName} ${arrival.lastName}`.trim();
  const requiredSteps = getRequiredOnboardingSteps(arrival);
  const onboardingState = getStoredOnboardingState();
  const nextStep = getNextIncompleteOnboardingStep(arrival);
  const facePreview = getCapturePreviewUrl(onboardingState.faceCapture);
  const documentPreview = getCapturePreviewUrl(onboardingState.documentCapture);

  const submitOnboarding = useMutation({
    mutationFn: async () => {
      await onboardReceptionKioskArrival(arrival.id, buildOnboardingRequest(arrival));
    },
    onSuccess: async () => {
      clearOnboardingState();
      clearReceptionKioskArrival();
      saveReceptionKioskResult('onboarding-success');
      await navigate({ to: '/reception-kiosk/success' });
    },
    onError: () => {
      saveReceptionKioskResult('action-failed');
      void navigate({ to: '/reception-kiosk/failed' });
    },
  });

  function handleStartOver() {
    clearOnboardingState();
    clearReceptionKioskArrival();
    void navigate({ to: '/reception-kiosk' });
  }

  async function handlePrimaryAction() {
    if (nextStep) {
      await navigate({ to: getOnboardingStepPath(nextStep) });
      return;
    }

    submitOnboarding.mutate();
  }

  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-5 shadow-sm sm:p-8 lg:p-10">
      <Link to="/reception-kiosk" className="inline-flex items-center gap-2 text-[16px] font-medium text-muted-foreground hover:text-foreground">
        <ArrowLeft className="size-5" aria-hidden="true" />
        Back
      </Link>

      <div className="mt-6 grid gap-6 xl:grid-cols-[1.1fr_0.9fr] xl:items-start">
        <div className="grid gap-6">
          <div className="rounded-[2rem] bg-hover-blue p-6 sm:p-8">
            <div className="flex size-18 items-center justify-center rounded-full bg-content text-primary sm:size-20">
              <UserRound className="size-9 sm:size-10" aria-hidden="true" />
            </div>
            <p className="mt-7 text-[13px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">Expected arrival</p>
            <h2 className="mt-3 text-[34px] font-semibold tracking-tight sm:text-[52px]">{fullName}</h2>
            {arrival.company ? <p className="mt-3 text-[22px] text-muted-foreground sm:text-[24px]">{arrival.company}</p> : null}
            <div className="mt-8 grid gap-3 text-[17px] sm:grid-cols-2">
              <Detail label="Arrival type" value={arrival.type} />
              <Detail label="Status" value={arrival.status} />
              <Detail label="Expected arrival" value={formatDateTime(arrival.expectedArrivalTime)} />
              <Detail label="Expected offboard" value={formatDateTime(arrival.expectedOffboardTime)} />
            </div>
          </div>

          {arrival.visitor?.visit ? (
            <article className="rounded-[2rem] border border-border p-6">
              <div className="flex items-center gap-3 text-primary">
                <CalendarClock className="size-7" aria-hidden="true" />
                <h3 className="text-[24px] font-semibold">Visit details</h3>
              </div>
              <dl className="mt-6 grid gap-4 text-[17px] sm:grid-cols-2">
                <Detail label="Summary" value={arrival.visitor.visit.summary} />
                <Detail label="Host" value={arrival.visitor.visit.hostName} />
                <Detail label="Host email" value={arrival.visitor.visit.hostEmail ?? 'Not provided'} />
                <Detail label="Visit start" value={formatDateTime(arrival.visitor.visit.start)} />
                <Detail label="Visit stop" value={formatDateTime(arrival.visitor.visit.stop)} />
                <Detail label="Confirmation" value={arrival.visitor.confirmationStatus} />
                {arrival.visitor.transport ? <Detail label="Transport" value={arrival.visitor.transport} /> : null}
                {arrival.visitor.licensePlate ? <Detail label="License plate" value={arrival.visitor.licensePlate} /> : null}
              </dl>
            </article>
          ) : null}
        </div>

        <div className="grid gap-4">
          <div className="rounded-[2rem] border border-border p-6">
            <div className="flex items-center gap-3 text-success">
              <CheckCircle2 className="size-7" aria-hidden="true" />
              <h3 className="text-[24px] font-semibold">Self-onboarding</h3>
            </div>
            <p className="mt-3 text-[17px] leading-7 text-muted-foreground">
              {requiredSteps.length === 0
                ? 'No pictures are required for this kiosk. You can complete self-onboarding immediately.'
                : 'We will guide you through each required picture one step at a time.'}
            </p>

            <div className="mt-6 grid gap-4">
              <StepCard
                label="1"
                title="Face picture"
                enabled={requiredSteps.includes('face')}
                ready={!!onboardingState.faceCapture}
                previewUrl={facePreview}
              />
              <StepCard
                label={requiredSteps.includes('face') ? '2' : '1'}
                title="Identity document picture"
                enabled={requiredSteps.includes('document')}
                ready={!!onboardingState.documentCapture}
                previewUrl={documentPreview}
              />
            </div>

            <Button className="mt-6 h-14 w-full rounded-[1rem] text-[18px]" disabled={submitOnboarding.isPending} onClick={() => void handlePrimaryAction()}>
              {submitOnboarding.isPending
                ? 'Completing self-onboarding...'
                : nextStep
                  ? `${onboardingState.faceCapture || onboardingState.documentCapture ? 'Continue' : 'Start'} ${nextStep === 'face' ? 'face scan' : 'document scan'}`
                  : 'Complete self-onboarding'}
            </Button>
          </div>

          <Button type="button" variant="outline" className="h-14 rounded-[1rem] text-[18px]" onClick={handleStartOver}>Start over</Button>
          <Link to="/reception-kiosk" className={buttonVariants({ variant: 'outline', size: 'lg', className: 'h-14 rounded-[1rem] text-[18px]' })}>Home</Link>
        </div>
      </div>
    </section>
  );
}

function StepCard({
  enabled,
  label,
  previewUrl,
  ready,
  title,
}: {
  readonly enabled: boolean;
  readonly label: string;
  readonly previewUrl: string | null;
  readonly ready: boolean;
  readonly title: string;
}) {
  return (
    <div className={`rounded-[1.5rem] border p-4 ${enabled ? 'border-border bg-hover-gray/50' : 'border-border/60 bg-content'}`}>
      <div className="flex items-start gap-4">
        <div className={`flex size-12 shrink-0 items-center justify-center rounded-full text-[18px] font-semibold ${enabled ? 'bg-content text-foreground' : 'bg-hover-gray text-muted-foreground'}`}>{label}</div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center justify-between gap-3">
            <h4 className="text-[18px] font-semibold">{title}</h4>
            <span className="rounded-full bg-content px-3 py-1 text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
              {enabled ? (ready ? 'Ready' : 'Required') : 'Skipped'}
            </span>
          </div>
          <p className="mt-1 text-[15px] text-muted-foreground">
            {enabled
              ? ready
                ? 'Picture captured. You can continue.'
                : 'This step will open the camera with auto-capture.'
              : 'This kiosk does not require this step.'}
          </p>
          {previewUrl ? <img src={previewUrl} alt={title} className="mt-4 h-28 w-full rounded-[1rem] border border-border object-cover" /> : null}
        </div>
      </div>
    </div>
  );
}

function Detail({ label, value }: { readonly label: string; readonly value: string }) {
  return (
    <div>
      <dt className="text-[13px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{label}</dt>
      <dd className="mt-1 font-medium text-foreground">{value}</dd>
    </div>
  );
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}
