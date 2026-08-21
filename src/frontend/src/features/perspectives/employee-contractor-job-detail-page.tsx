import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft, ChevronRight } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getGrantComplianceLabel, getGrantComplianceVariant } from '@/shared/access-grants/grant-status';
import { getLocationLabel, LocationSelector, type LocationResponse } from '@/shared/components/location-selector';
import { Badge } from '@/shared/components/ui/badge';
import { buttonVariants, Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { Textarea } from '@/shared/components/ui/textarea';
import { cn } from '@/shared/utils/cn';

import { DetailRow, EmptyText, ErrorText, formatDateTimeLabel, getAssignmentStatusVariant, getContractorJobFormState, getContractorJobStatusVariant, invalidateContractorJobQueries, MutedText, toApiDateTimeValue, type ContractorJobFormState } from './employee-contractor-job-shared';

type CompanyResponse = components['schemas']['CompanyResponse'];
type GrantComplianceSummaryResponse = components['schemas']['GrantComplianceSummaryResponse'];
type ContractorJobAssignmentResponse = components['schemas']['ContractorJobAssignmentResponse'];
type ContractorResponse = components['schemas']['ContractorResponse'];
type JobTypeResponse = components['schemas']['JobTypeResponse'];
type UpdateContractorJobRequest = components['schemas']['UpdateContractorJobRequest'];

export default function EmployeeContractorJobDetailPage() {
  const { t } = useTranslation();
  const { jobId } = useParams({ from: '/main/employee/contractors/jobs/$jobId' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<ContractorJobFormState>(getContractorJobFormStatePlaceholder());
  const [activeTab, setActiveTab] = useState<'edit' | 'assignments'>('edit');

  const jobQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobId, 'detail'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/jobs/{id}', { params: { path: { id: jobId } } });

      if (error || !data) {
        throw new Error('Could not load contractor job.');
      }

      return data;
    },
  });

  const assignmentsQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobId, 'assignments'],
    enabled: Boolean(jobQuery.data),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/jobs/{contractorJobId}/assignments', {
        params: { path: { contractorJobId: jobId }, query: { ContractorId: undefined, Status: [], AssignedAfter: undefined, AssignedBefore: undefined, Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load contractor assignments.');
      }

      return data?.items ?? [];
    },
  });

  const companiesQuery = useQuery({
    queryKey: ['employee', 'contractors', 'job-form', 'companies'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/companies', {
        params: { query: { Query: undefined, IsActive: undefined, Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load contractor companies.');
      }

      return new Map((data?.items ?? []).map((item: CompanyResponse) => [item.id, item]));
    },
  });

  const jobTypesQuery = useQuery({
    queryKey: ['employee', 'contractors', 'job-form', 'job-types'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/job-types', {
        params: { query: { Query: undefined, IsActive: undefined, Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load contractor job types.');
      }

      return new Map((data?.items ?? []).map((item: JobTypeResponse) => [item.id, item]));
    },
  });

  const locationQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobId, 'location', jobQuery.data?.locationId],
    enabled: Boolean(jobQuery.data?.locationId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/locations', { params: { query: { ids: [jobQuery.data?.locationId ?? ''] } } });
      if (error) {
        throw new Error('Could not load job location.');
      }

      return data?.[0] ?? null;
    },
  });

  const contractorIds = Array.from(new Set((assignmentsQuery.data ?? []).map((assignment) => assignment.contractorId)));
  const assignments = assignmentsQuery.data ?? [];
  const assignmentComplianceQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobId, 'assignments', 'grant-compliance', assignments.map((assignment) => assignment.id).join(',')],
    enabled: assignments.length > 0,
    queryFn: async () => {
      const { data, error } = await api.POST('/api/access-catalog/access-grants/grant-compliance-summaries/by-source', {
        body: assignments.map((assignment) => ({ sourceKind: 'ContractorJob', sourceId: assignment.id })),
      });

      if (error) {
        throw new Error('Could not load assignment grant compliance.');
      }

      return new Map((data ?? []).map((item: GrantComplianceSummaryResponse) => [item.sourceId, item]));
    },
  });

  const contractorsQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobId, 'contractors', contractorIds.join(',')],
    enabled: contractorIds.length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/contractors', {
        params: { query: { Query: undefined, CompanyId: undefined, IdentityId: undefined, IsArchived: undefined, Page: 0, PageSize: 200, ids: contractorIds } as never },
      });

      if (error) {
        throw new Error('Could not load contractors.');
      }

      return new Map((data?.items ?? []).map((item: ContractorResponse) => [item.id, item]));
    },
  });

  const saveJob = useMutation({
    mutationFn: async (request: UpdateContractorJobRequest) => {
      const { data, error } = await api.PUT('/api/contractors/jobs/{id}', { params: { path: { id: jobId } }, body: request });
      if (error || !data) {
        throw new Error('Could not save contractor job.');
      }

      return data;
    },
    onSuccess: async (job) => {
      toast.success(t('perspectives.employee.contractors.jobs.saved'));
      setForm(getContractorJobFormState(job));
      await invalidateContractorJobQueries(queryClient, jobId);
    },
    onError: () => toast.error(t('perspectives.employee.contractors.jobs.saveError')),
  });

  const setJobStatus = useMutation({
    mutationFn: async (status: 'activate' | 'complete' | 'cancel') => {
      const response = status === 'activate'
        ? await api.POST('/api/contractors/jobs/{id}/activate', { params: { path: { id: jobId } } })
        : status === 'complete'
          ? await api.POST('/api/contractors/jobs/{id}/complete', { params: { path: { id: jobId } } })
          : await api.POST('/api/contractors/jobs/{id}/cancel', { params: { path: { id: jobId } } });

      const { data, error } = response;
      if (error || !data) {
        throw new Error(`Could not ${status} contractor job.`);
      }

      return { job: data, status };
    },
    onSuccess: async ({ job, status }) => {
      const messages = {
        activate: t('perspectives.employee.contractors.jobs.activated'),
        complete: t('perspectives.employee.contractors.jobs.completed'),
        cancel: t('perspectives.employee.contractors.jobs.cancelled'),
      } as const;
      toast.success(messages[status]);
      setForm(getContractorJobFormState(job));
      await invalidateContractorJobQueries(queryClient, jobId);
    },
    onError: (_, status) => {
      const messages = {
        activate: t('perspectives.employee.contractors.jobs.activateError'),
        complete: t('perspectives.employee.contractors.jobs.completeError'),
        cancel: t('perspectives.employee.contractors.jobs.cancelError'),
      } as const;
      toast.error(messages[status]);
    },
  });

  const isLoading = jobQuery.isLoading || assignmentsQuery.isLoading || assignmentComplianceQuery.isLoading || companiesQuery.isLoading || jobTypesQuery.isLoading || locationQuery.isLoading || contractorsQuery.isLoading;
  const isError = jobQuery.isError || assignmentsQuery.isError || assignmentComplianceQuery.isError || companiesQuery.isError || jobTypesQuery.isError || locationQuery.isError || contractorsQuery.isError;
  const job = jobQuery.data;

  useEffect(() => {
    if (!job) {
      return;
    }

    setForm(getContractorJobFormState(job));
  }, [job]);

  function submit() {
    if (!form.companyId) {
      toast.error(t('perspectives.employee.contractors.jobs.validation.companyRequired'));
      return;
    }

    if (!form.jobTypeId) {
      toast.error(t('perspectives.employee.contractors.jobs.validation.jobTypeRequired'));
      return;
    }

    if (!form.locationId) {
      toast.error(t('perspectives.employee.contractors.jobs.validation.locationRequired'));
      return;
    }

    if (!form.name.trim()) {
      toast.error(t('perspectives.employee.contractors.jobs.validation.nameRequired'));
      return;
    }

    if (!form.plannedStart || !form.plannedEnd) {
      toast.error(t('perspectives.employee.contractors.jobs.validation.windowRequired'));
      return;
    }

    if (new Date(form.plannedStart) > new Date(form.plannedEnd)) {
      toast.error(t('perspectives.employee.contractors.jobs.validation.windowOrder'));
      return;
    }

    saveJob.mutate({
      companyId: form.companyId,
      jobTypeId: form.jobTypeId,
      locationId: form.locationId,
      name: form.name.trim(),
      description: form.description.trim() || null,
      plannedStart: toApiDateTimeValue(form.plannedStart),
      plannedEnd: toApiDateTimeValue(form.plannedEnd),
    });
  }

  return (
    <section className="grid gap-6">
      <Link to="/employee/contractors" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('perspectives.employee.contractors.detail.back')}
      </Link>

      {isLoading ? <MutedText message={t('perspectives.employee.contractors.detail.loading')} /> : null}
      {isError ? <ErrorText message={t('perspectives.employee.contractors.detail.error')} /> : null}

      {job && !isLoading && !isError ? (
        <>
          <div className="rounded-structural border border-border bg-content p-6 sm:p-8">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <p className="text-[14px] font-semibold uppercase text-primary">{t('perspectives.employee.contractors.detail.kicker')}</p>
                <h1 className="mt-3 text-[30px] font-semibold tracking-tight">{job.name}</h1>
                <p className="mt-3 max-w-3xl text-[14px] leading-6 text-muted-foreground">{job.description || t('perspectives.employee.contractors.jobs.noDescription')}</p>
                <div className="mt-4 flex flex-wrap gap-2">
                  <RequestDetailChip>{companiesQuery.data?.get(job.companyId)?.name ?? t('perspectives.employee.contractors.unknownCompany')}</RequestDetailChip>
                  <RequestDetailChip>{jobTypesQuery.data?.get(job.jobTypeId)?.name ?? t('perspectives.employee.contractors.unknownJobType')}</RequestDetailChip>
                  <RequestDetailChip>{getLocationLabel(locationQuery.data as LocationResponse | null | undefined)}</RequestDetailChip>
                  <RequestDetailChip>{`${formatDateTimeLabel(job.plannedStart)} -> ${formatDateTimeLabel(job.plannedEnd)}`}</RequestDetailChip>
                  <Badge variant="secondary">{job.assignmentCount} assignment{job.assignmentCount === 1 ? '' : 's'}</Badge>
                </div>
              </div>
              <Badge variant={getContractorJobStatusVariant(job.status)}>{job.status}</Badge>
            </div>
          </div>

          <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as 'edit' | 'assignments')}>
            <TabsList>
              <TabsTrigger value="edit">Edit</TabsTrigger>
              <TabsTrigger value="assignments">Assignments <span className="text-[12px] font-medium text-muted-foreground">{assignments.length}</span></TabsTrigger>
            </TabsList>

            <TabsContent value="edit" className="mt-5">
              <Card>
                <CardHeader>
                  <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <CardTitle>{t('perspectives.employee.contractors.jobs.editTitle')}</CardTitle>
                      <CardDescription>{t('perspectives.employee.contractors.jobs.editDescription')}</CardDescription>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      {job.status !== 'Active' ? <Button type="button" variant="outline" onClick={() => setJobStatus.mutate('activate')} disabled={setJobStatus.isPending}>{t('perspectives.employee.contractors.jobs.activate')}</Button> : null}
                      {job.status !== 'Completed' ? <Button type="button" variant="outline" onClick={() => setJobStatus.mutate('complete')} disabled={setJobStatus.isPending}>{t('perspectives.employee.contractors.jobs.complete')}</Button> : null}
                      {job.status !== 'Cancelled' ? <Button type="button" variant="outline" onClick={() => setJobStatus.mutate('cancel')} disabled={setJobStatus.isPending}>{t('perspectives.employee.contractors.jobs.cancel')}</Button> : null}
                    </div>
                  </div>
                </CardHeader>
                <CardContent className="grid gap-6">
                  <section className="grid gap-4">
                    <div>
                      <h3 className="text-[16px] font-semibold tracking-tight text-foreground">Classification</h3>
                      <p className="mt-1 text-[13px] text-muted-foreground">Set company, job type, and location.</p>
                    </div>
                    <div className="grid gap-4 md:grid-cols-2">
                      <label className="grid gap-2 text-[14px] font-medium">
                        <span>{t('perspectives.employee.contractors.jobs.columns.company')}</span>
                        <select className="h-10 w-full rounded-interactive border border-border bg-content px-3.5 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20" value={form.companyId} onChange={(event) => setForm((current) => ({ ...current, companyId: event.target.value }))}>
                          <option value="">{t('perspectives.employee.contractors.jobs.selectCompany')}</option>
                          {Array.from(companiesQuery.data?.values() ?? []).map((company) => <option key={company.id} value={company.id}>{company.name}</option>)}
                        </select>
                      </label>
                      <label className="grid gap-2 text-[14px] font-medium">
                        <span>{t('perspectives.employee.contractors.jobs.columns.jobType')}</span>
                        <select className="h-10 w-full rounded-interactive border border-border bg-content px-3.5 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20" value={form.jobTypeId} onChange={(event) => setForm((current) => ({ ...current, jobTypeId: event.target.value }))}>
                          <option value="">{t('perspectives.employee.contractors.jobs.selectJobType')}</option>
                          {Array.from(jobTypesQuery.data?.values() ?? []).map((jobType) => <option key={jobType.id} value={jobType.id}>{jobType.name}</option>)}
                        </select>
                      </label>
                    </div>
                    <div className="grid gap-2">
                      <span className="text-[14px] font-medium">{t('perspectives.employee.contractors.jobs.columns.location')}</span>
                      <LocationSelector value={form.locationId} onChange={(value) => setForm((current) => ({ ...current, locationId: value }))} level="Room" />
                    </div>
                  </section>

                  <section className="grid gap-4 border-t border-border pt-6">
                    <div>
                      <h3 className="text-[16px] font-semibold tracking-tight text-foreground">Job Details</h3>
                      <p className="mt-1 text-[13px] text-muted-foreground">Update the job name and optional context.</p>
                    </div>
                    <label className="grid gap-2 text-[14px] font-medium">
                      <span>{t('perspectives.employee.contractors.jobs.columns.job')}</span>
                      <Input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} />
                    </label>
                    <label className="grid gap-2 text-[14px] font-medium">
                      <span>{t('perspectives.employee.contractors.jobs.fields.description')}</span>
                      <Textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={4} />
                    </label>
                  </section>

                  <section className="grid gap-4 border-t border-border pt-6">
                    <div>
                      <h3 className="text-[16px] font-semibold tracking-tight text-foreground">Planning Window</h3>
                      <p className="mt-1 text-[13px] text-muted-foreground">Set when the job starts and ends.</p>
                    </div>
                    <div className="grid gap-4 md:grid-cols-2">
                      <label className="grid gap-2 text-[14px] font-medium">
                        <span>{t('perspectives.employee.contractors.jobs.columns.plannedStart')}</span>
                        <Input type="datetime-local" value={form.plannedStart} onChange={(event) => setForm((current) => ({ ...current, plannedStart: event.target.value }))} />
                      </label>
                      <label className="grid gap-2 text-[14px] font-medium">
                        <span>{t('perspectives.employee.contractors.jobs.columns.plannedEnd')}</span>
                        <Input type="datetime-local" value={form.plannedEnd} onChange={(event) => setForm((current) => ({ ...current, plannedEnd: event.target.value }))} />
                      </label>
                    </div>
                  </section>

                  <div className="flex justify-end">
                    <Button type="button" onClick={submit} disabled={saveJob.isPending}>{t('perspectives.employee.contractors.jobs.save')}</Button>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="assignments" className="mt-5">
              <Card>
                <CardHeader>
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <CardTitle>{t('perspectives.employee.contractors.detail.assignmentsTitle')}</CardTitle>
                      <CardDescription>{t('perspectives.employee.contractors.detail.assignmentsDescription')}</CardDescription>
                    </div>
                    <Link to="/employee/contractors/jobs/$jobId/assignments/new" params={{ jobId }} className={buttonVariants()}>{t('perspectives.employee.contractors.assignments.new')}</Link>
                  </div>
                </CardHeader>
                <CardContent>
                  {assignments.length === 0 ? <EmptyText message={t('perspectives.employee.contractors.detail.assignmentsEmpty')} /> : null}

                  {assignments.length > 0 ? (
                    <>
                      <div className="hidden overflow-hidden rounded-structural border border-border lg:block">
                        <table className="min-w-full text-left text-[14px]">
                          <thead className="border-b border-border bg-background/70 text-[11px] uppercase tracking-[0.18em] text-muted-foreground">
                            <tr>
                              <th className="px-5 py-4 font-semibold">{t('perspectives.employee.contractors.detail.assignmentsColumns.contractor')}</th>
                              <th className="px-5 py-4 font-semibold">{t('perspectives.employee.contractors.detail.assignmentsColumns.assignedFrom')}</th>
                              <th className="px-5 py-4 font-semibold">{t('perspectives.employee.contractors.detail.assignmentsColumns.assignedUntil')}</th>
                              <th className="px-5 py-4 font-semibold">Compliance</th>
                              <th className="px-5 py-4 font-semibold">{t('perspectives.employee.contractors.detail.assignmentsColumns.status')}</th>
                              <th className="px-5 py-4 text-right font-semibold">Open</th>
                            </tr>
                          </thead>
                          <tbody>
                            {assignments.map((assignment) => {
                              const contractor = contractorsQuery.data?.get(assignment.contractorId);
                              const compliance = assignmentComplianceQuery.data?.get(assignment.id);

                              return (
                                <tr
                                  key={assignment.id}
                                  className="cursor-pointer border-t border-border transition hover:bg-hover-blue/45"
                                  role="link"
                                  tabIndex={0}
                                  onClick={() => void navigate({ to: '/employee/contractors/jobs/$jobId/assignments/$assignmentId', params: { jobId, assignmentId: assignment.id } })}
                                  onKeyDown={(event) => {
                                    if (event.key === 'Enter' || event.key === ' ') {
                                      event.preventDefault();
                                      void navigate({ to: '/employee/contractors/jobs/$jobId/assignments/$assignmentId', params: { jobId, assignmentId: assignment.id } });
                                    }
                                  }}
                                >
                                  <td className="px-5 py-5">
                                    <div>
                                      <p className="font-semibold text-foreground">{formatContractorName(contractor, assignment)}</p>
                                      <p className="mt-1 text-[13px] text-muted-foreground">{contractor?.email || t('perspectives.employee.contractors.detail.noEmail')}</p>
                                    </div>
                                  </td>
                                  <td className="px-5 py-5 text-muted-foreground">{formatDateTimeLabel(assignment.assignedFrom)}</td>
                                  <td className="px-5 py-5 text-muted-foreground">{formatDateTimeLabel(assignment.assignedUntil)}</td>
                                  <td className="px-5 py-5">{renderComplianceBadge(compliance)}</td>
                                  <td className="px-5 py-5"><Badge variant={getAssignmentStatusVariant(assignment.status)}>{assignment.status}</Badge></td>
                                  <td className="px-5 py-5 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                                </tr>
                              );
                            })}
                          </tbody>
                        </table>
                      </div>

                      <div className="grid gap-3 lg:hidden">
                        {assignments.map((assignment) => {
                          const contractor = contractorsQuery.data?.get(assignment.contractorId);
                          const compliance = assignmentComplianceQuery.data?.get(assignment.id);

                          return (
                            <div
                              key={assignment.id}
                              className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)] transition hover:border-primary/20 hover:shadow-[0_14px_34px_rgba(17,24,39,0.1)]"
                              role="button"
                              tabIndex={0}
                              onClick={() => void navigate({ to: '/employee/contractors/jobs/$jobId/assignments/$assignmentId', params: { jobId, assignmentId: assignment.id } })}
                              onKeyDown={(event) => {
                                if (event.key === 'Enter' || event.key === ' ') {
                                  event.preventDefault();
                                  void navigate({ to: '/employee/contractors/jobs/$jobId/assignments/$assignmentId', params: { jobId, assignmentId: assignment.id } });
                                }
                              }}
                            >
                              <div className="flex items-start justify-between gap-3">
                                <div>
                                  <p className="font-semibold text-foreground">{formatContractorName(contractor, assignment)}</p>
                                  <p className="mt-1 text-[13px] text-muted-foreground">{contractor?.email || t('perspectives.employee.contractors.detail.noEmail')}</p>
                                </div>
                                <div className="flex items-center gap-3"><Badge variant={getAssignmentStatusVariant(assignment.status)}>{assignment.status}</Badge><ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" /></div>
                              </div>
                              <div className="mt-4 flex flex-wrap gap-2">
                                <RequestDetailChip>{`from ${formatDateTimeLabel(assignment.assignedFrom)}`}</RequestDetailChip>
                                <RequestDetailChip>{`until ${formatDateTimeLabel(assignment.assignedUntil)}`}</RequestDetailChip>
                                {compliance?.complianceStatus ? renderComplianceBadge(compliance) : null}
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
          </Tabs>
        </>
      ) : null}
    </section>
  );
}

function formatContractorName(contractor: ContractorResponse | undefined, assignment: ContractorJobAssignmentResponse) {
  if (!contractor) {
    return assignment.contractorId;
  }

  return `${contractor.firstName} ${contractor.lastName}`;
}

function renderComplianceBadge(compliance: GrantComplianceSummaryResponse | undefined) {
  if (!compliance?.complianceStatus) {
    return <span className="text-[13px] text-muted-foreground">-</span>;
  }

  return <Badge variant={getGrantComplianceVariant(compliance.complianceStatus)}>{getGrantComplianceLabel(compliance.complianceStatus)}</Badge>;
}

function RequestDetailChip({ children }: { readonly children: React.ReactNode }) {
  return <span className="inline-flex items-center gap-1.5 rounded-[10px] border border-border bg-background px-3 py-1 text-[12px] font-medium text-muted-foreground">{children}</span>;
}

function getContractorJobFormStatePlaceholder(): ContractorJobFormState {
  return {
    companyId: '',
    jobTypeId: '',
    locationId: null,
    name: '',
    description: '',
    plannedStart: '',
    plannedEnd: '',
  };
}
