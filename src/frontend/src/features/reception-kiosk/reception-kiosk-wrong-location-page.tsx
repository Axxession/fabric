import { Link, Navigate } from '@tanstack/react-router';
import { Home, MapPinned, RotateCcw } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { buttonVariants } from '@/shared/components/ui/button';

import { clearReceptionKioskResult, getReceptionKioskResult } from './reception-kiosk-result';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskWrongLocationPage() {
  const { t } = useTranslation();
  const result = getReceptionKioskResult();

  if (!hasReceptionKioskSettings()) {
    return <Navigate to="/reception-kiosk/setup" replace />;
  }

  if (result?.kind !== 'wrong-location') {
    return <Navigate to="/reception-kiosk" replace />;
  }

  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-8 text-center shadow-sm sm:p-12">
      <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-error-background text-error">
        <MapPinned className="size-12" aria-hidden="true" />
      </div>

      <p className="mt-8 text-[14px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">{t('receptionKiosk.eyebrow')}</p>
      <h2 className="mt-3 text-[36px] font-semibold tracking-tight sm:text-[56px]">{result.title ?? t('receptionKiosk.wrongLocation.fallbackTitle')}</h2>
      <p className="mx-auto mt-5 max-w-2xl text-[18px] leading-8 text-muted-foreground sm:text-[22px] sm:leading-9">
        {result.message ?? t('receptionKiosk.wrongLocation.fallbackDescription')}
      </p>

      <div className="mt-10 grid gap-4 sm:grid-cols-2">
        <Link to="/reception-kiosk/scan-qr" className={buttonVariants({ size: 'lg', className: 'h-16 rounded-[1rem] text-[20px]' })} onClick={() => clearReceptionKioskResult()}>
          <RotateCcw className="size-6" aria-hidden="true" />
          {t('receptionKiosk.wrongLocation.scanAnotherQr')}
        </Link>
        <Link to="/reception-kiosk" className={buttonVariants({ variant: 'outline', size: 'lg', className: 'h-16 rounded-[1rem] text-[20px]' })} onClick={() => clearReceptionKioskResult()}>
          <Home className="size-6" aria-hidden="true" />
          {t('receptionKiosk.wrongLocation.home')}
        </Link>
      </div>
    </section>
  );
}
