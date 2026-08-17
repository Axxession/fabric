import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, Navigate, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

type CreateContractorRequest = components['schemas']['CreateContractorRequest'];

type ContractorFormState = {
  firstName: string;
  lastName: string;
  email: string;
};

const contractorEnrollmentRole = 'contractor-enrollment';

export default function EmployeeContractorCreatePage() {
  const { t } = useTranslation();
  const { companyId } = useParams({ from: '/main/employee/contractors/companies/$companyId/contractors/new' });
  const actorQuery = useCurrentActor();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<ContractorFormState>({ firstName: '', lastName: '', email: '' });

  const isEnrollmentRole = (actorQuery.data?.roles ?? []).includes(contractorEnrollmentRole);

  const companyQuery = useQuery({
    queryKey: ['employee', 'contractors', 'companies', companyId, 'detail'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/companies/{id}', { params: { path: { id: companyId } } });
      if (error || !data) {
        throw new Error('Could not load contractor company.');
      }
      return data;
    },
  });

  const createContractor = useMutation({
    mutationFn: async (request: CreateContractorRequest) => {
      const { data, error } = await api.POST('/api/contractors/contractors', { body: request });
      if (error || !data) {
        throw new Error('Could not create contractor.');
      }

      return data;
    },
    onSuccess: async (contractor) => {
      toast.success(t('perspectives.employee.contractors.contractors.created'));
      await queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'companies', companyId, 'contractors'] });
      void navigate({ to: '/employee/contractors/companies/$companyId/contractors/$contractorId', params: { companyId, contractorId: contractor.id } });
    },
    onError: () => toast.error(t('perspectives.employee.contractors.contractors.createError')),
  });

  if (actorQuery.isLoading || companyQuery.isLoading) {
    return <MutedText message={t('perspectives.employee.contractors.contractors.detail.loading')} />;
  }

  if (!isEnrollmentRole) {
    return <Navigate to="/employee/contractors" replace />;
  }

  if (companyQuery.isError || !companyQuery.data) {
    return <ErrorText message={t('perspectives.employee.contractors.contractors.detail.error')} />;
  }

  function submit() {
    if (!form.firstName.trim()) {
      toast.error(t('perspectives.employee.contractors.contractors.validation.firstNameRequired'));
      return;
    }

    if (!form.lastName.trim()) {
      toast.error(t('perspectives.employee.contractors.contractors.validation.lastNameRequired'));
      return;
    }

    createContractor.mutate({
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      email: form.email.trim() || null,
      companyId,
    });
  }

  return (
    <section className="grid gap-6">
      <Link to="/employee/contractors/companies/$companyId" params={{ companyId }} className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('perspectives.employee.contractors.contractors.backToCompany')}
      </Link>

      <Card>
        <CardHeader>
          <CardTitle>{t('perspectives.employee.contractors.contractors.createTitle')}</CardTitle>
          <CardDescription>{t('perspectives.employee.contractors.contractors.createForCompany', { companyName: companyQuery.data.name })}</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4">
          <label className="grid gap-2 text-[14px] font-medium">
            <span>{t('perspectives.employee.contractors.contractors.fields.firstName')}</span>
            <Input value={form.firstName} onChange={(event) => setForm((current) => ({ ...current, firstName: event.target.value }))} />
          </label>
          <label className="grid gap-2 text-[14px] font-medium">
            <span>{t('perspectives.employee.contractors.contractors.fields.lastName')}</span>
            <Input value={form.lastName} onChange={(event) => setForm((current) => ({ ...current, lastName: event.target.value }))} />
          </label>
          <label className="grid gap-2 text-[14px] font-medium">
            <span>{t('perspectives.employee.contractors.contractors.fields.email')}</span>
            <Input value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} />
          </label>
          <p className="text-[13px] text-muted-foreground">{t('perspectives.employee.contractors.contractors.identityCreatedAutomatically')}</p>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => window.history.back()}>{t('perspectives.employee.contractors.contractors.cancel')}</Button>
            <Button type="button" onClick={submit} disabled={createContractor.isPending}>{t('perspectives.employee.contractors.contractors.create')}</Button>
          </div>
        </CardContent>
      </Card>
    </section>
  );
}

function ErrorText({ message }: { message: string }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{message}</p>;
}

function MutedText({ message }: { message: string }) {
  return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{message}</p>;
}
