import { Link, Navigate } from '@tanstack/react-router';
import { AlertCircle, Home } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { buttonVariants } from '@/shared/components/ui/button';

import { clearReceptionKioskResult, getReceptionKioskResult } from './reception-kiosk-result';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskFailedPage() {
  const { t } = useTranslation();
  const result = getReceptionKioskResult();
  const title = result?.message ?? t('receptionKiosk.failed.fallbackTitle');

  if (!hasReceptionKioskSettings()) {
    return <Navigate to="/reception-kiosk/setup" replace />;
  }

  if (result?.kind !== 'action-failed') {
    return <Navigate to="/reception-kiosk" replace />;
  }

  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-8 text-center shadow-sm sm:p-12">
      <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-error-background text-error">
        <AlertCircle className="size-12" aria-hidden="true" />
      </div>

      <p className="mt-8 text-[14px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">{t('receptionKiosk.eyebrow')}</p>
      <h2 className="mt-3 text-[36px] font-semibold tracking-tight sm:text-[56px]">{title}</h2>
      <p className="mx-auto mt-5 max-w-2xl text-[18px] leading-8 text-muted-foreground sm:text-[22px] sm:leading-9">
        {t('receptionKiosk.failed.description')}
      </p>

      <div className="mt-10 grid gap-4 sm:grid-cols-1">
        <Link to="/reception-kiosk" className={buttonVariants({ size: 'lg', className: 'h-16 rounded-[1rem] text-[20px]' })} onClick={() => clearReceptionKioskResult()}>
          <Home className="size-6" aria-hidden="true" />
          {t('receptionKiosk.failed.goHome')}
        </Link>
      </div>
    </section>
  );
}
