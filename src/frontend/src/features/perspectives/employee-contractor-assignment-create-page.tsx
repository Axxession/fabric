import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from '@tanstack/react-router';
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

import { ErrorText, formatDateTimeLabel, getEmptyContractorAssignmentFormState, invalidateContractorJobQueries, MutedText, toApiDateTimeValue } from './employee-contractor-job-shared';

type ContractorResponse = components['schemas']['ContractorResponse'];
type ContractorAssignmentCompliancePreviewResponse = components['schemas']['ContractorAssignmentCompliancePreviewResponse'];
type ContractorAssignmentCompliancePreviewPackageResponse = components['schemas']['ContractorAssignmentCompliancePreviewPackageResponse'];
type AssignmentRequirementComplianceResponse = components['schemas']['AssignmentRequirementComplianceResponse'];
type CreateContractorJobAssignmentRequest = components['schemas']['CreateContractorJobAssignmentRequest'];
type ContractorAssignmentFormState = components['schemas']['CreateContractorJobAssignmentRequest'] & { assignedFrom: string; assignedUntil: string };

export default function EmployeeContractorAssignmentCreatePage() {
  const { t } = useTranslation();
  const { jobId } = useParams({ from: '/main/employee/contractors/jobs/$jobId/assignments/new' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();

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

  const [form, setForm] = useState<ContractorAssignmentFormState>({ contractorId: '', assignedFrom: '', assignedUntil: '' });

  const contractorsQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobId, 'assignment-form', jobQuery.data?.companyId],
    enabled: Boolean(jobQuery.data?.companyId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/contractors', {
        params: { query: { Query: undefined, CompanyId: jobQuery.data?.companyId, IdentityId: undefined, IsArchived: undefined, Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load contractors.');
      }

      return data?.items ?? [];
    },
  });

  const createAssignment = useMutation({
    mutationFn: async (request: CreateContractorJobAssignmentRequest) => {
      const { data, error } = await api.POST('/api/contractors/jobs/{contractorJobId}/assignments', { params: { path: { contractorJobId: jobId } }, body: request });

      if (error || !data) {
        throw new Error('Could not create contractor assignment.');
      }

      return data;
    },
    onSuccess: async (assignment) => {
      toast.success(t('perspectives.employee.contractors.assignments.created'));
      await invalidateContractorJobQueries(queryClient, jobId);
      void navigate({ to: '/employee/contractors/jobs/$jobId/assignments/$assignmentId', params: { jobId, assignmentId: assignment.id } });
    },
    onError: () => toast.error(t('perspectives.employee.contractors.assignments.createError')),
  });

  const job = jobQuery.data;
  const isPreviewReady = Boolean(job && form.contractorId && form.assignedFrom && form.assignedUntil && new Date(form.assignedFrom) <= new Date(form.assignedUntil) && new Date(form.assignedUntil) <= new Date(job.plannedEnd));
  const previewQuery = useQuery({
    queryKey: ['employee', 'contractors', 'jobs', jobId, 'assignment-preview', form.contractorId, form.assignedFrom, form.assignedUntil],
    enabled: isPreviewReady,
    queryFn: async () => {
      const { data, error } = await api.POST('/api/access-catalog/access-grants/contractor-assignment-preview', {
        body: {
          contractorId: form.contractorId,
          contractorJobId: jobId,
          assignedFrom: toApiDateTimeValue(form.assignedFrom),
          assignedUntil: toApiDateTimeValue(form.assignedUntil),
        },
      });

      if (error || !data) {
        throw new Error('Could not load compliance preview.');
      }

      return data;
    },
  });

  useEffect(() => {
    if (!job) {
      return;
    }

    setForm((current) => current.assignedFrom || current.assignedUntil ? current : getEmptyContractorAssignmentFormState(job));
  }, [job]);

  if (jobQuery.isLoading || contractorsQuery.isLoading) {
    return <MutedText message={t('perspectives.employee.contractors.assignments.createLoading')} />;
  }

  if (jobQuery.isError || contractorsQuery.isError || !job) {
    return <ErrorText message={t('perspectives.employee.contractors.assignments.error')} />;
  }

  const contractors = contractorsQuery.data ?? [];

  function submit() {
    if (!job) {
      return;
    }

    if (!form.contractorId) {
      toast.error(t('perspectives.employee.contractors.assignments.validation.contractorRequired'));
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

    createAssignment.mutate({
      contractorId: form.contractorId,
      assignedFrom: toApiDateTimeValue(form.assignedFrom),
      assignedUntil: toApiDateTimeValue(form.assignedUntil),
    });
  }

  return (
    <section className="grid gap-6">
      <Link to="/employee/contractors/jobs/$jobId" params={{ jobId }} className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('perspectives.employee.contractors.assignments.backToJob')}
      </Link>

      <Card>
        <CardHeader>
          <CardTitle>{t('perspectives.employee.contractors.assignments.createTitle')}</CardTitle>
          <CardDescription>{t('perspectives.employee.contractors.assignments.createDescription', { jobName: job.name })}</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4">
          <dl className="grid gap-2 text-[14px] text-muted-foreground">
            <div className="flex items-center justify-between gap-3"><dt>{t('perspectives.employee.contractors.assignments.jobWindow')}</dt><dd className="text-right text-foreground">{formatDateTimeLabel(job.plannedStart)} to {formatDateTimeLabel(job.plannedEnd)}</dd></div>
          </dl>

          <label className="grid gap-2 text-[14px] font-medium">
            <span>{t('perspectives.employee.contractors.assignments.contractor')}</span>
            <select className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20" value={form.contractorId} onChange={(event) => setForm((current) => ({ ...current, contractorId: event.target.value }))}>
              <option value="">{t('perspectives.employee.contractors.assignments.selectContractor')}</option>
              {contractors.map((contractor: ContractorResponse) => <option key={contractor.id} value={contractor.id}>{contractor.firstName} {contractor.lastName}</option>)}
            </select>
          </label>

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

          {previewQuery.isError ? <ErrorText message="Could not load compliance preview." /> : null}
          {previewQuery.isLoading ? <MutedText message="Loading compliance preview..." /> : null}
          {previewQuery.data ? <CompliancePreviewCard preview={previewQuery.data} /> : null}

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => window.history.back()}>{t('perspectives.employee.contractors.assignments.cancelAction')}</Button>
            <Button type="button" onClick={submit} disabled={createAssignment.isPending}>{t('perspectives.employee.contractors.assignments.create')}</Button>
          </div>
        </CardContent>
      </Card>
    </section>
  );
}

function CompliancePreviewCard({ preview }: { readonly preview: ContractorAssignmentCompliancePreviewResponse }) {
  return (
    <div className="rounded-structural border border-border p-4">
      <div>
        <h3 className="text-[18px] font-semibold tracking-tight">Compliance preview</h3>
        <p className="mt-2 text-[14px] text-muted-foreground">Preview what access packages would be assigned and what is currently missing for compliance before you create the assignment.</p>
      </div>

      {preview.unavailableReason ? <p className="mt-4 rounded-interactive border border-border bg-background px-4 py-3 text-[14px] text-muted-foreground">{preview.unavailableReason}</p> : null}
      {!preview.unavailableReason && preview.packages.length === 0 ? <p className="mt-4 rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No automatic access packages would be assigned for this assignment.</p> : null}

      {preview.packages.length > 0 ? (
        <div className="mt-4 grid gap-4">
          {preview.packages.map((item) => (
            <div key={item.packageId} className="rounded-structural border border-border bg-background p-4">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <p className="font-medium text-foreground">{item.packageName}</p>
                  {item.compliantUntil ? <p className="mt-1 text-[14px] text-muted-foreground">Compliant until {formatDateTimeLabel(item.compliantUntil)}</p> : null}
                </div>
                <Badge variant={getGrantComplianceVariant(item.status)}>{getGrantComplianceLabel(item.status)}</Badge>
              </div>
              {item.requirements.length === 0 ? <p className="mt-4 text-[14px] text-muted-foreground">No compliance requirements for this package.</p> : <div className="mt-4 grid gap-3">{item.requirements.map((requirement) => <PreviewRequirementCard key={requirement.requirementDefinitionId} requirement={requirement} />)}</div>}
            </div>
          ))}
        </div>
      ) : null}
    </div>
  );
}

function PreviewRequirementCard({ requirement }: { readonly requirement: AssignmentRequirementComplianceResponse }) {
  return (
    <div className="rounded-structural border border-border p-3">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="font-medium text-foreground">{requirement.name}</p>
          <p className="mt-1 text-[14px] text-muted-foreground">{requirement.code}{requirement.isBlocking ? ' • blocking' : ''}</p>
        </div>
        <Badge variant={getRequirementComplianceVariant(requirement.status)}>{formatRequirementComplianceStatus(requirement.status)}</Badge>
      </div>
      <p className="mt-3 text-[14px] text-muted-foreground">{requirement.reason}</p>
      {requirement.validUntil ? <p className="mt-1 text-[13px] text-muted-foreground">Valid until {formatDateTimeLabel(requirement.validUntil)}</p> : null}
    </div>
  );
}

function formatRequirementComplianceStatus(status: AssignmentRequirementComplianceResponse['status']) {
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

function getRequirementComplianceVariant(status: AssignmentRequirementComplianceResponse['status']): 'success' | 'secondary' | 'error' {
  switch (status) {
    case 'Fulfilled':
      return 'success';
    case 'Expired':
      return 'secondary';
    default:
      return 'error';
  }
}
