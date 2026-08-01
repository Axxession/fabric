import { fabricThemeSchema, type FabricTheme } from '@/shared/theme/fabric-theme';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';

type TenantSettingsResponse = components['schemas']['TenantSettingsResponse'];
type AdminTenantSettingsResponse = components['schemas']['AdminTenantSettingsResponse'];
type OidcSettingsResponse = components['schemas']['OidcSettingsResponse'];
type LogoSettingsResponse = components['schemas']['LogoSettingsResponse'];
type GraphEmailSettingsResponse = components['schemas']['GraphEmailSettingsResponse'];

export type UpdateTenantSettingsRequest = components['schemas']['UpdateTenantSettingsRequest'];

export type TenantSettings = {
  version: string;
  oidc: TenantOidcSettings;
  theme: FabricTheme;
  logo: TenantLogoSettings | null;
};

export type AdminTenantSettings = TenantSettings & {
  email: TenantEmailSettings | null;
};

export type TenantOidcSettings = {
  metadataUrl: string;
  clientId: string;
  requireHttpsMetadata: boolean;
};

export type TenantLogoSettings = Required<LogoSettingsResponse>;

export type TenantEmailSettings = Required<GraphEmailSettingsResponse>;

export const tenantSettingsQueryKey = ['settings', 'tenant'] as const;

export async function fetchTenantSettings(): Promise<TenantSettings> {
  const { data, error } = await api.GET('/api/tenants/settings');

  if (error || !data) {
    throw new Error('Tenant settings request failed.');
  }

  return parseTenantSettings(data);
}

export async function fetchAdminTenantSettings(): Promise<AdminTenantSettings> {
  const { data, error } = await api.GET('/api/tenants/admin/settings');

  if (error || !data) {
    throw new Error('Tenant settings request failed.');
  }

  return parseAdminTenantSettings(data);
}

export async function updateAdminTenantSettings(values: UpdateTenantSettingsRequest): Promise<AdminTenantSettings> {
  const { data, error } = await api.PUT('/api/tenants/admin/settings', {
    body: values,
  });

  if (error || !data) {
    throw new Error('Tenant settings update failed.');
  }

  return parseAdminTenantSettings(data);
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

function parseAdminTenantSettings(value: AdminTenantSettingsResponse): AdminTenantSettings {
  const settings = parseTenantSettings(value);
  const email = parseEmailSettings(value.email);

  return { ...settings, email };
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

function parseEmailSettings(value: GraphEmailSettingsResponse | null | undefined): TenantEmailSettings | null {
  if (value === null || value === undefined) {
    return null;
  }

  if (
    typeof value.fromEmail !== 'string'
    || typeof value.fromName !== 'string'
    || typeof value.azureTenantId !== 'string'
    || typeof value.applicationId !== 'string'
    || typeof value.saveSentItems !== 'boolean'
    || typeof value.hasSecret !== 'boolean'
  ) {
    throw new Error('Tenant email settings response is invalid.');
  }

  return {
    fromEmail: value.fromEmail,
    fromName: value.fromName,
    azureTenantId: value.azureTenantId,
    applicationId: value.applicationId,
    saveSentItems: value.saveSentItems,
    hasSecret: value.hasSecret,
  };
}
