import { apiBaseUrl, getAccessToken } from '@/shared/api/client';

export type PlatformTenantListItem = {
  id: string;
  displayName: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  oidc: PlatformTenantOidcSettings;
  host: PlatformTenantHostSettings;
};

export type PlatformTenant = PlatformTenantListItem & {
  theme: PlatformTenantThemeSettings;
  logo: PlatformTenantLogoSettings | null;
  keycloak: PlatformTenantIntegrationSummary;
  microsoftGraph: PlatformTenantIntegrationSummary;
};

export type PlatformTenantOidcSettings = {
  metadataUrl: string;
  clientId: string;
  requireHttpsMetadata: boolean;
};

export type PlatformTenantHostSettings = {
  assignmentMode: string;
};

export type PlatformTenantThemeSettings = {
  backgroundColor: string;
  contentColor: string;
  primaryColor: string;
  textColor: string;
  textMutedColor: string;
  borderColor: string;
  hoverBlueColor: string;
  activeBlueColor: string;
  hoverGrayColor: string;
  errorColor: string;
  errorBackgroundColor: string;
  dangerColor: string;
  successColor: string;
  successBackgroundColor: string;
};

export type PlatformTenantLogoSettings = {
  contentType: string;
  data: string;
};

export type PlatformTenantIntegrationSummary = {
  isConfigured: boolean;
  isEnabled: boolean;
  hasSecret: boolean;
  updatedAtUtc: string | null;
};

export type PlatformTenantUpsertValues = {
  displayName: string;
  oidc: PlatformTenantOidcSettings;
};

export const platformTenantsQueryKey = ['platform', 'tenants'] as const;

export async function fetchPlatformTenants(): Promise<PlatformTenantListItem[]> {
  return requestJson<PlatformTenantListItem[]>('/api/platform/tenants');
}

export async function fetchPlatformTenant(tenantId: string): Promise<PlatformTenant> {
  return requestJson<PlatformTenant>(`/api/platform/tenants/${encodeURIComponent(tenantId)}`);
}

export async function createPlatformTenant(values: PlatformTenantUpsertValues & { id: string }): Promise<PlatformTenant> {
  return requestJson<PlatformTenant>('/api/platform/tenants', {
    method: 'POST',
    body: JSON.stringify(values),
  });
}

export async function updatePlatformTenant(tenantId: string, values: PlatformTenantUpsertValues): Promise<PlatformTenant> {
  return requestJson<PlatformTenant>(`/api/platform/tenants/${encodeURIComponent(tenantId)}`, {
    method: 'PUT',
    body: JSON.stringify(values),
  });
}

export async function deactivatePlatformTenant(tenantId: string): Promise<PlatformTenant> {
  return requestJson<PlatformTenant>(`/api/platform/tenants/${encodeURIComponent(tenantId)}/deactivate`, {
    method: 'POST',
  });
}

export async function activatePlatformTenant(tenantId: string): Promise<PlatformTenant> {
  return requestJson<PlatformTenant>(`/api/platform/tenants/${encodeURIComponent(tenantId)}/activate`, {
    method: 'POST',
  });
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getAccessToken();
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    const detail = await readProblemDetail(response);
    throw new Error(detail);
  }

  return await response.json() as T;
}

async function readProblemDetail(response: Response) {
  try {
    const data = await response.json() as { detail?: string; title?: string };
    return data.detail ?? data.title ?? 'Platform request failed.';
  } catch {
    return 'Platform request failed.';
  }
}
