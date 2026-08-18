import { Link, useLocation } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';

import { FabricLogo } from '@/shared/branding/fabric-logo';
import { Sidebar, SidebarClose, SidebarContent, SidebarHeader } from '@/shared/components/ui/sidebar';
import { AccountMenu } from '@/shared/layout/account-menu';
import type { ResolvedAppPerspective } from '@/shared/perspectives/app-perspectives';
import { cn } from '@/shared/utils/cn';

export function PerspectiveSidebar({
  perspectives,
  version,
  currentUserName,
  currentUserSecondary,
  currentUserInitials,
  appName,
  logoUrl,
}: {
  perspectives: readonly ResolvedAppPerspective[];
  version: string;
  currentUserName: string;
  currentUserSecondary?: string;
  currentUserInitials: string;
  appName: string;
  logoUrl?: string;
}) {
  const location = useLocation();
  const { t } = useTranslation();
  const activePerspective = perspectives.find((perspective) => location.pathname === perspective.to || location.pathname.startsWith(`${perspective.to}/`));
  const displayVersion = getDisplayVersion(version);

  return (
    <Sidebar className="md:w-[25rem] md:shrink-0 md:border-r md:border-[var(--fabric-sidebar-panel-border)]">
      <div className="flex h-full min-h-0 bg-[var(--fabric-sidebar-panel)] md:sticky md:top-0 md:h-screen">
        <div className="hidden w-[5.5rem] shrink-0 flex-col items-center gap-3 overflow-y-auto bg-[var(--fabric-sidebar-rail)] px-3 py-[14px] text-[var(--fabric-sidebar-rail-foreground)] md:flex">
          <div className="mb-2 text-[11px] font-semibold uppercase tracking-[0.22em] text-[var(--fabric-sidebar-rail-muted)]">Mode</div>
          {perspectives.map((perspective) => {
            const isActive = location.pathname === perspective.to || location.pathname.startsWith(`${perspective.to}/`);
            const Icon = perspective.icon;

            return (
              <Link
                key={perspective.id}
                to={perspective.to}
                className={cn(
                  'relative flex w-full flex-col items-center gap-2 rounded-[13px] px-2 py-3 text-center transition',
                  isActive
                    ? 'bg-[var(--fabric-sidebar-rail-active)] text-white'
                    : 'text-[var(--fabric-sidebar-rail-muted)] hover:bg-[var(--fabric-sidebar-rail-hover)] hover:text-white',
                )}
                title={perspective.label}
              >
                <span className={cn('absolute left-[-12px] top-1/2 h-8 w-1 -translate-y-1/2 rounded-r bg-[#6da4dd] transition', isActive ? 'scale-y-100' : 'scale-y-0')} />
                <Icon className={cn('size-5', isActive ? 'text-[#9fc4ea]' : '')} />
                <span className="text-[10px] font-semibold leading-3">{perspective.shortLabel}</span>
              </Link>
            );
          })}

          <div className="mt-auto pt-4">
            <AccountMenu
              currentUserName={currentUserName}
              currentUserSecondary={currentUserSecondary}
              currentUserInitials={currentUserInitials}
              trigger={<button type="button" className="inline-flex size-11 items-center justify-center rounded-full bg-[rgba(255,255,255,0.1)] text-[14px] font-semibold tracking-[0.08em] text-white transition hover:bg-[var(--fabric-sidebar-rail-hover)] focus-visible:ring-[3px] focus-visible:ring-primary/20 focus-visible:outline-none" aria-label={t('common.openAccountMenu')} />}
              contentClassName="grid min-w-72 gap-4 border border-[var(--fabric-sidebar-panel-border)] bg-content p-4"
            />
          </div>
        </div>

        <div className="flex min-h-0 min-w-0 flex-1 flex-col border-r-0 border-[var(--fabric-sidebar-panel-border)] bg-[var(--fabric-sidebar-menu)] text-white">
          <SidebarHeader className="border-b border-[var(--fabric-sidebar-menu-border)] bg-[var(--fabric-sidebar-menu)] p-[18px] text-white md:hidden">
            <div className="flex items-center justify-between gap-3">
              <div className="min-w-0">
                <Link to="/" className="flex min-w-0 items-center gap-3" aria-label={`${appName} home`}>
                  <div className="rounded-[13px] bg-white px-1 py-1 text-primary"><FabricLogo logoUrl={logoUrl} /></div>
                  <div className="min-w-0">
                    <p className="truncate text-[18px] font-semibold tracking-tight text-white">{appName}</p>
                    <p className="truncate text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--fabric-sidebar-menu-muted)]">{activePerspective?.label ?? t('common.menu')}</p>
                  </div>
                </Link>
              </div>
              <SidebarClose />
            </div>
          </SidebarHeader>

          <SidebarContent className="flex min-h-0 flex-1 flex-col gap-6 overflow-y-auto p-[18px] md:p-5">
            <div className="grid gap-2 md:hidden">
              <p className="px-1 text-[12px] font-semibold uppercase tracking-[0.18em] text-[var(--fabric-sidebar-menu-muted)]">Perspectives</p>
              <div className="grid grid-cols-2 gap-2">
                {perspectives.map((perspective) => {
                  const isActive = location.pathname === perspective.to || location.pathname.startsWith(`${perspective.to}/`);
                  const Icon = perspective.icon;

                  return (
                    <Link
                      key={perspective.id}
                      to={perspective.to}
                      className={cn(
                        'flex items-center gap-3 rounded-interactive border px-3 py-3 transition',
                        isActive
                          ? 'border-transparent bg-white/12 text-white'
                          : 'border-[var(--fabric-sidebar-menu-border)] text-white hover:bg-white/8',
                      )}
                    >
                      <Icon className="size-4 shrink-0" />
                      <span className="min-w-0">
                        <span className="block text-[13px] font-semibold">{perspective.label}</span>
                        <span className="block text-[12px] text-[var(--fabric-sidebar-menu-muted)]">{perspective.shortLabel}</span>
                      </span>
                    </Link>
                  );
                })}
              </div>
            </div>

            <Link to="/" className="hidden min-w-0 items-center gap-3 md:flex" aria-label={`${appName} home`}>
              <div className="rounded-[14px] bg-white px-1 py-1 text-primary shadow-sm">
                <FabricLogo logoUrl={logoUrl} />
              </div>
              <div className="min-w-0">
                <p className="truncate text-[22px] font-semibold tracking-tight text-white">{appName}</p>
                <p className="truncate text-[11px] font-semibold uppercase tracking-[0.2em] text-[var(--fabric-sidebar-menu-muted)]">{activePerspective?.label ?? t('common.menu')}</p>
              </div>
            </Link>

            <div className="grid gap-3">
              <p className="px-1 text-[12px] font-semibold uppercase tracking-[0.18em] text-[var(--fabric-sidebar-menu-muted)]">{t('common.menu')}</p>
              <nav aria-label={t('shell.perspectiveNavigation')} className="grid gap-1.5">
                {activePerspective?.menuItems.map((item) => {
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

            <div className="mt-auto px-1 pt-2 text-[12px] text-[var(--fabric-sidebar-menu-muted)]" title={`v${version}`}>v{displayVersion}</div>
          </SidebarContent>
        </div>
      </div>
    </Sidebar>
  );
}

function isMenuItemActive(pathname: string, itemPath: string) {
  if (pathname === itemPath) {
    return true;
  }

  if (isPerspectiveRootPath(itemPath)) {
    return false;
  }

  return pathname.startsWith(`${itemPath}/`);
}

function isPerspectiveRootPath(itemPath: string) {
  return itemPath === '/employee'
    || itemPath === '/manager'
    || itemPath === '/security-officer'
    || itemPath === '/integrations'
    || itemPath === '/administration';
}

function getDisplayVersion(version: string) {
  const buildMetadataIndex = version.indexOf('+');
  return buildMetadataIndex === -1 ? version : version.slice(0, buildMetadataIndex);
}
