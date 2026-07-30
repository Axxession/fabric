import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { ChevronRight } from 'lucide-react';
import { useState } from 'react';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { buttonVariants } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

type EmployeeResponse = components['schemas']['EmployeeResponse'];
type EmployeeStatus = components['schemas']['EmployeeStatus'];

const pageSize = 10;
const employeeStatuses: readonly EmployeeStatus[] = ['PreHire', 'Active', 'Leave', 'Suspended', 'Terminated', 'Archived'];

export default function ManagerMyTeamPage() {
  const actorQuery = useCurrentActor();
  const navigate = useNavigate();

  const [page, setPage] = useState(0);
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState<'all' | EmployeeStatus>('all');
  const [includeIndirectReports, setIncludeIndirectReports] = useState(false);

  const managerEmployeeId = actorQuery.data?.employeeId ?? null;

  const teamQuery = useQuery({
    queryKey: ['manager', 'my-team', managerEmployeeId, page, query, status, includeIndirectReports],
    enabled: actorQuery.data?.isManager === true && Boolean(managerEmployeeId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees', {
        params: {
          query: {
            Query: query || undefined,
            Status: status === 'all' ? [] : [status],
            OrganizationUnitId: undefined,
            ManagerEmployeeId: managerEmployeeId ?? undefined,
            IncludeIndirectReports: includeIndirectReports,
            IncludeDescendants: true,
            Page: page,
            PageSize: pageSize,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load team.');
      }

      return data;
    },
  });

  const employees = teamQuery.data?.items ?? [];
  const totalItems = Number(teamQuery.data?.totalItems ?? employees.length);
  const totalPages = Math.max(Number(teamQuery.data?.totalPages ?? 1), 1);
  const currentPage = Math.min(Number(teamQuery.data?.currentPage ?? page), totalPages - 1);
  const firstItem = totalItems === 0 ? 0 : currentPage * pageSize + 1;
  const lastItem = Math.min((currentPage + 1) * pageSize, totalItems);

  function openEmployee(employeeId: string) {
    void navigate({ to: '/manager/my-team/$employeeId', params: { employeeId } });
  }

  return (
    <section className="grid gap-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-[28px] font-semibold tracking-tight">My Team</h1>
          <p className="mt-2 max-w-3xl text-[14px] text-muted-foreground">View the employees who report to you, then open an employee to review assigned packages and credentials.</p>
        </div>
        <Link to="/manager" className={buttonVariants({ variant: 'outline' })}>Back to Overview</Link>
      </div>

      {actorQuery.data && !actorQuery.data.isManager ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Manager access is required to view this page.</p> : null}
      {actorQuery.data?.isManager && !managerEmployeeId ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Your actor is missing an employee record.</p> : null}

      <Card className="p-4 sm:p-6">
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-[minmax(0,1fr)_14rem]">
          <label className="grid gap-2 text-[14px] font-medium">
            <span>Search employees</span>
            <input
              className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary"
              value={query}
              onChange={(event) => {
                setQuery(event.target.value);
                setPage(0);
              }}
              placeholder="Search name, email, or employee number"
            />
          </label>

          <label className="grid gap-2 text-[14px] font-medium">
            <span>Status</span>
            <select
              className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary"
              value={status}
              onChange={(event) => {
                setStatus(event.target.value as 'all' | EmployeeStatus);
                setPage(0);
              }}
            >
              <option value="all">All statuses</option>
              {employeeStatuses.map((value) => <option key={value} value={value}>{value}</option>)}
            </select>
          </label>

          <label className="flex items-center gap-3 rounded-structural border border-border px-4 py-3 text-[14px] font-medium xl:col-span-2">
            <input
              type="checkbox"
              className="size-4 rounded border border-border"
              checked={includeIndirectReports}
              onChange={(event) => {
                setIncludeIndirectReports(event.target.checked);
                setPage(0);
              }}
            />
            <span>{includeIndirectReports ? 'Include indirect reports' : 'Direct reports only'}</span>
          </label>
        </div>
      </Card>

      {teamQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load team.</p> : null}

      <Card className="p-4 sm:p-5">
        {teamQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading team...</p> : null}
        {!teamQuery.isLoading && employees.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No employees found for the selected reporting scope.</p> : null}

        {employees.length > 0 ? (
          <>
            <div className="hidden overflow-x-auto md:block">
              <table className="w-full min-w-[72rem] border-collapse text-left text-[14px]">
                <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-semibold">Name</th>
                    <th className="px-4 py-3 font-semibold">Reporting Level</th>
                    <th className="px-4 py-3 font-semibold">Email</th>
                    <th className="px-4 py-3 font-semibold">Organizational Unit</th>
                    <th className="px-4 py-3 font-semibold">Job Title</th>
                    <th className="px-4 py-3 font-semibold">Status</th>
                    <th className="px-4 py-3 text-right font-semibold">Open</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {employees.map((employee) => (
                    <tr
                      key={employee.id}
                      className="cursor-pointer transition hover:bg-hover-blue"
                      role="link"
                      tabIndex={0}
                      onClick={() => openEmployee(employee.id)}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' || event.key === ' ') {
                          event.preventDefault();
                          openEmployee(employee.id);
                        }
                      }}
                    >
                      <td className="px-4 py-4 font-medium text-foreground">{employee.firstName} {employee.lastName}</td>
                      <td className="px-4 py-4 text-muted-foreground">{getReportingLevel(employee, managerEmployeeId)}</td>
                      <td className="px-4 py-4 text-muted-foreground">{employee.email ?? '-'}</td>
                      <td className="px-4 py-4 text-muted-foreground">{employee.organizationUnit?.name ?? '-'}</td>
                      <td className="px-4 py-4 text-muted-foreground">{employee.jobTitle ?? '-'}</td>
                      <td className="px-4 py-4 text-muted-foreground">{employee.status}</td>
                      <td className="px-4 py-4 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="grid gap-3 md:hidden">
              {employees.map((employee) => (
                <article
                  key={employee.id}
                  className="rounded-structural border border-border p-4 transition hover:bg-hover-blue"
                  role="button"
                  tabIndex={0}
                  onClick={() => openEmployee(employee.id)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' || event.key === ' ') {
                      event.preventDefault();
                      openEmployee(employee.id);
                    }
                  }}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="font-medium text-foreground">{employee.firstName} {employee.lastName}</p>
                      <p className="mt-1 text-[13px] text-muted-foreground">{getReportingLevel(employee, managerEmployeeId)}</p>
                    </div>
                    <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
                  </div>
                  <dl className="mt-4 grid gap-2 text-[14px]">
                    <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Email</dt><dd className="text-right text-foreground">{employee.email ?? '-'}</dd></div>
                    <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Organizational Unit</dt><dd className="text-right text-foreground">{employee.organizationUnit?.name ?? '-'}</dd></div>
                    <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Job Title</dt><dd className="text-right text-foreground">{employee.jobTitle ?? '-'}</dd></div>
                    <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Status</dt><dd className="text-right text-foreground">{employee.status}</dd></div>
                  </dl>
                </article>
              ))}
            </div>
          </>
        ) : null}
      </Card>

      {!teamQuery.isLoading && totalItems > 0 ? (
        <div className="flex flex-col gap-3 text-[14px] text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
          <p>Showing {firstItem}-{lastItem} of {totalItems}</p>
          <div className="flex items-center gap-2">
            <button
              type="button"
              className="rounded-interactive border border-border px-3 py-2 transition disabled:cursor-not-allowed disabled:opacity-50"
              disabled={currentPage === 0}
              onClick={() => setPage(Math.max(0, currentPage - 1))}
            >
              Previous
            </button>
            <span>Page {currentPage + 1} of {totalPages}</span>
            <button
              type="button"
              className="rounded-interactive border border-border px-3 py-2 transition disabled:cursor-not-allowed disabled:opacity-50"
              disabled={currentPage >= totalPages - 1}
              onClick={() => setPage(Math.min(totalPages - 1, currentPage + 1))}
            >
              Next
            </button>
          </div>
        </div>
      ) : null}
    </section>
  );
}

function getReportingLevel(employee: EmployeeResponse, managerEmployeeId: string | null) {
  return employee.managerEmployeeId === managerEmployeeId ? 'Direct' : 'Indirect';
}
