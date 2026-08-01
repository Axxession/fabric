import { Link, Navigate, useNavigate } from '@tanstack/react-router';
import { CheckCircle2, Home } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { buttonVariants } from '@/shared/components/ui/button';

import { clearReceptionKioskResult, getReceptionKioskResult } from './reception-kiosk-result';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

const redirectSeconds = 10;

export default function ReceptionKioskSuccessPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
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

  if (!result || result.kind === 'action-failed' || result.kind === 'wrong-location') {
    return <Navigate to="/reception-kiosk" replace />;
  }

  const content = getSuccessContent(result.kind, t);

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
        {t('receptionKiosk.success.returningHome', { count: secondsLeft })}
      </p>

      <div className="mt-10 grid gap-4 sm:grid-cols-1">
        <Link to="/reception-kiosk" className={buttonVariants({ size: 'lg', className: 'h-16 rounded-[1rem] text-[20px]' })} onClick={() => clearReceptionKioskResult()}>
          <Home className="size-6" aria-hidden="true" />
          {t('receptionKiosk.success.goHome')}
        </Link>
      </div>
    </section>
  );
}

function getSuccessContent(kind: 'onboarding-success' | 'check-in-success' | 'check-out-success' | 'visit-completed', t: ReturnType<typeof useTranslation>['t']) {
  return {
    'onboarding-success': {
      eyebrow: t('receptionKiosk.success.arrivalRegisteredEyebrow'),
      title: t('receptionKiosk.success.arrivalRegisteredTitle'),
      message: t('receptionKiosk.success.arrivalRegisteredMessage'),
    },
    'check-in-success': {
      eyebrow: t('receptionKiosk.success.checkedInEyebrow'),
      title: t('receptionKiosk.success.checkedInTitle'),
      message: t('receptionKiosk.success.checkedInMessage'),
    },
    'check-out-success': {
      eyebrow: t('receptionKiosk.success.checkedOutEyebrow'),
      title: t('receptionKiosk.success.checkedOutTitle'),
      message: t('receptionKiosk.success.checkedOutMessage'),
    },
    'visit-completed': {
      eyebrow: t('receptionKiosk.success.visitCompletedEyebrow'),
      title: t('receptionKiosk.success.visitCompletedTitle'),
      message: t('receptionKiosk.success.visitCompletedMessage'),
    },
  }[kind];
}
