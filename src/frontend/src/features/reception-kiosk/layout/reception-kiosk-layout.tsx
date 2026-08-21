import { useRouterState } from '@tanstack/react-router';
import { type ReactNode } from 'react';

import { FabricLogo } from '@/shared/branding/fabric-logo';
import { useBranding } from '@/shared/branding/branding-context';

export function ReceptionKioskLayout({ children }: { readonly children: ReactNode }) {
  const branding = useBranding();
  const pathname = useRouterState({ select: (state) => state.location.pathname });
  const isWideSessionPage = pathname === '/reception-kiosk/session/compliance/course';

  return (
    <div className="min-h-screen bg-background text-foreground">
      <header className="border-b border-border bg-content/95 px-5 py-4 shadow-sm sm:px-8">
        <div className="mx-auto flex max-w-6xl items-center gap-4">
          <FabricLogo logoUrl={branding.logoUrl} />
          <div>
            <p className="text-[13px] font-semibold uppercase tracking-[0.24em] text-muted-foreground">Reception kiosk</p>
            <h1 className="text-[22px] font-semibold tracking-tight sm:text-[28px]">{branding.appName}</h1>
          </div>
        </div>
      </header>

      <main className="min-h-[calc(100vh-89px)] px-4 py-6 sm:px-8 sm:py-10">
        <div className={`mx-auto flex min-h-[calc(100vh-161px)] w-full ${isWideSessionPage ? 'max-w-none items-stretch justify-stretch' : 'max-w-6xl items-center justify-center'}`}>{children}</div>
      </main>
    </div>
  );
}
