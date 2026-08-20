import { Link } from '@tanstack/react-router';
import { type ReactNode } from 'react';
import { useAuth } from 'react-oidc-context';

import { useBranding } from '@/shared/branding/branding-context';
import { SidebarProvider, SidebarTrigger } from '@/shared/components/ui/sidebar';
import { AccountMenu } from '@/shared/layout/account-menu';
import { PlatformSidebar } from '@/shared/layout/platform-sidebar';

export function PlatformLayout({ children }: { children: ReactNode }) {
  const auth = useAuth();
  const branding = useBranding();
  const currentUserName = readProfileValue(auth.user?.profile.name) ?? readProfileValue(auth.user?.profile.preferred_username) ?? readProfileValue(auth.user?.profile.email) ?? 'Signed in';
  const currentUserSecondary = readProfileValue(auth.user?.profile.email) ?? readProfileValue(auth.user?.profile.preferred_username);
  const currentUserInitials = getUserInitials(currentUserName, currentUserSecondary);

  return (
    <SidebarProvider>
      <div className="min-h-screen bg-background text-foreground">
        <main className="min-w-0">
          <div className="flex min-h-screen items-stretch">
            <PlatformSidebar
              currentUserName={currentUserName}
              currentUserSecondary={currentUserSecondary}
              currentUserInitials={currentUserInitials}
              appName={branding.appName}
              logoUrl={branding.logoUrl}
            />
            <div className="min-w-0 flex-1 px-4 py-4 sm:px-6 sm:py-6 md:px-8 md:py-8 xl:px-10">
              <div className="mb-5 flex items-center gap-3 md:hidden">
                <SidebarTrigger className="border-[var(--fabric-sidebar-panel-border)] bg-content" />
                <Link to="/platform/tenants" className="min-w-0 text-[18px] font-semibold tracking-tight text-foreground" aria-label={`${branding.appName} platform home`}>
                  {branding.appName}
                </Link>
                <div className="ml-auto">
                  <AccountMenu
                    currentUserName={currentUserName}
                    currentUserSecondary={currentUserSecondary}
                    currentUserInitials={currentUserInitials}
                    trigger={<button type="button" className="inline-flex size-11 items-center justify-center rounded-full bg-[var(--fabric-sidebar-rail)] text-[14px] font-semibold tracking-[0.08em] text-white transition hover:bg-[var(--fabric-sidebar-rail-hover)] focus-visible:ring-[3px] focus-visible:ring-primary/20 focus-visible:outline-none" aria-label="Open account menu" />}
                  />
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
