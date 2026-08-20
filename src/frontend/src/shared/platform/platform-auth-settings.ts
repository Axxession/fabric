import { apiBaseUrl } from '@/shared/api/client';

export type PlatformAuthSettings = {
  oidc: {
    metadataUrl: string;
    clientId: string;
    requireHttpsMetadata: boolean;
  };
};

export async function fetchPlatformAuthSettings(): Promise<PlatformAuthSettings> {
  const response = await fetch(`${apiBaseUrl}/api/platform/auth/settings`, {
    credentials: 'same-origin',
  });

  if (!response.ok) {
    throw new Error('Platform authentication settings request failed.');
  }

  const data = await response.json() as {
    oidc?: {
      metadataUrl?: unknown;
      clientId?: unknown;
      requireHttpsMetadata?: unknown;
    };
  };

  if (
    typeof data.oidc?.metadataUrl !== 'string'
    || typeof data.oidc.clientId !== 'string'
    || typeof data.oidc.requireHttpsMetadata !== 'boolean'
  ) {
    throw new Error('Platform authentication settings response is invalid.');
  }

  return {
    oidc: {
      metadataUrl: data.oidc.metadataUrl,
      clientId: data.oidc.clientId,
      requireHttpsMetadata: data.oidc.requireHttpsMetadata,
    },
  };
}
