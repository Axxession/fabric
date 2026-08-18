import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { Check, ChevronRight } from 'lucide-react';
import { useState } from 'react';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { buttonVariants } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { cn } from '@/shared/utils/cn';

type EmployeeResponse = components['schemas']['EmployeeResponse'];
type EmployeeStatus = components['schemas']['EmployeeStatus'];

const pageSize = 10;
const employeeStatuses: readonly EmployeeStatus[] = ['PreHire', 'Active', 'Leave', 'Suspended', 'Terminated', 'Archived'];

export default function ManagerMyTeamPage() {
  const actorQuery = useCurrentActor();
  const navigate = useNavigate();

  const [page, setPage] = useState(0);
  const [query, setQuery] = useState('');
  const [statuses, setStatuses] = useState<EmployeeStatus[]>(['Active']);
  const [includeIndirectReports, setIncludeIndirectReports] = useState(false);

  const managerEmployeeId = actorQuery.data?.employeeId ?? null;

  const teamQuery = useQuery({
    queryKey: ['manager', 'my-team', managerEmployeeId, page, query, statuses.join(','), includeIndirectReports],
    enabled: actorQuery.data?.isManager === true && Boolean(managerEmployeeId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees', {
        params: {
          query: {
            Query: query || undefined,
            Status: statuses,
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
        <div className="grid gap-4">
          <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
            <div className="grid gap-2 md:min-w-0 md:flex-1 md:max-w-xl">
              <label className="text-[14px] font-medium" htmlFor="manager-team-query">Search employees</label>
              <Input
                id="manager-team-query"
                value={query}
                onChange={(event) => {
                  setQuery(event.target.value);
                  setPage(0);
                }}
                placeholder="Search name, email, or employee number"
              />
            </div>
            <button
              type="button"
              className={cn(
                'inline-flex items-center rounded-interactive border px-3 py-2 text-[13px] font-semibold transition',
                includeIndirectReports
                  ? 'border-primary/20 bg-active-blue text-primary'
                  : 'border-border bg-content text-muted-foreground hover:bg-hover-blue hover:text-foreground',
              )}
              onClick={() => {
                setIncludeIndirectReports((current) => !current);
                setPage(0);
              }}
            >
              {includeIndirectReports ? <Check className="size-3.5" aria-hidden="true" /> : null}
              Show indirect reports
            </button>
          </div>

          <div className="grid gap-2">
            <span className="text-[14px] font-medium">Status</span>
            <div className="flex flex-wrap gap-2">
              {employeeStatuses.map((value) => {
                const isActive = statuses.includes(value);

                return (
                  <button
                    key={value}
                    type="button"
                    className={cn(
                      'inline-flex items-center rounded-interactive border px-3 py-2 text-[13px] font-semibold transition',
                      isActive
                        ? 'border-primary/20 bg-active-blue text-primary'
                        : 'border-border bg-content text-muted-foreground hover:bg-hover-blue hover:text-foreground',
                    )}
                    onClick={() => {
                      setStatuses((current) => {
                        if (current.includes(value)) {
                          return current.length === 1 ? current : current.filter((item) => item !== value);
                        }

                        return [...current, value];
                      });
                      setPage(0);
                    }}
                  >
                    {isActive ? <Check className="size-3.5" aria-hidden="true" /> : null}
                    {value}
                  </button>
                );
              })}
            </div>
          </div>
        </div>
      </Card>

      {teamQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load team.</p> : null}

      <Card className="p-4 sm:p-5">
        {teamQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading team...</p> : null}
        {!teamQuery.isLoading && employees.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No employees found for the selected reporting scope.</p> : null}

        {employees.length > 0 ? (
          <>
            <div className="hidden overflow-x-auto md:block">
              <table className="w-full min-w-[68rem] border-collapse text-left text-[14px]">
                <thead className="border-b border-border bg-background/70 text-[11px] uppercase tracking-[0.18em] text-muted-foreground">
                  <tr>
                    <th className="px-5 py-4 font-semibold">Name</th>
                    <th className="px-5 py-4 font-semibold">Reporting Level</th>
                    <th className="px-5 py-4 font-semibold">Organizational Unit</th>
                    <th className="px-5 py-4 font-semibold">Job Title</th>
                    <th className="px-5 py-4 font-semibold">Status</th>
                    <th className="px-5 py-4 text-right font-semibold">Open</th>
                  </tr>
                </thead>
                <tbody>
                  {employees.map((employee) => (
                    <tr
                      key={employee.id}
                      className="cursor-pointer border-t border-border transition hover:bg-hover-blue/45"
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
                      <td className="px-5 py-5 align-top">
                        <div>
                          <p className="font-semibold text-foreground">{employee.firstName} {employee.lastName}</p>
                          <p className="mt-1 text-[13px] text-muted-foreground">{employee.email ?? '-'}</p>
                        </div>
                      </td>
                      <td className="px-5 py-5 align-top"><Badge variant="outline">{getReportingLevel(employee, managerEmployeeId)}</Badge></td>
                      <td className="px-5 py-5 align-top text-muted-foreground">{employee.organizationUnit?.name ?? '-'}</td>
                      <td className="px-5 py-5 align-top text-muted-foreground">{employee.jobTitle ?? '-'}</td>
                      <td className="px-5 py-5 align-top"><Badge variant={getEmployeeStatusVariant(employee.status)}>{employee.status}</Badge></td>
                      <td className="px-5 py-5 align-top text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="grid gap-3 md:hidden">
              {employees.map((employee) => (
                <article
                  key={employee.id}
                  className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)] transition hover:border-primary/20 hover:shadow-[0_14px_34px_rgba(17,24,39,0.1)]"
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
                      <p className="font-semibold text-foreground">{employee.firstName} {employee.lastName}</p>
                      <p className="mt-1 text-[13px] text-muted-foreground">{employee.email ?? '-'}</p>
                    </div>
                    <div className="flex items-center gap-3"><Badge variant={getEmployeeStatusVariant(employee.status)}>{employee.status}</Badge><ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" /></div>
                  </div>
                  <div className="mt-4 flex flex-wrap gap-2">
                    <Badge variant="outline">{getReportingLevel(employee, managerEmployeeId)}</Badge>
                    {employee.organizationUnit?.name ? <span className="inline-flex items-center rounded-[10px] border border-border bg-background px-3 py-1 text-[12px] font-medium text-muted-foreground">{employee.organizationUnit.name}</span> : null}
                    {employee.jobTitle ? <span className="inline-flex items-center rounded-[10px] border border-border bg-background px-3 py-1 text-[12px] font-medium text-muted-foreground">{employee.jobTitle}</span> : null}
                  </div>
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

function getEmployeeStatusVariant(status: EmployeeStatus) {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Leave':
    case 'Suspended':
    case 'PreHire':
      return 'secondary';
    case 'Terminated':
    case 'Archived':
      return 'error';
  }
}
