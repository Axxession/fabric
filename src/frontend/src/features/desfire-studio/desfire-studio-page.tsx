import { Navigate } from '@tanstack/react-router';
import { ArrowRight, ShieldCheck } from 'lucide-react';
import { useAuth } from 'react-oidc-context';

import { Button } from '@/shared/components/ui/button';

export default function DesfireStudioPage() {
  const auth = useAuth();

  if (auth.isLoading || auth.activeNavigator) {
    return <div className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">Loading DESFire Studio...</div>;
  }

  if (!auth.isAuthenticated) {
    return <DesfireStudioSignInLanding />;
  }

  return <Navigate to="/desfire-studio/key-management" replace />;
}

function DesfireStudioSignInLanding() {
  const auth = useAuth();

  return (
    <section className="grid w-full gap-8 lg:grid-cols-[1.1fr_0.9fr] lg:items-stretch">
      <div className="relative overflow-hidden rounded-structural border border-border bg-content p-6 sm:p-8 md:p-12">
        <div className="absolute right-[-80px] top-[-120px] size-72 rounded-full bg-primary/10" />
        <div className="relative max-w-2xl">
          <p className="text-[14px] font-semibold uppercase tracking-wide text-primary">DESFire Studio</p>
          <h2 className="mt-4 text-[34px] font-semibold leading-tight tracking-tight sm:text-[42px] md:text-[56px]">Sign in before running DESFire operations.</h2>
          <p className="mt-5 max-w-xl text-[16px] leading-7 text-muted-foreground">
            DESFire Studio centralizes hardware, key management, chip design, and encoding workflows in one authenticated workspace.
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Button type="button" onClick={() => void auth.signinRedirect({ state: { returnTo: '/desfire-studio' } })} className="h-12 px-6 text-[15px]">
              Sign in
              <ArrowRight className="ml-2 size-4" />
            </Button>
          </div>
        </div>
      </div>

      <div className="rounded-structural border border-border bg-content p-6 md:p-8">
        <div className="flex size-12 items-center justify-center rounded-interactive bg-active-blue text-primary">
          <ShieldCheck className="size-6" />
        </div>
        <h3 className="mt-6 text-[24px] font-semibold tracking-tight">Operator sign-in only</h3>
        <p className="mt-3 text-[14px] leading-6 text-muted-foreground">
          This shell does not need workstation setup or local API keys. Authenticated operators can access DESFire tools immediately.
        </p>
      </div>
    </section>
  );
}
