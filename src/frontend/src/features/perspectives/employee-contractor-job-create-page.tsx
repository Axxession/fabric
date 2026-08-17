import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { LocationSelector } from '@/shared/components/location-selector';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Textarea } from '@/shared/components/ui/textarea';

import { ErrorText, getEmptyContractorJobFormState, invalidateContractorJobQueries, MutedText, toApiDateTimeValue } from './employee-contractor-job-shared';

type CompanyResponse = components['schemas']['CompanyResponse'];
type CreateContractorJobRequest = components['schemas']['CreateContractorJobRequest'];
type JobTypeResponse = components['schemas']['JobTypeResponse'];

export default function EmployeeContractorJobCreatePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState(getEmptyContractorJobFormState());

  const companiesQuery = useQuery({
    queryKey: ['employee', 'contractors', 'job-form', 'companies'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/companies', {
        params: { query: { Query: undefined, IsActive: undefined, Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load contractor companies.');
      }

      return data?.items ?? [];
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

      return data?.items ?? [];
    },
  });

  const createJob = useMutation({
    mutationFn: async (request: CreateContractorJobRequest) => {
      const { data, error } = await api.POST('/api/contractors/jobs', { body: request });

      if (error || !data) {
        throw new Error('Could not create contractor job.');
      }

      return data;
    },
    onSuccess: async (job) => {
      toast.success(t('perspectives.employee.contractors.jobs.created'));
      await invalidateContractorJobQueries(queryClient, job.id);
      void navigate({ to: '/employee/contractors/jobs/$jobId', params: { jobId: job.id } });
    },
    onError: () => toast.error(t('perspectives.employee.contractors.jobs.createError')),
  });

  if (companiesQuery.isLoading || jobTypesQuery.isLoading) {
    return <MutedText message={t('perspectives.employee.contractors.jobs.createLoading')} />;
  }

  if (companiesQuery.isError || jobTypesQuery.isError) {
    return <ErrorText message={t('perspectives.employee.contractors.jobs.error')} />;
  }

  const companies = companiesQuery.data ?? [];
  const jobTypes = jobTypesQuery.data ?? [];

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

    createJob.mutate({
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
        {t('perspectives.employee.contractors.jobs.back')}
      </Link>

      <Card>
        <CardHeader>
          <CardTitle>{t('perspectives.employee.contractors.jobs.createTitle')}</CardTitle>
          <CardDescription>{t('perspectives.employee.contractors.jobs.createDescription')}</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4">
          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              <span>{t('perspectives.employee.contractors.jobs.columns.company')}</span>
              <select className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20" value={form.companyId} onChange={(event) => setForm((current) => ({ ...current, companyId: event.target.value }))}>
                <option value="">{t('perspectives.employee.contractors.jobs.selectCompany')}</option>
                {companies.map((company: CompanyResponse) => <option key={company.id} value={company.id}>{company.name}</option>)}
              </select>
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              <span>{t('perspectives.employee.contractors.jobs.columns.jobType')}</span>
              <select className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20" value={form.jobTypeId} onChange={(event) => setForm((current) => ({ ...current, jobTypeId: event.target.value }))}>
                <option value="">{t('perspectives.employee.contractors.jobs.selectJobType')}</option>
                {jobTypes.map((jobType: JobTypeResponse) => <option key={jobType.id} value={jobType.id}>{jobType.name}</option>)}
              </select>
            </label>
          </div>

          <div className="grid gap-2">
            <span className="text-[14px] font-medium">{t('perspectives.employee.contractors.jobs.columns.location')}</span>
            <LocationSelector value={form.locationId} onChange={(value) => setForm((current) => ({ ...current, locationId: value }))} level="Room" />
          </div>

          <label className="grid gap-2 text-[14px] font-medium">
            <span>{t('perspectives.employee.contractors.jobs.columns.job')}</span>
            <Input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} />
          </label>

          <label className="grid gap-2 text-[14px] font-medium">
            <span>{t('perspectives.employee.contractors.jobs.fields.description')}</span>
            <Textarea value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} rows={4} />
          </label>

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

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => window.history.back()}>{t('perspectives.employee.contractors.jobs.cancelAction')}</Button>
            <Button type="button" onClick={submit} disabled={createJob.isPending}>{t('perspectives.employee.contractors.jobs.create')}</Button>
          </div>
        </CardContent>
      </Card>
    </section>
  );
}
