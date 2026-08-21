import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation, useNavigate } from '@tanstack/react-router';
import { ChevronRight, Pencil, Trash2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getLocationLabel, LocationSelector, type LocationResponse } from '@/shared/components/location-selector';
import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from '@/shared/components/ui/pagination';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { fetchVisitorPreOnboardingConfig, updateVisitorPreOnboardingConfig, visitorPreOnboardingConfigQueryKey } from '@/features/settings/visitor-pre-onboarding-config';

type AccessModelTab = 'packages' | 'catalogues' | 'approval-groups' | 'hr-policies' | 'contractor-policies' | 'visitor-policies' | 'compliancy';
type PackageResponse = components['schemas']['PackageResponse'];
type CatalogResponse = components['schemas']['CatalogResponse'];
type ApprovalGroupResponse = components['schemas']['ApprovalGroupResponse'];
type AccessRuleAssignmentResponse = components['schemas']['AccessRuleAssignmentResponse'];
type CredentialTypeResponse = components['schemas']['CredentialTypeResponse'];
type CreateAccessRuleAssignmentRequest = components['schemas']['CreateAccessRuleAssignmentRequest'];
type CreateContractorJobPackageRuleRequest = components['schemas']['CreateContractorJobPackageRuleRequest'];
type EmployeeLifecycleAutomationSettingsResponse = components['schemas']['EmployeeLifecycleAutomationSettingsResponse'];
type CreateOrganizationalUnitPackageRuleRequest = components['schemas']['CreateOrganizationalUnitPackageRuleRequest'];
type CreatePersonaPackageRuleRequest = components['schemas']['CreatePersonaPackageRuleRequest'];
type ContractorJobPackageRuleResponse = components['schemas']['ContractorJobPackageRuleResponse'];
type JobTypeResponse = components['schemas']['JobTypeResponse'];
type OrganizationalUnitPackageRuleResponse = components['schemas']['OrganizationalUnitPackageRuleResponse'];
type PersonaPackageRuleResponse = components['schemas']['PersonaPackageRuleResponse'];
type SetContractorJobPackageRuleEnabledRequest = components['schemas']['SetContractorJobPackageRuleEnabledRequest'];
type SetRuleEnabledRequest = components['schemas']['SetRuleEnabledRequest'];
type OrganizationUnitResponse = components['schemas']['OrganizationUnitResponse'];
type PersonaResponse = components['schemas']['PersonaResponse'];
type RequirementDefinitionResponse = components['schemas']['RequirementDefinitionResponse'];
type UpdateEmployeeLifecycleAutomationSettingsRequest = components['schemas']['UpdateEmployeeLifecycleAutomationSettingsRequest'];
type ReceptionAccessPolicyTrigger = components['schemas']['ReceptionAccessPolicyTrigger'];
type RuleOption = { readonly value: string; readonly label: string };
type RuleGroup<T extends { id: string; name: string }> = {
  readonly source: T;
  readonly rules: readonly RuleWithPackageName[];
};
type RuleWithPackageName = {
  readonly id: string;
  readonly packageName: string;
  readonly isEnabled: boolean;
  readonly detail?: string;
};

type PaginationState = {
  readonly currentPage: number;
  readonly firstItem: number;
  readonly lastItem: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly visiblePages: readonly (number | 'ellipsis')[];
};

const pageSize = 10;

const visitorTriggerOptions: { readonly label: string; readonly value: ReceptionAccessPolicyTrigger }[] = [
  { label: 'Expected visitor added', value: 'ExpectedVisitorAdded' },
  { label: 'Visitor confirmed', value: 'VisitorConfirmed' },
  { label: 'Visitor onboarded', value: 'VisitorOnboarded' },
];

export default function AccessModelPage() {
  const { t } = useTranslation();
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = getActiveTab(location.searchStr);

  const [packagesPage, setPackagesPage] = useState(0);
  const [packagesName, setPackagesName] = useState('');

  const [cataloguesPage, setCataloguesPage] = useState(0);
  const [cataloguesName, setCataloguesName] = useState('');

  const [approvalGroupsPage, setApprovalGroupsPage] = useState(0);
  const [approvalGroupsName, setApprovalGroupsName] = useState('');
  const [requirementsPage, setRequirementsPage] = useState(0);
  const [requirementsQueryText, setRequirementsQueryText] = useState('');

  const packagesQuery = useQuery({
    queryKey: ['administration', 'access-model', 'packages', packagesPage, packagesName],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/packages', {
        params: { query: { Name: packagesName || undefined, Page: packagesPage, PageSize: pageSize } as never },
      });
      if (error) throw new Error(t('accessModel.packages.couldNotLoad'));
      return data;
    },
  });

  const cataloguesQuery = useQuery({
    queryKey: ['administration', 'access-model', 'catalogues', cataloguesPage, cataloguesName],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/catalogs', {
        params: { query: { Name: cataloguesName || undefined, Page: cataloguesPage, PageSize: pageSize } as never },
      });
      if (error) throw new Error(t('accessModel.catalogs.couldNotLoad'));
      return data;
    },
  });

  const approvalGroupsQuery = useQuery({
    queryKey: ['administration', 'access-model', 'approval-groups', approvalGroupsPage, approvalGroupsName],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/approval-groups', {
        params: { query: { Name: approvalGroupsName || undefined, ids: [], Page: approvalGroupsPage, PageSize: pageSize } as never },
      });
      if (error) throw new Error(t('accessModel.approvalGroups.couldNotLoad'));
      return data;
    },
  });

  const requirementsQuery = useQuery({
    queryKey: ['administration', 'access-model', 'compliancy', 'requirements', requirementsPage, requirementsQueryText],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/requirements/definitions', {
        params: { query: { Query: requirementsQueryText || undefined, IsActive: undefined, Page: requirementsPage, PageSize: pageSize } as never },
      });
      if (error) throw new Error('Could not load requirements.');
      return data;
    },
  });

  const employeeLifecycleSettingsQuery = useQuery({
    queryKey: ['administration', 'access-model', 'hr-policies', 'settings'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/sagas/employee-lifecycle/settings');
      if (error || !data) throw new Error('Could not load HR policy settings.');
      return data;
    },
  });

  const ouRulesQuery = useQuery({
    queryKey: ['administration', 'access-model', 'hr-policies', 'ou-rules'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/sagas/employee-lifecycle/ou-package-rules', {
        params: { query: { Page: 0, PageSize: 100 } },
      });
      if (error) throw new Error('Could not load OU package rules.');
      return data;
    },
  });

  const personaRulesQuery = useQuery({
    queryKey: ['administration', 'access-model', 'hr-policies', 'persona-rules'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/sagas/employee-lifecycle/persona-package-rules', {
        params: { query: { Page: 0, PageSize: 100 } },
      });
      if (error) throw new Error('Could not load persona package rules.');
      return data;
    },
  });

  const contractorRulesQuery = useQuery({
    queryKey: ['administration', 'access-model', 'contractor-policies', 'rules'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/sagas/contractor-jobs/access-package-rules', {
        params: { query: { Page: 0, PageSize: 200 } },
      });
      if (error) throw new Error('Could not load contractor package rules.');
      return data;
    },
  });

  const contractorJobTypesQuery = useQuery({
    queryKey: ['administration', 'access-model', 'contractor-policies', 'job-types'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/job-types', {
        params: { query: { Code: undefined, Name: undefined, IsActive: undefined, Page: 0, PageSize: 200 } as never },
      });
      if (error) throw new Error('Could not load contractor job types.');
      return data?.items ?? [];
    },
  });

  const contractorRuleLocationsQuery = useQuery({
    queryKey: ['administration', 'access-model', 'contractor-policies', 'locations', (contractorRulesQuery.data?.items ?? []).map((item) => item.locationId).filter(Boolean).sort().join(',')],
    queryFn: async () => {
      const locationIds = (contractorRulesQuery.data?.items ?? []).flatMap((item) => item.locationId ? [item.locationId] : []);
      if (locationIds.length === 0) {
        return [] as LocationResponse[];
      }

      const { data, error } = await api.GET('/api/locations/locations', { params: { query: { ids: locationIds } } });
      if (error) throw new Error('Could not load contractor rule locations.');
      return data ?? [];
    },
    enabled: (contractorRulesQuery.data?.items ?? []).some((item) => !!item.locationId),
  });

  const organizationUnitsQuery = useQuery({
    queryKey: ['administration', 'access-model', 'hr-policies', 'organization-units'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/organization-units', {
        params: { query: { Query: undefined, ParentId: undefined, IsActive: undefined, Page: 0, PageSize: 200 } as never },
      });
      if (error) throw new Error('Could not load organizational units.');
      return data?.items ?? [];
    },
  });

  const personasQuery = useQuery({
    queryKey: ['administration', 'access-model', 'hr-policies', 'personas'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/personas', {
        params: { query: { Query: undefined, IsActive: undefined, Page: 0, PageSize: 200 } as never },
      });
      if (error) throw new Error('Could not load personas.');
      return data?.items ?? [];
    },
  });

  const packagesOptionsQuery = useQuery({
    queryKey: ['administration', 'access-model', 'packages', 'options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/packages', {
        params: { query: { Name: undefined, Page: 0, PageSize: 200 } as never },
      });
      if (error) throw new Error('Could not load package options.');
      return data?.items ?? [];
    },
  });

  const visitorAssignmentsQuery = useQuery({
    queryKey: ['administration', 'access-model', 'visitor-policies', 'assignments'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/access-rule-assignments', {
        params: { query: { Page: 0, PageSize: 100 } },
      });
      if (error) throw new Error('Could not load visitor trigger assignments.');
      return data?.items ?? [];
    },
  });

  const visitorPreOnboardingConfigQuery = useQuery({
    queryKey: visitorPreOnboardingConfigQueryKey,
    queryFn: fetchVisitorPreOnboardingConfig,
  });

  const qrCredentialTypesQuery = useQuery({
    queryKey: ['administration', 'access-model', 'visitor-policies', 'qr-credential-types'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/credential-management/credential-types', {
        params: { query: { Query: undefined, Technology: undefined, Status: 'Active', Page: 0, PageSize: 200 } as never },
      });
      if (error) throw new Error('Could not load QR credential types.');
      return (data?.items ?? []).filter((item: CredentialTypeResponse) => item.technology === 'Qr' && item.allocationMode === 'Range');
    },
  });

  function changeTab(nextTab: string) {
    if (!isAccessModelTab(nextTab)) {
      return;
    }

    void navigate({ to: '/administration/access-model', search: { tab: nextTab } as never, replace: true });
  }

  return (
    <section className="rounded-structural border border-border bg-content p-4 sm:p-6">
      <Tabs value={activeTab} onValueChange={changeTab}>
        <TabsList>
          <TabsTrigger value="packages">{t('accessModel.tabs.packages')}</TabsTrigger>
          <TabsTrigger value="catalogues">{t('accessModel.tabs.catalogs')}</TabsTrigger>
          <TabsTrigger value="approval-groups">{t('accessModel.tabs.approvalGroups')}</TabsTrigger>
          <TabsTrigger value="compliancy">Compliancy</TabsTrigger>
          <TabsTrigger value="hr-policies">{t('accessModel.tabs.hrPolicies')}</TabsTrigger>
          <TabsTrigger value="contractor-policies">{t('accessModel.tabs.contractorPolicies')}</TabsTrigger>
          <TabsTrigger value="visitor-policies">{t('accessModel.tabs.visitorPolicies')}</TabsTrigger>
        </TabsList>

        <TabsContent value="packages">
          <PackagesPanel name={packagesName} onNameChange={(value) => { setPackagesName(value); setPackagesPage(0); }} onOpenPackage={(packageId) => void navigate({ to: '/administration/access-model/packages/$packageId/edit', params: { packageId } })} response={packagesQuery.data} isLoading={packagesQuery.isLoading} isError={packagesQuery.isError} page={packagesPage} setPage={setPackagesPage} />
        </TabsContent>

        <TabsContent value="catalogues">
          <CataloguesPanel name={cataloguesName} onNameChange={(value) => { setCataloguesName(value); setCataloguesPage(0); }} onOpenCatalogue={(catalogueId) => void navigate({ to: '/administration/access-model/catalogues/$catalogueId/edit', params: { catalogueId } })} response={cataloguesQuery.data} isLoading={cataloguesQuery.isLoading} isError={cataloguesQuery.isError} page={cataloguesPage} setPage={setCataloguesPage} />
        </TabsContent>

        <TabsContent value="approval-groups">
          <ApprovalGroupsPanel name={approvalGroupsName} onNameChange={(value) => { setApprovalGroupsName(value); setApprovalGroupsPage(0); }} onOpenApprovalGroup={(approvalGroupId) => void navigate({ to: '/administration/access-model/approval-groups/$approvalGroupId/edit', params: { approvalGroupId } })} response={approvalGroupsQuery.data} isLoading={approvalGroupsQuery.isLoading} isError={approvalGroupsQuery.isError} page={approvalGroupsPage} setPage={setApprovalGroupsPage} />
        </TabsContent>

        <TabsContent value="compliancy">
          <RequirementsPanel name={requirementsQueryText} onNameChange={(value) => { setRequirementsQueryText(value); setRequirementsPage(0); }} onOpenRequirement={(requirementId) => void navigate({ to: '/administration/access-model/compliancy/$requirementId/edit', params: { requirementId } })} response={requirementsQuery.data} isLoading={requirementsQuery.isLoading} isError={requirementsQuery.isError} page={requirementsPage} setPage={setRequirementsPage} />
        </TabsContent>

        <TabsContent value="hr-policies">
          <HrPoliciesPanel settings={employeeLifecycleSettingsQuery.data} ouRules={ouRulesQuery.data?.items ?? []} personaRules={personaRulesQuery.data?.items ?? []} organizationUnits={organizationUnitsQuery.data ?? []} personas={personasQuery.data ?? []} packages={packagesOptionsQuery.data ?? []} isLoading={employeeLifecycleSettingsQuery.isLoading || ouRulesQuery.isLoading || personaRulesQuery.isLoading || organizationUnitsQuery.isLoading || personasQuery.isLoading || packagesOptionsQuery.isLoading} isError={employeeLifecycleSettingsQuery.isError || ouRulesQuery.isError || personaRulesQuery.isError || organizationUnitsQuery.isError || personasQuery.isError || packagesOptionsQuery.isError} />
        </TabsContent>

        <TabsContent value="contractor-policies">
          <ContractorPoliciesPanel rules={contractorRulesQuery.data?.items ?? []} jobTypes={contractorJobTypesQuery.data ?? []} packages={packagesOptionsQuery.data ?? []} locations={contractorRuleLocationsQuery.data ?? []} isLoading={contractorRulesQuery.isLoading || contractorJobTypesQuery.isLoading || packagesOptionsQuery.isLoading || contractorRuleLocationsQuery.isLoading} isError={contractorRulesQuery.isError || contractorJobTypesQuery.isError || packagesOptionsQuery.isError || contractorRuleLocationsQuery.isError} />
        </TabsContent>

        <TabsContent value="visitor-policies">
          <VisitorPoliciesPanel assignments={visitorAssignmentsQuery.data ?? []} packages={packagesOptionsQuery.data ?? []} qrCredentialTypes={qrCredentialTypesQuery.data ?? []} visitorConfig={visitorPreOnboardingConfigQuery.data} isLoading={visitorAssignmentsQuery.isLoading || packagesOptionsQuery.isLoading || visitorPreOnboardingConfigQuery.isLoading || qrCredentialTypesQuery.isLoading} isError={visitorAssignmentsQuery.isError || packagesOptionsQuery.isError || visitorPreOnboardingConfigQuery.isError || qrCredentialTypesQuery.isError} />
        </TabsContent>
      </Tabs>
    </section>
  );
}

function RequirementsPanel({ name, onNameChange, onOpenRequirement, response, isLoading, isError, page, setPage }: { readonly name: string; readonly onNameChange: (value: string) => void; readonly onOpenRequirement: (requirementId: string) => void; readonly response: components['schemas']['PageOfRequirementDefinitionResponse'] | undefined; readonly isLoading: boolean; readonly isError: boolean; readonly page: number; readonly setPage: (page: number) => void; }) {
  const queryClient = useQueryClient();
  const items = response?.items ?? [];
  const pagination = getPaginationState(response, items.length, page, pageSize);

  const deleteRequirement = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/requirements/definitions/{id}', { params: { path: { id } } });
      if (error) throw new Error('Could not delete requirement.');
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'compliancy', 'requirements'] });
      toast.success('Requirement deleted.');
    },
    onError: () => toast.error('Could not delete requirement. It may still be in use.'),
  });

  return (
    <ListSection
      title="Compliancy"
      description="Review, add, edit, and delete requirement definitions."
      isLoading={isLoading}
      isError={isError}
      errorMessage="Could not load requirements."
      emptyTitle="No requirements found"
      emptyDescription="Try a different search."
      totalItems={pagination.totalItems}
      firstItem={pagination.firstItem}
      lastItem={pagination.lastItem}
      currentPage={pagination.currentPage}
      totalPages={pagination.totalPages}
      visiblePages={pagination.visiblePages}
      setPage={setPage}
      actions={
        <Link to="/administration/access-model/compliancy/new" className={buttonVariants()}>
          Add requirement
        </Link>
      }
      filters={
        <FilterInput
          label="Search requirements"
          value={name}
          onChange={onNameChange}
          placeholder="Search by code or name"
        />
      }
      table={
        <table className="w-full min-w-[64rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Code</th>
              <th className="px-4 py-3 font-semibold">Evidence kinds</th>
              <th className="px-4 py-3 font-semibold">Sensitive</th>
              <th className="px-4 py-3 font-semibold">Status</th>
              <th className="px-4 py-3 text-right font-semibold">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {items.map((item) => (
              <tr key={item.id} className="transition hover:bg-hover-blue">
                <td className="px-4 py-4 font-medium text-foreground">{item.name}</td>
                <td className="px-4 py-4 text-muted-foreground">{item.code}</td>
                <td className="px-4 py-4 text-muted-foreground">{formatAllowedEvidenceKinds(item.allowedEvidenceKinds)}</td>
                <td className="px-4 py-4 text-muted-foreground">{item.isSensitive ? 'Yes' : 'No'}</td>
                <td className="px-4 py-4"><StatusBadge status={item.isActive ? 'Active' : 'Inactive'} /></td>
                <td className="px-4 py-4">
                  <div className="flex justify-end gap-2">
                    <button type="button" className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${item.name}`} onClick={() => onOpenRequirement(item.id)}>
                      <Pencil className="size-4" aria-hidden="true" />
                    </button>
                    <Button type="button" variant="outline" size="sm" disabled={deleteRequirement.isPending} onClick={() => deleteRequirement.mutate(item.id)}>
                      <Trash2 className="size-4" aria-hidden="true" />
                      Delete
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      mobileList={
        <div className="grid gap-3 md:hidden">
          {items.map((item) => (
            <article key={item.id} className="rounded-structural border border-border p-4">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h3 className="text-[15px] font-semibold text-foreground">{item.name}</h3>
                  <p className="mt-1 text-[13px] text-muted-foreground">{item.code}</p>
                </div>
                <StatusBadge status={item.isActive ? 'Active' : 'Inactive'} />
              </div>
              <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                <div><dt className="font-medium text-foreground">Evidence kinds</dt><dd>{formatAllowedEvidenceKinds(item.allowedEvidenceKinds)}</dd></div>
                <div><dt className="font-medium text-foreground">Sensitive</dt><dd>{item.isSensitive ? 'Yes' : 'No'}</dd></div>
              </dl>
              <div className="mt-4 flex gap-2">
                <Button type="button" variant="outline" size="sm" onClick={() => onOpenRequirement(item.id)}>
                  <Pencil className="size-4" aria-hidden="true" />
                  Edit
                </Button>
                <Button type="button" variant="outline" size="sm" disabled={deleteRequirement.isPending} onClick={() => deleteRequirement.mutate(item.id)}>
                  <Trash2 className="size-4" aria-hidden="true" />
                  Delete
                </Button>
              </div>
            </article>
          ))}
        </div>
      }
      hasItems={items.length > 0}
    />
  );
}

function PackagesPanel({ name, onNameChange, onOpenPackage, response, isLoading, isError, page, setPage }: { readonly name: string; readonly onNameChange: (value: string) => void; readonly onOpenPackage: (packageId: string) => void; readonly response: components['schemas']['PageOfPackageResponse'] | undefined; readonly isLoading: boolean; readonly isError: boolean; readonly page: number; readonly setPage: (page: number) => void; }) {
  const { t } = useTranslation();
  const items = response?.items ?? [];
  const pagination = getPaginationState(response, items.length, page, pageSize);

  return (
    <ListSection
      title={t('accessModel.tabs.packages')}
      description="Review access packages and their current status."
      isLoading={isLoading}
      isError={isError}
      errorMessage={t('accessModel.packages.couldNotLoad')}
      emptyTitle="No packages found"
      emptyDescription="Try a different search."
      totalItems={pagination.totalItems}
      firstItem={pagination.firstItem}
      lastItem={pagination.lastItem}
      currentPage={pagination.currentPage}
      totalPages={pagination.totalPages}
      visiblePages={pagination.visiblePages}
      setPage={setPage}
      actions={
        <Link to="/administration/access-model/packages/new" className={buttonVariants()}>
          Add package
        </Link>
      }
      filters={
        <FilterInput
          label="Search packages"
          value={name}
          onChange={onNameChange}
          placeholder="Search by package name"
        />
      }
      table={
        <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Description</th>
              <th className="px-4 py-3 font-semibold">Status</th>
              <th className="px-4 py-3 text-right font-semibold">Open</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {items.map((item) => (
              <tr
                key={item.id}
                className="cursor-pointer transition hover:bg-hover-blue"
                role="link"
                tabIndex={0}
                onClick={() => onOpenPackage(item.id)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    onOpenPackage(item.id);
                  }
                }}
              >
                <td className="px-4 py-4 font-medium text-foreground">{item.name}</td>
                <td className="px-4 py-4 text-muted-foreground">{item.description ?? '-'}</td>
                <td className="px-4 py-4">
                  <StatusBadge status={item.status} />
                </td>
                <td className="px-4 py-4 text-right text-muted-foreground">
                  <span className="inline-flex items-center justify-center">
                    <ChevronRight className="size-4" aria-hidden="true" />
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      mobileList={
        <div className="grid gap-3 md:hidden">
          {items.map((item) => (
            <article
              key={item.id}
              className="rounded-structural border border-border p-4 transition hover:bg-hover-blue"
              role="button"
              tabIndex={0}
              onClick={() => onOpenPackage(item.id)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault();
                  onOpenPackage(item.id);
                }
              }}
            >
              <div className="flex items-start justify-between gap-3">
                <h3 className="text-[15px] font-semibold text-foreground">{item.name}</h3>
                <ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
              </div>
              <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                <div>
                  <dt className="font-medium text-foreground">Description</dt>
                  <dd>{item.description ?? '-'}</dd>
                </div>
                <div>
                  <dt className="font-medium text-foreground">Status</dt>
                  <dd>
                    <StatusBadge status={item.status} />
                  </dd>
                </div>
              </dl>
            </article>
          ))}
        </div>
      }
      hasItems={items.length > 0}
    />
  );
}

function CataloguesPanel({ name, onNameChange, onOpenCatalogue, response, isLoading, isError, page, setPage }: { readonly name: string; readonly onNameChange: (value: string) => void; readonly onOpenCatalogue: (catalogueId: string) => void; readonly response: components['schemas']['PageOfCatalogResponse'] | undefined; readonly isLoading: boolean; readonly isError: boolean; readonly page: number; readonly setPage: (page: number) => void; }) {
  const { t } = useTranslation();
  const items = response?.items ?? [];
  const pagination = getPaginationState(response, items.length, page, pageSize);

  return (
    <ListSection
      title={t('accessModel.tabs.catalogs')}
      description="Review catalogs and their current status."
      isLoading={isLoading}
      isError={isError}
      errorMessage={t('accessModel.catalogs.couldNotLoad')}
      emptyTitle="No catalogs found"
      emptyDescription="Try a different search."
      totalItems={pagination.totalItems}
      firstItem={pagination.firstItem}
      lastItem={pagination.lastItem}
      currentPage={pagination.currentPage}
      totalPages={pagination.totalPages}
      visiblePages={pagination.visiblePages}
      setPage={setPage}
      actions={
        <Link to="/administration/access-model/catalogues/new" className={buttonVariants()}>
          Add catalog
        </Link>
      }
      filters={
        <FilterInput
          label="Search catalogs"
          value={name}
          onChange={onNameChange}
          placeholder="Search by catalog name"
        />
      }
      table={
        <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Description</th>
              <th className="px-4 py-3 font-semibold">Status</th>
              <th className="px-4 py-3 text-right font-semibold">Open</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {items.map((item) => (
              <tr
                key={item.id}
                className="cursor-pointer transition hover:bg-hover-blue"
                role="link"
                tabIndex={0}
                onClick={() => onOpenCatalogue(item.id)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    onOpenCatalogue(item.id);
                  }
                }}
              >
                <td className="px-4 py-4 font-medium text-foreground">{item.name}</td>
                <td className="px-4 py-4 text-muted-foreground">{item.description ?? '-'}</td>
                <td className="px-4 py-4">
                  <StatusBadge status={item.status} />
                </td>
                <td className="px-4 py-4 text-right text-muted-foreground">
                  <span className="inline-flex items-center justify-center">
                    <ChevronRight className="size-4" aria-hidden="true" />
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      mobileList={
        <div className="grid gap-3 md:hidden">
          {items.map((item) => (
            <article
              key={item.id}
              className="rounded-structural border border-border p-4 transition hover:bg-hover-blue"
              role="button"
              tabIndex={0}
              onClick={() => onOpenCatalogue(item.id)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault();
                  onOpenCatalogue(item.id);
                }
              }}
            >
              <div className="flex items-start justify-between gap-3">
                <h3 className="text-[15px] font-semibold text-foreground">{item.name}</h3>
                <ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
              </div>
              <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                <div>
                  <dt className="font-medium text-foreground">Description</dt>
                  <dd>{item.description ?? '-'}</dd>
                </div>
                <div>
                  <dt className="font-medium text-foreground">Status</dt>
                  <dd>
                    <StatusBadge status={item.status} />
                  </dd>
                </div>
              </dl>
            </article>
          ))}
        </div>
      }
      hasItems={items.length > 0}
    />
  );
}

function ApprovalGroupsPanel({ name, onNameChange, onOpenApprovalGroup, response, isLoading, isError, page, setPage }: { readonly name: string; readonly onNameChange: (value: string) => void; readonly onOpenApprovalGroup: (approvalGroupId: string) => void; readonly response: components['schemas']['PageOfApprovalGroupResponse'] | undefined; readonly isLoading: boolean; readonly isError: boolean; readonly page: number; readonly setPage: (page: number) => void; }) {
  const { t } = useTranslation();
  const items = response?.items ?? [];
  const pagination = getPaginationState(response, items.length, page, pageSize);

  return (
    <ListSection
      title={t('accessModel.tabs.approvalGroups')}
      description="Review approval groups and their current status."
      isLoading={isLoading}
      isError={isError}
      errorMessage={t('accessModel.approvalGroups.couldNotLoad')}
      emptyTitle="No approval groups found"
      emptyDescription="Try a different search."
      totalItems={pagination.totalItems}
      firstItem={pagination.firstItem}
      lastItem={pagination.lastItem}
      currentPage={pagination.currentPage}
      totalPages={pagination.totalPages}
      visiblePages={pagination.visiblePages}
      setPage={setPage}
      actions={
        <Link to="/administration/access-model/approval-groups/new" className={buttonVariants()}>
          Add approval group
        </Link>
      }
      filters={
        <FilterInput
          label="Search approval groups"
          value={name}
          onChange={onNameChange}
          placeholder="Search by group name"
        />
      }
      table={
        <table className="w-full min-w-[48rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Status</th>
              <th className="px-4 py-3 text-right font-semibold">Open</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {items.map((item) => (
              <tr
                key={item.id}
                className="cursor-pointer transition hover:bg-hover-blue"
                role="link"
                tabIndex={0}
                onClick={() => onOpenApprovalGroup(item.id)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    onOpenApprovalGroup(item.id);
                  }
                }}
              >
                <td className="px-4 py-4 font-medium text-foreground">{item.name}</td>
                <td className="px-4 py-4">
                  <StatusBadge status={item.status} />
                </td>
                <td className="px-4 py-4 text-right text-muted-foreground">
                  <span className="inline-flex items-center justify-center">
                    <ChevronRight className="size-4" aria-hidden="true" />
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      mobileList={
        <div className="grid gap-3 md:hidden">
          {items.map((item) => (
            <article
              key={item.id}
              className="rounded-structural border border-border p-4 transition hover:bg-hover-blue"
              role="button"
              tabIndex={0}
              onClick={() => onOpenApprovalGroup(item.id)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                  event.preventDefault();
                  onOpenApprovalGroup(item.id);
                }
              }}
            >
              <div className="flex items-start justify-between gap-3">
                <h3 className="text-[15px] font-semibold text-foreground">{item.name}</h3>
                <ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
              </div>
              <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                <div>
                  <dt className="font-medium text-foreground">Status</dt>
                  <dd>
                    <StatusBadge status={item.status} />
                  </dd>
                </div>
              </dl>
            </article>
          ))}
        </div>
      }
      hasItems={items.length > 0}
    />
  );
}

function HrPoliciesPanel({ settings, ouRules, personaRules, organizationUnits, personas, packages, isLoading, isError }: { readonly settings: EmployeeLifecycleAutomationSettingsResponse | undefined; readonly ouRules: OrganizationalUnitPackageRuleResponse[]; readonly personaRules: PersonaPackageRuleResponse[]; readonly organizationUnits: OrganizationUnitResponse[]; readonly personas: PersonaResponse[]; readonly packages: PackageResponse[]; readonly isLoading: boolean; readonly isError: boolean; }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const packageById = new Map(packages.map((item) => [item.id, item]));
  const packageOptions = packages.map((item) => ({ value: item.id, label: item.name }));
  const [settingsValues, setSettingsValues] = useState({ isEnabled: false, disableEmployeeOnLeave: false });
  const [isAddOuRuleOpen, setIsAddOuRuleOpen] = useState(false);
  const [selectedOuId, setSelectedOuId] = useState('');
  const [selectedOuPackageId, setSelectedOuPackageId] = useState('');
  const [isAddPersonaRuleOpen, setIsAddPersonaRuleOpen] = useState(false);
  const [selectedPersonaId, setSelectedPersonaId] = useState('');
  const [selectedPersonaPackageId, setSelectedPersonaPackageId] = useState('');

  useEffect(() => {
    setSettingsValues({
      isEnabled: settings?.isEnabled ?? false,
      disableEmployeeOnLeave: settings?.disableEmployeeOnLeave ?? false,
    });
  }, [settings]);

  const saveSettings = useMutation({
    mutationFn: async (request: UpdateEmployeeLifecycleAutomationSettingsRequest) => {
      const { error } = await api.PUT('/api/sagas/employee-lifecycle/settings', { body: request });
      if (error) throw new Error(t('accessModel.hrPolicies.couldNotSave'));
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'hr-policies', 'settings'] });
      toast.success(t('accessModel.hrPolicies.saved'));
    },
    onError: () => toast.error(t('accessModel.hrPolicies.couldNotSave')),
  });

  const addOuRule = useMutation({
    mutationFn: async (request: CreateOrganizationalUnitPackageRuleRequest) => {
      const { error } = await api.POST('/api/sagas/employee-lifecycle/ou-package-rules', { body: request });
      if (error) throw new Error('Could not add OU package rule.');
    },
    onSuccess: async () => {
      setSelectedOuId('');
      setSelectedOuPackageId('');
      setIsAddOuRuleOpen(false);
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'hr-policies', 'ou-rules'] });
      toast.success('OU package rule added.');
    },
    onError: () => toast.error('Could not add OU package rule.'),
  });

  const toggleOuRule = useMutation({
    mutationFn: async ({ id, isEnabled }: { id: string; isEnabled: boolean }) => {
      const request: SetRuleEnabledRequest = { isEnabled };
      const { error } = await api.PUT('/api/sagas/employee-lifecycle/ou-package-rules/{id}/enabled', { params: { path: { id } }, body: request });
      if (error) throw new Error('Could not update OU package rule.');
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'hr-policies', 'ou-rules'] });
      toast.success('OU package rule updated.');
    },
    onError: () => toast.error('Could not update OU package rule.'),
  });

  const removeOuRule = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/sagas/employee-lifecycle/ou-package-rules/{id}', { params: { path: { id } } });
      if (error) throw new Error('Could not remove OU package rule.');
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'hr-policies', 'ou-rules'] });
      toast.success('OU package rule removed.');
    },
    onError: () => toast.error('Could not remove OU package rule.'),
  });

  const addPersonaRule = useMutation({
    mutationFn: async (request: CreatePersonaPackageRuleRequest) => {
      const { error } = await api.POST('/api/sagas/employee-lifecycle/persona-package-rules', { body: request });
      if (error) throw new Error('Could not add persona package rule.');
    },
    onSuccess: async () => {
      setSelectedPersonaId('');
      setSelectedPersonaPackageId('');
      setIsAddPersonaRuleOpen(false);
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'hr-policies', 'persona-rules'] });
      toast.success('Persona package rule added.');
    },
    onError: () => toast.error('Could not add persona package rule.'),
  });

  const togglePersonaRule = useMutation({
    mutationFn: async ({ id, isEnabled }: { id: string; isEnabled: boolean }) => {
      const request: SetRuleEnabledRequest = { isEnabled };
      const { error } = await api.PUT('/api/sagas/employee-lifecycle/persona-package-rules/{id}/enabled', { params: { path: { id } }, body: request });
      if (error) throw new Error('Could not update persona package rule.');
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'hr-policies', 'persona-rules'] });
      toast.success('Persona package rule updated.');
    },
    onError: () => toast.error('Could not update persona package rule.'),
  });

  const removePersonaRule = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/sagas/employee-lifecycle/persona-package-rules/{id}', { params: { path: { id } } });
      if (error) throw new Error('Could not remove persona package rule.');
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'hr-policies', 'persona-rules'] });
      toast.success('Persona package rule removed.');
    },
    onError: () => toast.error('Could not remove persona package rule.'),
  });

  const availableOuPackages = getAvailablePackageOptions(packageOptions, new Set(ouRules.filter((rule) => rule.organizationUnitId === selectedOuId).map((rule) => rule.packageId)));
  const availablePersonaPackages = getAvailablePackageOptions(packageOptions, new Set(personaRules.filter((rule) => rule.personaId === selectedPersonaId).map((rule) => rule.packageId)));
  const ouGroups = groupOuRules(organizationUnits, ouRules, packageById);
  const personaGroups = groupPersonaRules(personas, personaRules, packageById);

  return (
    <div className="grid gap-6 pt-4">
      <div>
        <h2 className="text-[20px] font-semibold tracking-tight">{t('accessModel.hrPolicies.title')}</h2>
        <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{t('accessModel.hrPolicies.description')}</p>
      </div>

      {isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{t('accessModel.hrPolicies.couldNotLoad')}</p> : null}
      {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">{t('accessModel.hrPolicies.loading')}</p> : null}

      {!isLoading && !isError ? (
        <>
          <div className="rounded-structural border border-border p-4">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <h3 className="text-[18px] font-semibold tracking-tight">{t('accessModel.hrPolicies.lifecycleSettings')}</h3>
                <p className="mt-2 text-[14px] text-muted-foreground">{t('accessModel.hrPolicies.lifecycleDescription')}</p>
              </div>
              <Button type="button" disabled={saveSettings.isPending} onClick={() => saveSettings.mutate(settingsValues)}>{saveSettings.isPending ? t('accessModel.hrPolicies.saving') : t('accessModel.hrPolicies.saveSettings')}</Button>
            </div>

            <div className="mt-4 grid gap-4 md:grid-cols-3">
              <label className="rounded-structural border border-border bg-background p-4 text-[14px] font-medium"><span className="block text-[12px] uppercase tracking-[0.18em] text-muted-foreground">{t('accessModel.hrPolicies.automation')}</span><span className="mt-3 block text-[20px] font-semibold tracking-tight text-foreground">{settingsValues.isEnabled ? t('accessModel.common.enabled') : t('accessModel.common.disabled')}</span><span className="mt-2 block text-[13px] text-muted-foreground">{t('accessModel.hrPolicies.employeeLifecycleAutomation')}</span><span className="mt-4 flex items-center gap-3"><input type="checkbox" checked={settingsValues.isEnabled} onChange={(event) => setSettingsValues((current) => ({ ...current, isEnabled: event.target.checked }))} />{t('accessModel.common.enabled')}</span></label>
              <label className="rounded-structural border border-border bg-background p-4 text-[14px] font-medium"><span className="block text-[12px] uppercase tracking-[0.18em] text-muted-foreground">{t('accessModel.hrPolicies.disableOnLeave')}</span><span className="mt-3 block text-[20px] font-semibold tracking-tight text-foreground">{settingsValues.disableEmployeeOnLeave ? t('accessModel.hrPolicies.yes') : t('accessModel.hrPolicies.no')}</span><span className="mt-2 block text-[13px] text-muted-foreground">{t('accessModel.hrPolicies.disableOnLeaveDescription')}</span><span className="mt-4 flex items-center gap-3"><input type="checkbox" checked={settingsValues.disableEmployeeOnLeave} onChange={(event) => setSettingsValues((current) => ({ ...current, disableEmployeeOnLeave: event.target.checked }))} />{t('accessModel.common.enabled')}</span></label>
              <SummaryCard title={t('accessModel.hrPolicies.lastFullReconcile')} value={settings?.lastFullReconciledAt ? formatDateTime(settings.lastFullReconciledAt) : t('accessModel.hrPolicies.never')} hint={t('accessModel.hrPolicies.lastFullReconcileHint')} />
            </div>
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            <RuleListCard title={t('accessModel.hrPolicies.ouAutomation')} description={t('accessModel.hrPolicies.ouAutomationDescription')} empty={t('accessModel.hrPolicies.noOuRules')} action={<Button type="button" variant="outline" size="sm" disabled={addOuRule.isPending || organizationUnits.length === 0 || packageOptions.length === 0} onClick={() => setIsAddOuRuleOpen((current) => !current)}>{isAddOuRuleOpen ? t('accessModel.common.cancel') : t('accessModel.hrPolicies.addOuAutomation')}</Button>}>
              {isAddOuRuleOpen ? <RuleAddForm labelA={t('accessModel.hrPolicies.organizationalUnit')} valueA={selectedOuId} onChangeA={(value) => { setSelectedOuId(value); setSelectedOuPackageId(''); }} optionsA={organizationUnits.map((item) => ({ value: item.id, label: item.name }))} labelB={t('accessModel.hrPolicies.package')} valueB={selectedOuPackageId} onChangeB={setSelectedOuPackageId} optionsB={availableOuPackages} submitLabel={t('accessModel.hrPolicies.addAutomation')} disabled={addOuRule.isPending || !selectedOuId || !selectedOuPackageId} onSubmit={() => addOuRule.mutate({ organizationUnitId: selectedOuId, packageId: selectedOuPackageId })} emptyStateMessage={selectedOuId && availableOuPackages.length === 0 ? t('accessModel.hrPolicies.allPackagesAddedToOrganizationUnit') : undefined} /> : null}
              {ouGroups.map((group) => <RuleGroupCard key={group.source.id} name={group.source.name} rules={group.rules} onToggle={(rule) => toggleOuRule.mutate({ id: rule.id, isEnabled: !rule.isEnabled })} onRemove={(rule) => removeOuRule.mutate(rule.id)} busy={toggleOuRule.isPending || removeOuRule.isPending} />)}
            </RuleListCard>
            <RuleListCard title={t('accessModel.hrPolicies.personaAutomation')} description={t('accessModel.hrPolicies.personaAutomationDescription')} empty={t('accessModel.hrPolicies.noPersonaRules')} action={<Button type="button" variant="outline" size="sm" disabled={addPersonaRule.isPending || personas.length === 0 || packageOptions.length === 0} onClick={() => setIsAddPersonaRuleOpen((current) => !current)}>{isAddPersonaRuleOpen ? t('accessModel.common.cancel') : t('accessModel.hrPolicies.addPersonaAutomation')}</Button>}>
              {isAddPersonaRuleOpen ? <RuleAddForm labelA={t('accessModel.hrPolicies.persona')} valueA={selectedPersonaId} onChangeA={(value) => { setSelectedPersonaId(value); setSelectedPersonaPackageId(''); }} optionsA={personas.map((item) => ({ value: item.id, label: item.name }))} labelB={t('accessModel.hrPolicies.package')} valueB={selectedPersonaPackageId} onChangeB={setSelectedPersonaPackageId} optionsB={availablePersonaPackages} submitLabel={t('accessModel.hrPolicies.addAutomation')} disabled={addPersonaRule.isPending || !selectedPersonaId || !selectedPersonaPackageId} onSubmit={() => addPersonaRule.mutate({ personaId: selectedPersonaId, packageId: selectedPersonaPackageId })} emptyStateMessage={selectedPersonaId && availablePersonaPackages.length === 0 ? t('accessModel.hrPolicies.allPackagesAddedToPersona') : undefined} /> : null}
              {personaGroups.map((group) => <RuleGroupCard key={group.source.id} name={group.source.name} rules={group.rules} onToggle={(rule) => togglePersonaRule.mutate({ id: rule.id, isEnabled: !rule.isEnabled })} onRemove={(rule) => removePersonaRule.mutate(rule.id)} busy={togglePersonaRule.isPending || removePersonaRule.isPending} />)}
            </RuleListCard>
          </div>
        </>
      ) : null}
    </div>
  );
}

function ContractorPoliciesPanel({ rules, jobTypes, packages, locations, isLoading, isError }: { readonly rules: ContractorJobPackageRuleResponse[]; readonly jobTypes: JobTypeResponse[]; readonly packages: PackageResponse[]; readonly locations: LocationResponse[]; readonly isLoading: boolean; readonly isError: boolean; }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const packageById = new Map(packages.map((item) => [item.id, item]));
  const locationById = new Map(locations.map((item) => [item.id, item]));
  const packageOptions = packages.map((item) => ({ value: item.id, label: item.name }));
  const [isAddRuleOpen, setIsAddRuleOpen] = useState(false);
  const [selectedJobTypeId, setSelectedJobTypeId] = useState('');
  const [selectedPackageId, setSelectedPackageId] = useState('');
  const [selectedLocationId, setSelectedLocationId] = useState<string | null>(null);

  const addRule = useMutation({
    mutationFn: async (request: CreateContractorJobPackageRuleRequest) => {
      const { error } = await api.POST('/api/sagas/contractor-jobs/access-package-rules', { body: request });
      if (error) throw new Error(t('accessModel.contractorPolicies.couldNotAddRule'));
    },
    onSuccess: async () => {
      setSelectedJobTypeId('');
      setSelectedPackageId('');
      setSelectedLocationId(null);
      setIsAddRuleOpen(false);
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'contractor-policies', 'rules'] });
      toast.success(t('accessModel.contractorPolicies.ruleAdded'));
    },
    onError: () => toast.error(t('accessModel.contractorPolicies.couldNotAddRule')),
  });

  const toggleRule = useMutation({
    mutationFn: async ({ id, isEnabled }: { id: string; isEnabled: boolean }) => {
      const request: SetContractorJobPackageRuleEnabledRequest = { isEnabled };
      const { error } = await api.PUT('/api/sagas/contractor-jobs/access-package-rules/{id}/enabled', { params: { path: { id } }, body: request });
      if (error) throw new Error(t('accessModel.contractorPolicies.couldNotUpdateRule'));
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'contractor-policies', 'rules'] });
      toast.success(t('accessModel.contractorPolicies.ruleUpdated'));
    },
    onError: () => toast.error(t('accessModel.contractorPolicies.couldNotUpdateRule')),
  });

  const removeRule = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/sagas/contractor-jobs/access-package-rules/{id}', { params: { path: { id } } });
      if (error) throw new Error(t('accessModel.contractorPolicies.couldNotRemoveRule'));
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'contractor-policies', 'rules'] });
      toast.success(t('accessModel.contractorPolicies.ruleRemoved'));
    },
    onError: () => toast.error(t('accessModel.contractorPolicies.couldNotRemoveRule')),
  });

  const availablePackages = getAvailableContractorPackageOptions(packageOptions, rules, selectedJobTypeId, selectedLocationId);
  const groups = groupContractorRules(jobTypes, rules, packageById, locationById, t('accessModel.contractorPolicies.globalScope'));

  function handleAddRule() {
    if (!selectedJobTypeId || !selectedPackageId) {
      toast.error(t('accessModel.contractorPolicies.completeJobTypeAndPackage'));
      return;
    }

    addRule.mutate({
      jobTypeId: selectedJobTypeId,
      packageId: selectedPackageId,
      locationId: selectedLocationId,
    });
  }

  return (
    <div className="grid gap-6 pt-4">
      <div>
        <h2 className="text-[20px] font-semibold tracking-tight">{t('accessModel.contractorPolicies.title')}</h2>
        <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{t('accessModel.contractorPolicies.description')}</p>
      </div>

      {isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{t('accessModel.contractorPolicies.couldNotLoad')}</p> : null}
      {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">{t('accessModel.contractorPolicies.loading')}</p> : null}

      {!isLoading && !isError ? (
        <RuleListCard
          title={t('accessModel.contractorPolicies.jobTypeAutomation')}
          description={t('accessModel.contractorPolicies.jobTypeAutomationDescription')}
          empty={t('accessModel.contractorPolicies.noRules')}
          action={<Button type="button" variant="outline" size="sm" disabled={addRule.isPending || jobTypes.length === 0 || packageOptions.length === 0} onClick={() => setIsAddRuleOpen((current) => !current)}>{isAddRuleOpen ? t('accessModel.common.cancel') : t('accessModel.contractorPolicies.addAutomation')}</Button>}
        >
          {isAddRuleOpen ? (
            <div className="grid gap-4 rounded-structural border border-border p-4">
              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('accessModel.contractorPolicies.jobType')}</span>
                <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedJobTypeId} onChange={(event) => {
                  setSelectedJobTypeId(event.target.value);
                  setSelectedPackageId('');
                }}>
                  <option value="">{t('accessModel.common.selectPrefix', { label: t('accessModel.contractorPolicies.jobType').toLowerCase() })}</option>
                  {jobTypes.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
                </select>
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('accessModel.contractorPolicies.package')}</span>
                <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedPackageId} onChange={(event) => setSelectedPackageId(event.target.value)} disabled={!selectedJobTypeId || availablePackages.length === 0}>
                  <option value="">{t('accessModel.common.selectPrefix', { label: t('accessModel.contractorPolicies.package').toLowerCase() })}</option>
                  {availablePackages.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                </select>
              </label>
              <div className="grid gap-2 text-[14px] font-medium">
                <span>{t('accessModel.contractorPolicies.location')}</span>
                <LocationSelector value={selectedLocationId} onChange={setSelectedLocationId} level="Room" disabled={addRule.isPending} />
                <p className="text-[13px] text-muted-foreground">{t('accessModel.contractorPolicies.locationHint')}</p>
              </div>
              {selectedJobTypeId && availablePackages.length === 0 ? <p className="text-[13px] text-muted-foreground">{t('accessModel.contractorPolicies.allPackagesAddedForScope')}</p> : null}
              <div className="flex justify-end">
                <Button type="button" disabled={addRule.isPending || !selectedJobTypeId || !selectedPackageId} onClick={handleAddRule}>{t('accessModel.contractorPolicies.addRule')}</Button>
              </div>
            </div>
          ) : null}
          {groups.map((group) => <RuleGroupCard key={group.source.id} name={group.source.name} rules={group.rules} onToggle={(rule) => toggleRule.mutate({ id: rule.id, isEnabled: !rule.isEnabled })} onRemove={(rule) => removeRule.mutate(rule.id)} busy={toggleRule.isPending || removeRule.isPending} />)}
        </RuleListCard>
      ) : null}
    </div>
  );
}

function VisitorPoliciesPanel({ assignments, packages, qrCredentialTypes, visitorConfig, isLoading, isError }: { readonly assignments: AccessRuleAssignmentResponse[]; readonly packages: PackageResponse[]; readonly qrCredentialTypes: CredentialTypeResponse[]; readonly visitorConfig: components['schemas']['VisitorPreOnboardingSagaConfigRequest'] | undefined; readonly isLoading: boolean; readonly isError: boolean; }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [isAddRuleOpen, setIsAddRuleOpen] = useState(false);
  const [selectedTrigger, setSelectedTrigger] = useState<ReceptionAccessPolicyTrigger>('ExpectedVisitorAdded');
  const [selectedPackageId, setSelectedPackageId] = useState('');
  const [gracePeriodMinutes, setGracePeriodMinutes] = useState('0');
  const [selectedQrCredentialTypeId, setSelectedQrCredentialTypeId] = useState<string>('');
  const [credentialGraceStartMinutes, setCredentialGraceStartMinutes] = useState('30');
  const [credentialGraceEndMinutes, setCredentialGraceEndMinutes] = useState('30');
  const packageById = new Map(packages.map((item) => [item.id, item]));
  const visitorAssignments = assignments.filter((assignment) => assignment.trigger !== 'ContractorExpectedAdded' && assignment.trigger !== 'ContractorOnboarded');

  useEffect(() => {
    setSelectedQrCredentialTypeId(visitorConfig?.qrCredentialTypeId ?? '');
    setCredentialGraceStartMinutes(String(visitorConfig?.graceStartMinutes ?? 30));
    setCredentialGraceEndMinutes(String(visitorConfig?.graceEndMinutes ?? 30));
  }, [visitorConfig?.qrCredentialTypeId, visitorConfig?.graceStartMinutes, visitorConfig?.graceEndMinutes]);

  const createAssignment = useMutation({
    mutationFn: async (request: CreateAccessRuleAssignmentRequest) => {
      const { error } = await api.POST('/api/reception/access-rule-assignments', { body: request });
      if (error) throw new Error(t('accessModel.visitorPolicies.couldNotCreateTriggerAssignment'));
    },
    onSuccess: async () => {
      setSelectedTrigger('ExpectedVisitorAdded');
      setSelectedPackageId('');
      setGracePeriodMinutes('0');
      setIsAddRuleOpen(false);
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'visitor-policies', 'assignments'] });
      toast.success(t('accessModel.visitorPolicies.triggerAssignmentAdded'));
    },
    onError: () => toast.error(t('accessModel.visitorPolicies.couldNotCreateTriggerAssignment')),
  });

  const removeAssignment = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/reception/access-rule-assignments/{id}', { params: { path: { id } } });
      if (error) throw new Error(t('accessModel.visitorPolicies.couldNotRemoveTriggerAssignment'));
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'access-model', 'visitor-policies', 'assignments'] });
      toast.success(t('accessModel.visitorPolicies.triggerAssignmentRemoved'));
    },
    onError: () => toast.error(t('accessModel.visitorPolicies.couldNotRemoveTriggerAssignment')),
  });

  const updateVisitorConfig = useMutation({
    mutationFn: async () => {
      const parsedGraceStartMinutes = Number.parseInt(credentialGraceStartMinutes, 10);
      const parsedGraceEndMinutes = Number.parseInt(credentialGraceEndMinutes, 10);

      if (Number.isNaN(parsedGraceStartMinutes) || parsedGraceStartMinutes < 0 || Number.isNaN(parsedGraceEndMinutes) || parsedGraceEndMinutes < 0) {
        throw new Error(t('accessModel.visitorPolicies.graceValidation'));
      }

      return updateVisitorPreOnboardingConfig({
        ...(visitorConfig ?? {
          useCustomInviteNotification: false,
          customInviteNotification: null,
          qrCredentialTypeId: null,
          graceStartMinutes: 30,
          graceEndMinutes: 30,
          sendConfirmNotificationToHost: false,
          useCustomConfirmNotification: false,
          customConfirmNotification: null,
          sendCancellationNotification: false,
          useCustomCancellationNotification: false,
          customCancellationNotification: null,
          sendRescheduleNotification: false,
          useCustomRescheduleNotification: false,
          customRescheduleNotification: null,
          sendRelocationNotification: false,
          useCustomRelocationNotification: false,
          customRelocationNotification: null,
          sendArrivalNotificationToHost: false,
          useCustomArrivalNotification: false,
          customArrivalNotification: null,
        }),
        qrCredentialTypeId: selectedQrCredentialTypeId || null,
        graceStartMinutes: parsedGraceStartMinutes,
        graceEndMinutes: parsedGraceEndMinutes,
      });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: visitorPreOnboardingConfigQueryKey });
      toast.success(t('accessModel.visitorPolicies.saveQrSettingsSaved'));
    },
    onError: (error) => toast.error(error.message === t('accessModel.visitorPolicies.graceValidation') ? error.message : t('accessModel.visitorPolicies.couldNotSaveQrSettings')),
  });

  function handleAddAssignment() {
    const parsedGracePeriodMinutes = Number.parseInt(gracePeriodMinutes, 10);
    if (!selectedPackageId || Number.isNaN(parsedGracePeriodMinutes) || parsedGracePeriodMinutes < 0) {
      toast.error(t('accessModel.visitorPolicies.completeTriggerPackageGrace'));
      return;
    }

    createAssignment.mutate({
      packageId: selectedPackageId,
      trigger: selectedTrigger,
      gracePeriodMinutes: parsedGracePeriodMinutes,
    });
  }

  return (
    <div className="grid gap-6 pt-4">
      <div>
        <h2 className="text-[20px] font-semibold tracking-tight">{t('accessModel.visitorPolicies.title')}</h2>
        <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{t('accessModel.visitorPolicies.description')}</p>
      </div>

      {isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{t('accessModel.visitorPolicies.couldNotLoad')}</p> : null}
      {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">{t('accessModel.visitorPolicies.loading')}</p> : null}

      {!isLoading && !isError ? (
        <div className="grid gap-5">
          <div className="rounded-structural border border-border bg-content p-5">
            <div>
                <h3 className="text-[18px] font-semibold tracking-tight">{t('accessModel.visitorPolicies.qrCredentialTitle')}</h3>
                <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{t('accessModel.visitorPolicies.qrCredentialDescription')}</p>
              </div>

            <div className="mt-5 grid gap-4 lg:max-w-3xl lg:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium lg:col-span-2">
                <span>{t('accessModel.visitorPolicies.credentialType')}</span>
                <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedQrCredentialTypeId} onChange={(event) => setSelectedQrCredentialTypeId(event.target.value)}>
                  <option value="">{t('accessModel.visitorPolicies.noCredentialTypeConfigured')}</option>
                  {qrCredentialTypes.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
                </select>
                <span className="text-[12px] font-normal text-muted-foreground">{t('accessModel.visitorPolicies.credentialTypeHint')}</span>
              </label>

              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('accessModel.visitorPolicies.graceBeforeStart')}</span>
                <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" type="number" min="0" step="1" value={credentialGraceStartMinutes} onChange={(event) => setCredentialGraceStartMinutes(event.target.value)} />
              </label>

              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('accessModel.visitorPolicies.graceAfterEnd')}</span>
                <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" type="number" min="0" step="1" value={credentialGraceEndMinutes} onChange={(event) => setCredentialGraceEndMinutes(event.target.value)} />
              </label>
            </div>

            <div className="mt-4 space-y-2 text-[13px] text-muted-foreground">
              {!selectedQrCredentialTypeId ? <p>{t('accessModel.visitorPolicies.retryUntilConfigured')}</p> : null}
            </div>

            <div className="mt-5 flex justify-end">
              <Button type="button" variant="outline" disabled={updateVisitorConfig.isPending} onClick={() => updateVisitorConfig.mutate()}>{updateVisitorConfig.isPending ? t('accessModel.hrPolicies.saving') : t('accessModel.visitorPolicies.saveQrSettings')}</Button>
            </div>
          </div>

          <div className="rounded-structural border border-border bg-content p-5">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <h3 className="text-[18px] font-semibold tracking-tight">{t('accessModel.visitorPolicies.receptionTriggerAssignments')}</h3>
                <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{t('accessModel.visitorPolicies.receptionTriggerAssignmentsDescription')}</p>
              </div>
              <Button type="button" variant="outline" size="sm" disabled={createAssignment.isPending || packages.length === 0} onClick={() => setIsAddRuleOpen((current) => !current)}>{isAddRuleOpen ? t('accessModel.common.cancel') : t('accessModel.visitorPolicies.addTriggerAssignment')}</Button>
            </div>

            {isAddRuleOpen ? (
            <div className="mt-4 grid gap-3 rounded-structural border border-border p-4 lg:max-w-3xl">
              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('accessModel.visitorPolicies.trigger')}</span>
                <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedTrigger} onChange={(event) => setSelectedTrigger(event.target.value as ReceptionAccessPolicyTrigger)}>
                  {visitorTriggerOptions.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
                </select>
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('accessModel.visitorPolicies.package')}</span>
                <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedPackageId} onChange={(event) => setSelectedPackageId(event.target.value)}>
                  <option value="">{t('accessModel.visitorPolicies.selectPackage')}</option>
                  {packages.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
                </select>
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('accessModel.visitorPolicies.gracePeriodMinutes')}</span>
                <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" type="number" min="0" step="1" value={gracePeriodMinutes} onChange={(event) => setGracePeriodMinutes(event.target.value)} />
              </label>
              <div className="flex justify-end">
                <Button type="button" disabled={createAssignment.isPending || !selectedPackageId} onClick={handleAddAssignment}>{createAssignment.isPending ? t('accessModel.visitorPolicies.adding') : t('accessModel.visitorPolicies.addAssignment')}</Button>
              </div>
            </div>
            ) : null}

          {visitorAssignments.length === 0 ? (
            <p className="mt-4 text-[14px] text-muted-foreground">{t('accessModel.visitorPolicies.noVisitorTriggerAssignments')}</p>
          ) : (
            <>
              <div className="mt-4 grid gap-3 md:hidden">
                {visitorAssignments.map((assignment) => (
                  <article key={assignment.id} className="rounded-structural border border-border p-4">
                    <p className="font-medium text-foreground">{getVisitorTriggerLabel(assignment.trigger)}</p>
                    <p className="mt-1 text-[14px] text-muted-foreground">{t('accessModel.visitorPolicies.packageLabel', { value: packageById.get(assignment.packageId)?.name ?? assignment.packageId })}</p>
                    <p className="mt-1 text-[14px] text-muted-foreground">{t('accessModel.visitorPolicies.graceLabel', { value: assignment.gracePeriodMinutes })}</p>
                    <div className="mt-4 flex justify-end">
                      <Button type="button" variant="outline" size="sm" disabled={removeAssignment.isPending} onClick={() => removeAssignment.mutate(assignment.id)}>{t('accessModel.common.remove')}</Button>
                    </div>
                  </article>
                ))}
              </div>

              <div className="mt-4 hidden overflow-x-auto rounded-structural border border-border md:block">
                <table className="w-full min-w-[48rem] border-collapse text-left text-[14px]">
                  <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                    <tr>
                      <th className="px-4 py-3 font-semibold">{t('accessModel.visitorPolicies.trigger')}</th>
                      <th className="px-4 py-3 font-semibold">{t('accessModel.visitorPolicies.package')}</th>
                      <th className="px-4 py-3 font-semibold">{t('accessModel.visitorPolicies.grace')}</th>
                      <th className="px-4 py-3 text-right font-semibold">{t('accessModel.visitorPolicies.actions')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {visitorAssignments.map((assignment) => (
                      <tr key={assignment.id}>
                        <td className="px-4 py-4 font-medium text-foreground">{getVisitorTriggerLabel(assignment.trigger)}</td>
                        <td className="px-4 py-4 text-muted-foreground">{packageById.get(assignment.packageId)?.name ?? assignment.packageId}</td>
                        <td className="px-4 py-4 text-muted-foreground">{t('accessModel.visitorPolicies.graceLabel', { value: assignment.gracePeriodMinutes })}</td>
                        <td className="px-4 py-4 text-right">
                          <Button type="button" variant="outline" size="sm" disabled={removeAssignment.isPending} onClick={() => removeAssignment.mutate(assignment.id)}>{t('accessModel.common.remove')}</Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
          </div>
        </div>
      ) : null}
    </div>
  );
}

function ListSection({ title, description, isLoading, isError, errorMessage, emptyTitle, emptyDescription, totalItems, firstItem, lastItem, currentPage, totalPages, visiblePages, setPage, actions, filters, table, mobileList, hasItems }: { readonly title: string; readonly description: string; readonly isLoading: boolean; readonly isError: boolean; readonly errorMessage: string; readonly emptyTitle: string; readonly emptyDescription: string; readonly totalItems: number; readonly firstItem: number; readonly lastItem: number; readonly currentPage: number; readonly totalPages: number; readonly visiblePages: readonly (number | 'ellipsis')[]; readonly setPage: (page: number) => void; readonly actions?: React.ReactNode; readonly filters: React.ReactNode; readonly table: React.ReactNode; readonly mobileList: React.ReactNode; readonly hasItems: boolean; }) {
  const { t } = useTranslation();

  return (
    <div className="grid gap-4 pt-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">{title}</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{description}</p>
        </div>
        {actions ? <div>{actions}</div> : null}
      </div>
      <div className="grid gap-3 rounded-structural border border-border p-4 md:grid-cols-2">{filters}</div>
      {isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{errorMessage}</p> : null}
      {!isLoading && !isError && !hasItems ? (
        <Empty><EmptyHeader><EmptyTitle>{emptyTitle}</EmptyTitle><EmptyDescription>{emptyDescription}</EmptyDescription></EmptyHeader></Empty>
      ) : (
        <div className="grid gap-4">
          <div className="md:hidden">{isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">{t('accessModel.common.loading')}</p> : null}{!isLoading ? mobileList : null}</div>
          <div className="hidden overflow-x-auto rounded-structural border border-border md:block">{isLoading ? <p className="px-4 py-5 text-[14px] text-muted-foreground">{t('accessModel.common.loading')}</p> : table}</div>
          {!isLoading && !isError && totalItems > 0 ? (
            <div className="flex flex-col gap-3 text-[14px] text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
              <p>{t('accessModel.common.showing', { first: firstItem, last: lastItem, total: totalItems })}</p>
              <Pagination className="sm:mx-0 sm:w-auto"><PaginationContent><PaginationItem><PaginationPrevious disabled={currentPage === 0} onClick={() => setPage(Math.max(0, currentPage - 1))} /></PaginationItem>{visiblePages.map((visiblePage, index) => visiblePage === 'ellipsis' ? <PaginationItem key={`${visiblePage}-${index}`}><PaginationEllipsis /></PaginationItem> : <PaginationItem key={visiblePage}><PaginationLink isActive={visiblePage === currentPage} onClick={() => setPage(visiblePage)}>{visiblePage + 1}</PaginationLink></PaginationItem>)}<PaginationItem><PaginationNext disabled={currentPage >= totalPages - 1} onClick={() => setPage(Math.min(totalPages - 1, currentPage + 1))} /></PaginationItem></PaginationContent></Pagination>
            </div>
          ) : null}
        </div>
      )}
    </div>
  );
}

function SummaryCard({ title, value, hint }: { readonly title: string; readonly value: string; readonly hint: string; }) {
  return <div className="rounded-structural border border-border bg-background p-4"><p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{title}</p><p className="mt-3 text-[20px] font-semibold tracking-tight text-foreground">{value}</p><p className="mt-2 text-[13px] text-muted-foreground">{hint}</p></div>;
}

function RuleListCard({ title, description, empty, action, children }: { readonly title: string; readonly description: string; readonly empty: string; readonly action?: React.ReactNode; readonly children: React.ReactNode; }) {
  const items = Array.isArray(children) ? children.filter(Boolean) : children ? [children] : [];
  return <div className="rounded-structural border border-border p-4"><div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between"><div><h3 className="text-[18px] font-semibold tracking-tight">{title}</h3><p className="mt-2 text-[14px] text-muted-foreground">{description}</p></div>{action ? <div>{action}</div> : null}</div>{items.length === 0 ? <p className="mt-4 text-[14px] text-muted-foreground">{empty}</p> : <div className="mt-4 grid gap-3">{children}</div>}</div>;
}

function RuleGroupCard({ name, rules, onToggle, onRemove, busy }: { readonly name: string; readonly rules: readonly RuleWithPackageName[]; readonly onToggle: (rule: RuleWithPackageName) => void; readonly onRemove: (rule: RuleWithPackageName) => void; readonly busy: boolean; }) {
  return <div className="rounded-structural border border-border p-4"><div className="flex items-center justify-between gap-3"><div className="min-w-0"><p className="font-medium text-foreground">{name}</p><p className="mt-1 text-[14px] text-muted-foreground">{rules.length} package{rules.length === 1 ? '' : 's'} configured</p></div><Badge variant="secondary">{rules.length}</Badge></div><div className="mt-4 grid gap-3">{rules.map((rule) => <RulePackageRow key={rule.id} packageName={rule.packageName} detail={rule.detail} isEnabled={rule.isEnabled} onToggle={() => onToggle(rule)} onRemove={() => onRemove(rule)} busy={busy} />)}</div></div>;
}

function RulePackageRow({ packageName, detail, isEnabled, onToggle, onRemove, busy }: { readonly packageName: string; readonly detail?: string; readonly isEnabled: boolean; readonly onToggle: () => void; readonly onRemove: () => void; readonly busy: boolean; }) {
  const { t } = useTranslation();

  return <div className="flex items-center justify-between gap-4 rounded-structural border border-border bg-background p-3"><div className="min-w-0"><p className="font-medium text-foreground">{packageName}</p><p className="mt-1 text-[14px] text-muted-foreground">{detail ?? t('accessModel.common.packageAutomation')}</p></div><div className="flex items-center gap-2"><Badge variant={isEnabled ? 'success' : 'secondary'}>{isEnabled ? t('accessModel.common.enabled') : t('accessModel.common.disabled')}</Badge><Button type="button" variant="outline" size="sm" disabled={busy} onClick={onToggle}>{isEnabled ? t('accessModel.common.disable') : t('accessModel.common.enable')}</Button><Button type="button" variant="outline" size="sm" disabled={busy} onClick={onRemove}>{t('accessModel.common.remove')}</Button></div></div>;
}

function RuleAddForm({ labelA, valueA, onChangeA, optionsA, labelB, valueB, onChangeB, optionsB, submitLabel, disabled, onSubmit, emptyStateMessage }: { readonly labelA: string; readonly valueA: string; readonly onChangeA: (value: string) => void; readonly optionsA: readonly RuleOption[]; readonly labelB: string; readonly valueB: string; readonly onChangeB: (value: string) => void; readonly optionsB: readonly RuleOption[]; readonly submitLabel: string; readonly disabled: boolean; readonly onSubmit: () => void; readonly emptyStateMessage?: string; }) {
  const { t } = useTranslation();

  return <div className="grid gap-3 rounded-structural border border-border p-4"><label className="grid gap-2 text-[14px] font-medium"><span>{labelA}</span><select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={valueA} onChange={(event) => onChangeA(event.target.value)}><option value="">{t('accessModel.common.selectPrefix', { label: labelA.toLowerCase() })}</option>{optionsA.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></label><label className="grid gap-2 text-[14px] font-medium"><span>{labelB}</span><select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={valueB} onChange={(event) => onChangeB(event.target.value)} disabled={!valueA || optionsB.length === 0}><option value="">{t('accessModel.common.selectPrefix', { label: labelB.toLowerCase() })}</option>{optionsB.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></label>{emptyStateMessage ? <p className="text-[13px] text-muted-foreground">{emptyStateMessage}</p> : null}<div className="flex justify-end"><Button type="button" disabled={disabled} onClick={onSubmit}>{submitLabel}</Button></div></div>;
}

function getAvailablePackageOptions(options: readonly RuleOption[], usedPackageIds: ReadonlySet<string>) {
  return options.filter((option) => !usedPackageIds.has(option.value));
}

function groupOuRules(organizationUnits: readonly OrganizationUnitResponse[], rules: readonly OrganizationalUnitPackageRuleResponse[], packageById: ReadonlyMap<string, PackageResponse>) {
  return groupRulesBySource(organizationUnits, rules, (rule) => rule.organizationUnitId, (rule) => ({ id: rule.id, packageName: packageById.get(rule.packageId)?.name ?? rule.packageId, isEnabled: rule.isEnabled }));
}

function groupPersonaRules(personas: readonly PersonaResponse[], rules: readonly PersonaPackageRuleResponse[], packageById: ReadonlyMap<string, PackageResponse>) {
  return groupRulesBySource(personas, rules, (rule) => rule.personaId, (rule) => ({ id: rule.id, packageName: packageById.get(rule.packageId)?.name ?? rule.packageId, isEnabled: rule.isEnabled }));
}

function groupContractorRules(jobTypes: readonly JobTypeResponse[], rules: readonly ContractorJobPackageRuleResponse[], packageById: ReadonlyMap<string, PackageResponse>, locationById: ReadonlyMap<string, LocationResponse>, globalScopeLabel: string) {
  return groupRulesBySource(jobTypes, rules, (rule) => rule.jobTypeId, (rule) => ({
    id: rule.id,
    packageName: packageById.get(rule.packageId)?.name ?? rule.packageId,
    isEnabled: rule.isEnabled,
    detail: rule.locationId ? getLocationLabel(locationById.get(rule.locationId)) : globalScopeLabel,
  }));
}

function getAvailableContractorPackageOptions(options: readonly RuleOption[], rules: readonly ContractorJobPackageRuleResponse[], jobTypeId: string, locationId: string | null) {
  if (!jobTypeId) {
    return [];
  }

  const usedPackageIds = new Set(
    rules
      .filter((rule) => rule.jobTypeId === jobTypeId && rule.locationId === locationId)
      .map((rule) => rule.packageId),
  );

  return options.filter((option) => !usedPackageIds.has(option.value));
}

function groupRulesBySource<TSource extends { id: string; name: string }, TRule>(sources: readonly TSource[], rules: readonly TRule[], getSourceId: (rule: TRule) => string, toRule: (rule: TRule) => RuleWithPackageName): RuleGroup<TSource>[] {
  const sourceById = new Map(sources.map((item) => [item.id, item]));
  const groups = new Map<string, RuleGroup<TSource>>();

  rules.forEach((rule) => {
    const sourceId = getSourceId(rule);
    const source = sourceById.get(sourceId);
    if (!source) {
      return;
    }

    const existingGroup = groups.get(sourceId);
    const mappedRule = toRule(rule);

    if (existingGroup) {
      groups.set(sourceId, { ...existingGroup, rules: [...existingGroup.rules, mappedRule] });
      return;
    }

    groups.set(sourceId, { source, rules: [mappedRule] });
  });

  return Array.from(groups.values()).sort((left, right) => left.source.name.localeCompare(right.source.name));
}

function FilterInput({ label, value, onChange, placeholder }: { readonly label: string; readonly value: string; readonly onChange: (value: string) => void; readonly placeholder: string; }) {
  return <label className="grid gap-2 text-[14px] font-medium md:max-w-md"><span>{label}</span><input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={value} onChange={(event) => onChange(event.target.value)} placeholder={placeholder} /></label>;
}

function StatusBadge({ status }: { readonly status: string; }) {
  return <Badge variant={status === 'Active' || status === 'Enabled' ? 'success' : 'secondary'}>{status}</Badge>;
}

function getVisitorTriggerLabel(trigger: ReceptionAccessPolicyTrigger) {
  return visitorTriggerOptions.find((option) => option.value === trigger)?.label ?? trigger;
}

function getActiveTab(searchStr: string): AccessModelTab {
  const tab = new URLSearchParams(searchStr).get('tab');
  return isAccessModelTab(tab) ? tab : 'packages';
}

function isAccessModelTab(value: string | null | undefined): value is AccessModelTab {
  return value === 'packages' || value === 'catalogues' || value === 'approval-groups' || value === 'compliancy' || value === 'hr-policies' || value === 'contractor-policies' || value === 'visitor-policies';
}

function getPaginationState(page: { currentPage?: number | string; totalPages?: null | number | string; totalItems?: null | number | string } | undefined, itemCount: number, requestedPage: number, resolvedPageSize: number): PaginationState {
  const totalItems = Number(page?.totalItems ?? itemCount);
  const totalPages = Math.max(Number(page?.totalPages ?? 1), 1);
  const currentPage = Math.min(Number(page?.currentPage ?? requestedPage), totalPages - 1);
  const firstItem = totalItems === 0 ? 0 : currentPage * resolvedPageSize + 1;
  const lastItem = Math.min((currentPage + 1) * resolvedPageSize, totalItems);
  const visiblePages = getVisiblePages(totalPages, currentPage);
  return { currentPage, firstItem, lastItem, totalItems, totalPages, visiblePages };
}

function getVisiblePages(totalPages: number, currentPage: number) {
  if (totalPages <= 5) {
    return Array.from({ length: totalPages }, (_, index) => index);
  }

  const pages = new Set([0, totalPages - 1, currentPage - 1, currentPage, currentPage + 1]);
  const sortedPages = Array.from(pages).filter((pageNumber) => pageNumber >= 0 && pageNumber < totalPages).sort((left, right) => left - right);
  const visiblePages: Array<number | 'ellipsis'> = [];
  sortedPages.forEach((pageNumber, index) => {
    if (index > 0 && pageNumber - sortedPages[index - 1] > 1) {
      visiblePages.push('ellipsis');
    }
    visiblePages.push(pageNumber);
  });
  return visiblePages;
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function formatAllowedEvidenceKinds(allowedEvidenceKinds: Array<string | null> | null | undefined) {
  if (!allowedEvidenceKinds || allowedEvidenceKinds.length === 0) return 'None';
  return allowedEvidenceKinds.filter((item): item is string => item !== null).map((item) => item === 'CourseCompletion' ? 'Course completion' : item === 'RequirementWaiver' ? 'Requirement waiver' : 'Document').join(', ');
}
