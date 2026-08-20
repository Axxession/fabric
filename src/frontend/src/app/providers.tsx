import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { I18nextProvider } from 'react-i18next';
import { useTranslation } from 'react-i18next';
import { type ReactNode, useEffect, useState } from 'react';

import { i18n } from '@/shared/i18n/i18n';
import { Toaster } from '@/shared/components/ui/sonner';
import { applyFabricTheme, defaultFabricTheme } from '@/shared/theme/fabric-theme';
import { BrandingProvider } from '@/shared/branding/branding-context';
import { appBranding } from '@/shared/branding/fabric-branding';
import { FabricAuthProvider } from '@/shared/auth/auth-provider';
import { AuthTokenBridge } from '@/shared/auth/auth-token-bridge';
import { fetchPlatformAuthSettings, type PlatformAuthSettings } from '@/shared/platform/platform-auth-settings';
import { TenantSettingsProvider } from '@/shared/tenant/tenant-settings-context';
import { fetchTenantSettings, getLogoDataUrl, type TenantSettings } from '@/shared/tenant/tenant-settings';

export function GlobalAppProviders({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            refetchOnWindowFocus: false,
          },
        },
      }),
  );

  return (
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={queryClient}>
        {children}
        <Toaster />
      </QueryClientProvider>
    </I18nextProvider>
  );
}

export function TenantAppProviders({ children }: { children: ReactNode }) {
  const [tenantSettings, setTenantSettings] = useState<TenantSettings | null>(null);
  const [tenantSettingsError, setTenantSettingsError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function loadTenantSettings() {
      try {
        const settings = await fetchTenantSettings();
        applyFabricTheme(defaultFabricTheme);
        setTenantSettings(settings);
      } catch (error) {
        if (controller.signal.aborted) {
          return;
        }

        applyFabricTheme(defaultFabricTheme);
        setTenantSettingsError(error instanceof Error ? error.message : 'Tenant settings could not be loaded.');
      }
    }

    void loadTenantSettings();

    return () => controller.abort();
  }, []);

  if (tenantSettingsError) {
    return <TenantSettingsError message={tenantSettingsError} />;
  }

  if (!tenantSettings) {
    return <TenantSettingsLoading />;
  }

  const branding = { ...appBranding, logoUrl: getLogoDataUrl(tenantSettings.logo) };

  return (
    <TenantSettingsProvider settings={tenantSettings}>
      <BrandingProvider branding={branding}>
        <FabricAuthProvider
          settings={tenantSettings.oidc}
          callbackPath="/auth/callback"
          postLogoutPath="/"
          storageKeyPrefix="fabric.tenant.oidc"
        >
          <AuthTokenBridge />
          {children}
        </FabricAuthProvider>
      </BrandingProvider>
    </TenantSettingsProvider>
  );
}

export function PlatformAppProviders({ children }: { children: ReactNode }) {
  const [platformAuthSettings, setPlatformAuthSettings] = useState<PlatformAuthSettings | null>(null);
  const [platformAuthError, setPlatformAuthError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    async function loadPlatformAuthSettings() {
      try {
        applyFabricTheme(defaultFabricTheme);
        const settings = await fetchPlatformAuthSettings();
        setPlatformAuthSettings(settings);
      } catch (error) {
        if (controller.signal.aborted) {
          return;
        }

        setPlatformAuthError(error instanceof Error ? error.message : 'Platform authentication settings could not be loaded.');
      }
    }

    void loadPlatformAuthSettings();

    return () => controller.abort();
  }, []);

  if (platformAuthError) {
    return <TenantSettingsError message={platformAuthError} />;
  }

  if (!platformAuthSettings) {
    return <TenantSettingsLoading />;
  }

  return (
    <BrandingProvider branding={appBranding}>
      <FabricAuthProvider
        settings={platformAuthSettings.oidc}
        callbackPath="/platform/auth/callback"
        postLogoutPath="/platform"
        storageKeyPrefix="fabric.platform.oidc"
      >
        <AuthTokenBridge />
        {children}
      </FabricAuthProvider>
    </BrandingProvider>
  );
}

function TenantSettingsLoading() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4 text-foreground">
      <div className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('shell.loadingTenantSettings')}</div>
    </div>
  );
}

function TenantSettingsError({ message }: { message: string }) {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4 text-foreground">
      <div className="max-w-md rounded-structural border border-border bg-content p-6">
        <p className="text-[14px] font-semibold uppercase text-error">{t('common.configurationError')}</p>
        <h1 className="mt-3 text-[24px] font-semibold tracking-tight">{t('common.fabricCannotStart')}</h1>
        <p className="mt-3 text-[14px] text-muted-foreground">{message}</p>
      </div>
    </div>
  );
}
