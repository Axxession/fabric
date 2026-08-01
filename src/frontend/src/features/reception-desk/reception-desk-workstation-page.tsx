import { Navigate } from '@tanstack/react-router';
import { ArrowRight, ShieldCheck } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

import { Button } from '@/shared/components/ui/button';

import { hasReceptionDeskWorkstationSettings } from './reception-desk-workstation-settings';

export default function ReceptionDeskWorkstationPage() {
  const auth = useAuth();
  const { t } = useTranslation();

  if (auth.isLoading || auth.activeNavigator) {
    return <div className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('receptionDesk.workstation.loading')}</div>;
  }

  if (!auth.isAuthenticated) {
    return <ReceptionDeskWorkstationSignInLanding />;
  }

  if (!hasReceptionDeskWorkstationSettings()) {
    return <Navigate to="/reception-desk-workstation/setup" replace />;
  }

  return <Navigate to="/reception-desk-workstation/expected-arrivals" replace />;
}

function ReceptionDeskWorkstationSignInLanding() {
  const auth = useAuth();
  const { t } = useTranslation();

  return (
    <section className="grid w-full gap-8 lg:grid-cols-[1.1fr_0.9fr] lg:items-stretch">
      <div className="relative overflow-hidden rounded-structural border border-border bg-content p-6 sm:p-8 md:p-12">
        <div className="absolute right-[-80px] top-[-120px] size-72 rounded-full bg-primary/10" />
        <div className="relative max-w-2xl">
          <p className="text-[14px] font-semibold uppercase tracking-wide text-primary">{t('receptionDesk.workstation.eyebrow')}</p>
          <h2 className="mt-4 text-[34px] font-semibold leading-tight tracking-tight sm:text-[42px] md:text-[56px]">{t('receptionDesk.workstation.title')}</h2>
          <p className="mt-5 max-w-xl text-[16px] leading-7 text-muted-foreground">
            {t('receptionDesk.workstation.description')}
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Button type="button" onClick={() => void auth.signinRedirect({ state: { returnTo: '/reception-desk-workstation' } })} className="h-12 px-6 text-[15px]">
              {t('common.signIn')}
              <ArrowRight className="ml-2 size-4" />
            </Button>
          </div>
        </div>
      </div>

      <div className="rounded-structural border border-border bg-content p-6 md:p-8">
        <div className="flex size-12 items-center justify-center rounded-interactive bg-active-blue text-primary">
          <ShieldCheck className="size-6" />
        </div>
        <h3 className="mt-6 text-[24px] font-semibold tracking-tight">{t('receptionDesk.workstation.dualAuthTitle')}</h3>
        <p className="mt-3 text-[14px] leading-6 text-muted-foreground">
          {t('receptionDesk.workstation.dualAuthDescription')}
        </p>
      </div>
    </section>
  );
}
