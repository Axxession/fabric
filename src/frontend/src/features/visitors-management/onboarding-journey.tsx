import type { components } from '@/shared/api/generated/schema';
import { cn } from '@/shared/utils/cn';

type VisitorPreOnboardingState = components['schemas']['VisitorPreOnboardingState'];

const STEPS = [
  { key: 'invited', label: 'Invited' },
  { key: 'qr', label: 'Generate QR' },
  { key: 'arrival', label: 'Registering Arrival' },
  { key: 'sent', label: 'Sent Invitation' },
  { key: 'await', label: 'Awaiting Confirmation' },
] as const;

type StepStatus = 'done' | 'current' | 'todo' | 'fail';

const ACTIVE_STATES: readonly VisitorPreOnboardingState[] = [
  'GeneratingQr',
  'RegisteringArrival',
  'SendingInvitation',
  'AwaitingConfirmation',
] as const;

const TERMINAL_DONE: readonly VisitorPreOnboardingState[] = [
  'Confirmed',
  'Rejected',
  'Cancelled',
  'Cancelling',
] as const;

function getStepStatus(
  state: VisitorPreOnboardingState | null | undefined,
  stepIdx: number,
): StepStatus {
  if (!state) return 'todo';

  if (TERMINAL_DONE.includes(state as VisitorPreOnboardingState)) return 'done';

  if (stepIdx === 0) return 'done';

  const pos = ACTIVE_STATES.indexOf(state as VisitorPreOnboardingState);
  if (pos === -1) return 'todo';

  if (stepIdx - 1 < pos) return 'done';
  if (stepIdx - 1 === pos) return 'current';
  return 'todo';
}

function isFailState(state?: VisitorPreOnboardingState | null): boolean {
  return state === 'Rejected' || state === 'Cancelled';
}

const circleStyle: Record<StepStatus, string> = {
  done: 'bg-primary border-primary',
  current: 'bg-primary border-primary ring-2 ring-primary/30',
  todo: 'border-border bg-transparent',
  fail: 'bg-error border-error',
};

const labelStyle: Record<StepStatus, string> = {
  done: 'text-foreground',
  current: 'text-primary font-medium',
  todo: 'text-muted-foreground/50',
  fail: 'text-error',
};

function CircleInner({ status }: { status: StepStatus }) {
  if (status === 'current') {
    return <div className="size-1.5 rounded-full bg-white" />;
  }
  if (status === 'done') {
    return (
      <svg viewBox="0 0 12 12" className="size-full p-[3px]" fill="none">
        <path d="M3 6L5 8L9 4" stroke="white" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    );
  }
  return null;
}

function Connector({ prev }: { prev: StepStatus }) {
  const color = ({
    done: 'bg-primary',
    current: 'bg-primary',
    todo: 'bg-border',
    fail: 'bg-error',
  } satisfies Record<StepStatus, string>)[prev];

  return <div className={cn('w-2.5 h-px', color)} />;
}

function TerminalTag({ state }: { state: VisitorPreOnboardingState }) {
  const colors: Record<string, string> = {
    Confirmed: 'bg-success text-white',
    Rejected: 'bg-error text-white',
    Cancelled: 'bg-muted-foreground/20 text-muted-foreground',
    Cancelling: 'bg-muted-foreground/20 text-muted-foreground',
  };

  const bg = colors[state];
  if (!bg) return null;

  return (
    <div className="flex items-center gap-0">
      <Connector prev="done" />
      <span className={cn('inline-flex items-center rounded-full px-1.5 py-0.5 text-[9px] font-semibold leading-none whitespace-nowrap', bg)}>
        {state}
      </span>
    </div>
  );
}

type SagaProp = {
  state?: VisitorPreOnboardingState | null;
  retryCount?: number | string | null;
};

export function OnboardingJourney({
  saga,
}: {
  saga?: SagaProp | null;
}) {
  const state = saga?.state ?? null;

  if (state === 'Expired') return null;

  const isDoneAll = state != null && TERMINAL_DONE.includes(state);
  const globalFail = isFailState(state);
  const retryCount = Number(saga?.retryCount ?? 0);

  return (
    <div className="flex items-center gap-0 overflow-hidden">
      {STEPS.map((step, i) => {
        const raw = getStepStatus(state, i);
        const status: StepStatus = raw === 'todo' && globalFail ? 'fail' : raw;

        return (
          <div key={step.key} className="flex items-center gap-0">
            {i > 0 ? <Connector prev={getStepStatus(state, i - 1)} /> : null}
            <div className="flex flex-col items-center gap-1" style={{ width: 96 }}>
              <div className={cn('size-3 rounded-full border-2 flex items-center justify-center shrink-0', circleStyle[status])}>
                <CircleInner status={status} />
              </div>
              <div className="flex flex-col items-center">
                <span className={cn('text-[10px] leading-tight whitespace-nowrap', labelStyle[status])}>
                  {step.label}
                </span>
                {status === 'current' && retryCount > 0 ? (
                  <span className="text-[8px] leading-none text-muted-foreground/50 whitespace-nowrap">
                    attempt {retryCount + 1}
                  </span>
                ) : null}
              </div>
            </div>
          </div>
        );
      })}

      {isDoneAll ? <TerminalTag state={state} /> : null}
    </div>
  );
}
