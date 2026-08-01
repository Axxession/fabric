import { Link, Navigate } from '@tanstack/react-router';
import { AlertCircle, Home, RotateCcw } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { buttonVariants } from '@/shared/components/ui/button';

import { getReceptionKioskMissedCode } from './reception-kiosk-api';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskNoRegistrationPage() {
  const { t } = useTranslation();
  if (!hasReceptionKioskSettings()) {
    return <Navigate to="/reception-kiosk/setup" replace />;
  }

  const code = getReceptionKioskMissedCode();

  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-8 text-center shadow-sm sm:p-12">
      <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-error-background text-error">
        <AlertCircle className="size-12" aria-hidden="true" />
      </div>

      <p className="mt-8 text-[14px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">{t('receptionKiosk.noRegistration.eyebrow')}</p>
      <h2 className="mt-3 text-[36px] font-semibold tracking-tight sm:text-[56px]">{t('receptionKiosk.noRegistration.title')}</h2>
      <p className="mx-auto mt-5 max-w-2xl text-[18px] leading-8 text-muted-foreground sm:text-[22px] sm:leading-9">
        {t('receptionKiosk.noRegistration.description')}
      </p>

      {code ? (
        <div className="mx-auto mt-8 max-w-2xl rounded-[1.5rem] border border-border bg-background p-5">
          <p className="text-[13px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{t('receptionKiosk.noRegistration.scannedCode')}</p>
          <p className="mt-2 break-all font-mono text-[18px] text-foreground">{code}</p>
        </div>
      ) : null}

      <div className="mt-10 grid gap-4 sm:grid-cols-2">
        <Link to="/reception-kiosk/scan-qr" className={buttonVariants({ size: 'lg', className: 'h-16 rounded-[1rem] text-[20px]' })}>
          <RotateCcw className="size-6" aria-hidden="true" />
          {t('receptionKiosk.noRegistration.retryScan')}
        </Link>
        <Link to="/reception-kiosk" className={buttonVariants({ variant: 'outline', size: 'lg', className: 'h-16 rounded-[1rem] text-[20px]' })}>
          <Home className="size-6" aria-hidden="true" />
          {t('receptionKiosk.noRegistration.home')}
        </Link>
      </div>
    </section>
  );
}
