import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft, ArchiveRestore, ArchiveX, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { getLocationLabel, LocationSelector, type LocationResponse } from '@/shared/components/location-selector';
import { IdentityEvidenceCard } from '@/features/requirements/identity-evidence-card';

import { EmployeeForm, type EmployeeFormValues } from './employee-form';

type EmployeeResponse = components['schemas']['EmployeeResponse'];
type EmployeeWorkLocationResponse = components['schemas']['EmployeeWorkLocationResponse'];
type PersonaSummaryResponse = components['schemas']['PersonaSummaryResponse'];
type UpdateEmployeeRequest = components['schemas']['UpdateEmployeeRequest'];

const employeesQueryKey = ['administration', 'my-organization', 'employees'] as const;
const personasQueryKey = ['administration', 'my-organization', 'personas'] as const;

export default function EmployeeEditPage() {
  const { employeeId } = useParams({ from: '/main/administration/my-organization/employees/$employeeId/edit' });
  const queryClient = useQueryClient();
  const [selectedPersonaId, setSelectedPersonaId] = useState('');
  const [selectedWorkLocationId, setSelectedWorkLocationId] = useState<string | null>(null);
  const [newWorkLocationPrimary, setNewWorkLocationPrimary] = useState(true);
  const [isAssignPersonaOpen, setIsAssignPersonaOpen] = useState(false);
  const [isAssignWorkLocationOpen, setIsAssignWorkLocationOpen] = useState(false);

  const employeeQuery = useQuery({
    queryKey: [...employeesQueryKey, employeeId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees/{id}', { params: { path: { id: employeeId } } });
      if (error || !data) throw new Error('Could not load employee.');
      return data;
    },
  });

  const organizationUnitsQuery = useQuery({
    queryKey: [...employeesQueryKey, 'organization-units-options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/organization-units', { params: { query: { Query: undefined, ParentId: undefined, IsActive: true, Page: 0, PageSize: 200 } as never } });
      if (error) throw new Error('Could not load organizational units.');
      return data?.items ?? [];
    },
  });

  const managersQuery = useQuery({
    queryKey: [...employeesQueryKey, 'manager-options', employeeId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees', { params: { query: { Query: undefined, Status: ['Active'], OrganizationUnitId: undefined, IncludeDescendants: true, Page: 0, PageSize: 200 } as never } });
      if (error) throw new Error('Could not load managers.');
      return (data?.items ?? []).filter((employee) => employee.id !== employeeId);
    },
  });

  const employeePersonasQuery = useQuery({
    queryKey: [...employeesQueryKey, employeeId, 'personas'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees/{id}/personas', { params: { path: { id: employeeId } } });
      if (error || !data) throw new Error('Could not load employee personas.');
      return data;
    },
  });

  const availablePersonasQuery = useQuery({
    queryKey: [...personasQueryKey, 'options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/personas', { params: { query: { Query: undefined, IsActive: true, Page: 0, PageSize: 200 } as never } });
      if (error) throw new Error('Could not load personas.');
      return data?.items ?? [];
    },
  });

  const employeeWorkLocationsQuery = useQuery({
    queryKey: [...employeesQueryKey, employeeId, 'work-locations'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees/{id}/work-locations', { params: { path: { id: employeeId } } });
      if (error || !data) throw new Error('Could not load employee work locations.');
      return data;
    },
  });

  const workLocationDetailsQuery = useQuery({
    queryKey: [...employeesQueryKey, employeeId, 'work-location-details', employeeWorkLocationsQuery.data?.map((workLocation) => workLocation.locationId).join(',') ?? ''],
    enabled: Boolean(employeeWorkLocationsQuery.data && employeeWorkLocationsQuery.data.length > 0),
    queryFn: async () => {
      const locations = await Promise.all(
        (employeeWorkLocationsQuery.data ?? []).map(async (workLocation) => {
          const { data, error } = await api.GET('/api/locations/locations/{id}', { params: { path: { id: workLocation.locationId } } });
          if (error || !data) {
            throw new Error('Could not load work location details.');
          }
          return data;
        }),
      );

      return new Map(locations.map((location) => [location.id, location]));
    },
  });

  const updateEmployee = useMutation({
    mutationFn: async (values: EmployeeFormValues) => {
      const request: UpdateEmployeeRequest = {
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

      const { error } = await api.PUT('/api/employees/employees/{id}', { params: { path: { id: employeeId } }, body: request });
      if (error) throw new Error('Could not save employee.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: employeesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...employeesQueryKey, employeeId] }),
      ]);
      toast.success('Employee saved.');
    },
    onError: () => toast.error('Could not save employee.'),
  });

  const archiveEmployee = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/employees/employees/{id}/archive', { params: { path: { id: employeeId } } });
      if (error) throw new Error('Could not archive employee.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: employeesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...employeesQueryKey, employeeId] }),
      ]);
      toast.success('Employee archived.');
    },
    onError: () => toast.error('Could not archive employee.'),
  });

  const unarchiveEmployee = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/employees/employees/{id}/unarchive', { params: { path: { id: employeeId } } });
      if (error) throw new Error('Could not restore employee.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: employeesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...employeesQueryKey, employeeId] }),
      ]);
      toast.success('Employee restored.');
    },
    onError: () => toast.error('Could not restore employee.'),
  });

  const replaceEmployeePersonas = useMutation({
    mutationFn: async (personaIds: string[]) => {
      const { error } = await api.PUT('/api/employees/employees/{id}/personas', {
        params: { path: { id: employeeId } },
        body: { personaIds },
      });
      if (error) throw new Error('Could not update employee personas.');
    },
    onSuccess: async () => {
      setSelectedPersonaId('');
      setIsAssignPersonaOpen(false);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: employeesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...employeesQueryKey, employeeId] }),
        queryClient.invalidateQueries({ queryKey: [...employeesQueryKey, employeeId, 'personas'] }),
      ]);
      toast.success('Employee personas updated.');
    },
    onError: () => toast.error('Could not update employee personas.'),
  });

  const replaceEmployeeWorkLocations = useMutation({
    mutationFn: async (workLocations: { locationId: string; isPrimary: boolean }[]) => {
      const { error } = await api.PUT('/api/employees/employees/{id}/work-locations', {
        params: { path: { id: employeeId } },
        body: { workLocations },
      });
      if (error) throw new Error('Could not update employee work locations.');
    },
    onSuccess: async () => {
      setSelectedWorkLocationId(null);
      setNewWorkLocationPrimary(false);
      setIsAssignWorkLocationOpen(false);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: employeesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...employeesQueryKey, employeeId] }),
        queryClient.invalidateQueries({ queryKey: [...employeesQueryKey, employeeId, 'work-locations'] }),
        queryClient.invalidateQueries({ queryKey: [...employeesQueryKey, employeeId, 'work-location-details'] }),
      ]);
      toast.success('Employee work locations updated.');
    },
    onError: () => toast.error('Could not update employee work locations.'),
  });

  const employee = employeeQuery.data;
  const initialValues = toFormValues(employee);
  const isArchived = Boolean(employee?.archivedAt);
  const assignedPersonas = employeePersonasQuery.data ?? [];
  const assignedPersonaIds = new Set(assignedPersonas.map((persona) => persona.id));
  const availablePersonas = (availablePersonasQuery.data ?? []).filter((persona) => !assignedPersonaIds.has(persona.id));
  const workLocations = employeeWorkLocationsQuery.data ?? [];
  const workLocationDetails = workLocationDetailsQuery.data ?? new Map<string, LocationResponse>();

  function handleAssignPersona() {
    if (!selectedPersonaId) {
      return;
    }

    replaceEmployeePersonas.mutate([...assignedPersonas.map((persona) => persona.id), selectedPersonaId]);
  }

  function handleRemovePersona(persona: PersonaSummaryResponse) {
    replaceEmployeePersonas.mutate(assignedPersonas.filter((item) => item.id !== persona.id).map((item) => item.id));
  }

  function handleAssignWorkLocation() {
    if (!selectedWorkLocationId) {
      return;
    }

    const shouldSetPrimary = workLocations.length === 0 || newWorkLocationPrimary;
    const nextWorkLocations = [
      ...workLocations.map((workLocation) => ({
        locationId: workLocation.locationId,
        isPrimary: shouldSetPrimary ? false : workLocation.isPrimary,
      })),
      { locationId: selectedWorkLocationId, isPrimary: shouldSetPrimary || (workLocations.length === 0 && !newWorkLocationPrimary) },
    ];

    replaceEmployeeWorkLocations.mutate(nextWorkLocations);
  }

  function handleRemoveWorkLocation(workLocation: EmployeeWorkLocationResponse) {
    const remaining = workLocations.filter((item) => item.locationId !== workLocation.locationId);
    const nextWorkLocations = remaining.map((item, index) => ({
      locationId: item.locationId,
      isPrimary: remaining.some((candidate) => candidate.isPrimary) ? item.isPrimary : index === 0,
    }));
    replaceEmployeeWorkLocations.mutate(nextWorkLocations);
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div className="flex-1">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h2 className="text-[20px] font-semibold tracking-tight">Edit employee</h2>
              <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update employee details in My Organization.</p>
            </div>
            {employee ? (
              <Button type="button" variant="outline" disabled={archiveEmployee.isPending || unarchiveEmployee.isPending} onClick={() => {
                if (isArchived) {
                  unarchiveEmployee.mutate();
                } else if (window.confirm(`Archive employee ${employee.firstName} ${employee.lastName}?`)) {
                  archiveEmployee.mutate();
                }
              }}>
                {isArchived ? <ArchiveRestore className="size-4" aria-hidden="true" /> : <ArchiveX className="size-4" aria-hidden="true" />}
                {isArchived ? 'Restore employee' : 'Archive employee'}
              </Button>
            ) : null}
          </div>
        </div>
      </header>

      <Card className="p-6">
        {employeeQuery.isError || organizationUnitsQuery.isError || managersQuery.isError || updateEmployee.isError || archiveEmployee.isError || unarchiveEmployee.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{employeeQuery.isError ? 'Could not load employee.' : organizationUnitsQuery.isError ? 'Could not load organizational units.' : managersQuery.isError ? 'Could not load managers.' : updateEmployee.isError ? 'Could not save employee.' : archiveEmployee.isError ? 'Could not archive employee.' : 'Could not restore employee.'}</p> : null}
        {employeeQuery.isLoading || organizationUnitsQuery.isLoading || managersQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading employee...</p> : null}
        {!employeeQuery.isLoading && !organizationUnitsQuery.isLoading && !managersQuery.isLoading && employee && !employeeQuery.isError && !organizationUnitsQuery.isError && !managersQuery.isError ? <EmployeeForm initialValues={initialValues} organizationUnits={organizationUnitsQuery.data ?? []} managers={managersQuery.data ?? []} isSubmitting={updateEmployee.isPending} submitLabel="Save" onSubmit={(values) => updateEmployee.mutate(values)} /> : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Personas</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Assign personas to this employee and remove them when no longer needed.</p>
          </div>
          <Button type="button" variant="outline" disabled={availablePersonasQuery.isLoading || replaceEmployeePersonas.isPending || availablePersonas.length === 0} onClick={() => setIsAssignPersonaOpen((current) => !current)}>
            <Plus className="size-4" aria-hidden="true" />
            {isAssignPersonaOpen ? 'Cancel' : 'Assign'}
          </Button>
        </div>

        {isAssignPersonaOpen ? (
          <div className="grid gap-2 rounded-structural border border-border p-4 sm:max-w-80">
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedPersonaId} onChange={(event) => setSelectedPersonaId(event.target.value)} disabled={availablePersonasQuery.isLoading || replaceEmployeePersonas.isPending || availablePersonas.length === 0}>
              <option value="">Select persona</option>
              {availablePersonas.map((persona) => <option key={persona.id} value={persona.id}>{persona.name}</option>)}
            </select>
            <Button type="button" disabled={!selectedPersonaId || replaceEmployeePersonas.isPending} onClick={handleAssignPersona}>
              <Plus className="size-4" aria-hidden="true" />
              Assign persona
            </Button>
          </div>
        ) : null}

        {employeePersonasQuery.isError || availablePersonasQuery.isError || replaceEmployeePersonas.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{employeePersonasQuery.isError ? 'Could not load employee personas.' : availablePersonasQuery.isError ? 'Could not load personas.' : 'Could not update employee personas.'}</p> : null}

        {employeePersonasQuery.isLoading || availablePersonasQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading personas...</p> : null}

        {!employeePersonasQuery.isLoading && !availablePersonasQuery.isLoading && assignedPersonas.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No personas assigned yet.</p> : null}

        {assignedPersonas.length > 0 ? (
          <div className="grid gap-3">
            {assignedPersonas.map((persona) => (
              <div key={persona.id} className="flex items-center justify-between gap-4 rounded-structural border border-border p-4">
                <div className="min-w-0">
                  <p className="font-medium text-foreground">{persona.name}</p>
                  <p className="mt-1 text-[14px] text-muted-foreground">{persona.isActive ? 'Active persona' : 'Inactive persona'}</p>
                </div>
                <Button type="button" variant="outline" size="sm" disabled={replaceEmployeePersonas.isPending} onClick={() => handleRemovePersona(persona)}>
                  <Trash2 className="size-4" aria-hidden="true" />
                  Remove
                </Button>
              </div>
            ))}
          </div>
        ) : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Work Locations</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Assign work locations to this employee and remove them when no longer needed.</p>
          </div>
          <Button type="button" variant="outline" disabled={replaceEmployeeWorkLocations.isPending} onClick={() => setIsAssignWorkLocationOpen((current) => !current)}>
            <Plus className="size-4" aria-hidden="true" />
            {isAssignWorkLocationOpen ? 'Cancel' : 'Assign'}
          </Button>
        </div>

        {isAssignWorkLocationOpen ? (
          <div className="grid gap-4 rounded-structural border border-border p-4">
            <LocationSelector value={selectedWorkLocationId} onChange={setSelectedWorkLocationId} level="Room" disabled={replaceEmployeeWorkLocations.isPending} />
            <label className="flex items-center gap-3 text-[14px] font-medium">
              <input type="checkbox" className="size-4 rounded border border-border" checked={workLocations.length === 0 ? true : newWorkLocationPrimary} disabled={replaceEmployeeWorkLocations.isPending || workLocations.length === 0} onChange={(event) => setNewWorkLocationPrimary(event.target.checked)} />
              Set as primary work location
            </label>
            <div className="flex justify-end">
              <Button type="button" disabled={!selectedWorkLocationId || replaceEmployeeWorkLocations.isPending || workLocations.some((workLocation) => workLocation.locationId === selectedWorkLocationId)} onClick={handleAssignWorkLocation}>
                <Plus className="size-4" aria-hidden="true" />
                Assign work location
              </Button>
            </div>
          </div>
        ) : null}

        {employeeWorkLocationsQuery.isError || workLocationDetailsQuery.isError || replaceEmployeeWorkLocations.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{employeeWorkLocationsQuery.isError ? 'Could not load employee work locations.' : workLocationDetailsQuery.isError ? 'Could not load work location details.' : 'Could not update employee work locations.'}</p> : null}

        {employeeWorkLocationsQuery.isLoading || workLocationDetailsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading work locations...</p> : null}

        {!employeeWorkLocationsQuery.isLoading && !workLocationDetailsQuery.isLoading && workLocations.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No work locations assigned yet.</p> : null}

        {workLocations.length > 0 ? (
          <div className="grid gap-3">
            {workLocations.map((workLocation) => (
              <div key={workLocation.locationId} className="flex items-center justify-between gap-4 rounded-structural border border-border p-4">
                <div className="min-w-0">
                  <p className="font-medium text-foreground">{getLocationLabel(workLocationDetails.get(workLocation.locationId))}</p>
                  <p className="mt-1 text-[14px] text-muted-foreground">{workLocation.isPrimary ? 'Primary work location' : 'Secondary work location'}</p>
                </div>
                <Button type="button" variant="outline" size="sm" disabled={replaceEmployeeWorkLocations.isPending} onClick={() => handleRemoveWorkLocation(workLocation)}>
                  <Trash2 className="size-4" aria-hidden="true" />
                  Remove
                </Button>
              </div>
            ))}
          </div>
        ) : null}
      </Card>

      {employee?.identityId ? <IdentityEvidenceCard identityId={employee.identityId} title="Requirement evidence" description="Evidence attached to this employee identity. Expired evidence is hidden by default and entries are grouped by requirement." /> : null}
    </div>
  );
}

function toFormValues(employee: EmployeeResponse | undefined): EmployeeFormValues {
  if (!employee) {
    return {
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
  }

  return {
    firstName: employee.firstName,
    lastName: employee.lastName,
    birthDate: employee.birthDate ?? '',
    employeeNumber: employee.employeeNumber ?? '',
    directoryId: employee.directoryId ?? '',
    email: employee.email ?? '',
    organizationUnitId: employee.organizationUnit?.id ?? '',
    managerEmployeeId: employee.managerEmployeeId ?? '',
    jobTitle: employee.jobTitle ?? '',
    contractStartDate: employee.contractStartDate ?? '',
    contractEndDate: employee.contractEndDate ?? '',
  };
}

function nullIfEmpty(value: string) {
  return value.trim() === '' ? null : value;
}
