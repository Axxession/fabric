import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import * as React from 'react';

import { deleteKeycloakRole, getKeycloakRole, updateKeycloakRole } from '@/features/administration/keycloak-user-management';
import { KeycloakRoleForm, type KeycloakRoleFormValues } from '@/features/administration/keycloak-role-form';
import { fetchKeycloakIntegration, keycloakIntegrationQueryKey } from '@/features/integrations/tenant-integrations';
import { useCurrentActor } from '@/shared/actors/current-actor';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';

export default function KeycloakRoleEditPage() {
  const { roleId } = useParams({ from: '/main/administration/user-management/roles/$roleId/edit' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const actorQuery = useCurrentActor();
  const settingsQuery = useQuery({ queryKey: keycloakIntegrationQueryKey, queryFn: fetchKeycloakIntegration });
  const roleQuery = useQuery({ queryKey: ['administration', 'keycloak', 'roles', roleId], queryFn: async () => await getKeycloakRole(roleId), enabled: settingsQuery.data?.adminApi.isEnabled ?? false, retry: false });
  const [values, setValues] = React.useState<KeycloakRoleFormValues | null>(null);
  React.useEffect(() => { if (roleQuery.data) setValues({ name: roleQuery.data.name, description: roleQuery.data.description }); }, [roleQuery.data]);
  const saveRole = useMutation({ mutationFn: async () => await updateKeycloakRole(roleId, values!), onSuccess: async () => { await Promise.all([queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'roles'] }), queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'roles', roleId] })]); toast.success('Role saved.'); }, onError: (error: Error) => toast.error(error.message) });
  const removeRole = useMutation({ mutationFn: async () => await deleteKeycloakRole(roleId), onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'roles'] }); toast.success('Role deleted.'); await navigate({ to: '/administration/user-management', search: { tab: 'roles' } as never, replace: true }); }, onError: (error: Error) => toast.error(error.message) });
  return <SimpleEntityLayout backTo="/administration/user-management" title="Edit role" description="Update the realm role name and description." settingsEnabled={settingsQuery.data?.adminApi.isEnabled ?? false} settingsLoading={settingsQuery.isLoading} settingsError={settingsQuery.isError} showSettingsLink={actorQuery.data?.roles.includes('integrator') ?? false}>{roleQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load role.</p> : null}{roleQuery.isLoading || !values ? <p className="text-[14px] text-muted-foreground">Loading role...</p> : null}{values ? <Card className="p-6"><div className="mb-4 flex items-start justify-between gap-3"><div><h3 className="text-[18px] font-semibold tracking-tight">Role details</h3><p className="mt-1 text-[13px] text-muted-foreground">Update role details or delete the role.</p></div><Button variant="outline" onClick={() => removeRole.mutate()} disabled={removeRole.isPending}><Trash2 className="size-4" />{removeRole.isPending ? 'Deleting...' : 'Delete role'}</Button></div><KeycloakRoleForm values={values} onChange={setValues} isSubmitting={saveRole.isPending} submitLabel="Save role" onSubmit={() => saveRole.mutate()} /></Card> : null}</SimpleEntityLayout>;
}

function SimpleEntityLayout({ backTo, title, description, settingsEnabled, settingsLoading, settingsError, showSettingsLink, children }: { readonly backTo: string; readonly title: string; readonly description: string; readonly settingsEnabled: boolean; readonly settingsLoading: boolean; readonly settingsError: boolean; readonly showSettingsLink: boolean; readonly children: React.ReactNode; }) { return <div className="grid gap-6"><header className="flex items-start gap-4"><Link to={backTo} className="inline-flex size-10 items-center justify-center rounded-interactive border border-border bg-content text-foreground transition hover:bg-hover-blue" aria-label="Go back"><ArrowLeft className="size-4" aria-hidden="true" /></Link><div><h2 className="text-[20px] font-semibold tracking-tight">{title}</h2><p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{description}</p></div></header>{settingsLoading ? <p className="text-[14px] text-muted-foreground">Loading Keycloak integration status...</p> : null}{settingsError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load Keycloak integration status.</p> : null}{!settingsLoading && !settingsError && !settingsEnabled ? <Empty className="min-h-0 bg-content"><EmptyHeader><EmptyTitle>Keycloak user management is not activated for this tenant</EmptyTitle><EmptyDescription>Enable the tenant Keycloak Admin API before using this page.</EmptyDescription></EmptyHeader>{showSettingsLink ? <EmptyContent><Link to="/integrations/keycloak" className="inline-flex h-10 items-center rounded-interactive bg-primary px-4 text-[14px] font-semibold text-white transition hover:opacity-90">Open Keycloak integration settings</Link></EmptyContent> : null}</Empty> : null}{!settingsLoading && !settingsError && settingsEnabled ? children : null}</div>; }
