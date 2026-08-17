import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

import { ErrorText, formatDateTimeLabel, getEmptyContractorAssignmentFormState, invalidateContractorJobQueries, MutedText, toApiDateTimeValue } from './employee-contractor-job-shared';

type ContractorResponse = components['schemas']['ContractorResponse'];
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

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => window.history.back()}>{t('perspectives.employee.contractors.assignments.cancelAction')}</Button>
            <Button type="button" onClick={submit} disabled={createAssignment.isPending}>{t('perspectives.employee.contractors.assignments.create')}</Button>
          </div>
        </CardContent>
      </Card>
    </section>
  );
}
