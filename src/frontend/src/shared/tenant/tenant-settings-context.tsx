import { createContext, useContext, type ReactNode } from 'react';

import type { TenantSettings } from './tenant-settings';

const TenantSettingsContext = createContext<TenantSettings | null>(null);

export function TenantSettingsProvider({ settings, children }: { settings: TenantSettings; children: ReactNode }) {
  return <TenantSettingsContext.Provider value={settings}>{children}</TenantSettingsContext.Provider>;
}

export function useTenantSettings() {
  const context = useContext(TenantSettingsContext);

  if (!context) {
    throw new Error('Tenant settings context is unavailable.');
  }

  return context;
}
