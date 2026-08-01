import { ArrowRight, ShieldCheck } from 'lucide-react';
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
  const branding = useBranding();
  const { t } = useTranslation();

  const highlights = [
    t('home.highlights.tenantSignIn'),
    t('home.highlights.oidcPkce'),
    t('home.highlights.protectedRoutes'),
    t('home.highlights.bearerTokens'),
  ];

  return (
    <section className="grid gap-8 lg:grid-cols-[1.1fr_0.9fr] lg:items-stretch">
      <div className="relative overflow-hidden rounded-structural border border-border bg-content p-5 sm:p-8 md:p-12">
        <div className="absolute right-[-80px] top-[-120px] size-72 rounded-full bg-primary/10" />
        <div className="relative max-w-2xl">
          <div className="flex items-center gap-3">
            <FabricLogo logoUrl={branding.logoUrl} />
            <span className="text-[18px] font-semibold tracking-tight">{branding.appName}</span>
          </div>
          <p className="mt-10 text-[14px] font-semibold uppercase tracking-wide text-primary">{t('home.productTagline')}</p>
          <h1 className="mt-4 text-[34px] font-semibold leading-tight tracking-tight sm:text-[42px] md:text-[56px]">
            {t('home.title')}
          </h1>
          <p className="mt-5 max-w-xl text-[16px] leading-7 text-muted-foreground">
            {t('home.description')}
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Button type="button" onClick={() => void auth.signinRedirect({ state: { returnTo: '/' } })} className="h-12 px-6 text-[15px]">
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
        <h2 className="mt-6 text-[24px] font-semibold tracking-tight">{t('home.secureByDefault')}</h2>
        <p className="mt-3 text-[14px] leading-6 text-muted-foreground">
          {t('home.secureByDefaultDescription')}
        </p>
        <div className="mt-6 grid gap-3 text-[14px]">
          {highlights.map((item) => (
            <div key={item} className="rounded-interactive bg-hover-blue px-4 py-3 font-medium text-foreground">
              {item}
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
