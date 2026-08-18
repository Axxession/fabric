import { Link, useLocation } from '@tanstack/react-router';
import { type ReactNode, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { useBranding } from '@/shared/branding/branding-context';
import { isElsaStudioFullscreenRoute } from '@/features/automation/elsa-studio-fullscreen';
import { SidebarProvider, SidebarTrigger } from '@/shared/components/ui/sidebar';
import { AccountMenu } from '@/shared/layout/account-menu';
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
  const currentUserInitials = getUserInitials(currentUserName, currentUserSecondary);

  useEffect(() => {
    document.body.classList.toggle('fabric-app-body', !isFullscreenElsaRoute);

    return () => {
      document.body.classList.add('fabric-app-body');
    };
  }, [isFullscreenElsaRoute]);

  return (
    <SidebarProvider>
      <div className="min-h-screen bg-background text-foreground">
        {isFullscreenElsaRoute ? (
          <main className="min-h-screen">{children}</main>
        ) : (
          <>
            <main className="min-w-0">
              {showNoPerspectiveWarning ? <NoPerspectiveWarning /> : null}
              {!showNoPerspectiveWarning && showPerspectiveShell ? (
                <div className="flex min-h-screen items-stretch">
                  <PerspectiveSidebar
                    perspectives={availablePerspectives}
                    version={tenantSettings.version}
                    currentUserName={currentUserName}
                    currentUserSecondary={currentUserSecondary}
                    currentUserInitials={currentUserInitials}
                    appName={branding.appName}
                    logoUrl={branding.logoUrl}
                  />
                  <div className="min-w-0 flex-1 px-4 py-4 sm:px-6 sm:py-6 md:px-8 md:py-8 xl:px-10">
                    <div className="mb-5 flex items-center gap-3 md:hidden">
                      <SidebarTrigger className="border-[var(--fabric-sidebar-panel-border)] bg-content" />
                      <Link to="/" className="min-w-0 text-[18px] font-semibold tracking-tight text-foreground" aria-label={`${branding.appName} home`}>
                        {branding.appName}
                      </Link>
                      <div className="ml-auto">
                        <AccountMenu
                          currentUserName={currentUserName}
                          currentUserSecondary={currentUserSecondary}
                          currentUserInitials={currentUserInitials}
                          trigger={<button type="button" className="inline-flex size-11 items-center justify-center rounded-full bg-[var(--fabric-sidebar-rail)] text-[14px] font-semibold tracking-[0.08em] text-white transition hover:bg-[var(--fabric-sidebar-rail-hover)] focus-visible:ring-[3px] focus-visible:ring-primary/20 focus-visible:outline-none" aria-label={t('common.openAccountMenu')} />}
                        />
                      </div>
                    </div>
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
    </SidebarProvider>
  );
}

function readProfileValue(value: unknown) {
  return typeof value === 'string' && value.trim() ? value : undefined;
}

function getUserInitials(name: string, fallback?: string) {
  const source = name.trim() || fallback?.trim() || '';
  const parts = source
    .split(/\s+/)
    .map((part) => part.replace(/[^a-zA-Z0-9]/g, ''))
    .filter(Boolean);

  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  }

  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }

  return '??';
}
