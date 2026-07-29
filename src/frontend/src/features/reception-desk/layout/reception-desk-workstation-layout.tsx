import { Link, useLocation } from '@tanstack/react-router';
import { ChevronDown, LogIn, Settings2 } from 'lucide-react';
import { type ReactNode } from 'react';
import { useAuth } from 'react-oidc-context';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { FabricLogo } from '@/shared/branding/fabric-logo';
import { useBranding } from '@/shared/branding/branding-context';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/components/ui/popover';
import { cn } from '@/shared/utils/cn';

export function ReceptionDeskWorkstationLayout({ children }: { readonly children: ReactNode }) {
  const auth = useAuth();
  const actorQuery = useCurrentActor();
  const branding = useBranding();
  const location = useLocation();
  const currentUserName = actorQuery.data?.displayName ?? readProfileValue(auth.user?.profile.name) ?? readProfileValue(auth.user?.profile.preferred_username) ?? readProfileValue(auth.user?.profile.email) ?? 'Signed in';
  const currentUserSecondary = actorQuery.data?.email ?? readProfileValue(auth.user?.profile.email) ?? readProfileValue(auth.user?.profile.preferred_username);

  return (
    <div className="min-h-screen bg-background text-foreground">
      <header className="border-b border-border bg-content/95 px-4 py-3 shadow-sm sm:px-6">
        <div className="mx-auto flex max-w-7xl flex-col gap-3">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex items-center gap-3">
              <FabricLogo logoUrl={branding.logoUrl} />
              <div>
                <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-muted-foreground">Reception desk workstation</p>
                <h1 className="text-[18px] font-semibold tracking-tight sm:text-[22px]">{branding.appName}</h1>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <WorkstationNavLink to="/reception-desk-workstation/expected-arrivals" currentPath={location.pathname}>Expected Arrivals</WorkstationNavLink>
              <WorkstationNavLink to="/reception-desk-workstation/arrivals" currentPath={location.pathname}>Arrivals</WorkstationNavLink>
              <WorkstationNavLink to="/reception-desk-workstation/history" currentPath={location.pathname}>History</WorkstationNavLink>
              {auth.isAuthenticated ? (
                <Popover>
                  <PopoverTrigger render={<Button type="button" variant="outline" size="sm" className="ml-0 max-w-[16rem] justify-between lg:ml-2" aria-label="Open operator menu" />}>
                    <span className="truncate text-left text-[14px] font-semibold">{currentUserName}</span>
                    <ChevronDown className="size-4 text-muted-foreground" aria-hidden="true" />
                  </PopoverTrigger>
                  <PopoverContent align="end" className="grid min-w-64 gap-3 p-3">
                    <div className="min-w-0 border-b border-border pb-3">
                      <p className="truncate text-[14px] font-semibold text-foreground">{currentUserName}</p>
                      {currentUserSecondary && currentUserSecondary !== currentUserName ? <p className="mt-1 truncate text-[13px] text-muted-foreground">{currentUserSecondary}</p> : null}
                    </div>
                    <Link to="/reception-desk-workstation/setup" className={buttonVariants({ variant: 'ghost', className: 'justify-start' })}>
                      <Settings2 className="size-4" aria-hidden="true" />
                      Setup
                    </Link>
                    <Button type="button" variant="ghost" className="justify-start" onClick={() => void auth.signoutRedirect().catch(() => auth.removeUser())}>
                      Sign out
                    </Button>
                  </PopoverContent>
                </Popover>
              ) : (
                <Button type="button" variant="outline" size="sm" className="ml-0 lg:ml-2" onClick={() => void auth.signinRedirect({ state: { returnTo: '/reception-desk-workstation' } })}>
                  <LogIn className="size-4" aria-hidden="true" />
                  Sign in
                </Button>
              )}
            </div>
          </div>
        </div>
      </header>

      <main className="px-4 py-4 sm:px-6 sm:py-5">
        <div className="mx-auto w-full max-w-7xl">{children}</div>
      </main>
    </div>
  );
}

function WorkstationNavLink({ children, currentPath, to }: { readonly children: ReactNode; readonly currentPath: string; readonly to: string }) {
  const isActive = currentPath === to || currentPath.startsWith(`${to}/`);

  return (
    <Link
      to={to}
      className={cn(
        'inline-flex h-9 items-center justify-center rounded-interactive border px-3 text-[14px] font-medium transition',
        isActive ? 'border-primary bg-active-blue text-foreground' : 'border-border bg-content text-muted-foreground hover:bg-hover-blue hover:text-foreground',
      )}
    >
      {children}
    </Link>
  );
}

function readProfileValue(value: unknown) {
  return typeof value === 'string' && value.trim() ? value : undefined;
}
