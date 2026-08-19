import { useQuery } from '@tanstack/react-query';
import { Link, useLocation, useNavigate } from '@tanstack/react-router';
import { ChevronRight } from 'lucide-react';
import { useState } from 'react';

import { listKeycloakGroups, listKeycloakRoles, listKeycloakUsers } from '@/features/administration/keycloak-user-management';
import { fetchKeycloakIntegration, keycloakIntegrationQueryKey } from '@/features/integrations/tenant-integrations';
import { useCurrentActor } from '@/shared/actors/current-actor';
import { Badge } from '@/shared/components/ui/badge';
import { buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Input } from '@/shared/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

type UserManagementTab = 'users' | 'roles' | 'groups';

export default function UserManagementPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const actorQuery = useCurrentActor();
  const activeTab = getActiveTab(location.searchStr);
  const [userSearch, setUserSearch] = useState('');
  const [roleSearch, setRoleSearch] = useState('');
  const [groupSearch, setGroupSearch] = useState('');

  const settingsQuery = useQuery({ queryKey: keycloakIntegrationQueryKey, queryFn: fetchKeycloakIntegration });
  const enabled = settingsQuery.data?.adminApi.isEnabled ?? false;

  const usersQuery = useQuery({ queryKey: ['administration', 'keycloak', 'users', userSearch], queryFn: async () => await listKeycloakUsers(userSearch), enabled: enabled && activeTab === 'users', retry: false });
  const rolesQuery = useQuery({ queryKey: ['administration', 'keycloak', 'roles', roleSearch], queryFn: async () => await listKeycloakRoles(roleSearch), enabled: enabled && activeTab === 'roles', retry: false });
  const groupsQuery = useQuery({ queryKey: ['administration', 'keycloak', 'groups', groupSearch], queryFn: async () => await listKeycloakGroups(groupSearch), enabled: enabled && activeTab === 'groups', retry: false });

  function changeTab(nextTab: string) {
    if (!isUserManagementTab(nextTab)) return;
    void navigate({ to: '/administration/user-management', search: { tab: nextTab } as never, replace: true });
  }

  return (
    <div className="grid gap-6">
      <div>
        <p className="text-[13px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Administration</p>
        <h1 className="mt-3 text-[30px] font-semibold tracking-tight text-foreground">User Management</h1>
        <p className="mt-3 max-w-3xl text-[14px] text-muted-foreground">Manage Keycloak users, roles, and groups for the current tenant.</p>
      </div>

      {settingsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading Keycloak integration status...</p> : null}
      {settingsQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load Keycloak integration status.</p> : null}

      {!settingsQuery.isLoading && !settingsQuery.isError && !enabled ? (
        <Empty className="min-h-0 bg-content">
          <EmptyHeader>
            <EmptyTitle>Keycloak user management is not activated for this tenant</EmptyTitle>
            <EmptyDescription>Enable the tenant Keycloak Admin API before managing users, roles, and groups here.</EmptyDescription>
          </EmptyHeader>
          {actorQuery.data?.roles.includes('integrator') ? <EmptyContent><Link to="/integrations/keycloak" className="inline-flex h-10 items-center rounded-interactive bg-primary px-4 text-[14px] font-semibold text-white transition hover:opacity-90">Open Keycloak integration settings</Link></EmptyContent> : null}
        </Empty>
      ) : null}

      {!settingsQuery.isLoading && !settingsQuery.isError && enabled ? (
        <Tabs value={activeTab} onValueChange={changeTab}>
          <TabsList className="h-auto w-fit max-w-full flex-wrap justify-start gap-7">
            <TabsTrigger value="users">Users</TabsTrigger>
            <TabsTrigger value="roles">Roles</TabsTrigger>
            <TabsTrigger value="groups">Groups</TabsTrigger>
          </TabsList>

          <section className="rounded-structural border border-border bg-content p-4 sm:p-6">
            <TabsContent value="users">
              <EntityTableSection
                title="Users"
                description="Review tenant users and open the detail page for editing and role assignment."
                searchLabel="Search users"
                searchValue={userSearch}
                onSearchChange={setUserSearch}
                searchPlaceholder="Search by name or email"
                isLoading={usersQuery.isLoading}
                isError={usersQuery.isError}
                errorMessage="Could not load Keycloak users."
                emptyTitle="No users found"
                emptyDescription="Try a different search or create a new user."
                action={<Link to="/administration/user-management/users/new" className={buttonVariants()}>Create user</Link>}
                table={<table className="w-full min-w-[64rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Username</th><th className="px-4 py-3 font-semibold">Email</th><th className="px-4 py-3 font-semibold">Status</th><th className="px-4 py-3 text-right font-semibold">Open</th></tr></thead><tbody className="divide-y divide-border">{(usersQuery.data ?? []).map((user) => <tr key={user.id} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => void navigate({ to: '/administration/user-management/users/$userId/edit', params: { userId: user.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/user-management/users/$userId/edit', params: { userId: user.id } }); } }}><td className="px-4 py-4 font-medium text-foreground">{`${user.firstName} ${user.lastName}`.trim() || user.email}</td><td className="px-4 py-4 text-muted-foreground">{user.username}</td><td className="px-4 py-4 text-muted-foreground">{user.email}</td><td className="px-4 py-4"><Badge variant={user.isActive ? 'success' : 'secondary'}>{user.isActive ? 'Active' : 'Inactive'}</Badge></td><td className="px-4 py-4 text-right text-muted-foreground"><ChevronRight className="ml-auto size-4" /></td></tr>)}</tbody></table>}
                mobileList={<div className="grid gap-3 md:hidden">{(usersQuery.data ?? []).map((user) => <article key={user.id} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => void navigate({ to: '/administration/user-management/users/$userId/edit', params: { userId: user.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/user-management/users/$userId/edit', params: { userId: user.id } }); } }}><div className="flex items-start justify-between gap-3"><h3 className="text-[15px] font-semibold text-foreground">{`${user.firstName} ${user.lastName}`.trim() || user.email}</h3><ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" /></div><dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground"><div><dt className="font-medium text-foreground">Username</dt><dd>{user.username}</dd></div><div><dt className="font-medium text-foreground">Email</dt><dd>{user.email}</dd></div><div><dt className="font-medium text-foreground">Status</dt><dd><Badge variant={user.isActive ? 'success' : 'secondary'}>{user.isActive ? 'Active' : 'Inactive'}</Badge></dd></div></dl></article>)}</div>}
                hasItems={(usersQuery.data?.length ?? 0) > 0}
              />
            </TabsContent>

            <TabsContent value="roles">
              <EntityTableSection
                title="Roles"
                description="Review tenant realm roles and open the detail page for editing."
                searchLabel="Search roles"
                searchValue={roleSearch}
                onSearchChange={setRoleSearch}
                searchPlaceholder="Search by role name"
                isLoading={rolesQuery.isLoading}
                isError={rolesQuery.isError}
                errorMessage="Could not load Keycloak roles."
                emptyTitle="No roles found"
                emptyDescription="Create a role for tenant user and group access."
                action={<Link to="/administration/user-management/roles/new" className={buttonVariants()}>Create role</Link>}
                table={<table className="w-full min-w-[56rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Description</th><th className="px-4 py-3 text-right font-semibold">Open</th></tr></thead><tbody className="divide-y divide-border">{(rolesQuery.data ?? []).map((role) => <tr key={role.id} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => void navigate({ to: '/administration/user-management/roles/$roleId/edit', params: { roleId: role.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/user-management/roles/$roleId/edit', params: { roleId: role.id } }); } }}><td className="px-4 py-4 font-medium text-foreground">{role.name}</td><td className="px-4 py-4 text-muted-foreground">{role.description || '-'}</td><td className="px-4 py-4 text-right text-muted-foreground"><ChevronRight className="ml-auto size-4" /></td></tr>)}</tbody></table>}
                mobileList={<div className="grid gap-3 md:hidden">{(rolesQuery.data ?? []).map((role) => <article key={role.id} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => void navigate({ to: '/administration/user-management/roles/$roleId/edit', params: { roleId: role.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/user-management/roles/$roleId/edit', params: { roleId: role.id } }); } }}><div className="flex items-start justify-between gap-3"><h3 className="text-[15px] font-semibold text-foreground">{role.name}</h3><ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" /></div><p className="mt-3 text-[14px] text-muted-foreground">{role.description || 'No description'}</p></article>)}</div>}
                hasItems={(rolesQuery.data?.length ?? 0) > 0}
              />
            </TabsContent>

            <TabsContent value="groups">
              <EntityTableSection
                title="Groups"
                description="Review tenant groups and open the detail page for editing and role assignment."
                searchLabel="Search groups"
                searchValue={groupSearch}
                onSearchChange={setGroupSearch}
                searchPlaceholder="Search by group name"
                isLoading={groupsQuery.isLoading}
                isError={groupsQuery.isError}
                errorMessage="Could not load Keycloak groups."
                emptyTitle="No groups found"
                emptyDescription="Create a group for shared tenant access management."
                action={<Link to="/administration/user-management/groups/new" className={buttonVariants()}>Create group</Link>}
                table={<table className="w-full min-w-[56rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Path</th><th className="px-4 py-3 text-right font-semibold">Open</th></tr></thead><tbody className="divide-y divide-border">{(groupsQuery.data ?? []).map((group) => <tr key={group.id} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => void navigate({ to: '/administration/user-management/groups/$groupId/edit', params: { groupId: group.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/user-management/groups/$groupId/edit', params: { groupId: group.id } }); } }}><td className="px-4 py-4 font-medium text-foreground">{group.name}</td><td className="px-4 py-4 text-muted-foreground">{group.path}</td><td className="px-4 py-4 text-right text-muted-foreground"><ChevronRight className="ml-auto size-4" /></td></tr>)}</tbody></table>}
                mobileList={<div className="grid gap-3 md:hidden">{(groupsQuery.data ?? []).map((group) => <article key={group.id} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => void navigate({ to: '/administration/user-management/groups/$groupId/edit', params: { groupId: group.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/user-management/groups/$groupId/edit', params: { groupId: group.id } }); } }}><div className="flex items-start justify-between gap-3"><h3 className="text-[15px] font-semibold text-foreground">{group.name}</h3><ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" /></div><p className="mt-3 text-[14px] text-muted-foreground">{group.path}</p></article>)}</div>}
                hasItems={(groupsQuery.data?.length ?? 0) > 0}
              />
            </TabsContent>
          </section>
        </Tabs>
      ) : null}
    </div>
  );
}

function EntityTableSection({ title, description, searchLabel, searchValue, onSearchChange, searchPlaceholder, isLoading, isError, errorMessage, emptyTitle, emptyDescription, action, table, mobileList, hasItems }: { readonly title: string; readonly description: string; readonly searchLabel: string; readonly searchValue: string; readonly onSearchChange: (value: string) => void; readonly searchPlaceholder: string; readonly isLoading: boolean; readonly isError: boolean; readonly errorMessage: string; readonly emptyTitle: string; readonly emptyDescription: string; readonly action: React.ReactNode; readonly table: React.ReactNode; readonly mobileList: React.ReactNode; readonly hasItems: boolean; }) {
  return (
    <div className="grid gap-4 pt-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">{title}</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{description}</p>
        </div>
        <div>{action}</div>
      </div>

      <div className="grid gap-3 rounded-structural border border-border p-4 md:grid-cols-2">
        <label className="grid gap-2 text-[14px] font-medium md:max-w-md">
          <span>{searchLabel}</span>
          <Input value={searchValue} onChange={(event) => onSearchChange(event.target.value)} placeholder={searchPlaceholder} />
        </label>
      </div>

      {isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">{errorMessage}</p> : null}
      {!isLoading && !isError && !hasItems ? <Empty><EmptyHeader><EmptyTitle>{emptyTitle}</EmptyTitle><EmptyDescription>{emptyDescription}</EmptyDescription></EmptyHeader></Empty> : null}
      {(isLoading || hasItems) && !isError ? <div className="grid gap-4"><div className="md:hidden">{isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading...</p> : mobileList}</div><div className="hidden overflow-x-auto rounded-structural border border-border md:block">{isLoading ? <p className="px-4 py-5 text-[14px] text-muted-foreground">Loading...</p> : table}</div></div> : null}
    </div>
  );
}

function getActiveTab(searchStr: string): UserManagementTab {
  const tab = new URLSearchParams(searchStr).get('tab');
  return isUserManagementTab(tab) ? tab : 'users';
}

function isUserManagementTab(value: string | null | undefined): value is UserManagementTab {
  return value === 'users' || value === 'roles' || value === 'groups';
}
