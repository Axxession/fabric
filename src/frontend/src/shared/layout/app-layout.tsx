import { Link, useLocation } from '@tanstack/react-router';
import { ChevronDown } from 'lucide-react';
import { type ReactNode, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { FabricLogo } from '@/shared/branding/fabric-logo';
import { useBranding } from '@/shared/branding/branding-context';
import { isElsaStudioFullscreenRoute } from '@/features/automation/elsa-studio-fullscreen';
import { Button } from '@/shared/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/components/ui/popover';
import { AppLanguageSelect } from '@/shared/i18n/app-language-select';
import { PerspectiveSidebar } from '@/shared/layout/perspective-sidebar';
import { NoPerspectiveWarning } from '@/shared/perspectives/no-perspective-warning';
import { getAvailablePerspectives, getPerspectiveByPathname } from '@/shared/perspectives/app-perspectives';
import { useTenantSettings } from '@/shared/tenant/tenant-settings-context';

export function AppLayout({ children }: { children: ReactNode }) {
  const location = useLocation();
  const { t } = useTranslation();
  const auth = useAuth();
  const branding = useBranding();
  const actorQuery = useCurrentActor();
  const tenantSettings = useTenantSettings();
  const isFullscreenElsaRoute = isElsaStudioFullscreenRoute(location.pathname);
  const availablePerspectives = getAvailablePerspectives(actorQuery.data);
  const activePerspective = getPerspectiveByPathname(location.pathname);
  const showPerspectiveShell = auth.isAuthenticated && !isFullscreenElsaRoute && activePerspective && availablePerspectives.length > 0;
  const showNoPerspectiveWarning = auth.isAuthenticated && !isFullscreenElsaRoute && !actorQuery.isLoading && !actorQuery.isError && availablePerspectives.length === 0;
  const currentUserName = actorQuery.data?.displayName ?? readProfileValue(auth.user?.profile.name) ?? readProfileValue(auth.user?.profile.preferred_username) ?? readProfileValue(auth.user?.profile.email) ?? t('common.signedIn');
  const currentUserSecondary = actorQuery.data?.email ?? readProfileValue(auth.user?.profile.email) ?? readProfileValue(auth.user?.profile.preferred_username);

  useEffect(() => {
    document.body.classList.toggle('fabric-app-body', !isFullscreenElsaRoute);

    return () => {
      document.body.classList.add('fabric-app-body');
    };
  }, [isFullscreenElsaRoute]);

  return (
    <div className="min-h-screen bg-background text-foreground">
      {isFullscreenElsaRoute ? (
        <main className="min-h-screen">{children}</main>
      ) : (
        <>
          <header className="sticky top-0 z-10 border-b border-border bg-content">
            <div className="flex items-center gap-3 px-3 py-3 sm:px-4 sm:py-4">
              <Link to="/" className="flex items-center gap-3" aria-label={`${branding.appName} home`}>
                <FabricLogo logoUrl={branding.logoUrl} />
                <span className="hidden text-[20px] font-semibold tracking-tight min-[380px]:inline">{branding.appName}</span>
              </Link>
              <div className="ml-auto flex items-center gap-2">
                {auth.isAuthenticated ? (
                  <>
                    <AppLanguageSelect />
                    <Popover>
                      <PopoverTrigger render={<Button type="button" variant="outline" className="max-w-[16rem] justify-between sm:max-w-[20rem]" aria-label={t('common.openAccountMenu')} />}>
                        <span className="truncate text-left text-[14px] font-semibold">{currentUserName}</span>
                        <ChevronDown className="size-4 text-muted-foreground" aria-hidden="true" />
                      </PopoverTrigger>
                      <PopoverContent align="end" className="grid min-w-64 gap-3 p-3">
                        <div className="min-w-0 border-b border-border pb-3">
                          <p className="truncate text-[14px] font-semibold text-foreground">{currentUserName}</p>
                          {currentUserSecondary && currentUserSecondary !== currentUserName ? <p className="mt-1 truncate text-[13px] text-muted-foreground">{currentUserSecondary}</p> : null}
                        </div>
                        <Button type="button" variant="ghost" className="justify-start" onClick={() => void auth.signoutRedirect().catch(() => auth.removeUser())}>
                          {t('common.signOut')}
                        </Button>
                      </PopoverContent>
                    </Popover>
                  </>
                ) : null}
              </div>
            </div>
          </header>
          <main className="min-w-0">
            {showNoPerspectiveWarning ? <NoPerspectiveWarning /> : null}
            {!showNoPerspectiveWarning && showPerspectiveShell ? (
              <div className="flex min-h-[calc(100vh-73px)] items-stretch">
                <PerspectiveSidebar perspectives={availablePerspectives} version={tenantSettings.version} />
                <div className="min-w-0 flex-1 px-4 py-5 sm:px-6 sm:py-6 md:px-10 md:py-8">
                  <div className="mx-auto w-full max-w-7xl">{children}</div>
                </div>
              </div>
            ) : null}
            {!showNoPerspectiveWarning && !showPerspectiveShell ? (
              <div className="px-3 py-5 sm:px-4 sm:py-6 md:px-8 md:py-8">
                <div className="mx-auto max-w-7xl">{children}</div>
              </div>
            ) : null}
          </main>
        </>
      )}
    </div>
  );
}

function readProfileValue(value: unknown) {
  return typeof value === 'string' && value.trim() ? value : undefined;
}
