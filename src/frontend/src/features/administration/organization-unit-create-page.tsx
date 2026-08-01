import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { OrganizationUnitForm, type OrganizationUnitFormValues } from './organization-unit-form';

type CreateOrganizationUnitRequest = components['schemas']['CreateOrganizationUnitRequest'];

const organizationUnitsQueryKey = ['administration', 'my-organization', 'organization-units'] as const;
const emptyOrganizationUnit: OrganizationUnitFormValues = { name: '', code: '', type: '', parentId: '' };

export default function OrganizationUnitCreatePage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const parentOptionsQuery = useQuery({
    queryKey: [...organizationUnitsQueryKey, 'parent-options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/organization-units', { params: { query: { Query: undefined, ParentId: undefined, IsActive: true, Page: 0, PageSize: 200 } as never } });
      if (error) throw new Error('Could not load organizational units.');
      return data?.items ?? [];
    },
  });

  const createOrganizationUnit = useMutation({
    mutationFn: async (values: OrganizationUnitFormValues) => {
      const request: CreateOrganizationUnitRequest = {
        name: values.name,
        code: nullIfEmpty(values.code),
        type: values.type,
        parentId: nullIfEmpty(values.parentId),
      };

      const { data, error } = await api.POST('/api/employees/organization-units', { body: request });
      if (error || !data) throw new Error('Could not create organizational unit.');
      return data;
    },
    onSuccess: async (organizationUnit) => {
      await queryClient.invalidateQueries({ queryKey: organizationUnitsQueryKey });
      toast.success('Organizational unit created.');
      await navigate({ to: '/administration/my-organization/organizational-units/$organizationUnitId/edit', params: { organizationUnitId: organizationUnit.id }, replace: true });
    },
    onError: () => toast.error('Could not create organizational unit.'),
  });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add organizational unit</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new organizational unit in My Organization.</p>
        </div>
      </header>

      <Card className="p-6">
        {parentOptionsQuery.isError || createOrganizationUnit.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{parentOptionsQuery.isError ? 'Could not load organizational units.' : 'Could not create organizational unit.'}</p> : null}
        {parentOptionsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading organizational unit form...</p> : null}
        {!parentOptionsQuery.isLoading && !parentOptionsQuery.isError ? <OrganizationUnitForm initialValues={emptyOrganizationUnit} parentOptions={parentOptionsQuery.data ?? []} isSubmitting={createOrganizationUnit.isPending} submitLabel="Create organizational unit" onSubmit={(values) => createOrganizationUnit.mutate(values)} /> : null}
      </Card>
    </div>
  );
}

function nullIfEmpty(value: string) {
  return value.trim() === '' ? null : value;
}
