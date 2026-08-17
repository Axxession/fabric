import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
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

type CompanyResponse = components['schemas']['CompanyResponse'];
type ContractorJobStatus = components['schemas']['ContractorJobStatus'];
type JobTypeResponse = components['schemas']['JobTypeResponse'];

type ContractorTab = 'jobs' | 'companies';
type CompanyStatusFilter = 'active' | 'all';

const contractorJobStatuses: readonly ContractorJobStatus[] = ['Planned', 'Active', 'Completed', 'Cancelled'];
const contractorEnrollmentRole = 'contractor-enrollment';

export default function EmployeeContractorsPage() {
  const { t } = useTranslation();
  const actorQuery = useCurrentActor();
  const [activeTab, setActiveTab] = useState<ContractorTab>('jobs');
  const [jobQuery, setJobQuery] = useState('');
  const [jobStatus, setJobStatus] = useState<'all' | ContractorJobStatus>('all');
  const [companyQuery, setCompanyQuery] = useState('');
  const [companyStatus, setCompanyStatus] = useState<CompanyStatusFilter>('all');

  const roles = actorQuery.data?.roles ?? [];
  const isEnrollmentRole = roles.includes(contractorEnrollmentRole);

  const jobsQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobQuery, jobStatus],
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
            Status: jobStatus === 'all' ? [] : [jobStatus],
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

  const jobsLookupLoading = jobCompaniesQuery.isLoading || jobTypesQuery.isLoading || jobLocationsQuery.isLoading;
  const jobsLookupError = jobCompaniesQuery.isError || jobTypesQuery.isError || jobLocationsQuery.isError;
  const jobs = jobsQuery.data ?? [];
  const companies = companiesQuery.data ?? [];

  return (
    <section className="grid gap-6">
      <div className="rounded-structural border border-border bg-content p-6 sm:p-8">
        <p className="text-[14px] font-semibold uppercase text-primary">{t('perspectives.employee.contractors.kicker')}</p>
        <h1 className="mt-3 text-[30px] font-semibold tracking-tight">{t('perspectives.employee.contractors.title')}</h1>
        <p className="mt-3 max-w-3xl text-[14px] leading-6 text-muted-foreground">{t('perspectives.employee.contractors.description')}</p>
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
              <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_220px]">
                <div className="grid gap-2">
                  <label className="text-[14px] font-medium" htmlFor="contractor-job-query">{t('perspectives.employee.contractors.filters.search')}</label>
                  <Input id="contractor-job-query" value={jobQuery} onChange={(event) => setJobQuery(event.target.value)} placeholder={t('perspectives.employee.contractors.jobs.searchPlaceholder')} />
                </div>
                <div className="grid gap-2">
                  <label className="text-[14px] font-medium" htmlFor="contractor-job-status">{t('perspectives.employee.contractors.filters.status')}</label>
                  <select
                    id="contractor-job-status"
                    className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20"
                    value={jobStatus}
                    onChange={(event) => setJobStatus(event.target.value as 'all' | ContractorJobStatus)}
                  >
                    <option value="all">{t('perspectives.employee.contractors.jobs.allStatuses')}</option>
                    {contractorJobStatuses.map((status) => <option key={status} value={status}>{status}</option>)}
                  </select>
                </div>
              </div>

              <div className="flex justify-end">
                <Link to="/employee/contractors/jobs/new" className={buttonVariants()}>{t('perspectives.employee.contractors.jobs.new')}</Link>
              </div>

              {jobsQuery.isLoading || jobsLookupLoading ? <MutedText message={t('perspectives.employee.contractors.jobs.loading')} /> : null}
              {jobsQuery.isError || jobsLookupError ? <ErrorText message={t('perspectives.employee.contractors.jobs.error')} /> : null}
              {!jobsQuery.isLoading && !jobsLookupLoading && !jobsQuery.isError && !jobsLookupError && jobs.length === 0 ? <EmptyText message={t('perspectives.employee.contractors.jobs.empty')} /> : null}

              {!jobsQuery.isLoading && !jobsLookupLoading && !jobsQuery.isError && !jobsLookupError && jobs.length > 0 ? (
                <>
                  <div className="hidden overflow-hidden rounded-structural border border-border lg:block">
                    <table className="min-w-full text-left text-[14px]">
                      <thead className="bg-muted/40 text-muted-foreground">
                        <tr>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.job')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.company')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.jobType')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.location')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.plannedStart')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.plannedEnd')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.status')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.jobs.columns.assignments')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {jobs.map((job) => {
                          const company = jobCompaniesQuery.data?.get(job.companyId);
                          const jobType = jobTypesQuery.data?.get(job.jobTypeId);
                          const location = jobLocationsQuery.data?.get(job.locationId);

                          return (
                            <tr key={job.id} className="border-t border-border align-top">
                              <td className="px-4 py-4">
                                <Link to="/employee/contractors/jobs/$jobId" params={{ jobId: job.id }} className="font-medium text-foreground underline-offset-4 hover:underline">
                                  {job.name}
                                </Link>
                                <p className="mt-1 text-[13px] text-muted-foreground">{job.description || t('perspectives.employee.contractors.jobs.noDescription')}</p>
                              </td>
                              <td className="px-4 py-4 text-muted-foreground">{company?.name ?? t('perspectives.employee.contractors.unknownCompany')}</td>
                              <td className="px-4 py-4 text-muted-foreground">{jobType?.name ?? t('perspectives.employee.contractors.unknownJobType')}</td>
                              <td className="px-4 py-4 text-muted-foreground">{getLocationLabel(location)}</td>
                              <td className="px-4 py-4 text-muted-foreground">{formatDateTimeLabel(job.plannedStart)}</td>
                              <td className="px-4 py-4 text-muted-foreground">{formatDateTimeLabel(job.plannedEnd)}</td>
                              <td className="px-4 py-4"><Badge variant={getContractorJobStatusVariant(job.status)}>{job.status}</Badge></td>
                              <td className="px-4 py-4 text-muted-foreground">{job.assignmentCount}</td>
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
                        <div key={job.id} className="rounded-structural border border-border p-4">
                          <div className="flex items-start justify-between gap-3">
                            <div>
                              <Link to="/employee/contractors/jobs/$jobId" params={{ jobId: job.id }} className="font-medium text-foreground underline-offset-4 hover:underline">
                                {job.name}
                              </Link>
                              <p className="mt-1 text-[13px] text-muted-foreground">{job.description || t('perspectives.employee.contractors.jobs.noDescription')}</p>
                            </div>
                            <Badge variant={getContractorJobStatusVariant(job.status)}>{job.status}</Badge>
                          </div>

                          <dl className="mt-4 grid gap-2 text-[13px] text-muted-foreground">
                            <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.jobs.columns.company')}</dt><dd className="text-right text-foreground">{company?.name ?? t('perspectives.employee.contractors.unknownCompany')}</dd></div>
                            <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.jobs.columns.jobType')}</dt><dd className="text-right text-foreground">{jobType?.name ?? t('perspectives.employee.contractors.unknownJobType')}</dd></div>
                            <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.jobs.columns.location')}</dt><dd className="text-right text-foreground">{getLocationLabel(location)}</dd></div>
                            <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.jobs.columns.plannedStart')}</dt><dd className="text-right text-foreground">{formatDateTimeLabel(job.plannedStart)}</dd></div>
                            <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.jobs.columns.plannedEnd')}</dt><dd className="text-right text-foreground">{formatDateTimeLabel(job.plannedEnd)}</dd></div>
                            <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.jobs.columns.assignments')}</dt><dd className="text-right text-foreground">{job.assignmentCount}</dd></div>
                          </dl>
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
                      <thead className="bg-muted/40 text-muted-foreground">
                        <tr>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.name')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.code')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.companyNumber')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.status')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.updated')}</th>
                          <th className="px-4 py-3 font-semibold">{t('perspectives.employee.contractors.companies.columns.actions')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {companies.map((company) => (
                          <tr key={company.id} className="border-t border-border">
                            <td className="px-4 py-4 font-medium text-foreground">{company.name}</td>
                            <td className="px-4 py-4 text-muted-foreground">{company.code}</td>
                            <td className="px-4 py-4 text-muted-foreground">{company.companyNumber || t('perspectives.employee.contractors.companies.noCompanyNumber')}</td>
                            <td className="px-4 py-4"><Badge variant={company.isActive ? 'success' : 'secondary'}>{company.isActive ? t('perspectives.employee.contractors.companies.active') : t('perspectives.employee.contractors.companies.inactive')}</Badge></td>
                            <td className="px-4 py-4 text-muted-foreground">{formatDateTimeLabel(company.updatedAt)}</td>
                            <td className="px-4 py-4">
                              <Link to="/employee/contractors/companies/$companyId" params={{ companyId: company.id }} className={buttonVariants({ variant: 'outline' })}>
                                {isEnrollmentRole ? t('perspectives.employee.contractors.companies.edit') : t('perspectives.employee.contractors.companies.open')}
                              </Link>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>

                  <div className="grid gap-3 lg:hidden">
                    {companies.map((company) => (
                      <div key={company.id} className="rounded-structural border border-border p-4">
                        <p className="font-medium text-foreground">{company.name}</p>
                        <dl className="mt-3 grid gap-2 text-[13px] text-muted-foreground">
                          <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.companies.columns.code')}</dt><dd className="text-right text-foreground">{company.code}</dd></div>
                          <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.companies.columns.companyNumber')}</dt><dd className="text-right text-foreground">{company.companyNumber || t('perspectives.employee.contractors.companies.noCompanyNumber')}</dd></div>
                          <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.companies.columns.status')}</dt><dd className="text-right"><Badge variant={company.isActive ? 'success' : 'secondary'}>{company.isActive ? t('perspectives.employee.contractors.companies.active') : t('perspectives.employee.contractors.companies.inactive')}</Badge></dd></div>
                          <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.companies.columns.updated')}</dt><dd className="text-right text-foreground">{formatDateTimeLabel(company.updatedAt)}</dd></div>
                        </dl>
                        <div className="mt-4 flex flex-wrap gap-2">
                          <Link to="/employee/contractors/companies/$companyId" params={{ companyId: company.id }} className={buttonVariants({ variant: 'outline' })}>
                            {isEnrollmentRole ? t('perspectives.employee.contractors.companies.edit') : t('perspectives.employee.contractors.companies.open')}
                          </Link>
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

function ErrorText({ message }: { message: string }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{message}</p>;
}

function MutedText({ message }: { message: string }) {
  return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{message}</p>;
}

function EmptyText({ message }: { message: string }) {
  return <p className="rounded-structural border border-dashed border-border bg-content p-6 text-[14px] text-muted-foreground">{message}</p>;
}
