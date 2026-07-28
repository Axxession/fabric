import { Link, Navigate, useNavigate } from '@tanstack/react-router';
import { CheckCircle2, Home } from 'lucide-react';
import { useEffect, useState } from 'react';

import { buttonVariants } from '@/shared/components/ui/button';

import { clearReceptionKioskResult, getReceptionKioskResult } from './reception-kiosk-result';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

const redirectSeconds = 10;

export default function ReceptionKioskSuccessPage() {
  const navigate = useNavigate();
  const [secondsLeft, setSecondsLeft] = useState(redirectSeconds);
  const result = getReceptionKioskResult();

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setSecondsLeft((current) => (current <= 1 ? 0 : current - 1));
    }, 1000);

    return () => window.clearInterval(intervalId);
  }, []);

  useEffect(() => {
    if (secondsLeft !== 0) {
      return;
    }

    clearReceptionKioskResult();
    void navigate({ to: '/reception-kiosk' });
  }, [navigate, secondsLeft]);

  if (!hasReceptionKioskSettings()) {
    return <Navigate to="/reception-kiosk/setup" replace />;
  }

  if (!result || result.kind === 'action-failed') {
    return <Navigate to="/reception-kiosk" replace />;
  }

  const content = getSuccessContent(result.kind);

  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-8 text-center shadow-sm sm:p-12">
      <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-success-background text-success">
        <CheckCircle2 className="size-12" aria-hidden="true" />
      </div>

      <p className="mt-8 text-[14px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">{content.eyebrow}</p>
      <h2 className="mt-3 text-[36px] font-semibold tracking-tight sm:text-[56px]">{content.title}</h2>
      <p className="mx-auto mt-5 max-w-2xl text-[18px] leading-8 text-muted-foreground sm:text-[22px] sm:leading-9">
        {content.message}
      </p>
      <p className="mx-auto mt-4 max-w-2xl text-[16px] leading-7 text-muted-foreground sm:text-[18px]">
        Returning to home in {secondsLeft} second{secondsLeft === 1 ? '' : 's'}.
      </p>

      <div className="mt-10 grid gap-4 sm:grid-cols-1">
        <Link to="/reception-kiosk" className={buttonVariants({ size: 'lg', className: 'h-16 rounded-[1rem] text-[20px]' })} onClick={() => clearReceptionKioskResult()}>
          <Home className="size-6" aria-hidden="true" />
          Go to home
        </Link>
      </div>
    </section>
  );
}

function getSuccessContent(kind: 'onboarding-success' | 'check-in-success' | 'check-out-success' | 'visit-completed') {
  return {
    'onboarding-success': {
      eyebrow: 'Arrival registered',
      title: 'Thank you',
      message: 'Host has been notified of your arrival.',
    },
    'check-in-success': {
      eyebrow: 'Checked in',
      title: 'You have been checked in',
      message: 'Have a great visit.',
    },
    'check-out-success': {
      eyebrow: 'Checked out',
      title: 'You have been checked out',
      message: 'Thank you for visiting.',
    },
    'visit-completed': {
      eyebrow: 'Visit completed',
      title: 'This visit has already been completed',
      message: 'If you still need access, please contact the reception or host.',
    },
  }[kind];
}
