import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { EmployeeForm, type EmployeeFormValues } from './employee-form';

type CreateEmployeeRequest = components['schemas']['CreateEmployeeRequest'];

const employeesQueryKey = ['administration', 'my-organization', 'employees'] as const;
const emptyEmployee: EmployeeFormValues = {
  firstName: '',
  lastName: '',
  birthDate: '',
  employeeNumber: '',
  directoryId: '',
  email: '',
  organizationUnitId: '',
  managerEmployeeId: '',
  jobTitle: '',
  contractStartDate: '',
  contractEndDate: '',
};

export default function EmployeeCreatePage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const organizationUnitsQuery = useQuery({
    queryKey: [...employeesQueryKey, 'organization-units-options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/organization-units', { params: { query: { Query: undefined, ParentId: undefined, IsActive: true, Page: 0, PageSize: 200 } as never } });
      if (error) throw new Error('Could not load organizational units.');
      return data?.items ?? [];
    },
  });

  const managersQuery = useQuery({
    queryKey: [...employeesQueryKey, 'manager-options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees', { params: { query: { Query: undefined, Status: ['Active'], OrganizationUnitId: undefined, IncludeDescendants: true, Page: 0, PageSize: 200 } as never } });
      if (error) throw new Error('Could not load managers.');
      return data?.items ?? [];
    },
  });

  const createEmployee = useMutation({
    mutationFn: async (values: EmployeeFormValues) => {
      const request: CreateEmployeeRequest = {
        firstName: values.firstName,
        lastName: values.lastName,
        birthDate: nullIfEmpty(values.birthDate),
        employeeNumber: nullIfEmpty(values.employeeNumber),
        directoryId: nullIfEmpty(values.directoryId),
        email: nullIfEmpty(values.email),
        organizationUnitId: values.organizationUnitId,
        managerEmployeeId: nullIfEmpty(values.managerEmployeeId),
        jobTitle: nullIfEmpty(values.jobTitle),
        contractStartDate: nullIfEmpty(values.contractStartDate),
        contractEndDate: nullIfEmpty(values.contractEndDate),
      };

      const { data, error } = await api.POST('/api/employees/employees', { body: request });
      if (error || !data) throw new Error('Could not create employee.');
      return data;
    },
    onSuccess: async (employee) => {
      await queryClient.invalidateQueries({ queryKey: employeesQueryKey });
      toast.success('Employee created.');
      await navigate({ to: '/administration/my-organization/employees/$employeeId/edit', params: { employeeId: employee.id }, replace: true });
    },
    onError: () => toast.error('Could not create employee.'),
  });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add employee</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new employee record in My Organization.</p>
        </div>
      </header>

      <Card className="p-6">
        {organizationUnitsQuery.isError || managersQuery.isError || createEmployee.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{organizationUnitsQuery.isError ? 'Could not load organizational units.' : managersQuery.isError ? 'Could not load managers.' : 'Could not create employee.'}</p> : null}
        {organizationUnitsQuery.isLoading || managersQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading employee form...</p> : null}
        {!organizationUnitsQuery.isLoading && !managersQuery.isLoading && !organizationUnitsQuery.isError && !managersQuery.isError ? <EmployeeForm initialValues={emptyEmployee} organizationUnits={organizationUnitsQuery.data ?? []} managers={managersQuery.data ?? []} isSubmitting={createEmployee.isPending} submitLabel="Create employee" onSubmit={(values) => createEmployee.mutate(values)} /> : null}
      </Card>
    </div>
  );
}

function nullIfEmpty(value: string) {
  return value.trim() === '' ? null : value;
}
