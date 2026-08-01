import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft, ToggleLeft, ToggleRight, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from '@/shared/components/ui/pagination';

import { PersonaForm, type PersonaFormValues } from './persona-form';

type EmployeeResponse = components['schemas']['EmployeeResponse'];
type EmployeeStatus = components['schemas']['EmployeeStatus'];
type PersonaResponse = components['schemas']['PersonaResponse'];
type UpdatePersonaRequest = components['schemas']['UpdatePersonaRequest'];

const employeesQueryKey = ['administration', 'my-organization', 'employees'] as const;
const personasQueryKey = ['administration', 'my-organization', 'personas'] as const;
const pageSize = 10;
const employeeStatuses: readonly EmployeeStatus[] = ['PreHire', 'Active', 'Leave', 'Suspended', 'Terminated', 'Archived'];

export default function PersonaEditPage() {
  const { personaId } = useParams({ from: '/main/administration/my-organization/personas/$personaId/edit' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [employeePage, setEmployeePage] = useState(0);
  const [employeeQuery, setEmployeeQuery] = useState('');
  const [employeeStatus, setEmployeeStatus] = useState<'all' | EmployeeStatus>('all');

  const personaQuery = useQuery({
    queryKey: [...personasQueryKey, personaId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/personas/{id}', { params: { path: { id: personaId } } });
      if (error || !data) throw new Error('Could not load persona.');
      return data;
    },
  });

  const personaEmployeesQuery = useQuery({
    queryKey: [...personasQueryKey, personaId, 'employees', employeePage, employeeQuery, employeeStatus],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/personas/{id}/employees', {
        params: {
          path: { id: personaId },
          query: {
            Query: employeeQuery || undefined,
            Status: employeeStatus === 'all' ? [] : [employeeStatus],
            OrganizationUnitId: undefined,
            IncludeDescendants: true,
            Page: employeePage,
            PageSize: pageSize,
          } as never,
        },
      });
      if (error) throw new Error('Could not load persona employees.');
      return data;
    },
  });

  const updatePersona = useMutation({
    mutationFn: async (values: PersonaFormValues) => {
      const request: UpdatePersonaRequest = { name: values.name };
      const { error } = await api.PUT('/api/employees/personas/{id}', { params: { path: { id: personaId } }, body: request });
      if (error) throw new Error('Could not save persona.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: personasQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...personasQueryKey, personaId] }),
      ]);
      toast.success('Persona saved.');
    },
    onError: () => toast.error('Could not save persona.'),
  });

  const activatePersona = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/employees/personas/{id}/activate', { params: { path: { id: personaId } } });
      if (error) throw new Error('Could not activate persona.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: personasQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...personasQueryKey, personaId] }),
      ]);
      toast.success('Persona activated.');
    },
    onError: () => toast.error('Could not activate persona.'),
  });

  const deactivatePersona = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/employees/personas/{id}/deactivate', { params: { path: { id: personaId } } });
      if (error) throw new Error('Could not deactivate persona.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: personasQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...personasQueryKey, personaId] }),
      ]);
      toast.success('Persona deactivated.');
    },
    onError: () => toast.error('Could not deactivate persona.'),
  });

  const removePersonaAssociation = useMutation({
    mutationFn: async (employee: EmployeeResponse) => {
      const { error } = await api.PUT('/api/employees/employees/{id}/personas', {
        params: { path: { id: employee.id } },
        body: { personaIds: employee.personas.filter((item) => item.id !== personaId).map((item) => item.id) },
      });
      if (error) throw new Error('Could not remove persona association.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: personasQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...personasQueryKey, personaId] }),
        queryClient.invalidateQueries({ queryKey: [...personasQueryKey, personaId, 'employees'] }),
        queryClient.invalidateQueries({ queryKey: employeesQueryKey }),
      ]);
      toast.success('Persona removed from employee.');
    },
    onError: () => toast.error('Could not remove persona from employee.'),
  });

  const persona = personaQuery.data;
  const personaEmployees = personaEmployeesQuery.data?.items ?? [];
  const pagination = getPaginationState(personaEmployeesQuery.data, personaEmployees.length, employeePage, pageSize);

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div className="flex-1">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h2 className="text-[20px] font-semibold tracking-tight">Edit persona</h2>
              <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update persona details in My Organization.</p>
            </div>
            {persona ? (
              <Button type="button" variant="outline" disabled={activatePersona.isPending || deactivatePersona.isPending} onClick={() => {
                if (persona.isActive) {
                  deactivatePersona.mutate();
                } else {
                  activatePersona.mutate();
                }
              }}>
                {persona.isActive ? <ToggleLeft className="size-4" aria-hidden="true" /> : <ToggleRight className="size-4" aria-hidden="true" />}
                {persona.isActive ? 'Deactivate persona' : 'Activate persona'}
              </Button>
            ) : null}
          </div>
        </div>
      </header>

      <Card className="p-6">
        {personaQuery.isError || updatePersona.isError || activatePersona.isError || deactivatePersona.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{personaQuery.isError ? 'Could not load persona.' : updatePersona.isError ? 'Could not save persona.' : activatePersona.isError ? 'Could not activate persona.' : 'Could not deactivate persona.'}</p> : null}
        {personaQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading persona...</p> : null}
        {!personaQuery.isLoading && persona && !personaQuery.isError ? <PersonaForm initialValues={toFormValues(persona)} isSubmitting={updatePersona.isPending} submitLabel="Save" onSubmit={(values) => updatePersona.mutate(values)} /> : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Employees</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Employees currently assigned to this persona.</p>
          </div>
        </div>

        <div className="grid gap-3 rounded-structural border border-border p-4 md:grid-cols-2">
          <label className="grid gap-2 text-[14px] font-medium">
            <span>Search employees</span>
            <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={employeeQuery} onChange={(event) => { setEmployeeQuery(event.target.value); setEmployeePage(0); }} placeholder="Search name, email, or employee number" />
          </label>
          <label className="grid gap-2 text-[14px] font-medium">
            <span>Status</span>
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={employeeStatus} onChange={(event) => { setEmployeeStatus(event.target.value as 'all' | EmployeeStatus); setEmployeePage(0); }}>
              <option value="all">All statuses</option>
              {employeeStatuses.map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </label>
        </div>

        {personaEmployeesQuery.isError || removePersonaAssociation.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{personaEmployeesQuery.isError ? 'Could not load persona employees.' : 'Could not remove persona from employee.'}</p> : null}

        {!personaEmployeesQuery.isLoading && !personaEmployeesQuery.isError && pagination.totalItems === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No employees assigned to this persona.</p> : null}

        <div className="grid gap-3 md:hidden">
          {personaEmployeesQuery.isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading employees...</p> : null}
          {personaEmployees.map((employee) => (
            <article key={employee.id} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => void navigate({ to: '/administration/my-organization/employees/$employeeId/edit', params: { employeeId: employee.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/my-organization/employees/$employeeId/edit', params: { employeeId: employee.id } }); } }}>
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <h4 className="truncate text-[15px] font-semibold text-foreground">{employee.firstName} {employee.lastName}</h4>
                  <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                    <div><dt className="font-medium text-foreground">Employee Number</dt><dd>{employee.employeeNumber ?? '-'}</dd></div>
                    <div><dt className="font-medium text-foreground">Email</dt><dd>{employee.email ?? '-'}</dd></div>
                    <div><dt className="font-medium text-foreground">Organizational Unit</dt><dd>{employee.organizationUnit?.name ?? '-'}</dd></div>
                    <div><dt className="font-medium text-foreground">Job Title</dt><dd>{employee.jobTitle ?? '-'}</dd></div>
                    <div><dt className="font-medium text-foreground">Status</dt><dd>{employee.status}</dd></div>
                  </dl>
                </div>
                <Button type="button" variant="outline" size="sm" disabled={removePersonaAssociation.isPending} onClick={(event) => { event.stopPropagation(); if (persona && window.confirm(`Remove persona ${persona.name} from ${employee.firstName} ${employee.lastName}?`)) { removePersonaAssociation.mutate(employee); } }}>
                  <Trash2 className="size-4" aria-hidden="true" />
                  Remove
                </Button>
              </div>
            </article>
          ))}
        </div>

        <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
          <table className="w-full min-w-[66rem] border-collapse text-left text-[14px]">
            <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
              <tr>
                <th className="px-4 py-3 font-semibold">Name</th>
                <th className="px-4 py-3 font-semibold">Employee Number</th>
                <th className="px-4 py-3 font-semibold">Email</th>
                <th className="px-4 py-3 font-semibold">Organizational Unit</th>
                <th className="px-4 py-3 font-semibold">Job Title</th>
                <th className="px-4 py-3 font-semibold">Status</th>
                <th className="px-4 py-3 text-right font-semibold">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {personaEmployeesQuery.isLoading ? (
                <tr>
                  <td className="px-4 py-5 text-muted-foreground" colSpan={7}>Loading employees...</td>
                </tr>
              ) : null}
              {personaEmployees.map((employee) => (
                <tr key={employee.id} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => void navigate({ to: '/administration/my-organization/employees/$employeeId/edit', params: { employeeId: employee.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/my-organization/employees/$employeeId/edit', params: { employeeId: employee.id } }); } }}>
                  <td className="px-4 py-4 font-medium text-foreground">{employee.firstName} {employee.lastName}</td>
                  <td className="px-4 py-4 text-muted-foreground">{employee.employeeNumber ?? '-'}</td>
                  <td className="px-4 py-4 text-muted-foreground">{employee.email ?? '-'}</td>
                  <td className="px-4 py-4 text-muted-foreground">{employee.organizationUnit?.name ?? '-'}</td>
                  <td className="px-4 py-4 text-muted-foreground">{employee.jobTitle ?? '-'}</td>
                  <td className="px-4 py-4 text-muted-foreground">{employee.status}</td>
                  <td className="px-4 py-4" onClick={(event) => event.stopPropagation()} onKeyDown={(event) => event.stopPropagation()}>
                    <div className="flex justify-end">
                      <Button type="button" variant="outline" size="sm" disabled={removePersonaAssociation.isPending} onClick={() => { if (persona && window.confirm(`Remove persona ${persona.name} from ${employee.firstName} ${employee.lastName}?`)) { removePersonaAssociation.mutate(employee); } }}>
                        <Trash2 className="size-4" aria-hidden="true" />
                        Remove
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {!personaEmployeesQuery.isLoading && !personaEmployeesQuery.isError && pagination.totalItems > 0 ? (
          <div className="flex flex-col gap-3 text-[14px] text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
            <p>Showing {pagination.firstItem}-{pagination.lastItem} of {pagination.totalItems}</p>
            <Pagination className="sm:mx-0 sm:w-auto">
              <PaginationContent>
                <PaginationItem>
                  <PaginationPrevious disabled={pagination.currentPage === 0} onClick={() => setEmployeePage(Math.max(0, pagination.currentPage - 1))} />
                </PaginationItem>
                {pagination.visiblePages.map((visiblePage, index) =>
                  visiblePage === 'ellipsis' ? (
                    <PaginationItem key={`${visiblePage}-${index}`}>
                      <PaginationEllipsis />
                    </PaginationItem>
                  ) : (
                    <PaginationItem key={visiblePage}>
                      <PaginationLink isActive={visiblePage === pagination.currentPage} onClick={() => setEmployeePage(visiblePage)}>
                        {visiblePage + 1}
                      </PaginationLink>
                    </PaginationItem>
                  ),
                )}
                <PaginationItem>
                  <PaginationNext disabled={pagination.currentPage >= pagination.totalPages - 1} onClick={() => setEmployeePage(Math.min(pagination.totalPages - 1, pagination.currentPage + 1))} />
                </PaginationItem>
              </PaginationContent>
            </Pagination>
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function toFormValues(persona: PersonaResponse): PersonaFormValues {
  return { name: persona.name };
}

function getPaginationState(page: components['schemas']['PageOfEmployeeResponse'] | undefined, itemCount: number, requestedPage: number, resolvedPageSize: number) {
  const totalItems = Number(page?.totalItems ?? itemCount);
  const totalPages = Math.max(Number(page?.totalPages ?? 1), 1);
  const currentPage = Math.min(Number(page?.currentPage ?? requestedPage), totalPages - 1);
  const firstItem = totalItems === 0 ? 0 : currentPage * resolvedPageSize + 1;
  const lastItem = Math.min((currentPage + 1) * resolvedPageSize, totalItems);
  const visiblePages = getVisiblePages(totalPages, currentPage);
  return { currentPage, firstItem, lastItem, totalItems, totalPages, visiblePages };
}

function getVisiblePages(totalPages: number, currentPage: number) {
  if (totalPages <= 5) {
    return Array.from({ length: totalPages }, (_, index) => index);
  }

  const pages = new Set([0, totalPages - 1, currentPage - 1, currentPage, currentPage + 1]);
  const sortedPages = Array.from(pages).filter((pageNumber) => pageNumber >= 0 && pageNumber < totalPages).sort((left, right) => left - right);
  const visiblePages: Array<number | 'ellipsis'> = [];

  sortedPages.forEach((pageNumber, index) => {
    if (index > 0 && pageNumber - sortedPages[index - 1] > 1) {
      visiblePages.push('ellipsis');
    }

    visiblePages.push(pageNumber);
  });

  return visiblePages;
}
