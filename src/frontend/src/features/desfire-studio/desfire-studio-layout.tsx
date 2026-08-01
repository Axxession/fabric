import { Link, useLocation } from '@tanstack/react-router';
import { ChevronDown } from 'lucide-react';
import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { FabricLogo } from '@/shared/branding/fabric-logo';
import { useBranding } from '@/shared/branding/branding-context';
import { Button } from '@/shared/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/components/ui/popover';
import { i18n } from '@/shared/i18n/i18n';
import { useTenantSettings } from '@/shared/tenant/tenant-settings-context';

const desfireStudioMenuItems = [
  { label: i18n.t('desfireStudio.menu.hardwareAgents.label'), description: i18n.t('desfireStudio.menu.hardwareAgents.description'), to: '/desfire-studio/hardware-agents' },
  { label: i18n.t('desfireStudio.menu.keyManagement.label'), description: i18n.t('desfireStudio.menu.keyManagement.description'), to: '/desfire-studio/key-management' },
  { label: i18n.t('desfireStudio.menu.chipDesigner.label'), description: i18n.t('desfireStudio.menu.chipDesigner.description'), to: '/desfire-studio/chip-designer' },
  { label: i18n.t('desfireStudio.menu.printing.label'), description: i18n.t('desfireStudio.menu.printing.description'), to: '/desfire-studio/printing' },
] as const;

export function DesfireStudioLayout({ children }: { readonly children: ReactNode }) {
  const location = useLocation();
  const { t } = useTranslation();
  const auth = useAuth();
  const branding = useBranding();
  const actorQuery = useCurrentActor();
  const tenantSettings = useTenantSettings();
  const currentUserName = actorQuery.data?.displayName ?? readProfileValue(auth.user?.profile.name) ?? readProfileValue(auth.user?.profile.preferred_username) ?? readProfileValue(auth.user?.profile.email) ?? t('common.signedIn');
  const currentUserSecondary = actorQuery.data?.email ?? readProfileValue(auth.user?.profile.email) ?? readProfileValue(auth.user?.profile.preferred_username);

  return (
    <div className="min-h-screen bg-background text-foreground">
      <header className="sticky top-0 z-10 border-b border-border bg-content">
        <div className="flex items-center gap-3 px-3 py-3 sm:px-4 sm:py-4">
          <Link to="/desfire-studio" className="flex items-center gap-3" aria-label={t('desfireStudio.homeAriaLabel', { appName: branding.appName })}>
            <FabricLogo logoUrl={branding.logoUrl} />
            <div>
              <span className="hidden text-[20px] font-semibold tracking-tight min-[380px]:inline">{branding.appName}</span>
              <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{t('desfireStudio.title')}</p>
            </div>
          </Link>
          <div className="ml-auto">
            {auth.isAuthenticated ? (
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
            ) : null}
          </div>
        </div>
      </header>

      <main className="min-w-0">
        <div className="flex min-h-[calc(100vh-73px)] items-stretch">
          <aside className="flex w-80 shrink-0 flex-col border-r border-border bg-content p-4 md:sticky md:top-[73px] md:h-[calc(100vh-73px)]">
            <div className="px-1">
              <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{t('common.menu')}</p>
            </div>
            <nav aria-label={t('desfireStudio.navLabel')} className="mt-3 grid gap-2">
              {desfireStudioMenuItems.map((item) => {
                const isActive = isMenuItemActive(location.pathname, item.to);

                return (
                  <Link key={item.to} to={item.to} className={isActive ? 'flex gap-3 rounded-interactive bg-active-blue p-3 text-foreground' : 'flex gap-3 rounded-interactive p-3 text-foreground transition hover:bg-hover-blue'}>
                    <span className="min-w-0">
                      <span className="block font-semibold">{item.label}</span>
                      <span className="mt-1 block text-[13px] leading-5 text-muted-foreground">{item.description}</span>
                    </span>
                  </Link>
                );
              })}
            </nav>

            <div className="mt-auto px-1 pt-6 text-[12px] text-muted-foreground" title={`v${tenantSettings.version}`}>v{getDisplayVersion(tenantSettings.version)}</div>
          </aside>

          <div className="min-w-0 flex-1 px-4 py-5 sm:px-6 sm:py-6 md:px-10 md:py-8">
            <div className="mx-auto w-full max-w-7xl">{children}</div>
          </div>
        </div>
      </main>
    </div>
  );
}

function isMenuItemActive(pathname: string, itemPath: string) {
  return pathname === itemPath || pathname.startsWith(`${itemPath}/`);
}

function getDisplayVersion(version: string) {
  const buildMetadataIndex = version.indexOf('+');
  return buildMetadataIndex === -1 ? version : version.slice(0, buildMetadataIndex);
}

function readProfileValue(value: unknown) {
  return typeof value === 'string' && value.trim() ? value : undefined;
}
