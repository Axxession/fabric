import { fabricThemeSchema, type FabricTheme } from '@/shared/theme/fabric-theme';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';

type TenantSettingsResponse = components['schemas']['TenantSettingsResponse'];
type OidcSettingsResponse = components['schemas']['OidcSettingsResponse'];
type LogoSettingsResponse = components['schemas']['LogoSettingsResponse'];

export type TenantSettings = {
  version: string;
  oidc: TenantOidcSettings;
  theme: FabricTheme;
  logo: TenantLogoSettings | null;
};

export type TenantOidcSettings = {
  metadataUrl: string;
  clientId: string;
  requireHttpsMetadata: boolean;
};

export type TenantLogoSettings = Required<LogoSettingsResponse>;

export const tenantSettingsQueryKey = ['settings', 'tenant'] as const;

export async function fetchTenantSettings(): Promise<TenantSettings> {
  const { data, error } = await api.GET('/api/tenants/settings');

  if (error || !data) {
    throw new Error('Tenant settings request failed.');
  }

  return parseTenantSettings(data);
}

export function getLogoDataUrl(logo: TenantLogoSettings | null): string | undefined {
  if (!logo) {
    return undefined;
  }

  return `data:${logo.contentType};base64,${logo.data}`;
}

function parseTenantSettings(value: TenantSettingsResponse): TenantSettings {
  if (typeof value.version !== 'string') {
    throw new Error('Tenant settings version response is invalid.');
  }

  const oidc = parseOidcSettings(value.oidc);
  const theme = fabricThemeSchema.parse(value.theme);
  const logo = parseLogoSettings(value.logo);

  return { version: value.version, oidc, theme, logo };
}
function parseOidcSettings(value: OidcSettingsResponse | undefined): TenantOidcSettings {
  if (!value || typeof value.metadataUrl !== 'string' || typeof value.clientId !== 'string' || typeof value.requireHttpsMetadata !== 'boolean') {
    throw new Error('Tenant OIDC settings response is invalid.');
  }

  return {
    metadataUrl: value.metadataUrl,
    clientId: value.clientId,
    requireHttpsMetadata: value.requireHttpsMetadata,
  };
}

function parseLogoSettings(value: LogoSettingsResponse | null | undefined): TenantLogoSettings | null {
  if (value === null || value === undefined) {
    return null;
  }

  if (typeof value.contentType !== 'string' || typeof value.data !== 'string') {
    throw new Error('Tenant logo settings response is invalid.');
  }

  return { contentType: value.contentType, data: value.data };
}
