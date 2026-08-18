import { Link, useLocation } from '@tanstack/react-router';
import { LayoutGrid, LogIn, Server } from 'lucide-react';
import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from 'react-oidc-context';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { FabricLogo } from '@/shared/branding/fabric-logo';
import { useBranding } from '@/shared/branding/branding-context';
import { Button } from '@/shared/components/ui/button';
import { Sidebar, SidebarClose, SidebarContent, SidebarHeader, SidebarProvider, SidebarTrigger } from '@/shared/components/ui/sidebar';
import { AccountMenu } from '@/shared/layout/account-menu';
import { useTenantSettings } from '@/shared/tenant/tenant-settings-context';
import { cn } from '@/shared/utils/cn';

type DesfireStudioSection = 'encoding' | 'infra';

const desfireStudioSections = [
  { id: 'encoding', shortLabel: 'Encode', icon: LayoutGrid, to: '/desfire-studio/key-management' },
  { id: 'infra', shortLabel: 'Infra', icon: Server, to: '/desfire-studio/hardware-agents' },
] as const satisfies readonly { id: DesfireStudioSection; shortLabel: string; icon: typeof LayoutGrid; to: string }[];

export function DesfireStudioLayout({ children }: { readonly children: ReactNode }) {
  const location = useLocation();
  const { t } = useTranslation();
  const auth = useAuth();
  const branding = useBranding();
  const actorQuery = useCurrentActor();
  const tenantSettings = useTenantSettings();
  const currentSection = getCurrentSection(location.pathname);
  const menuItems = getSectionMenuItems(t, currentSection);
  const currentUserName = actorQuery.data?.displayName ?? readProfileValue(auth.user?.profile.name) ?? readProfileValue(auth.user?.profile.preferred_username) ?? readProfileValue(auth.user?.profile.email) ?? t('common.signedIn');
  const currentUserSecondary = actorQuery.data?.email ?? readProfileValue(auth.user?.profile.email) ?? readProfileValue(auth.user?.profile.preferred_username);
  const currentUserInitials = getUserInitials(currentUserName, currentUserSecondary);

  return (
    <SidebarProvider>
      <div className="min-h-screen bg-background text-foreground">
        <main className="min-w-0">
          <div className="flex min-h-screen items-stretch">
            <Sidebar className="md:w-[25rem] md:shrink-0 md:border-r md:border-[var(--fabric-sidebar-panel-border)]">
              <div className="flex h-full min-h-0 bg-[var(--fabric-sidebar-panel)] md:sticky md:top-0 md:h-screen">
                <div className="hidden w-[5.5rem] shrink-0 flex-col items-center gap-3 overflow-y-auto bg-[var(--fabric-sidebar-rail)] px-3 py-[14px] text-[var(--fabric-sidebar-rail-foreground)] md:flex">
                  <div className="mb-2 text-[11px] font-semibold uppercase tracking-[0.22em] text-[var(--fabric-sidebar-rail-muted)]">Mode</div>
                  {desfireStudioSections.map((section) => {
                    const isActive = currentSection === section.id;
                    const Icon = section.icon;

                    return (
                      <Link
                        key={section.id}
                        to={section.to}
                        className={cn(
                          'relative flex w-full flex-col items-center gap-2 rounded-[13px] px-2 py-3 text-center transition',
                          isActive
                            ? 'bg-[var(--fabric-sidebar-rail-active)] text-white'
                            : 'text-[var(--fabric-sidebar-rail-muted)] hover:bg-[var(--fabric-sidebar-rail-hover)] hover:text-white',
                        )}
                        title={t(`desfireStudio.sections.${section.id}`)}
                      >
                        <span className={cn('absolute left-[-12px] top-1/2 h-8 w-1 -translate-y-1/2 rounded-r bg-[#6da4dd] transition', isActive ? 'scale-y-100' : 'scale-y-0')} />
                        <Icon className={cn('size-5', isActive ? 'text-[#9fc4ea]' : '')} />
                        <span className="text-[10px] font-semibold leading-3">{section.shortLabel}</span>
                      </Link>
                    );
                  })}

                  <div className="mt-auto pt-4">
                    {auth.isAuthenticated ? (
                      <AccountMenu
                        currentUserName={currentUserName}
                        currentUserSecondary={currentUserSecondary}
                        currentUserInitials={currentUserInitials}
                        trigger={<button type="button" className="inline-flex size-11 items-center justify-center rounded-full bg-[rgba(255,255,255,0.1)] text-[14px] font-semibold tracking-[0.08em] text-white transition hover:bg-[var(--fabric-sidebar-rail-hover)] focus-visible:ring-[3px] focus-visible:ring-primary/20 focus-visible:outline-none" aria-label={t('desfireStudio.openOperatorMenu')} />}
                        contentClassName="grid min-w-72 gap-4 border border-[var(--fabric-sidebar-panel-border)] bg-content p-4"
                      />
                    ) : (
                      <button
                        type="button"
                        className="inline-flex size-11 items-center justify-center rounded-full bg-[rgba(255,255,255,0.1)] text-white transition hover:bg-[var(--fabric-sidebar-rail-hover)] focus-visible:ring-[3px] focus-visible:ring-primary/20 focus-visible:outline-none"
                        aria-label={t('common.signIn')}
                        onClick={() => void auth.signinRedirect({ state: { returnTo: '/desfire-studio' } })}
                      >
                        <LogIn className="size-5" aria-hidden="true" />
                      </button>
                    )}
                  </div>
                </div>

                <div className="flex min-h-0 min-w-0 flex-1 flex-col bg-[var(--fabric-sidebar-menu)] text-white">
                  <SidebarHeader className="border-b border-[var(--fabric-sidebar-menu-border)] bg-[var(--fabric-sidebar-menu)] p-[18px] text-white md:hidden">
                    <div className="flex items-center justify-between gap-3">
                      <div className="min-w-0">
                        <Link to="/desfire-studio" className="flex min-w-0 items-center gap-3" aria-label={t('desfireStudio.homeAriaLabel', { appName: branding.appName })}>
                          <div className="rounded-[13px] bg-white px-1 py-1 text-primary"><FabricLogo logoUrl={branding.logoUrl} /></div>
                          <div className="min-w-0">
                            <p className="truncate text-[18px] font-semibold tracking-tight text-white">{branding.appName}</p>
                            <p className="truncate text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--fabric-sidebar-menu-muted)]">{t(`desfireStudio.sections.${currentSection}`)}</p>
                          </div>
                        </Link>
                      </div>
                      <SidebarClose />
                    </div>
                  </SidebarHeader>

                  <SidebarContent className="flex min-h-0 flex-1 flex-col gap-6 overflow-y-auto p-[18px] md:p-5">
                    <div className="grid gap-2 md:hidden">
                      <p className="px-1 text-[12px] font-semibold uppercase tracking-[0.18em] text-[var(--fabric-sidebar-menu-muted)]">{t('desfireStudio.sectionsLabel')}</p>
                      <div className="grid grid-cols-2 gap-2">
                        {desfireStudioSections.map((section) => {
                          const isActive = currentSection === section.id;
                          const Icon = section.icon;

                          return (
                            <Link
                              key={section.id}
                              to={section.to}
                              className={cn(
                                'flex items-center gap-3 rounded-interactive border px-3 py-3 transition',
                                isActive
                                  ? 'border-transparent bg-white/12 text-white'
                                  : 'border-[var(--fabric-sidebar-menu-border)] text-white hover:bg-white/8',
                              )}
                            >
                              <Icon className="size-4 shrink-0" />
                              <span className="block text-[13px] font-semibold">{t(`desfireStudio.sections.${section.id}`)}</span>
                            </Link>
                          );
                        })}
                      </div>
                    </div>

                    <Link to="/desfire-studio" className="hidden min-w-0 items-center gap-3 md:flex" aria-label={t('desfireStudio.homeAriaLabel', { appName: branding.appName })}>
                      <div className="rounded-[14px] bg-white px-1 py-1 text-primary shadow-sm">
                        <FabricLogo logoUrl={branding.logoUrl} />
                      </div>
                      <div className="min-w-0">
                        <p className="truncate text-[22px] font-semibold tracking-tight text-white">{branding.appName}</p>
                        <p className="truncate text-[11px] font-semibold uppercase tracking-[0.2em] text-[var(--fabric-sidebar-menu-muted)]">{t(`desfireStudio.sections.${currentSection}`)}</p>
                      </div>
                    </Link>

                    <div className="grid gap-3">
                      <p className="px-1 text-[12px] font-semibold uppercase tracking-[0.18em] text-[var(--fabric-sidebar-menu-muted)]">{t('common.menu')}</p>
                      <nav aria-label={t('desfireStudio.navLabel')} className="grid gap-1.5">
                        {menuItems.map((item) => {
                          const isActive = isMenuItemActive(location.pathname, item.to);

                          return (
                            <Link
                              key={item.to}
                              to={item.to}
                              className={cn(
                                'relative rounded-interactive px-4 py-[11px] text-[14px] font-semibold transition',
                                isActive
                                  ? 'bg-white/12 text-white'
                                  : 'text-white hover:bg-white/8',
                              )}
                            >
                              <span className={cn('absolute left-0 top-1/2 h-7 w-1 -translate-y-1/2 rounded-r bg-[#6da4dd] transition', isActive ? 'scale-y-100' : 'scale-y-0')} />
                              <span className="block">{item.label}</span>
                            </Link>
                          );
                        })}
                      </nav>
                    </div>

                    <div className="mt-auto px-1 pt-2 text-[12px] text-[var(--fabric-sidebar-menu-muted)]" title={`v${tenantSettings.version}`}>v{getDisplayVersion(tenantSettings.version)}</div>
                  </SidebarContent>
                </div>
              </div>
            </Sidebar>

            <div className="min-w-0 flex-1 px-4 py-4 sm:px-6 sm:py-6 md:px-8 md:py-8 xl:px-10">
              <div className="mb-5 flex items-center gap-3 md:hidden">
                <SidebarTrigger className="border-[var(--fabric-sidebar-panel-border)] bg-content" />
                <Link to="/desfire-studio" className="min-w-0 text-[18px] font-semibold tracking-tight text-foreground" aria-label={t('desfireStudio.homeAriaLabel', { appName: branding.appName })}>
                  {branding.appName}
                </Link>
                <div className="ml-auto">
                  {auth.isAuthenticated ? (
                    <AccountMenu
                      currentUserName={currentUserName}
                      currentUserSecondary={currentUserSecondary}
                      currentUserInitials={currentUserInitials}
                      trigger={<button type="button" className="inline-flex size-11 items-center justify-center rounded-full bg-[var(--fabric-sidebar-rail)] text-[14px] font-semibold tracking-[0.08em] text-white transition hover:bg-[var(--fabric-sidebar-rail-hover)] focus-visible:ring-[3px] focus-visible:ring-primary/20 focus-visible:outline-none" aria-label={t('desfireStudio.openOperatorMenu')} />}
                    />
                  ) : (
                    <Button type="button" variant="outline" onClick={() => void auth.signinRedirect({ state: { returnTo: '/desfire-studio' } })}>
                      <LogIn className="size-4" aria-hidden="true" />
                      {t('common.signIn')}
                    </Button>
                  )}
                </div>
              </div>
              <div className="mx-auto w-full max-w-7xl">{children}</div>
            </div>
          </div>
        </main>
      </div>
    </SidebarProvider>
  );
}

function isMenuItemActive(pathname: string, itemPath: string) {
  return pathname === itemPath || pathname.startsWith(`${itemPath}/`);
}

function getCurrentSection(pathname: string): DesfireStudioSection {
  return pathname.startsWith('/desfire-studio/hardware-agents') ? 'infra' : 'encoding';
}

function getSectionMenuItems(t: ReturnType<typeof useTranslation>['t'], section: DesfireStudioSection) {
  if (section === 'infra') {
    return [{ label: t('desfireStudio.menu.hardwareAgents.label'), to: '/desfire-studio/hardware-agents' }];
  }

  return [
    { label: t('desfireStudio.menu.keyManagement.label'), to: '/desfire-studio/key-management' },
    { label: t('desfireStudio.menu.chipDesigner.label'), to: '/desfire-studio/chip-designer' },
    { label: t('desfireStudio.menu.cardEditor.label'), to: '/desfire-studio/card-editor' },
    { label: t('desfireStudio.menu.printing.label'), to: '/desfire-studio/printing' },
  ];
}

function getDisplayVersion(version: string) {
  const buildMetadataIndex = version.indexOf('+');
  return buildMetadataIndex === -1 ? version : version.slice(0, buildMetadataIndex);
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
