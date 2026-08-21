import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getGrantComplianceLabel, getGrantComplianceVariant } from '@/shared/access-grants/grant-status';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

import { DetailRow, ErrorText, formatDateTimeLabel, getAssignmentStatusVariant, getContractorAssignmentFormState, invalidateContractorJobQueries, MutedText, toApiDateTimeValue } from './employee-contractor-job-shared';

type ContractorResponse = components['schemas']['ContractorResponse'];
type GrantComplianceDetailResponse = components['schemas']['GrantComplianceDetailResponse'];
type RequirementComplianceResponse = components['schemas']['RequirementComplianceResponse'];
type UpdateContractorJobAssignmentRequest = components['schemas']['UpdateContractorJobAssignmentRequest'];

export default function EmployeeContractorAssignmentDetailPage() {
  const { t } = useTranslation();
  const { jobId, assignmentId } = useParams({ from: '/main/employee/contractors/jobs/$jobId/assignments/$assignmentId' });
  const queryClient = useQueryClient();
  const [form, setForm] = useState({ contractorId: '', assignedFrom: '', assignedUntil: '' });

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

  const assignmentQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobId, 'assignments', assignmentId, 'detail'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/jobs/{contractorJobId}/assignments/{assignmentId}', { params: { path: { contractorJobId: jobId, assignmentId } } });
      if (error || !data) {
        throw new Error('Could not load contractor assignment.');
      }

      return data;
    },
  });

  const contractorQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobId, 'assignments', assignmentId, 'contractor', assignmentQuery.data?.contractorId],
    enabled: Boolean(assignmentQuery.data?.contractorId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/contractors/{id}', { params: { path: { id: assignmentQuery.data?.contractorId ?? '' } } });
      if (error || !data) {
        throw new Error('Could not load contractor.');
      }

      return data;
    },
  });

  const complianceDetailQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobId, 'assignments', assignmentId, 'grant-compliance-detail'],
    enabled: Boolean(assignmentQuery.data),
    queryFn: async () => {
      const { data, error } = await api.POST('/api/access-catalog/access-grants/grant-compliance-details/by-source', {
        body: [{ sourceKind: 'ContractorJob', sourceId: assignmentId }],
      });
      if (error) {
        throw new Error('Could not load grant compliance.');
      }

      return data?.[0] ?? null;
    },
  });

  const saveAssignment = useMutation({
    mutationFn: async (request: UpdateContractorJobAssignmentRequest) => {
      const { data, error } = await api.PUT('/api/contractors/jobs/{contractorJobId}/assignments/{assignmentId}', { params: { path: { contractorJobId: jobId, assignmentId } }, body: request });
      if (error || !data) {
        throw new Error('Could not save contractor assignment.');
      }

      return data;
    },
    onSuccess: async (assignment) => {
      toast.success(t('perspectives.employee.contractors.assignments.saved'));
      setForm(getContractorAssignmentFormState(assignment));
      await invalidateContractorJobQueries(queryClient, jobId);
    },
    onError: () => toast.error(t('perspectives.employee.contractors.assignments.saveError')),
  });

  const setAssignmentStatus = useMutation({
    mutationFn: async (status: 'activate' | 'complete' | 'cancel') => {
      const requests = {
        activate: api.POST('/api/contractors/jobs/{contractorJobId}/assignments/{assignmentId}/activate', { params: { path: { contractorJobId: jobId, assignmentId } } }),
        complete: api.POST('/api/contractors/jobs/{contractorJobId}/assignments/{assignmentId}/complete', { params: { path: { contractorJobId: jobId, assignmentId } } }),
        cancel: api.POST('/api/contractors/jobs/{contractorJobId}/assignments/{assignmentId}/cancel', { params: { path: { contractorJobId: jobId, assignmentId } } }),
      };

      const { data, error } = await requests[status];
      if (error || !data) {
        throw new Error(`Could not ${status} contractor assignment.`);
      }

      return { assignment: data, status };
    },
    onSuccess: async ({ assignment, status }) => {
      const messages = {
        activate: t('perspectives.employee.contractors.assignments.activated'),
        complete: t('perspectives.employee.contractors.assignments.completed'),
        cancel: t('perspectives.employee.contractors.assignments.cancelled'),
      } as const;
      toast.success(messages[status]);
      setForm(getContractorAssignmentFormState(assignment));
      await invalidateContractorJobQueries(queryClient, jobId);
    },
    onError: (_, status) => {
      const messages = {
        activate: t('perspectives.employee.contractors.assignments.activateError'),
        complete: t('perspectives.employee.contractors.assignments.completeError'),
        cancel: t('perspectives.employee.contractors.assignments.cancelError'),
      } as const;
      toast.error(messages[status]);
    },
  });

  const job = jobQuery.data;
  const assignment = assignmentQuery.data;
  const contractor = contractorQuery.data;

  useEffect(() => {
    if (!assignment) {
      return;
    }

    setForm(getContractorAssignmentFormState(assignment));
  }, [assignment]);

  if (jobQuery.isLoading || assignmentQuery.isLoading || contractorQuery.isLoading || complianceDetailQuery.isLoading) {
    return <MutedText message={t('perspectives.employee.contractors.assignments.loading')} />;
  }

  if (jobQuery.isError || assignmentQuery.isError || contractorQuery.isError || complianceDetailQuery.isError || !job || !assignment) {
    return <ErrorText message={t('perspectives.employee.contractors.assignments.error')} />;
  }

  function submit() {
    if (!job) {
      return;
    }

    if (!form.assignedFrom || !form.assignedUntil) {
      toast.error(t('perspectives.employee.contractors.assignments.validation.windowRequired'));
      return;
    }

    if (new Date(form.assignedFrom) > new Date(form.assignedUntil)) {
      toast.error(t('perspectives.employee.contractors.assignments.validation.windowOrder'));
      return;
    }

    if (new Date(form.assignedUntil) > new Date(job.plannedEnd)) {
      toast.error(t('perspectives.employee.contractors.assignments.validation.mustFitJobWindow'));
      return;
    }

    saveAssignment.mutate({ assignedFrom: toApiDateTimeValue(form.assignedFrom), assignedUntil: toApiDateTimeValue(form.assignedUntil) });
  }

  const compliance = complianceDetailQuery.data;

  return (
    <section className="grid gap-6">
      <Link to="/employee/contractors/jobs/$jobId" params={{ jobId }} className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('perspectives.employee.contractors.assignments.backToJob')}
      </Link>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>{contractor ? `${contractor.firstName} ${contractor.lastName}` : assignment.contractorId}</CardTitle>
              <CardDescription>{t('perspectives.employee.contractors.assignments.detailDescription', { jobName: job.name })}</CardDescription>
            </div>
            <Badge variant={getAssignmentStatusVariant(assignment.status)}>{assignment.status}</Badge>
          </div>
        </CardHeader>
        <CardContent className="grid gap-4">
          <dl className="grid gap-3 text-[14px] text-muted-foreground">
            <DetailRow label={t('perspectives.employee.contractors.assignments.contractor')} value={contractor ? `${contractor.firstName} ${contractor.lastName}` : assignment.contractorId} />
            <DetailRow label={t('perspectives.employee.contractors.assignments.email')} value={contractor?.email ?? t('perspectives.employee.contractors.detail.noEmail')} />
            <DetailRow label={t('perspectives.employee.contractors.assignments.job')} value={job.name} />
            <DetailRow label={t('perspectives.employee.contractors.assignments.jobWindow')} value={`${formatDateTimeLabel(job.plannedStart)} to ${formatDateTimeLabel(job.plannedEnd)}`} />
          </dl>

          <div className="rounded-structural border border-border p-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h3 className="text-[16px] font-semibold tracking-tight text-foreground">Grant compliance</h3>
                <p className="mt-1 text-[13px] text-muted-foreground">Current persisted grant compliance for access issued from this assignment.</p>
              </div>
              {compliance?.complianceStatus ? <Badge variant={getGrantComplianceVariant(compliance.complianceStatus)}>{getGrantComplianceLabel(compliance.complianceStatus)}</Badge> : <span className="text-[13px] text-muted-foreground">No grant</span>}
            </div>
            {compliance?.compliantUntil ? <p className="mt-3 text-[13px] text-muted-foreground">Compliant until {formatDateTimeLabel(compliance.compliantUntil)}</p> : null}
            {!compliance || compliance.requirements.length === 0 ? <p className="mt-3 text-[13px] text-muted-foreground">No grant requirements attached to this assignment yet.</p> : null}
          </div>

          {compliance && compliance.requirements.length > 0 ? (
            <div className="rounded-structural border border-border p-4">
              <div>
                <h3 className="text-[16px] font-semibold tracking-tight text-foreground">Grant requirements</h3>
                <p className="mt-1 text-[13px] text-muted-foreground">See the grant-attached requirement snapshot and what is currently missing.</p>
              </div>
              <div className="mt-4 grid gap-3">
                {compliance.requirements.map((requirement) => (
                  <div key={requirement.requirementDefinitionId} className="rounded-structural border border-border bg-background p-4">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="font-medium text-foreground">{requirement.name}</p>
                      <Badge variant="secondary">{requirement.code}</Badge>
                      {requirement.isBlocking ? <Badge variant="secondary">Blocking</Badge> : <Badge variant="outline">Non-blocking</Badge>}
                      <Badge variant={getRequirementComplianceVariant(requirement.status)}>{formatRequirementComplianceStatus(requirement.status)}</Badge>
                    </div>
                    <p className="mt-2 text-[14px] text-muted-foreground">{requirement.reason}</p>
                    {requirement.validUntil ? <p className="mt-1 text-[13px] text-muted-foreground">Valid until {formatDateTimeLabel(requirement.validUntil)}</p> : null}
                  </div>
                ))}
              </div>
            </div>
          ) : null}

          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              <span>{t('perspectives.employee.contractors.detail.assignmentsColumns.assignedFrom')}</span>
              <Input type="datetime-local" value={form.assignedFrom} onChange={(event) => setForm((current) => ({ ...current, assignedFrom: event.target.value }))} />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              <span>{t('perspectives.employee.contractors.detail.assignmentsColumns.assignedUntil')}</span>
              <Input type="datetime-local" value={form.assignedUntil} onChange={(event) => setForm((current) => ({ ...current, assignedUntil: event.target.value }))} />
            </label>
          </div>

          <div className="flex flex-wrap justify-end gap-2">
            {assignment.status !== 'Active' ? <Button type="button" variant="outline" onClick={() => setAssignmentStatus.mutate('activate')} disabled={setAssignmentStatus.isPending}>{t('perspectives.employee.contractors.assignments.activate')}</Button> : null}
            {assignment.status !== 'Completed' ? <Button type="button" variant="outline" onClick={() => setAssignmentStatus.mutate('complete')} disabled={setAssignmentStatus.isPending}>{t('perspectives.employee.contractors.assignments.complete')}</Button> : null}
            {assignment.status !== 'Cancelled' ? <Button type="button" variant="outline" onClick={() => setAssignmentStatus.mutate('cancel')} disabled={setAssignmentStatus.isPending}>{t('perspectives.employee.contractors.assignments.cancel')}</Button> : null}
            <Button type="button" onClick={submit} disabled={saveAssignment.isPending}>{t('perspectives.employee.contractors.assignments.save')}</Button>
          </div>
        </CardContent>
      </Card>
    </section>
  );
}

function formatRequirementComplianceStatus(status: RequirementComplianceResponse['status']) {
  switch (status) {
    case 'Fulfilled':
      return 'Compliant';
    case 'Missing':
      return 'Missing';
    case 'Failed':
      return 'Failed';
    case 'Expired':
      return 'Expired';
    default:
      return status;
  }
}

function getRequirementComplianceVariant(status: RequirementComplianceResponse['status']): 'success' | 'secondary' | 'error' {
  switch (status) {
    case 'Fulfilled':
      return 'success';
    case 'Expired':
      return 'secondary';
    default:
      return 'error';
  }
}
