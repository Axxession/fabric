import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft, ToggleLeft, ToggleRight } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { OrganizationUnitForm, type OrganizationUnitFormValues } from './organization-unit-form';

type OrganizationUnitResponse = components['schemas']['OrganizationUnitResponse'];
type MoveOrganizationUnitRequest = components['schemas']['MoveOrganizationUnitRequest'];
type UpdateOrganizationUnitRequest = components['schemas']['UpdateOrganizationUnitRequest'];

const organizationUnitsQueryKey = ['administration', 'my-organization', 'organization-units'] as const;

export default function OrganizationUnitEditPage() {
  const { organizationUnitId } = useParams({ from: '/main/administration/my-organization/organizational-units/$organizationUnitId/edit' });
  const queryClient = useQueryClient();

  const organizationUnitQuery = useQuery({
    queryKey: [...organizationUnitsQueryKey, organizationUnitId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/organization-units/{id}', { params: { path: { id: organizationUnitId } } });
      if (error || !data) throw new Error('Could not load organizational unit.');
      return data;
    },
  });

  const parentOptionsQuery = useQuery({
    queryKey: [...organizationUnitsQueryKey, 'parent-options', organizationUnitId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/organization-units', { params: { query: { Query: undefined, ParentId: undefined, IsActive: true, Page: 0, PageSize: 200 } as never } });
      if (error) throw new Error('Could not load organizational units.');
      return (data?.items ?? []).filter((unit) => unit.id !== organizationUnitId);
    },
  });

  const updateOrganizationUnit = useMutation({
    mutationFn: async (values: OrganizationUnitFormValues) => {
      const request: UpdateOrganizationUnitRequest = {
        name: values.name,
        code: nullIfEmpty(values.code),
        type: values.type,
      };

      const { error } = await api.PUT('/api/employees/organization-units/{id}', { params: { path: { id: organizationUnitId } }, body: request });
      if (error) throw new Error('Could not save organizational unit.');

      if (values.parentId !== (organizationUnit?.parentId ?? '')) {
        const moveRequest: MoveOrganizationUnitRequest = { parentId: nullIfEmpty(values.parentId) };
        const moveResult = await api.POST('/api/employees/organization-units/{id}/move', { params: { path: { id: organizationUnitId } }, body: moveRequest });
        if (moveResult.error) throw new Error('Could not move organizational unit.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: organizationUnitsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...organizationUnitsQueryKey, organizationUnitId] }),
      ]);
      toast.success('Organizational unit saved.');
    },
    onError: () => toast.error('Could not save organizational unit.'),
  });

  const activateOrganizationUnit = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/employees/organization-units/{id}/activate', { params: { path: { id: organizationUnitId } } });
      if (error) throw new Error('Could not activate organizational unit.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: organizationUnitsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...organizationUnitsQueryKey, organizationUnitId] }),
      ]);
      toast.success('Organizational unit activated.');
    },
    onError: () => toast.error('Could not activate organizational unit.'),
  });

  const deactivateOrganizationUnit = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/employees/organization-units/{id}/deactivate', { params: { path: { id: organizationUnitId } } });
      if (error) throw new Error('Could not deactivate organizational unit.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: organizationUnitsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...organizationUnitsQueryKey, organizationUnitId] }),
      ]);
      toast.success('Organizational unit deactivated.');
    },
    onError: () => toast.error('Could not deactivate organizational unit.'),
  });

  const organizationUnit = organizationUnitQuery.data;
  const initialValues = toFormValues(organizationUnit);

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div className="flex-1">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h2 className="text-[20px] font-semibold tracking-tight">Edit organizational unit</h2>
              <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update organizational unit details in My Organization.</p>
            </div>
            {organizationUnit ? (
              <Button type="button" variant="outline" disabled={activateOrganizationUnit.isPending || deactivateOrganizationUnit.isPending} onClick={() => {
                if (organizationUnit.isActive) {
                  deactivateOrganizationUnit.mutate();
                } else {
                  activateOrganizationUnit.mutate();
                }
              }}>
                {organizationUnit.isActive ? <ToggleLeft className="size-4" aria-hidden="true" /> : <ToggleRight className="size-4" aria-hidden="true" />}
                {organizationUnit.isActive ? 'Deactivate organizational unit' : 'Activate organizational unit'}
              </Button>
            ) : null}
          </div>
        </div>
      </header>

      <Card className="p-6">
        {organizationUnitQuery.isError || parentOptionsQuery.isError || updateOrganizationUnit.isError || activateOrganizationUnit.isError || deactivateOrganizationUnit.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{organizationUnitQuery.isError ? 'Could not load organizational unit.' : parentOptionsQuery.isError ? 'Could not load organizational units.' : updateOrganizationUnit.isError ? 'Could not save organizational unit.' : activateOrganizationUnit.isError ? 'Could not activate organizational unit.' : 'Could not deactivate organizational unit.'}</p> : null}
        {organizationUnitQuery.isLoading || parentOptionsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading organizational unit...</p> : null}
        {!organizationUnitQuery.isLoading && !parentOptionsQuery.isLoading && organizationUnit && !organizationUnitQuery.isError && !parentOptionsQuery.isError ? <OrganizationUnitForm initialValues={initialValues} parentOptions={parentOptionsQuery.data ?? []} isSubmitting={updateOrganizationUnit.isPending} submitLabel="Save" onSubmit={(values) => updateOrganizationUnit.mutate(values)} /> : null}
      </Card>
    </div>
  );
}

function toFormValues(organizationUnit: OrganizationUnitResponse | undefined): OrganizationUnitFormValues {
  if (!organizationUnit) {
    return { name: '', code: '', type: '', parentId: '' };
  }

  return {
    name: organizationUnit.name,
    code: organizationUnit.code ?? '',
    type: organizationUnit.type,
    parentId: organizationUnit.parentId ?? '',
  };
}

function nullIfEmpty(value: string) {
  return value.trim() === '' ? null : value;
}
