import { apiBaseUrl, getAccessToken } from '@/shared/api/client';

export type KeycloakUser = {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
};

export type KeycloakRole = {
  id: string;
  name: string;
  description: string;
};

export type KeycloakGroup = {
  id: string;
  name: string;
  path: string;
};

export type KeycloakGroupMembership = KeycloakGroup;

export type KeycloakUserMembership = KeycloakUser;

export type KeycloakRoleAssignmentsRequest = {
  roleIds: string[];
};

export type CreateKeycloakUserRequest = Omit<KeycloakUser, 'id'>;
export type UpdateKeycloakUserRequest = Omit<KeycloakUser, 'id'>;
export type CreateKeycloakRoleRequest = Omit<KeycloakRole, 'id'>;
export type UpdateKeycloakRoleRequest = Omit<KeycloakRole, 'id'>;
export type CreateKeycloakGroupRequest = Pick<KeycloakGroup, 'name'>;
export type UpdateKeycloakGroupRequest = Pick<KeycloakGroup, 'name'>;
export type ResetKeycloakUserPasswordRequest = {
  password: string;
  temporary: boolean;
};

type ProblemDetails = {
  detail?: string;
  title?: string;
};

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getAccessToken();
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let detail = 'Request failed.';

    try {
      const problem = (await response.json()) as ProblemDetails;
      detail = problem.detail ?? problem.title ?? detail;
    } catch {
      detail = response.statusText || detail;
    }

    throw new Error(detail);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return await response.json() as T;
}

function buildListPath(basePath: string, search?: string) {
  const query = new URLSearchParams();
  query.set('page', '0');
  query.set('pageSize', '100');

  if (search?.trim()) {
    query.set('search', search.trim());
  }

  return `${basePath}?${query.toString()}`;
}

export async function listKeycloakUsers(search?: string) {
  return await request<KeycloakUser[]>(buildListPath('/api/integrations/keycloak/users', search));
}

export async function createKeycloakUser(body: CreateKeycloakUserRequest) {
  return await request<KeycloakUser>('/api/integrations/keycloak/users', { method: 'POST', body: JSON.stringify(body) });
}

export async function getKeycloakUser(id: string) {
  return await request<KeycloakUser>(`/api/integrations/keycloak/users/${id}`);
}

export async function updateKeycloakUser(id: string, body: UpdateKeycloakUserRequest) {
  return await request<KeycloakUser>(`/api/integrations/keycloak/users/${id}`, { method: 'PUT', body: JSON.stringify(body) });
}

export async function resetKeycloakUserPassword(id: string, body: ResetKeycloakUserPasswordRequest) {
  await request<void>(`/api/integrations/keycloak/users/${id}/reset-password`, { method: 'POST', body: JSON.stringify(body) });
}

export async function deleteKeycloakUser(id: string) {
  await request<void>(`/api/integrations/keycloak/users/${id}`, { method: 'DELETE' });
}

export async function listKeycloakUserGroups(id: string) {
  return await request<KeycloakGroupMembership[]>(`/api/integrations/keycloak/users/${id}/groups`);
}

export async function joinKeycloakUserGroup(id: string, groupId: string) {
  await request<void>(`/api/integrations/keycloak/users/${id}/groups/${groupId}`, { method: 'PUT' });
}

export async function leaveKeycloakUserGroup(id: string, groupId: string) {
  await request<void>(`/api/integrations/keycloak/users/${id}/groups/${groupId}`, { method: 'DELETE' });
}

export async function listKeycloakUserRoles(id: string) {
  return await request<KeycloakRole[]>(`/api/integrations/keycloak/users/${id}/roles`);
}

export async function assignKeycloakUserRoles(id: string, body: KeycloakRoleAssignmentsRequest) {
  return await request<KeycloakRole[]>(`/api/integrations/keycloak/users/${id}/roles`, { method: 'POST', body: JSON.stringify(body) });
}

export async function removeKeycloakUserRoles(id: string, body: KeycloakRoleAssignmentsRequest) {
  await request<void>(`/api/integrations/keycloak/users/${id}/roles`, { method: 'DELETE', body: JSON.stringify(body) });
}

export async function listKeycloakRoles(search?: string) {
  return await request<KeycloakRole[]>(buildListPath('/api/integrations/keycloak/roles', search));
}

export async function createKeycloakRole(body: CreateKeycloakRoleRequest) {
  return await request<KeycloakRole>('/api/integrations/keycloak/roles', { method: 'POST', body: JSON.stringify(body) });
}

export async function getKeycloakRole(id: string) {
  return await request<KeycloakRole>(`/api/integrations/keycloak/roles/${id}`);
}

export async function updateKeycloakRole(id: string, body: UpdateKeycloakRoleRequest) {
  return await request<KeycloakRole>(`/api/integrations/keycloak/roles/${id}`, { method: 'PUT', body: JSON.stringify(body) });
}

export async function deleteKeycloakRole(id: string) {
  await request<void>(`/api/integrations/keycloak/roles/${id}`, { method: 'DELETE' });
}

export async function listKeycloakGroups(search?: string) {
  return await request<KeycloakGroup[]>(buildListPath('/api/integrations/keycloak/groups', search));
}

export async function createKeycloakGroup(body: CreateKeycloakGroupRequest) {
  return await request<KeycloakGroup>('/api/integrations/keycloak/groups', { method: 'POST', body: JSON.stringify(body) });
}

export async function getKeycloakGroup(id: string) {
  return await request<KeycloakGroup>(`/api/integrations/keycloak/groups/${id}`);
}

export async function updateKeycloakGroup(id: string, body: UpdateKeycloakGroupRequest) {
  return await request<KeycloakGroup>(`/api/integrations/keycloak/groups/${id}`, { method: 'PUT', body: JSON.stringify(body) });
}

export async function deleteKeycloakGroup(id: string) {
  await request<void>(`/api/integrations/keycloak/groups/${id}`, { method: 'DELETE' });
}

export async function listKeycloakGroupMembers(id: string) {
  return await request<KeycloakUserMembership[]>(`/api/integrations/keycloak/groups/${id}/members`);
}

export async function listKeycloakGroupRoles(id: string) {
  return await request<KeycloakRole[]>(`/api/integrations/keycloak/groups/${id}/roles`);
}

export async function assignKeycloakGroupRoles(id: string, body: KeycloakRoleAssignmentsRequest) {
  return await request<KeycloakRole[]>(`/api/integrations/keycloak/groups/${id}/roles`, { method: 'POST', body: JSON.stringify(body) });
}

export async function removeKeycloakGroupRoles(id: string, body: KeycloakRoleAssignmentsRequest) {
  await request<void>(`/api/integrations/keycloak/groups/${id}/roles`, { method: 'DELETE', body: JSON.stringify(body) });
}
