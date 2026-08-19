import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft, Trash2 } from 'lucide-react';
import * as React from 'react';
import { toast } from 'sonner';

import {
  assignKeycloakUserRoles,
  deleteKeycloakUser,
  getKeycloakUser,
  joinKeycloakUserGroup,
  leaveKeycloakUserGroup,
  listKeycloakGroups,
  listKeycloakRoles,
  listKeycloakUserGroups,
  listKeycloakUserRoles,
  removeKeycloakUserRoles,
  resetKeycloakUserPassword,
  updateKeycloakUser,
  type KeycloakGroupMembership,
  type KeycloakRole,
} from '@/features/administration/keycloak-user-management';
import { KeycloakUserForm, type KeycloakUserFormValues } from '@/features/administration/keycloak-user-form';
import { fetchKeycloakIntegration, keycloakIntegrationQueryKey } from '@/features/integrations/tenant-integrations';
import { useCurrentActor } from '@/shared/actors/current-actor';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Combobox, ComboboxContent, ComboboxEmpty, ComboboxInput, ComboboxItem, ComboboxList } from '@/shared/components/ui/combobox';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Input } from '@/shared/components/ui/input';

export default function KeycloakUserEditPage() {
  const { userId } = useParams({ from: '/main/administration/user-management/users/$userId/edit' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const actorQuery = useCurrentActor();
  const settingsQuery = useQuery({ queryKey: keycloakIntegrationQueryKey, queryFn: fetchKeycloakIntegration });
  const enabled = settingsQuery.data?.adminApi.isEnabled ?? false;

  const userQuery = useQuery({ queryKey: ['administration', 'keycloak', 'users', userId], queryFn: async () => await getKeycloakUser(userId), enabled, retry: false });
  const rolesQuery = useQuery({ queryKey: ['administration', 'keycloak', 'roles', 'assignment'], queryFn: async () => await listKeycloakRoles(), enabled, retry: false });
  const userRolesQuery = useQuery({ queryKey: ['administration', 'keycloak', 'user-roles', userId], queryFn: async () => await listKeycloakUserRoles(userId), enabled, retry: false });
  const groupsQuery = useQuery({ queryKey: ['administration', 'keycloak', 'groups', 'membership'], queryFn: async () => await listKeycloakGroups(), enabled, retry: false });
  const userGroupsQuery = useQuery({ queryKey: ['administration', 'keycloak', 'user-groups', userId], queryFn: async () => await listKeycloakUserGroups(userId), enabled, retry: false });

  const [values, setValues] = React.useState<KeycloakUserFormValues | null>(null);
  const [selectedRoleIds, setSelectedRoleIds] = React.useState<string[]>([]);
  const [password, setPassword] = React.useState('');
  const [confirmPassword, setConfirmPassword] = React.useState('');
  const [temporaryPassword, setTemporaryPassword] = React.useState(true);
  const [groupToJoin, setGroupToJoin] = React.useState<{ id: string; name: string; path: string } | null>(null);
  const joinGroupAnchorRef = React.useRef<HTMLDivElement | null>(null);

  React.useEffect(() => {
    if (userQuery.data) {
      setValues({ username: userQuery.data.username, firstName: userQuery.data.firstName, lastName: userQuery.data.lastName, email: userQuery.data.email, isActive: userQuery.data.isActive });
    }
  }, [userQuery.data]);

  React.useEffect(() => {
    setSelectedRoleIds((userRolesQuery.data ?? []).map((item) => item.id));
  }, [userRolesQuery.data]);

  const saveUser = useMutation({
    mutationFn: async () => await updateKeycloakUser(userId, values!),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'users'] }),
        queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'users', userId] }),
      ]);
      toast.success('User saved.');
    },
    onError: (error: Error) => toast.error(error.message),
  });

  const saveRoles = useMutation({
    mutationFn: async () => {
      const current = new Set((userRolesQuery.data ?? []).map((item) => item.id));
      const next = new Set(selectedRoleIds);
      const toAdd = [...next].filter((id) => !current.has(id));
      const toRemove = [...current].filter((id) => !next.has(id));
      if (toAdd.length > 0) await assignKeycloakUserRoles(userId, { roleIds: toAdd });
      if (toRemove.length > 0) await removeKeycloakUserRoles(userId, { roleIds: toRemove });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'user-roles', userId] });
      toast.success('User roles updated.');
    },
    onError: (error: Error) => toast.error(error.message),
  });

  const joinGroup = useMutation({
    mutationFn: async () => {
      if (!groupToJoin) throw new Error('Select a group to join.');
      await joinKeycloakUserGroup(userId, groupToJoin.id);
    },
    onSuccess: async () => {
      setGroupToJoin(null);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'user-groups', userId] }),
        queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'groups', 'membership'] }),
      ]);
      toast.success('User joined group.');
    },
    onError: (error: Error) => toast.error(error.message),
  });

  const leaveGroup = useMutation({
    mutationFn: async (groupId: string) => await leaveKeycloakUserGroup(userId, groupId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'user-groups', userId] }),
        queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'groups', 'membership'] }),
      ]);
      toast.success('User left group.');
    },
    onError: (error: Error) => toast.error(error.message),
  });

  const removeUser = useMutation({
    mutationFn: async () => await deleteKeycloakUser(userId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'users'] });
      toast.success('User deleted.');
      await navigate({ to: '/administration/user-management', search: { tab: 'users' } as never, replace: true });
    },
    onError: (error: Error) => toast.error(error.message),
  });

  const resetPassword = useMutation({
    mutationFn: async () => {
      if (!password.trim()) throw new Error('Password is required.');
      if (password !== confirmPassword) throw new Error('Passwords do not match.');
      await resetKeycloakUserPassword(userId, { password, temporary: temporaryPassword });
    },
    onSuccess: () => {
      setPassword('');
      setConfirmPassword('');
      toast.success(temporaryPassword ? 'Temporary password set.' : 'Password reset.');
    },
    onError: (error: Error) => toast.error(error.message),
  });

  const currentGroups = userGroupsQuery.data ?? [];
  const currentGroupIds = new Set(currentGroups.map((group) => group.id));
  const availableGroups = (groupsQuery.data ?? []).filter((group) => !currentGroupIds.has(group.id));

  return (
    <EntityLayout backTo="/administration/user-management" title="Edit user" description="Update user profile details and maintain tenant access." settingsEnabled={enabled} settingsLoading={settingsQuery.isLoading} settingsError={settingsQuery.isError} showSettingsLink={actorQuery.data?.roles.includes('integrator') ?? false}>
      {userQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load user.</p> : null}
      {userQuery.isLoading || !values ? <p className="text-[14px] text-muted-foreground">Loading user...</p> : null}
      {values ? (
        <div className="grid gap-6">
          <Card className="p-6">
            <div className="mb-4 flex items-start justify-between gap-3">
              <div>
                <h3 className="text-[18px] font-semibold tracking-tight">User details</h3>
                <p className="mt-1 text-[13px] text-muted-foreground">Update the user profile and active state.</p>
              </div>
              <Button variant="outline" onClick={() => removeUser.mutate()} disabled={removeUser.isPending}><Trash2 className="size-4" />{removeUser.isPending ? 'Deleting...' : 'Delete user'}</Button>
            </div>
            <KeycloakUserForm values={values} onChange={setValues} isSubmitting={saveUser.isPending} submitLabel="Save user" onSubmit={() => saveUser.mutate()} />
          </Card>

          <Card className="p-6">
            <div className="mb-4">
              <h3 className="text-[18px] font-semibold tracking-tight">Groups</h3>
              <p className="mt-1 text-[13px] text-muted-foreground">View and manage the groups this user belongs to.</p>
            </div>
            {groupsQuery.isLoading || userGroupsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading groups...</p> : null}
            {groupsQuery.isError || userGroupsQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load user groups.</p> : null}
            {!groupsQuery.isLoading && !userGroupsQuery.isLoading && !groupsQuery.isError && !userGroupsQuery.isError ? (
              <div className="grid gap-4">
                <GroupMembershipList groups={currentGroups} leaveLabel="Leave" onLeave={(groupId) => leaveGroup.mutate(groupId)} isLeaving={leaveGroup.isPending} />
                <div className="grid gap-3 rounded-structural border border-border bg-content p-4">
                  <p className="text-[14px] font-medium text-foreground">Join group</p>
                  <div ref={joinGroupAnchorRef}>
                    <Combobox value={groupToJoin} onValueChange={setGroupToJoin} items={availableGroups} itemToStringLabel={(group) => group ? `${group.name} (${group.path})` : ''}>
                      <ComboboxInput placeholder={availableGroups.length === 0 ? 'No available groups' : 'Search groups...'} showClear disabled={availableGroups.length === 0} />
                      <ComboboxContent anchor={joinGroupAnchorRef.current}>
                        <ComboboxEmpty>No groups found.</ComboboxEmpty>
                        <ComboboxList>
                          {(group) => <ComboboxItem key={group.id} value={group}><div className="min-w-0"><p className="truncate font-medium text-foreground">{group.name}</p><p className="truncate text-[12px] text-muted-foreground">{group.path}</p></div></ComboboxItem>}
                        </ComboboxList>
                      </ComboboxContent>
                    </Combobox>
                  </div>
                  <div className="flex justify-end">
                    <Button onClick={() => joinGroup.mutate()} disabled={joinGroup.isPending || !groupToJoin}>{joinGroup.isPending ? 'Joining...' : 'Join group'}</Button>
                  </div>
                </div>
              </div>
            ) : null}
          </Card>

          <Card className="p-6">
            <div className="mb-4">
              <h3 className="text-[18px] font-semibold tracking-tight">Realm roles</h3>
              <p className="mt-1 text-[13px] text-muted-foreground">Assign tenant roles to this user.</p>
            </div>
            {rolesQuery.isLoading || userRolesQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading roles...</p> : null}
            {rolesQuery.isError || userRolesQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load user roles.</p> : null}
            {!rolesQuery.isLoading && !userRolesQuery.isLoading && !rolesQuery.isError && !userRolesQuery.isError ? <RoleChecklist roles={rolesQuery.data ?? []} selectedRoleIds={selectedRoleIds} onChange={setSelectedRoleIds} onSave={() => saveRoles.mutate()} isSaving={saveRoles.isPending} /> : null}
          </Card>

          <Card className="p-6">
            <div className="mb-4">
              <h3 className="text-[18px] font-semibold tracking-tight">Reset password</h3>
              <p className="mt-1 text-[13px] text-muted-foreground">Set a new password for this user and decide whether Keycloak should require a change on next sign-in.</p>
            </div>
            <form className="grid gap-4" onSubmit={(event) => { event.preventDefault(); resetPassword.mutate(); }}>
              <Field label="New password"><Input type="password" value={password} onChange={(event) => setPassword(event.target.value)} /></Field>
              <Field label="Confirm new password"><Input type="password" value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} /></Field>
              <label className="flex items-center gap-3 rounded-interactive border border-border px-3 py-2 text-[14px] font-medium text-foreground"><input type="checkbox" checked={temporaryPassword} onChange={(event) => setTemporaryPassword(event.target.checked)} />Temporary password</label>
              <p className="text-[13px] text-muted-foreground">Temporary passwords force the user to change it on next sign-in.</p>
              <div className="flex justify-end border-t border-border pt-4"><Button type="submit" disabled={resetPassword.isPending}>{resetPassword.isPending ? 'Resetting...' : 'Reset password'}</Button></div>
            </form>
          </Card>
        </div>
      ) : null}
    </EntityLayout>
  );
}

function GroupMembershipList({ groups, leaveLabel, onLeave, isLeaving }: { readonly groups: readonly KeycloakGroupMembership[]; readonly leaveLabel: string; readonly onLeave: (groupId: string) => void; readonly isLeaving: boolean; }) {
  if (groups.length === 0) return <p className="text-[14px] text-muted-foreground">This user is not currently in any groups.</p>;
  return <div className="grid gap-3">{groups.map((group) => <div key={group.id} className="flex flex-wrap items-start justify-between gap-3 rounded-structural border border-border bg-content px-4 py-3"><div><p className="text-[15px] font-semibold text-foreground">{group.name}</p><p className="mt-1 text-[13px] text-muted-foreground">{group.path}</p></div><Button variant="outline" onClick={() => onLeave(group.id)} disabled={isLeaving}>{leaveLabel}</Button></div>)}</div>;
}

function Field({ label, children }: { readonly label: string; readonly children: React.ReactNode; }) {
  return <label className="grid gap-2 text-[14px] font-medium text-foreground"><span>{label}</span>{children}</label>;
}

function EntityLayout({ backTo, title, description, settingsEnabled, settingsLoading, settingsError, showSettingsLink, children }: { readonly backTo: string; readonly title: string; readonly description: string; readonly settingsEnabled: boolean; readonly settingsLoading: boolean; readonly settingsError: boolean; readonly showSettingsLink: boolean; readonly children: React.ReactNode; }) {
  return <div className="grid gap-6"><header className="flex items-start gap-4"><Link to={backTo} className="inline-flex size-10 items-center justify-center rounded-interactive border border-border bg-content text-foreground transition hover:bg-hover-blue" aria-label="Go back"><ArrowLeft className="size-4" aria-hidden="true" /></Link><div><h2 className="text-[20px] font-semibold tracking-tight">{title}</h2><p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{description}</p></div></header>{settingsLoading ? <p className="text-[14px] text-muted-foreground">Loading Keycloak integration status...</p> : null}{settingsError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load Keycloak integration status.</p> : null}{!settingsLoading && !settingsError && !settingsEnabled ? <Empty className="min-h-0 bg-content"><EmptyHeader><EmptyTitle>Keycloak user management is not activated for this tenant</EmptyTitle><EmptyDescription>Enable the tenant Keycloak Admin API before using this page.</EmptyDescription></EmptyHeader>{showSettingsLink ? <EmptyContent><Link to="/integrations/keycloak" className="inline-flex h-10 items-center rounded-interactive bg-primary px-4 text-[14px] font-semibold text-white transition hover:opacity-90">Open Keycloak integration settings</Link></EmptyContent> : null}</Empty> : null}{!settingsLoading && !settingsError && settingsEnabled ? children : null}</div>;
}

function RoleChecklist({ roles, selectedRoleIds, onChange, onSave, isSaving }: { readonly roles: readonly KeycloakRole[]; readonly selectedRoleIds: readonly string[]; readonly onChange: (roleIds: string[]) => void; readonly onSave: () => void; readonly isSaving: boolean; }) {
  if (roles.length === 0) return <p className="text-[14px] text-muted-foreground">No roles available yet.</p>;
  return <div className="grid gap-4"><div className="grid gap-2 rounded-structural border border-border bg-content p-3">{roles.map((role) => <label key={role.id} className="flex items-start gap-3 rounded-interactive px-2 py-2 text-[14px] hover:bg-hover-blue"><input type="checkbox" checked={selectedRoleIds.includes(role.id)} onChange={(event) => onChange(event.target.checked ? [...selectedRoleIds, role.id] : selectedRoleIds.filter((item) => item !== role.id))} /><span><span className="block font-medium text-foreground">{role.name}</span><span className="block text-[13px] text-muted-foreground">{role.description || 'No description'}</span></span></label>)}</div><div className="flex justify-end"><Button onClick={onSave} disabled={isSaving}>{isSaving ? 'Saving...' : 'Save role assignments'}</Button></div></div>;
}
