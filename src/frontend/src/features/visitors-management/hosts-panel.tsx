import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Pencil, Plus, Trash2 } from 'lucide-react';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Input } from '@/shared/components/ui/input';
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from '@/shared/components/ui/pagination';

type Host = components['schemas']['HostResponse'];
type HostAssignmentMode = components['schemas']['HostAssignmentMode'];
type Employee = components['schemas']['EmployeeResponse'];

const hostsQueryKey = ['visitors-management', 'hosts'] as const;
const hostSettingsQueryKey = ['visitors-management', 'host-settings'] as const;
const pageSize = 10;

export function HostsPanel() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(0);
  const [query, setQuery] = useState('');
  const [employeeQuery, setEmployeeQuery] = useState('');

  const hostSettingsQuery = useQuery({
    queryKey: hostSettingsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/visitors/host-settings');

      if (error || !data) {
        throw new Error('Could not load host settings.');
      }

      return data;
    },
  });

  const hostsQuery = useQuery({
    queryKey: [...hostsQueryKey, query, page, pageSize],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/visitors/hosts', {
        params: { query: { Query: query || undefined } },
      });

      if (error || !data) {
        throw new Error('Could not load hosts.');
      }

      return data;
    },
  });

  const employeesQuery = useQuery({
    queryKey: ['visitors-management', 'host-candidates', employeeQuery],
    enabled: employeeQuery.trim().length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees', {
        params: { query: { Query: employeeQuery.trim(), Status: [], IncludeDescendants: false } },
      });

      if (error || !data) {
        throw new Error('Could not search employees.');
      }

      return data.items ?? [];
    },
  });

  const updateHostSettings = useMutation({
    mutationFn: async (assignmentMode: HostAssignmentMode) => {
      const { data, error } = await api.PUT('/api/visitors/host-settings', {
        body: { assignmentMode },
      });

      if (error || !data) {
        throw new Error('Could not update host settings.');
      }

      return data;
    },
    onSuccess: async (data) => {
      queryClient.setQueryData(hostSettingsQueryKey, data);
      await queryClient.invalidateQueries({ queryKey: hostsQueryKey });
    },
  });

  const addHost = useMutation({
    mutationFn: async (employeeId: string) => {
      const { error } = await api.POST('/api/visitors/hosts/{employeeId}', {
        params: { path: { employeeId } },
      });

      if (error) {
        throw new Error('Could not add host.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: hostsQueryKey });
    },
  });

  const removeHost = useMutation({
    mutationFn: async (employeeId: string) => {
      const { error } = await api.DELETE('/api/visitors/hosts/{employeeId}', {
        params: { path: { employeeId } },
      });

      if (error) {
        throw new Error('Could not remove host.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: hostsQueryKey });
    },
  });

  const pagedHosts = hostsQuery.data;
  const hosts = pagedHosts?.items ?? [];
  const totalItems = Number(pagedHosts?.totalItems ?? hosts.length);
  const totalPages = Math.max(Number(pagedHosts?.totalPages ?? 1), 1);
  const currentPage = Math.min(Number(pagedHosts?.currentPage ?? page), totalPages - 1);
  const firstItem = totalItems === 0 ? 0 : currentPage * pageSize + 1;
  const lastItem = Math.min((currentPage + 1) * pageSize, totalItems);
  const visiblePages = getVisiblePages(totalPages, currentPage);
  const currentAssignmentMode = hostSettingsQuery.data?.assignmentMode ?? 'AllEmployees';
  const canEditAllowList = currentAssignmentMode === 'AllowList';
  const allowListedEmployeeIds = useMemo(() => new Set(hosts.filter((host) => host.isAllowListed).map((host) => host.employeeId)), [hosts]);

  function handleModeSelectionChange(nextMode: HostAssignmentMode) {
    if (nextMode === currentAssignmentMode || updateHostSettings.isPending) {
      return;
    }

    updateHostSettings.mutate(nextMode);
  }

  function handleRemove(host: Host) {
    if (window.confirm(`Remove host ${getHostName(host)} from allow list?`)) {
      removeHost.mutate(host.employeeId);
    }
  }

  return (
    <div className="grid gap-6 pt-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Hosts</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Choose whether every active employee can host visits or only an explicit allow list.</p>
        </div>

        <div className="grid gap-2 text-[14px] font-medium text-foreground sm:min-w-64">
          <label htmlFor="host-mode-select">Host mode</label>
          <div className="flex gap-2">
            <select
              id="host-mode-select"
              className="min-w-0 flex-1 rounded-interactive border border-border bg-content px-3 py-2 text-[14px]"
              value={currentAssignmentMode}
              onChange={(event) => handleModeSelectionChange(event.target.value as HostAssignmentMode)}
              disabled={hostSettingsQuery.isLoading || updateHostSettings.isPending}
            >
              <option value="AllEmployees">All employees</option>
              <option value="AllowList">Allow list</option>
            </select>
            {updateHostSettings.isPending ? <Button type="button" disabled>Saving...</Button> : null}
          </div>
        </div>
      </div>

      {hostsQuery.isError || hostSettingsQuery.isError || addHost.isError || removeHost.isError || updateHostSettings.isError ? (
        <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
          Could not update host settings.
        </p>
      ) : null}

      {canEditAllowList ? (
        <div className="grid gap-4 rounded-structural border border-border p-4">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h3 className="text-[16px] font-semibold tracking-tight">Add host</h3>
              <p className="mt-1 text-[14px] text-muted-foreground">Search employees and add them to the host allow list.</p>
            </div>
          </div>

          <Input
            value={employeeQuery}
            onChange={(event) => setEmployeeQuery(event.target.value)}
            placeholder="Search employees by name or email"
          />

          <div className="grid gap-3">
            {employeesQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Searching employees...</p> : null}
            {employeesQuery.data?.map((employee: Employee) => (
              <div key={employee.id} className="flex items-center justify-between gap-3 rounded-interactive border border-border px-3 py-3">
                <div className="min-w-0">
                  <p className="truncate text-[14px] font-medium text-foreground">{getEmployeeName(employee)}</p>
                  <p className="truncate text-[13px] text-muted-foreground">{employee.email || 'No email'}</p>
                </div>
                <div className="flex shrink-0 gap-2">
                  <Link
                    to="/administration/my-organization/employees/$employeeId/edit"
                    params={{ employeeId: employee.id }}
                    className={buttonVariants({ variant: 'outline', size: 'sm' })}
                  >
                    <Pencil className="size-4" aria-hidden="true" />Edit
                  </Link>
                  <Button
                    size="sm"
                    disabled={allowListedEmployeeIds.has(employee.id) || addHost.isPending}
                    onClick={() => addHost.mutate(employee.id)}
                  >
                    <Plus className="size-4" aria-hidden="true" />
                    {allowListedEmployeeIds.has(employee.id) ? 'Added' : 'Add'}
                  </Button>
                </div>
              </div>
            ))}
            {employeeQuery.trim() && !employeesQuery.isLoading && (employeesQuery.data?.length ?? 0) === 0 ? (
              <p className="text-[14px] text-muted-foreground">No employees found.</p>
            ) : null}
          </div>
        </div>
      ) : (
        <p className="rounded-interactive border border-border bg-hover-blue px-4 py-3 text-[14px] text-foreground">All active employees can host visits in this mode.</p>
      )}

      {canEditAllowList ? (
        <div className="grid gap-4">
          <div className="grid gap-3 rounded-structural border border-border p-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              <span>Search allow list</span>
              <Input value={query} onChange={(event) => { setQuery(event.target.value); setPage(0); }} placeholder="Search hosts" />
            </label>
          </div>

          {!hostsQuery.isLoading && !hostsQuery.isError && totalItems === 0 ? (
            <Empty>
              <EmptyHeader>
                <EmptyTitle>No hosts found</EmptyTitle>
                <EmptyDescription>Add employees to the allow list to make them available as hosts.</EmptyDescription>
              </EmptyHeader>
              <EmptyContent />
            </Empty>
          ) : (
            <div className="grid gap-4">
              <div className="md:hidden">
                {hostsQuery.isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading hosts...</p> : null}
                {!hostsQuery.isLoading ? (
                  <div className="grid gap-3">
                    {hosts.map((host) => (
                      <article key={host.employeeId} className="rounded-structural border border-border p-4">
                        <h3 className="text-[15px] font-semibold text-foreground">{getHostName(host)}</h3>
                        <p className="mt-1 text-[14px] text-muted-foreground">{host.email || 'No email'}</p>
                        <div className="mt-3 flex gap-2">
                          <Link
                            to="/administration/my-organization/employees/$employeeId/edit"
                            params={{ employeeId: host.employeeId }}
                            className={buttonVariants({ variant: 'outline', size: 'sm' })}
                          >
                            <Pencil className="size-4" aria-hidden="true" />Edit employee
                          </Link>
                          <Button type="button" variant="outline" size="sm" onClick={() => handleRemove(host)} disabled={removeHost.isPending}>
                            <Trash2 className="size-4" aria-hidden="true" />Remove
                          </Button>
                        </div>
                      </article>
                    ))}
                  </div>
                ) : null}
              </div>

              <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
                {hostsQuery.isLoading ? (
                  <p className="px-4 py-5 text-[14px] text-muted-foreground">Loading hosts...</p>
                ) : (
                  <table className="w-full min-w-[52rem] border-collapse text-left text-[14px]">
                    <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                      <tr>
                        <th className="px-4 py-3 font-semibold">Name</th>
                        <th className="px-4 py-3 font-semibold">Email</th>
                        <th className="px-4 py-3 text-right font-semibold">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {hosts.map((host) => (
                        <tr key={host.employeeId}>
                          <td className="px-4 py-4 font-medium text-foreground">{getHostName(host)}</td>
                          <td className="px-4 py-4 text-muted-foreground">{host.email || 'No email'}</td>
                          <td className="px-4 py-4">
                            <div className="flex justify-end gap-2">
                              <Link
                                to="/administration/my-organization/employees/$employeeId/edit"
                                params={{ employeeId: host.employeeId }}
                                className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
                                aria-label={`Edit employee ${getHostName(host)}`}
                              >
                                <Pencil className="size-4" aria-hidden="true" />
                              </Link>
                              <Button
                                type="button"
                                variant="outline"
                                size="icon-sm"
                                aria-label={`Remove ${getHostName(host)}`}
                                disabled={removeHost.isPending}
                                onClick={() => handleRemove(host)}
                              >
                                <Trash2 className="size-4" aria-hidden="true" />
                              </Button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>

              {!hostsQuery.isLoading && !hostsQuery.isError && totalItems > 0 ? (
                <div className="flex flex-col gap-3 text-[14px] text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
                  <p>Showing {firstItem}-{lastItem} of {totalItems} hosts</p>
                  <Pagination className="sm:mx-0 sm:w-auto">
                    <PaginationContent>
                      <PaginationItem>
                        <PaginationPrevious disabled={currentPage === 0} onClick={() => setPage(Math.max(0, currentPage - 1))} />
                      </PaginationItem>
                      {visiblePages.map((visiblePage, index) =>
                        visiblePage === 'ellipsis' ? (
                          <PaginationItem key={`${visiblePage}-${index}`}>
                            <PaginationEllipsis />
                          </PaginationItem>
                        ) : (
                          <PaginationItem key={visiblePage}>
                            <PaginationLink isActive={visiblePage === currentPage} onClick={() => setPage(visiblePage)}>
                              {visiblePage + 1}
                            </PaginationLink>
                          </PaginationItem>
                        ),
                      )}
                      <PaginationItem>
                        <PaginationNext disabled={currentPage >= totalPages - 1} onClick={() => setPage(Math.min(totalPages - 1, currentPage + 1))} />
                      </PaginationItem>
                    </PaginationContent>
                  </Pagination>
                </div>
              ) : null}
            </div>
          )}
        </div>
      ) : null}
    </div>
  );
}

function getHostName(host: Host) {
  return [host.firstName, host.lastName].filter(Boolean).join(' ') || host.email || 'Unnamed host';
}

function getEmployeeName(employee: Employee) {
  return [employee.firstName, employee.lastName].filter(Boolean).join(' ') || employee.email || 'Unnamed employee';
}

function getVisiblePages(totalPages: number, currentPage: number) {
  if (totalPages <= 5) {
    return Array.from({ length: totalPages }, (_, index) => index);
  }

  const pages = new Set([0, totalPages - 1, currentPage - 1, currentPage, currentPage + 1]);
  const sortedPages = [...pages]
    .filter((pageNumber) => pageNumber >= 0 && pageNumber < totalPages)
    .sort((first, second) => first - second);

  return sortedPages.flatMap((pageNumber, index) => {
    const previousPage = sortedPages[index - 1];

    if (previousPage !== undefined && pageNumber - previousPage > 1) {
      return ['ellipsis' as const, pageNumber];
    }

    return [pageNumber];
  });
}
