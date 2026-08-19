import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';

export type MicrosoftGraphIntegrationResponse = components['schemas']['MicrosoftGraphIntegrationResponse'];
export type UpdateMicrosoftGraphIntegrationRequest = components['schemas']['UpdateMicrosoftGraphIntegrationRequest'];
export type KeycloakIntegrationResponse = components['schemas']['KeycloakIntegrationResponse'];
export type UpdateKeycloakIntegrationRequest = components['schemas']['UpdateKeycloakIntegrationRequest'];

export const microsoftGraphIntegrationQueryKey = ['tenant-integrations', 'microsoft-graph'] as const;
export const keycloakIntegrationQueryKey = ['tenant-integrations', 'keycloak'] as const;

export async function fetchMicrosoftGraphIntegration(): Promise<MicrosoftGraphIntegrationResponse> {
  const { data, error } = await api.GET('/api/tenant-integrations/microsoft-graph');

  if (error || !data) {
    throw new Error('Microsoft Graph integration request failed.');
  }

  return data;
}

export async function updateMicrosoftGraphIntegration(values: UpdateMicrosoftGraphIntegrationRequest): Promise<MicrosoftGraphIntegrationResponse> {
  const { data, error } = await api.PUT('/api/tenant-integrations/microsoft-graph', {
    body: values,
  });

  if (error || !data) {
    throw new Error('Microsoft Graph integration update failed.');
  }

  return data;
}

export async function fetchKeycloakIntegration(): Promise<KeycloakIntegrationResponse> {
  const { data, error } = await api.GET('/api/tenant-integrations/keycloak');

  if (error || !data) {
    throw new Error('Keycloak integration request failed.');
  }

  return data;
}

export async function updateKeycloakIntegration(values: UpdateKeycloakIntegrationRequest): Promise<KeycloakIntegrationResponse> {
  const { data, error } = await api.PUT('/api/tenant-integrations/keycloak', {
    body: values,
  });

  if (error || !data) {
    throw new Error('Keycloak integration update failed.');
  }

  return data;
}
