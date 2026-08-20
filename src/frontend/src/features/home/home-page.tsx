import { ArrowRight } from 'lucide-react';
import { Navigate } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { FabricLogo } from '@/shared/branding/fabric-logo';
import { useBranding } from '@/shared/branding/branding-context';
import { Button } from '@/shared/components/ui/button';
import { getDefaultPerspective } from '@/shared/perspectives/app-perspectives';
import { NoPerspectiveWarning } from '@/shared/perspectives/no-perspective-warning';

export default function HomePage() {
  const auth = useAuth();
  const { t } = useTranslation();
  const actorQuery = useCurrentActor();

  if (!auth.isAuthenticated) {
    return <PublicHomePage />;
  }

  const defaultPerspective = getDefaultPerspective(actorQuery.data);

  if (actorQuery.isLoading) {
    return <div className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('home.loadingPerspectives')}</div>;
  }

  if (actorQuery.isError) {
    return <div className="rounded-structural border border-error bg-error-background p-6 text-[14px] text-error">{t('home.couldNotLoadCurrentActor')}</div>;
  }

  if (!defaultPerspective) {
    return <NoPerspectiveWarning />;
  }

  return <Navigate to={defaultPerspective.to} replace />;
}

function PublicHomePage() {
  const auth = useAuth();
  const { t } = useTranslation();
  const branding = useBranding();

  return (
    <section className="fixed inset-0 flex items-center justify-center overflow-auto bg-[var(--fabric-sidebar-rail)] px-4 py-6 sm:px-6 md:px-8">
      <div className="relative w-full max-w-3xl overflow-hidden rounded-structural border border-border bg-content px-6 py-10 text-center shadow-[0_24px_80px_rgba(0,0,0,0.18)] sm:px-10 sm:py-14 md:px-14 md:py-16">
        <div className="absolute left-1/2 top-0 size-72 -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary/10 blur-2xl" />
        <div className="relative flex flex-col items-center">
          <FabricLogo logoUrl={branding.logoUrl} />
          <h1 className="mt-6 text-[34px] font-semibold tracking-tight sm:text-[42px] md:text-[50px]">{t('home.title')}</h1>
          <p className="mt-4 max-w-xl text-[16px] leading-7 text-muted-foreground">{t('home.slugLine')}</p>
          <div className="mt-8 flex flex-col items-center gap-3">
            <Button type="button" onClick={() => void auth.signinRedirect({ state: { returnTo: '/' } })} className="h-12 px-6 text-[15px]">
              {t('common.signIn')}
              <ArrowRight className="ml-2 size-4" />
            </Button>
            <p className="text-[13px] text-muted-foreground">{t('home.systemReminder')}</p>
          </div>
        </div>
      </div>
    </section>
  );
}
