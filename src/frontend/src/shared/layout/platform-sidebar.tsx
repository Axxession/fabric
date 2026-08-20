import { Building2, Layers3, Plus } from 'lucide-react';
import { Link, useLocation } from '@tanstack/react-router';

import { FabricLogo } from '@/shared/branding/fabric-logo';
import { Sidebar, SidebarClose, SidebarContent, SidebarHeader } from '@/shared/components/ui/sidebar';
import { AccountMenu } from '@/shared/layout/account-menu';
import { cn } from '@/shared/utils/cn';

const platformMenuItems = [
  { label: 'Tenant Directory', to: '/platform/tenants', icon: Layers3 },
  { label: 'Create Tenant', to: '/platform/tenants/new', icon: Plus },
] as const;

export function PlatformSidebar({
  currentUserName,
  currentUserSecondary,
  currentUserInitials,
  appName,
  logoUrl,
}: {
  currentUserName: string;
  currentUserSecondary?: string;
  currentUserInitials: string;
  appName: string;
  logoUrl?: string;
}) {
  const location = useLocation();

  return (
    <Sidebar className="md:w-[25rem] md:shrink-0 md:border-r md:border-[var(--fabric-sidebar-panel-border)]">
      <div className="flex h-full min-h-0 bg-[var(--fabric-sidebar-panel)] md:sticky md:top-0 md:h-screen">
        <div className="hidden w-[5.5rem] shrink-0 flex-col items-center gap-3 overflow-y-auto bg-[var(--fabric-sidebar-rail)] px-3 py-[14px] text-[var(--fabric-sidebar-rail-foreground)] md:flex">
          <div className="mb-2 text-[11px] font-semibold uppercase tracking-[0.22em] text-[var(--fabric-sidebar-rail-muted)]">Mode</div>
          <Link
            to="/platform/tenants"
            className={cn(
              'relative flex w-full flex-col items-center gap-2 rounded-[13px] px-2 py-3 text-center transition',
              location.pathname.startsWith('/platform')
                ? 'bg-[var(--fabric-sidebar-rail-active)] text-white'
                : 'text-[var(--fabric-sidebar-rail-muted)] hover:bg-[var(--fabric-sidebar-rail-hover)] hover:text-white',
            )}
            title="Platform"
          >
            <span className="absolute left-[-12px] top-1/2 h-8 w-1 -translate-y-1/2 rounded-r bg-[#6da4dd]" />
            <Building2 className="size-5 text-[#9fc4ea]" />
            <span className="text-[10px] font-semibold leading-3">Platform</span>
          </Link>

          <div className="mt-auto pt-4">
            <AccountMenu
              currentUserName={currentUserName}
              currentUserSecondary={currentUserSecondary}
              currentUserInitials={currentUserInitials}
              trigger={<button type="button" className="inline-flex size-11 items-center justify-center rounded-full bg-[rgba(255,255,255,0.1)] text-[14px] font-semibold tracking-[0.08em] text-white transition hover:bg-[var(--fabric-sidebar-rail-hover)] focus-visible:ring-[3px] focus-visible:ring-primary/20 focus-visible:outline-none" aria-label="Open account menu" />}
              contentClassName="grid min-w-72 gap-4 border border-[var(--fabric-sidebar-panel-border)] bg-content p-4"
            />
          </div>
        </div>

        <div className="flex min-h-0 min-w-0 flex-1 flex-col border-r-0 border-[var(--fabric-sidebar-panel-border)] bg-[var(--fabric-sidebar-menu)] text-white">
          <SidebarHeader className="border-b border-[var(--fabric-sidebar-menu-border)] bg-[var(--fabric-sidebar-menu)] p-[18px] text-white md:hidden">
            <div className="flex items-center justify-between gap-3">
              <Link to="/platform/tenants" className="flex min-w-0 items-center gap-3" aria-label={`${appName} platform home`}>
                <div className="rounded-[13px] bg-white px-1 py-1 text-primary"><FabricLogo logoUrl={logoUrl} /></div>
                <div className="min-w-0">
                  <p className="truncate text-[18px] font-semibold tracking-tight text-white">{appName}</p>
                  <p className="truncate text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--fabric-sidebar-menu-muted)]">Platform</p>
                </div>
              </Link>
              <SidebarClose />
            </div>
          </SidebarHeader>

          <SidebarContent className="flex min-h-0 flex-1 flex-col gap-6 overflow-y-auto p-[18px] md:p-5">
            <Link to="/platform/tenants" className="hidden min-w-0 items-center gap-3 md:flex" aria-label={`${appName} platform home`}>
              <div className="rounded-[14px] bg-white px-1 py-1 text-primary shadow-sm">
                <FabricLogo logoUrl={logoUrl} />
              </div>
              <div className="min-w-0">
                <p className="truncate text-[22px] font-semibold tracking-tight text-white">{appName}</p>
                <p className="truncate text-[11px] font-semibold uppercase tracking-[0.2em] text-[var(--fabric-sidebar-menu-muted)]">Platform</p>
              </div>
            </Link>

            <div className="grid gap-3">
              <p className="px-1 text-[12px] font-semibold uppercase tracking-[0.18em] text-[var(--fabric-sidebar-menu-muted)]">Menu</p>
              <nav aria-label="Platform navigation" className="grid gap-1.5">
                {platformMenuItems.map((item) => {
                  const isActive = location.pathname === item.to || location.pathname.startsWith(`${item.to}/`);
                  const Icon = item.icon;

                  return (
                    <Link
                      key={item.to}
                      to={item.to}
                      className={cn(
                        'relative flex items-center gap-3 rounded-interactive px-4 py-[11px] text-[14px] font-semibold transition',
                        isActive ? 'bg-white/12 text-white' : 'text-white hover:bg-white/8',
                      )}
                    >
                      <span className={cn('absolute left-0 top-1/2 h-7 w-1 -translate-y-1/2 rounded-r bg-[#6da4dd] transition', isActive ? 'scale-y-100' : 'scale-y-0')} />
                      <Icon className="size-4 shrink-0" />
                      <span>{item.label}</span>
                    </Link>
                  );
                })}
              </nav>
            </div>
          </SidebarContent>
        </div>
      </div>
    </Sidebar>
  );
}
