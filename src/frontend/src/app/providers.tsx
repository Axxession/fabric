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
import { TenantSettingsProvider } from '@/shared/tenant/tenant-settings-context';
import { fetchTenantSettings, getLogoDataUrl, type TenantSettings } from '@/shared/tenant/tenant-settings';

export function AppProviders({ children }: { children: ReactNode }) {
  const [tenantSettings, setTenantSettings] = useState<TenantSettings | null>(null);
  const [tenantSettingsError, setTenantSettingsError] = useState<string | null>(null);
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

  useEffect(() => {
    const controller = new AbortController();

    async function loadTenantSettings() {
      try {
        const settings = await fetchTenantSettings();
        applyFabricTheme(settings.theme);
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
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={queryClient}>
        <TenantSettingsProvider settings={tenantSettings}>
          <BrandingProvider branding={branding}>
            <FabricAuthProvider tenantSettings={tenantSettings}>
              <AuthTokenBridge />
              {children}
            </FabricAuthProvider>
          </BrandingProvider>
        </TenantSettingsProvider>
        <Toaster />
      </QueryClientProvider>
    </I18nextProvider>
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
