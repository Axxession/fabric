import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, Navigate, useNavigate } from '@tanstack/react-router';
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

type CreateCompanyRequest = components['schemas']['CreateCompanyRequest'];

type CompanyFormState = {
  code: string;
  name: string;
  companyNumber: string;
};

const contractorEnrollmentRole = 'contractor-enrollment';

export default function EmployeeContractorCompanyCreatePage() {
  const { t } = useTranslation();
  const actorQuery = useCurrentActor();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<CompanyFormState>({ code: '', name: '', companyNumber: '' });

  const isEnrollmentRole = (actorQuery.data?.roles ?? []).includes(contractorEnrollmentRole);

  const createCompany = useMutation({
    mutationFn: async (request: CreateCompanyRequest) => {
      const { data, error } = await api.POST('/api/contractors/companies', { body: request });
      if (error || !data) {
        throw new Error('Could not create contractor company.');
      }

      return data;
    },
    onSuccess: async (company) => {
      toast.success(t('perspectives.employee.contractors.companies.created'));
      await invalidateCompanyQueries(queryClient);
      void navigate({ to: '/employee/contractors/companies/$companyId', params: { companyId: company.id } });
    },
    onError: () => toast.error(t('perspectives.employee.contractors.companies.createError')),
  });

  if (actorQuery.isLoading) {
    return <MutedText message={t('perspectives.employee.contractors.companies.createLoading')} />;
  }

  if (!isEnrollmentRole) {
    return <Navigate to="/employee/contractors" replace />;
  }

  function submit() {
    if (!form.code.trim()) {
      toast.error(t('perspectives.employee.contractors.companies.validation.codeRequired'));
      return;
    }

    if (!form.name.trim()) {
      toast.error(t('perspectives.employee.contractors.companies.validation.nameRequired'));
      return;
    }

    createCompany.mutate({
      code: form.code.trim(),
      name: form.name.trim(),
      companyNumber: form.companyNumber.trim() || null,
    });
  }

  return (
    <section className="grid gap-6">
      <Link to="/employee/contractors" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('perspectives.employee.contractors.companies.back')}
      </Link>

      <Card>
        <CardHeader>
          <CardTitle>{t('perspectives.employee.contractors.companies.createTitle')}</CardTitle>
          <CardDescription>{t('perspectives.employee.contractors.companies.createDescription')}</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4">
          <label className="grid gap-2 text-[14px] font-medium">
            <span>{t('perspectives.employee.contractors.companies.fields.code')}</span>
            <Input value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} />
          </label>
          <label className="grid gap-2 text-[14px] font-medium">
            <span>{t('perspectives.employee.contractors.companies.fields.name')}</span>
            <Input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} />
          </label>
          <label className="grid gap-2 text-[14px] font-medium">
            <span>{t('perspectives.employee.contractors.companies.fields.companyNumber')}</span>
            <Input value={form.companyNumber} onChange={(event) => setForm((current) => ({ ...current, companyNumber: event.target.value }))} />
          </label>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={() => window.history.back()}>{t('perspectives.employee.contractors.companies.cancel')}</Button>
            <Button type="button" onClick={submit} disabled={createCompany.isPending}>{t('perspectives.employee.contractors.companies.create')}</Button>
          </div>
        </CardContent>
      </Card>
    </section>
  );
}

function MutedText({ message }: { message: string }) {
  return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{message}</p>;
}

async function invalidateCompanyQueries(queryClient: ReturnType<typeof useQueryClient>) {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'companies'] }),
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'jobs', 'companies'] }),
  ]);
}
