import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { IdentityEvidenceCard } from '@/features/requirements/identity-evidence-card';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

type ContractorResponse = components['schemas']['ContractorResponse'];
type UpdateContractorRequest = components['schemas']['UpdateContractorRequest'];

type ContractorFormState = {
  firstName: string;
  lastName: string;
  email: string;
};

const contractorEnrollmentRole = 'contractor-enrollment';

export default function EmployeeContractorDetailPage() {
  const { t } = useTranslation();
  const { companyId, contractorId } = useParams({ from: '/main/employee/contractors/companies/$companyId/contractors/$contractorId' });
  const actorQuery = useCurrentActor();
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

  const contractorQuery = useQuery({
    queryKey: ['employee', 'contractors', 'detail', contractorId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/contractors/{id}', { params: { path: { id: contractorId } } });
      if (error || !data) {
        throw new Error('Could not load contractor.');
      }
      return data;
    },
  });

  const saveContractor = useMutation({
    mutationFn: async (request: UpdateContractorRequest) => {
      const { data, error } = await api.PUT('/api/contractors/contractors/{id}', { params: { path: { id: contractorId } }, body: request });
      if (error || !data) {
        throw new Error('Could not save contractor.');
      }
      return data;
    },
    onSuccess: async (contractor) => {
      toast.success(t('perspectives.employee.contractors.contractors.saved'));
      setForm({ firstName: contractor.firstName, lastName: contractor.lastName, email: contractor.email ?? '' });
      await queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'detail', contractorId] });
      await queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'companies', companyId, 'contractors'] });
    },
    onError: () => toast.error(t('perspectives.employee.contractors.contractors.saveError')),
  });

  const setContractorArchived = useMutation({
    mutationFn: async (archived: boolean) => {
      const request = archived
        ? api.POST('/api/contractors/contractors/{id}/archive', { params: { path: { id: contractorId } } })
        : api.POST('/api/contractors/contractors/{id}/unarchive', { params: { path: { id: contractorId } } });
      const { data, error } = await request;
      if (error || !data) {
        throw new Error(archived ? 'Could not archive contractor.' : 'Could not restore contractor.');
      }
      return data;
    },
    onSuccess: async (contractor) => {
      toast.success(contractor.archivedAt ? t('perspectives.employee.contractors.contractors.archived') : t('perspectives.employee.contractors.contractors.restored'));
      setForm({ firstName: contractor.firstName, lastName: contractor.lastName, email: contractor.email ?? '' });
      await queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'detail', contractorId] });
      await queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'companies', companyId, 'contractors'] });
    },
    onError: (_, archived) => toast.error(archived ? t('perspectives.employee.contractors.contractors.archiveError') : t('perspectives.employee.contractors.contractors.restoreError')),
  });

  const company = companyQuery.data;
  const contractor = contractorQuery.data;

  useEffect(() => {
    if (!contractor) {
      return;
    }

    setForm({ firstName: contractor.firstName, lastName: contractor.lastName, email: contractor.email ?? '' });
  }, [contractor]);

  if (companyQuery.isLoading || contractorQuery.isLoading) {
    return <MutedText message={t('perspectives.employee.contractors.contractors.detail.loading')} />;
  }

  if (companyQuery.isError || contractorQuery.isError || !company || !contractor || contractor.companyId !== companyId) {
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

    saveContractor.mutate({
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
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>{contractor.firstName} {contractor.lastName}</CardTitle>
              <CardDescription>{isEnrollmentRole ? t('perspectives.employee.contractors.contractors.detail.enrollmentDescription', { companyName: company.name }) : t('perspectives.employee.contractors.contractors.detail.description', { companyName: company.name })}</CardDescription>
            </div>
            <Badge variant={contractor.archivedAt ? 'secondary' : 'success'}>{contractor.archivedAt ? t('perspectives.employee.contractors.contractors.archivedBadge') : t('perspectives.employee.contractors.contractors.activeBadge')}</Badge>
          </div>
        </CardHeader>
        <CardContent className="grid gap-4">
          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              <span>{t('perspectives.employee.contractors.contractors.fields.firstName')}</span>
              <Input value={form.firstName} disabled={!isEnrollmentRole} onChange={(event) => setForm((current) => ({ ...current, firstName: event.target.value }))} />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              <span>{t('perspectives.employee.contractors.contractors.fields.lastName')}</span>
              <Input value={form.lastName} disabled={!isEnrollmentRole} onChange={(event) => setForm((current) => ({ ...current, lastName: event.target.value }))} />
            </label>
            <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
              <span>{t('perspectives.employee.contractors.contractors.fields.email')}</span>
              <Input value={form.email} disabled={!isEnrollmentRole} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} />
            </label>
          </div>

          <dl className="grid gap-2 text-[14px] text-muted-foreground">
            <DetailRow label={t('perspectives.employee.contractors.contractors.fields.updated')} value={formatDateTimeLabel(contractor.updatedAt)} />
          </dl>

          {isEnrollmentRole ? (
            <div className="flex flex-wrap justify-end gap-2">
              <Button type="button" variant="outline" onClick={() => setContractorArchived.mutate(!contractor.archivedAt)} disabled={setContractorArchived.isPending}>
                {contractor.archivedAt ? t('perspectives.employee.contractors.contractors.restore') : t('perspectives.employee.contractors.contractors.archive')}
              </Button>
              <Button type="button" onClick={submit} disabled={saveContractor.isPending}>{t('perspectives.employee.contractors.contractors.save')}</Button>
            </div>
          ) : null}
        </CardContent>
      </Card>

      {contractor.identityId ? (
        <IdentityEvidenceCard
          identityId={contractor.identityId}
          title="Requirement evidence"
          description="Evidence attached to this contractor identity. Expired evidence is hidden by default and entries are grouped by requirement."
        />
      ) : null}
    </section>
  );
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return <div className="flex items-center justify-between gap-3"><dt>{label}</dt><dd className="text-right text-foreground">{value}</dd></div>;
}

function ErrorText({ message }: { message: string }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{message}</p>;
}

function MutedText({ message }: { message: string }) {
  return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{message}</p>;
}

function formatDateTimeLabel(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}
