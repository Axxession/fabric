import { Link, Navigate } from '@tanstack/react-router';
import { Keyboard, QrCode } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { Button, buttonVariants } from '@/shared/components/ui/button';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskPage() {
  const { t } = useTranslation();

  if (!hasReceptionKioskSettings()) {
    return <Navigate to="/reception-kiosk/setup" replace />;
  }

  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-8 text-center shadow-sm sm:p-12">
      <p className="text-[14px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">{t('receptionKiosk.eyebrow')}</p>
      <h2 className="mt-3 text-[36px] font-semibold tracking-tight sm:text-[56px]">{t('receptionKiosk.welcome')}</h2>
      <p className="mx-auto mt-5 max-w-2xl text-[18px] leading-8 text-muted-foreground sm:text-[22px] sm:leading-9">
        {t('receptionKiosk.chooseArrivalMethod')}
      </p>

      <div className="mt-10 grid gap-4 sm:grid-cols-2">
        <Link to="/reception-kiosk/scan-qr" className={buttonVariants({ size: 'lg', className: 'h-auto min-h-48 flex-col rounded-[1.5rem] p-8 text-[22px] sm:min-h-64 sm:text-[28px]' })}>
          <QrCode className="size-14" aria-hidden="true" />
          <span>{t('receptionKiosk.haveQr')}</span>
        </Link>
        <Button size="lg" variant="outline" disabled className="h-auto min-h-48 flex-col rounded-[1.5rem] p-8 text-[22px] sm:min-h-64 sm:text-[28px]">
          <Keyboard className="size-14" aria-hidden="true" />
          <span>{t('receptionKiosk.noQr')}</span>
        </Button>
      </div>
    </section>
  );
}
