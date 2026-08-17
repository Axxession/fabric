import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

type CompanyResponse = components['schemas']['CompanyResponse'];
type ContractorResponse = components['schemas']['ContractorResponse'];
type UpdateCompanyRequest = components['schemas']['UpdateCompanyRequest'];

type ContractorFilter = 'all' | 'active' | 'archived';
type CompanyFormState = {
  code: string;
  name: string;
  companyNumber: string;
};

const contractorEnrollmentRole = 'contractor-enrollment';

export default function EmployeeContractorCompanyDetailPage() {
  const { t } = useTranslation();
  const { companyId } = useParams({ from: '/main/employee/contractors/companies/$companyId' });
  const actorQuery = useCurrentActor();
  const queryClient = useQueryClient();
  const [companyForm, setCompanyForm] = useState<CompanyFormState>({ code: '', name: '', companyNumber: '' });
  const [contractorFilter, setContractorFilter] = useState<ContractorFilter>('all');
  const [contractorQuery, setContractorQuery] = useState('');

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

  const contractorsQuery = useQuery({
    queryKey: ['employee', 'contractors', 'companies', companyId, 'contractors', contractorQuery, contractorFilter],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/contractors', {
        params: {
          query: {
            Query: contractorQuery.trim() || undefined,
            CompanyId: companyId,
            IdentityId: undefined,
            IsArchived: contractorFilter === 'all' ? undefined : contractorFilter === 'archived',
            Page: 0,
            PageSize: 200,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load company contractors.');
      }

      return data?.items ?? [];
    },
  });

  const saveCompany = useMutation({
    mutationFn: async (request: UpdateCompanyRequest) => {
      const { data, error } = await api.PUT('/api/contractors/companies/{id}', { params: { path: { id: companyId } }, body: request });
      if (error || !data) {
        throw new Error('Could not save contractor company.');
      }

      return data;
    },
    onSuccess: async (company) => {
      toast.success(t('perspectives.employee.contractors.companies.saved'));
      setCompanyForm({ code: company.code, name: company.name, companyNumber: company.companyNumber ?? '' });
      await invalidateCompanyQueries(queryClient, companyId);
    },
    onError: () => toast.error(t('perspectives.employee.contractors.companies.saveError')),
  });

  const setCompanyStatus = useMutation({
    mutationFn: async (isActive: boolean) => {
      const request = isActive
        ? api.POST('/api/contractors/companies/{id}/activate', { params: { path: { id: companyId } } })
        : api.POST('/api/contractors/companies/{id}/deactivate', { params: { path: { id: companyId } } });
      const { data, error } = await request;
      if (error || !data) {
        throw new Error(isActive ? 'Could not activate contractor company.' : 'Could not deactivate contractor company.');
      }

      return data;
    },
    onSuccess: async (company) => {
      toast.success(company.isActive ? t('perspectives.employee.contractors.companies.activated') : t('perspectives.employee.contractors.companies.deactivated'));
      setCompanyForm({ code: company.code, name: company.name, companyNumber: company.companyNumber ?? '' });
      await invalidateCompanyQueries(queryClient, companyId);
    },
    onError: (_, isActive) => toast.error(isActive ? t('perspectives.employee.contractors.companies.activateError') : t('perspectives.employee.contractors.companies.deactivateError')),
  });

  const company = companyQuery.data;
  const contractors = contractorsQuery.data ?? [];

  useEffect(() => {
    if (!company) {
      return;
    }

    setCompanyForm({ code: company.code, name: company.name, companyNumber: company.companyNumber ?? '' });
  }, [company]);

  function submitCompany() {
    if (!companyForm.code.trim()) {
      toast.error(t('perspectives.employee.contractors.companies.validation.codeRequired'));
      return;
    }

    if (!companyForm.name.trim()) {
      toast.error(t('perspectives.employee.contractors.companies.validation.nameRequired'));
      return;
    }

    saveCompany.mutate({
      code: companyForm.code.trim(),
      name: companyForm.name.trim(),
      companyNumber: companyForm.companyNumber.trim() || null,
    });
  }

  return (
    <section className="grid gap-6">
      <Link to="/employee/contractors" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('perspectives.employee.contractors.companies.back')}
      </Link>

      {companyQuery.isLoading ? <MutedText message={t('perspectives.employee.contractors.companies.detail.loading')} /> : null}
      {companyQuery.isError ? <ErrorText message={t('perspectives.employee.contractors.companies.detail.error')} /> : null}

      {company ? (
        <>
          <Card>
            <CardHeader>
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <CardTitle>{company.name}</CardTitle>
                  <CardDescription>{isEnrollmentRole ? t('perspectives.employee.contractors.companies.detail.enrollmentDescription') : t('perspectives.employee.contractors.companies.detail.description')}</CardDescription>
                </div>
                <Badge variant={company.isActive ? 'success' : 'secondary'}>{company.isActive ? t('perspectives.employee.contractors.companies.active') : t('perspectives.employee.contractors.companies.inactive')}</Badge>
              </div>
            </CardHeader>
            <CardContent className="grid gap-4">
              <div className="grid gap-4 md:grid-cols-2">
                <label className="grid gap-2 text-[14px] font-medium">
                  <span>{t('perspectives.employee.contractors.companies.fields.code')}</span>
                  <Input value={companyForm.code} disabled={!isEnrollmentRole} onChange={(event) => setCompanyForm((current) => ({ ...current, code: event.target.value }))} />
                </label>
                <label className="grid gap-2 text-[14px] font-medium">
                  <span>{t('perspectives.employee.contractors.companies.fields.name')}</span>
                  <Input value={companyForm.name} disabled={!isEnrollmentRole} onChange={(event) => setCompanyForm((current) => ({ ...current, name: event.target.value }))} />
                </label>
                <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
                  <span>{t('perspectives.employee.contractors.companies.fields.companyNumber')}</span>
                  <Input value={companyForm.companyNumber} disabled={!isEnrollmentRole} onChange={(event) => setCompanyForm((current) => ({ ...current, companyNumber: event.target.value }))} />
                </label>
              </div>

              {isEnrollmentRole ? (
                <div className="flex flex-wrap justify-end gap-2">
                  <Button type="button" variant="outline" onClick={() => setCompanyStatus.mutate(!company.isActive)} disabled={setCompanyStatus.isPending}>
                    {company.isActive ? t('perspectives.employee.contractors.companies.deactivate') : t('perspectives.employee.contractors.companies.activate')}
                  </Button>
                  <Button type="button" onClick={submitCompany} disabled={saveCompany.isPending}>{t('perspectives.employee.contractors.companies.save')}</Button>
                </div>
              ) : null}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <CardTitle>{t('perspectives.employee.contractors.contractors.title')}</CardTitle>
                  <CardDescription>{isEnrollmentRole ? t('perspectives.employee.contractors.contractors.enrollmentDescription') : t('perspectives.employee.contractors.contractors.description')}</CardDescription>
                </div>
                {isEnrollmentRole ? <Link to="/employee/contractors/companies/$companyId/contractors/new" params={{ companyId }} className="inline-flex h-9 items-center rounded-interactive bg-primary px-4 text-[14px] font-medium text-primary-foreground transition hover:opacity-90">{t('perspectives.employee.contractors.contractors.new')}</Link> : null}
              </div>
            </CardHeader>
            <CardContent className="grid gap-4">
              <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_220px]">
                <label className="grid gap-2 text-[14px] font-medium">
                  <span>{t('perspectives.employee.contractors.filters.search')}</span>
                  <Input value={contractorQuery} onChange={(event) => setContractorQuery(event.target.value)} placeholder={t('perspectives.employee.contractors.contractors.searchPlaceholder')} />
                </label>
                <label className="grid gap-2 text-[14px] font-medium">
                  <span>{t('perspectives.employee.contractors.filters.status')}</span>
                  <select className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20" value={contractorFilter} onChange={(event) => setContractorFilter(event.target.value as ContractorFilter)}>
                    <option value="all">{t('perspectives.employee.contractors.contractors.statusFilter.all')}</option>
                    <option value="active">{t('perspectives.employee.contractors.contractors.statusFilter.active')}</option>
                    <option value="archived">{t('perspectives.employee.contractors.contractors.statusFilter.archived')}</option>
                  </select>
                </label>
              </div>

              {contractorsQuery.isLoading ? <MutedText message={t('perspectives.employee.contractors.contractors.loading')} /> : null}
              {contractorsQuery.isError ? <ErrorText message={t('perspectives.employee.contractors.contractors.error')} /> : null}
              {!contractorsQuery.isLoading && !contractorsQuery.isError && contractors.length === 0 ? <EmptyText message={t('perspectives.employee.contractors.contractors.empty')} /> : null}

              {!contractorsQuery.isLoading && !contractorsQuery.isError && contractors.length > 0 ? (
                <div className="grid gap-3">
                  {contractors.map((contractor) => (
                    <div key={contractor.id} className="rounded-structural border border-border p-4">
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <p className="font-medium text-foreground">{contractor.firstName} {contractor.lastName}</p>
                          <p className="mt-1 text-[13px] text-muted-foreground">{contractor.email || t('perspectives.employee.contractors.detail.noEmail')}</p>
                        </div>
                        <Badge variant={contractor.archivedAt ? 'secondary' : 'success'}>{contractor.archivedAt ? t('perspectives.employee.contractors.contractors.archivedBadge') : t('perspectives.employee.contractors.contractors.activeBadge')}</Badge>
                      </div>

                      <dl className="mt-4 grid gap-2 text-[13px] text-muted-foreground md:grid-cols-2">
                        <DetailRow label={t('perspectives.employee.contractors.contractors.fields.updated')} value={formatDateTimeLabel(contractor.updatedAt)} />
                      </dl>

                      {isEnrollmentRole ? (
                        <div className="mt-4 flex flex-wrap gap-2">
                          <Link to="/employee/contractors/companies/$companyId/contractors/$contractorId" params={{ companyId, contractorId: contractor.id }} className="inline-flex h-9 items-center rounded-interactive border border-border bg-content px-4 text-[14px] font-medium text-foreground transition hover:bg-hover-blue">{t('perspectives.employee.contractors.contractors.edit')}</Link>
                        </div>
                      ) : <div className="mt-4 flex flex-wrap gap-2"><Link to="/employee/contractors/companies/$companyId/contractors/$contractorId" params={{ companyId, contractorId: contractor.id }} className="inline-flex h-9 items-center rounded-interactive border border-border bg-content px-4 text-[14px] font-medium text-foreground transition hover:bg-hover-blue">{t('perspectives.employee.contractors.contractors.open')}</Link></div>}
                    </div>
                  ))}
                </div>
              ) : null}
            </CardContent>
          </Card>
        </>
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

function EmptyText({ message }: { message: string }) {
  return <p className="rounded-structural border border-dashed border-border bg-content p-6 text-[14px] text-muted-foreground">{message}</p>;
}

function formatDateTimeLabel(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

async function invalidateCompanyQueries(queryClient: ReturnType<typeof useQueryClient>, companyId: string) {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'companies'] }),
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'jobs', 'companies'] }),
    queryClient.invalidateQueries({ queryKey: ['employee', 'contractors', 'companies', companyId, 'detail'] }),
  ]);
}
