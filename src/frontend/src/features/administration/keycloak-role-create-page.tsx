import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';
import * as React from 'react';

import { createKeycloakRole } from '@/features/administration/keycloak-user-management';
import { KeycloakRoleForm, type KeycloakRoleFormValues } from '@/features/administration/keycloak-role-form';
import { fetchKeycloakIntegration, keycloakIntegrationQueryKey } from '@/features/integrations/tenant-integrations';
import { useCurrentActor } from '@/shared/actors/current-actor';
import { Card } from '@/shared/components/ui/card';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';

const emptyValues: KeycloakRoleFormValues = { name: '', description: '' };

export default function KeycloakRoleCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const actorQuery = useCurrentActor();
  const settingsQuery = useQuery({ queryKey: keycloakIntegrationQueryKey, queryFn: fetchKeycloakIntegration });
  const [values, setValues] = React.useState<KeycloakRoleFormValues>(emptyValues);
  const createRole = useMutation({
    mutationFn: async () => await createKeycloakRole(values),
    onSuccess: async (role) => { await queryClient.invalidateQueries({ queryKey: ['administration', 'keycloak', 'roles'] }); toast.success('Role created.'); await navigate({ to: '/administration/user-management/roles/$roleId/edit', params: { roleId: role.id }, replace: true }); },
    onError: (error: Error) => toast.error(error.message),
  });

  return <SimpleEntityLayout backTo="/administration/user-management" title="Create role" description="Create a Keycloak realm role for tenant user and group access." settingsEnabled={settingsQuery.data?.adminApi.isEnabled ?? false} settingsLoading={settingsQuery.isLoading} settingsError={settingsQuery.isError} showSettingsLink={actorQuery.data?.roles.includes('integrator') ?? false}><Card className="p-6"><KeycloakRoleForm values={values} onChange={setValues} isSubmitting={createRole.isPending} submitLabel="Create role" onSubmit={() => createRole.mutate()} /></Card></SimpleEntityLayout>;
}

function SimpleEntityLayout({ backTo, title, description, settingsEnabled, settingsLoading, settingsError, showSettingsLink, children }: { readonly backTo: string; readonly title: string; readonly description: string; readonly settingsEnabled: boolean; readonly settingsLoading: boolean; readonly settingsError: boolean; readonly showSettingsLink: boolean; readonly children: React.ReactNode; }) { return <div className="grid gap-6"><header className="flex items-start gap-4"><Link to={backTo} className="inline-flex size-10 items-center justify-center rounded-interactive border border-border bg-content text-foreground transition hover:bg-hover-blue" aria-label="Go back"><ArrowLeft className="size-4" aria-hidden="true" /></Link><div><h2 className="text-[20px] font-semibold tracking-tight">{title}</h2><p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{description}</p></div></header>{settingsLoading ? <p className="text-[14px] text-muted-foreground">Loading Keycloak integration status...</p> : null}{settingsError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load Keycloak integration status.</p> : null}{!settingsLoading && !settingsError && !settingsEnabled ? <Empty className="min-h-0 bg-content"><EmptyHeader><EmptyTitle>Keycloak user management is not activated for this tenant</EmptyTitle><EmptyDescription>Enable the tenant Keycloak Admin API before using this page.</EmptyDescription></EmptyHeader>{showSettingsLink ? <EmptyContent><Link to="/integrations/keycloak" className="inline-flex h-10 items-center rounded-interactive bg-primary px-4 text-[14px] font-semibold text-white transition hover:opacity-90">Open Keycloak integration settings</Link></EmptyContent> : null}</Empty> : null}{!settingsLoading && !settingsError && settingsEnabled ? children : null}</div>; }
