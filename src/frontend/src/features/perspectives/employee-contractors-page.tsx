import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { Check, ChevronRight } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getLocationLabel, type LocationResponse } from '@/shared/components/location-selector';
import { Badge } from '@/shared/components/ui/badge';
import { buttonVariants } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { cn } from '@/shared/utils/cn';

type CompanyResponse = components['schemas']['CompanyResponse'];
type ContractorJobStatus = components['schemas']['ContractorJobStatus'];
type JobTypeResponse = components['schemas']['JobTypeResponse'];

type ContractorTab = 'jobs' | 'companies';
type CompanyStatusFilter = 'active' | 'all';

const contractorJobStatuses: readonly ContractorJobStatus[] = ['Planned', 'Active', 'Completed', 'Cancelled'];
const contractorEnrollmentRole = 'contractor-enrollment';

export default function EmployeeContractorsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const actorQuery = useCurrentActor();
  const [activeTab, setActiveTab] = useState<ContractorTab>('jobs');
  const [jobQuery, setJobQuery] = useState('');
  const [jobStatuses, setJobStatuses] = useState<ContractorJobStatus[]>(['Planned', 'Active']);
  const [companyQuery, setCompanyQuery] = useState('');
  const [companyStatus, setCompanyStatus] = useState<CompanyStatusFilter>('all');

  const roles = actorQuery.data?.roles ?? [];
  const isEnrollmentRole = roles.includes(contractorEnrollmentRole);

  const jobsQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobQuery, jobStatuses.join(',')],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/jobs', {
        params: {
          query: {
            Query: jobQuery.trim() || undefined,
            CompanyId: undefined,
            JobTypeId: undefined,
            LocationId: undefined,
            PlannedStartAfter: undefined,
            PlannedEndBefore: undefined,
            Status: jobStatuses,
            Page: 0,
            PageSize: 200,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load contractor jobs.');
      }

      return data?.items ?? [];
    },
  });

  const jobsSummaryQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', 'summary'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/jobs', {
        params: {
          query: {
            Query: undefined,
            CompanyId: undefined,
            JobTypeId: undefined,
            LocationId: undefined,
            PlannedStartAfter: undefined,
            PlannedEndBefore: undefined,
            Status: [],
            Page: 0,
            PageSize: 500,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load contractor jobs summary.');
      }

      return data?.items ?? [];
    },
  });

  const companyIds = Array.from(new Set((jobsQuery.data ?? []).map((job) => job.companyId)));
  const jobTypeIds = Array.from(new Set((jobsQuery.data ?? []).map((job) => job.jobTypeId)));
  const locationIds = Array.from(new Set((jobsQuery.data ?? []).map((job) => job.locationId)));

  const jobCompaniesQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', 'companies', companyIds.join(',')],
    enabled: companyIds.length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/companies', {
        params: { query: { Query: undefined, IsActive: undefined, Page: 0, PageSize: 200, ids: companyIds } as never },
      });

      if (error) {
        throw new Error('Could not load contractor companies.');
      }

      return new Map((data?.items ?? []).map((item: CompanyResponse) => [item.id, item]));
    },
  });

  const jobTypesQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', 'job-types', jobTypeIds.join(',')],
    enabled: jobTypeIds.length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/job-types', {
        params: { query: { Query: undefined, IsActive: undefined, Page: 0, PageSize: 200, ids: jobTypeIds } as never },
      });

      if (error) {
        throw new Error('Could not load contractor job types.');
      }

      return new Map((data?.items ?? []).map((item: JobTypeResponse) => [item.id, item]));
    },
  });

  const jobLocationsQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', 'locations', locationIds.join(',')],
    enabled: locationIds.length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/locations', {
        params: { query: { ids: locationIds } },
      });

      if (error) {
        throw new Error('Could not load job locations.');
      }

      return new Map((data ?? []).map((item: LocationResponse) => [item.id, item]));
    },
  });

  const companiesQuery = useQuery({
    queryKey: ['employee', 'contractors', 'companies', companyQuery, companyStatus, isEnrollmentRole],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/companies', {
        params: {
          query: {
            Query: companyQuery.trim() || undefined,
            IsActive: isEnrollmentRole ? companyStatus === 'active' ? true : undefined : true,
            Page: 0,
            PageSize: 200,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load contractor companies.');
      }

      return data?.items ?? [];
    },
  });

  const companiesSummaryQuery = useQuery({
    queryKey: ['employee', 'contractors', 'companies', 'summary', isEnrollmentRole],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/companies', {
        params: {
          query: {
            Query: undefined,
            IsActive: isEnrollmentRole ? undefined : true,
            Page: 0,
            PageSize: 500,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load contractor companies summary.');
      }

      return data?.items ?? [];
    },
  });

  const jobsLookupLoading = jobCompaniesQuery.isLoading || jobTypesQuery.isLoading || jobLocationsQuery.isLoading;
  const jobsLookupError = jobCompaniesQuery.isError || jobTypesQuery.isError || jobLocationsQuery.isError;
  const jobs = jobsQuery.data ?? [];
  const companies = companiesQuery.data ?? [];
  const jobsSummary = jobsSummaryQuery.data ?? [];
  const companiesSummary = companiesSummaryQuery.data ?? [];
  const activeJobsCount = jobsSummary.filter((job) => job.status === 'Active').length;
  const plannedJobsCount = jobsSummary.filter((job) => job.status === 'Planned').length;
  const activeCompaniesCount = companiesSummary.filter((company) => company.isActive).length;
  const activeJobAssignmentsCount = jobsSummary.filter((job) => job.status === 'Active').reduce((total, job) => total + Number(job.assignmentCount), 0);

  return (
    <section className="grid gap-6">
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <OverviewStatCard label="Active Jobs" value={String(activeJobsCount)} detail="Currently running jobs" />
        <OverviewStatCard label="Planned Jobs" value={String(plannedJobsCount)} detail="Upcoming contractor jobs" />
        <OverviewStatCard label="Active Companies" value={String(activeCompaniesCount)} detail="Companies available in workspace" />
        <OverviewStatCard label="Assignments On Active Jobs" value={String(activeJobAssignmentsCount)} detail="Total active-job assignments" />
      </div>

      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as ContractorTab)}>
        <TabsList>
          <TabsTrigger value="jobs">{t('perspectives.employee.contractors.tabs.jobs')}</TabsTrigger>
          <TabsTrigger value="companies">{t('perspectives.employee.contractors.tabs.companies')}</TabsTrigger>
        </TabsList>

        <TabsContent value="jobs" className="grid gap-6">
          <Card>
            <CardHeader>
              <CardTitle>{t('perspectives.employee.contractors.jobs.title')}</CardTitle>
              <CardDescription>{t('perspectives.employee.contractors.jobs.description')}</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4">
              <div className="grid gap-4">
                <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
                  <div className="grid gap-2 md:min-w-0 md:flex-1 md:max-w-xl">
                    <label className="text-[14px] font-medium" htmlFor="contractor-job-query">{t('perspectives.employee.contractors.filters.search')}</label>
                    <Input id="contractor-job-query" value={jobQuery} onChange={(event) => setJobQuery(event.target.value)} placeholder={t('perspectives.employee.contractors.jobs.searchPlaceholder')} />
                  </div>
                  <Link to="/employee/contractors/jobs/new" className={buttonVariants()}>{t('perspectives.employee.contractors.jobs.new')}</Link>
                </div>

                <div className="grid gap-2">
                  <span className="text-[14px] font-medium">{t('perspectives.employee.contractors.filters.status')}</span>
                  <div className="flex flex-wrap gap-2">
                    {contractorJobStatuses.map((status) => {
                      const isActive = jobStatuses.includes(status);

                      return (
                        <button
                          key={status}
                          type="button"
                          className={cn(
                            'inline-flex items-center rounded-interactive border px-3 py-2 text-[13px] font-semibold transition',
                            isActive
                              ? 'border-primary/20 bg-active-blue text-primary'
                              : 'border-border bg-content text-muted-foreground hover:bg-hover-blue hover:text-foreground',
                          )}
                          onClick={() => setJobStatuses((current) => {
                            if (current.includes(status)) {
                              return current.length === 1 ? current : current.filter((item) => item !== status);
                            }

                            return [...current, status];
                          })}
                        >
                          {isActive ? <Check className="size-3.5" aria-hidden="true" /> : null}
                          {status}
                        </button>
                      );
                    })}
                  </div>
                </div>
              </div>

              {jobsQuery.isLoading || jobsLookupLoading ? <MutedText message={t('perspectives.employee.contractors.jobs.loading')} /> : null}
              {jobsQuery.isError || jobsLookupError ? <ErrorText message={t('perspectives.employee.contractors.jobs.error')} /> : null}
              {!jobsQuery.isLoading && !jobsLookupLoading && !jobsQuery.isError && !jobsLookupError && jobs.length === 0 ? <EmptyText message={t('perspectives.employee.contractors.jobs.empty')} /> : null}

              {!jobsQuery.isLoading && !jobsLookupLoading && !jobsQuery.isError && !jobsLookupError && jobs.length > 0 ? (
                <>
                  <div className="hidden overflow-hidden rounded-structural border border-border lg:block">
                    <table className="min-w-full text-left text-[14px]">
                      <thead className="border-b border-border bg-background/70 text-[11px] uppercase tracking-[0.18em] text-muted-foreground">
                        <tr>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.job')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.plannedStart')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.plannedEnd')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.status')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.assignments')}</th>
                          <th className="px-4 py-3 text-right font-semibold">Open</th>
                        </tr>
                      </thead>
                      <tbody>
                        {jobs.map((job) => {
                          const company = jobCompaniesQuery.data?.get(job.companyId);
                          const jobType = jobTypesQuery.data?.get(job.jobTypeId);
                          const location = jobLocationsQuery.data?.get(job.locationId);

                          return (
                            <tr
                              key={job.id}
                              className="cursor-pointer border-t border-border align-top transition hover:bg-hover-blue/45"
                              role="link"
                              tabIndex={0}
                              onClick={() => void navigate({ to: '/employee/contractors/jobs/$jobId', params: { jobId: job.id } })}
                              onKeyDown={(event) => {
                                if (event.key === 'Enter' || event.key === ' ') {
                                  event.preventDefault();
                                  void navigate({ to: '/employee/contractors/jobs/$jobId', params: { jobId: job.id } });
                                }
                              }}
                            >
                              <td className="px-5 py-5">
                                <Link to="/employee/contractors/jobs/$jobId" params={{ jobId: job.id }} className="font-semibold text-foreground underline-offset-4 hover:underline" onClick={(event) => event.stopPropagation()}>
                                  {job.name}
                                </Link>
                                <p className="mt-1 text-[13px] leading-5 text-muted-foreground">{company?.name ?? t('perspectives.employee.contractors.unknownCompany')} • {jobType?.name ?? t('perspectives.employee.contractors.unknownJobType')} • {getLocationLabel(location)}</p>
                              </td>
                              <td className="px-5 py-5 text-muted-foreground">{formatDateTimeLabel(job.plannedStart)}</td>
                              <td className="px-5 py-5 text-muted-foreground">{formatDateTimeLabel(job.plannedEnd)}</td>
                              <td className="px-5 py-5"><Badge variant={getContractorJobStatusVariant(job.status)}>{job.status}</Badge></td>
                              <td className="px-5 py-5"><Badge variant="secondary">{job.assignmentCount} assignment{job.assignmentCount === 1 ? '' : 's'}</Badge></td>
                              <td className="px-5 py-5 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>

                  <div className="grid gap-3 lg:hidden">
                    {jobs.map((job) => {
                      const company = jobCompaniesQuery.data?.get(job.companyId);
                      const jobType = jobTypesQuery.data?.get(job.jobTypeId);
                      const location = jobLocationsQuery.data?.get(job.locationId);

                      return (
                        <div
                          key={job.id}
                          className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)] transition hover:border-primary/20 hover:shadow-[0_14px_34px_rgba(17,24,39,0.1)]"
                          role="button"
                          tabIndex={0}
                          onClick={() => void navigate({ to: '/employee/contractors/jobs/$jobId', params: { jobId: job.id } })}
                          onKeyDown={(event) => {
                            if (event.key === 'Enter' || event.key === ' ') {
                              event.preventDefault();
                              void navigate({ to: '/employee/contractors/jobs/$jobId', params: { jobId: job.id } });
                            }
                          }}
                        >
                          <div className="flex items-start justify-between gap-3">
                            <div>
                              <Link to="/employee/contractors/jobs/$jobId" params={{ jobId: job.id }} className="font-semibold text-foreground underline-offset-4 hover:underline" onClick={(event) => event.stopPropagation()}>
                                {job.name}
                              </Link>
                              <p className="mt-1 text-[13px] leading-5 text-muted-foreground">{company?.name ?? t('perspectives.employee.contractors.unknownCompany')} • {jobType?.name ?? t('perspectives.employee.contractors.unknownJobType')} • {getLocationLabel(location)}</p>
                            </div>
                            <div className="flex items-center gap-3"><Badge variant={getContractorJobStatusVariant(job.status)}>{job.status}</Badge><ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" /></div>
                          </div>

                          <div className="mt-4 flex flex-wrap gap-2">
                            <span className="inline-flex items-center rounded-[10px] border border-border bg-background px-3 py-1 text-[12px] font-medium text-muted-foreground">starts {formatDateTimeLabel(job.plannedStart)}</span>
                            <span className="inline-flex items-center rounded-[10px] border border-border bg-background px-3 py-1 text-[12px] font-medium text-muted-foreground">ends {formatDateTimeLabel(job.plannedEnd)}</span>
                            <Badge variant="secondary">{job.assignmentCount} assignment{job.assignmentCount === 1 ? '' : 's'}</Badge>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </>
              ) : null}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="companies" className="grid gap-6">
          <Card>
            <CardHeader>
              <CardTitle>{t('perspectives.employee.contractors.companies.title')}</CardTitle>
              <CardDescription>{isEnrollmentRole ? t('perspectives.employee.contractors.companies.enrollmentDescription') : t('perspectives.employee.contractors.companies.description')}</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4">
              <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_220px_auto] md:items-end">
                <div className="grid gap-2 md:max-w-md">
                  <label className="text-[14px] font-medium" htmlFor="contractor-company-query">{t('perspectives.employee.contractors.filters.search')}</label>
                  <Input id="contractor-company-query" value={companyQuery} onChange={(event) => setCompanyQuery(event.target.value)} placeholder={t('perspectives.employee.contractors.companies.searchPlaceholder')} />
                </div>
                {isEnrollmentRole ? (
                  <div className="grid gap-2">
                    <label className="text-[14px] font-medium" htmlFor="contractor-company-status">{t('perspectives.employee.contractors.filters.status')}</label>
                    <select
                      id="contractor-company-status"
                      className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20"
                      value={companyStatus}
                      onChange={(event) => setCompanyStatus(event.target.value as CompanyStatusFilter)}
                    >
                      <option value="all">{t('perspectives.employee.contractors.companies.statusFilter.all')}</option>
                      <option value="active">{t('perspectives.employee.contractors.companies.statusFilter.active')}</option>
                    </select>
                  </div>
                ) : null}
                {isEnrollmentRole ? <Link to="/employee/contractors/companies/new" className={buttonVariants()}>{t('perspectives.employee.contractors.companies.new')}</Link> : null}
              </div>

              {companiesQuery.isLoading ? <MutedText message={t('perspectives.employee.contractors.companies.loading')} /> : null}
              {companiesQuery.isError ? <ErrorText message={t('perspectives.employee.contractors.companies.error')} /> : null}
              {!companiesQuery.isLoading && !companiesQuery.isError && companies.length === 0 ? <EmptyText message={t('perspectives.employee.contractors.companies.empty')} /> : null}

              {!companiesQuery.isLoading && !companiesQuery.isError && companies.length > 0 ? (
                <>
                  <div className="hidden overflow-hidden rounded-structural border border-border lg:block">
                    <table className="min-w-full text-left text-[14px]">
                      <thead className="border-b border-border bg-background/70 text-[11px] uppercase tracking-[0.18em] text-muted-foreground">
                        <tr>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.name')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.code')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.companyNumber')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.status')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.updated')}</th>
                          <th className="px-4 py-3 text-right font-semibold">Open</th>
                        </tr>
                      </thead>
                      <tbody>
                        {companies.map((company) => (
                          <tr
                            key={company.id}
                            className="cursor-pointer border-t border-border transition hover:bg-hover-blue/45"
                            role="link"
                            tabIndex={0}
                            onClick={() => void navigate({ to: '/employee/contractors/companies/$companyId', params: { companyId: company.id } })}
                            onKeyDown={(event) => {
                              if (event.key === 'Enter' || event.key === ' ') {
                                event.preventDefault();
                                void navigate({ to: '/employee/contractors/companies/$companyId', params: { companyId: company.id } });
                              }
                            }}
                          >
                            <td className="px-5 py-5 align-top">
                              <div>
                                <Link to="/employee/contractors/companies/$companyId" params={{ companyId: company.id }} className="font-semibold text-foreground underline-offset-4 hover:underline" onClick={(event) => event.stopPropagation()}>
                                  {company.name}
                                </Link>
                                <p className="mt-1 text-[13px] leading-5 text-muted-foreground">{company.code} • {company.companyNumber || t('perspectives.employee.contractors.companies.noCompanyNumber')}</p>
                              </div>
                            </td>
                            <td className="px-5 py-5 text-muted-foreground">{company.code}</td>
                            <td className="px-5 py-5 text-muted-foreground">{company.companyNumber || t('perspectives.employee.contractors.companies.noCompanyNumber')}</td>
                            <td className="px-5 py-5"><Badge variant={company.isActive ? 'success' : 'secondary'}>{company.isActive ? t('perspectives.employee.contractors.companies.active') : t('perspectives.employee.contractors.companies.inactive')}</Badge></td>
                            <td className="px-5 py-5 text-muted-foreground">{formatDateTimeLabel(company.updatedAt)}</td>
                            <td className="px-5 py-5 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>

                  <div className="grid gap-3 lg:hidden">
                    {companies.map((company) => (
                      <div
                        key={company.id}
                        className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)] transition hover:border-primary/20 hover:shadow-[0_14px_34px_rgba(17,24,39,0.1)]"
                        role="button"
                        tabIndex={0}
                        onClick={() => void navigate({ to: '/employee/contractors/companies/$companyId', params: { companyId: company.id } })}
                        onKeyDown={(event) => {
                          if (event.key === 'Enter' || event.key === ' ') {
                            event.preventDefault();
                            void navigate({ to: '/employee/contractors/companies/$companyId', params: { companyId: company.id } });
                          }
                        }}
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <Link to="/employee/contractors/companies/$companyId" params={{ companyId: company.id }} className="font-semibold text-foreground underline-offset-4 hover:underline" onClick={(event) => event.stopPropagation()}>
                              {company.name}
                            </Link>
                            <p className="mt-1 text-[13px] leading-5 text-muted-foreground">{company.code} • {company.companyNumber || t('perspectives.employee.contractors.companies.noCompanyNumber')}</p>
                          </div>
                          <div className="flex items-center gap-3"><Badge variant={company.isActive ? 'success' : 'secondary'}>{company.isActive ? t('perspectives.employee.contractors.companies.active') : t('perspectives.employee.contractors.companies.inactive')}</Badge><ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" /></div>
                        </div>
                        <div className="mt-4 flex flex-wrap gap-2">
                          <span className="inline-flex items-center rounded-[10px] border border-border bg-background px-3 py-1 text-[12px] font-medium text-muted-foreground">updated {formatDateTimeLabel(company.updatedAt)}</span>
                        </div>
                      </div>
                    ))}
                  </div>
                </>
              ) : null}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </section>
  );
}

function getContractorJobStatusVariant(status: ContractorJobStatus) {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Cancelled':
      return 'error';
    case 'Completed':
      return 'outline';
    default:
      return 'secondary';
  }
}

function formatDateTimeLabel(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function OverviewStatCard({ label, value, detail }: { label: string; value: string; detail: string }) {
  return (
    <div className="rounded-structural border border-border bg-content px-4 py-4 shadow-sm">
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{label}</p>
      <p className="mt-2 text-[32px] font-semibold tracking-tight text-primary">{value}</p>
      <p className="mt-1.5 text-[13px] leading-5 text-muted-foreground">{detail}</p>
    </div>
  );
}

function ErrorText({ message }: { message: string }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{message}</p>;
}

function MutedText({ message }: { message: string }) {
  return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{message}</p>;
}

function EmptyText({ message }: { message: string }) {
  return <p className="rounded-structural border border-dashed border-border bg-content p-6 text-[14px] text-muted-foreground">{message}</p>;
}
