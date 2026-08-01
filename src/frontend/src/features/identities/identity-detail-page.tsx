import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft, ChevronRight } from 'lucide-react';
import { Fragment, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getLocationLabel, type LocationResponse } from '@/shared/components/location-selector';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { i18n } from '@/shared/i18n/i18n';

type AccessControlSystemResponse = components['schemas']['AccessControlSystemResponse'];
type AccessLevelTargetResponse = components['schemas']['AccessLevelTargetResponse'];
type AccessItemResponse = components['schemas']['AccessItemResponse'];
type AccessGrantMaterializationOutcomeResponse = components['schemas']['AccessGrantMaterializationOutcomeResponse'];
type AccessGrantResponse = components['schemas']['AccessGrantResponse'];
type CredentialPACSAssignmentResponse = components['schemas']['CredentialPACSAssignmentResponse'];
type CredentialResponse = components['schemas']['CredentialResponse'];
type CredentialTypeResponse = components['schemas']['CredentialTypeResponse'];
type EmployeeResponse = components['schemas']['EmployeeResponse'];
type IdentityAffiliationSummaryResponse = components['schemas']['IdentityAffiliationSummaryResponse'];
type IdentityResponse = components['schemas']['IdentityResponse'];
type PackageAccessItemResponse = components['schemas']['PackageAccessItemResponse'];
type PackageRequestDetailDecisionResponse = components['schemas']['PackageRequestDetailDecisionResponse'];
type PackageRequestDetailFlowResponse = components['schemas']['PackageRequestDetailFlowResponse'];
type PackageRequestDetailResponse = components['schemas']['PackageRequestDetailResponse'];
type PackageRequestDetailRequirementResponse = components['schemas']['PackageRequestDetailRequirementResponse'];
type PACSAssignmentResponse = components['schemas']['PACSAssignmentResponse'];
type PACSProvisioningResponse = components['schemas']['PACSProvisioningResponse'];
type PACSSubjectResponse = components['schemas']['PACSSubjectResponse'];
type PackageRequestResponse = components['schemas']['PackageRequestResponse'];
type PackageResponse = components['schemas']['PackageResponse'];
type PackageRequestStatus = components['schemas']['PackageRequestStatus'];
type VisitorResponse = components['schemas']['VisitorResponse'];

type IdentitySection = 'overview' | 'assignments' | 'credentials' | 'known-in' | 'requests';

export default function IdentityDetailPage() {
  const { t } = useTranslation();
  const { identityId } = useParams({ from: '/main/security-officer/identities/$identityId' });
  const navigate = useNavigate();
  const [section, setSection] = useState<IdentitySection>('overview');
  const sections: readonly { id: IdentitySection; label: string; description: string }[] = [
    { id: 'overview', label: t('identities.detail.overview.label'), description: t('identities.detail.overview.description') },
    { id: 'assignments', label: t('identities.detail.assignments.label'), description: t('identities.detail.assignments.description') },
    { id: 'credentials', label: t('identities.detail.credentials.label'), description: t('identities.detail.credentials.description') },
    { id: 'known-in', label: t('identities.detail.knownIn.label'), description: t('identities.detail.knownIn.description') },
    { id: 'requests', label: t('identities.detail.requests.label'), description: t('identities.detail.requests.description') },
  ];

  const identityQuery = useQuery({
    queryKey: ['security-officer', 'identity-360', identityId, 'identity'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/identities/{id}', { params: { path: { id: identityId } } });
      if (error || !data) {
        throw new Error(t('identities.detail.couldNotLoad'));
      }
      return data;
    },
  });

  const systemsQuery = useQuery({
    queryKey: ['security-officer', 'identity-360', identityId, 'systems'],
    enabled: section === 'assignments' || section === 'credentials' || section === 'known-in',
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/systems', { params: { query: { Name: undefined, Page: 0, PageSize: 200 } as never } });
      if (error) {
        throw new Error('Could not load access control systems.');
      }
      return new Map((data?.items ?? []).map((item: AccessControlSystemResponse) => [item.id, item]));
    },
  });

  const employeeDetailsQuery = useQuery({
    queryKey: ['security-officer', 'identity-360', identityId, 'employee-details', identityQuery.data?.employeeAffiliations.map((item) => item.sourceId).join(',') ?? ''],
    enabled: section === 'overview' && (identityQuery.data?.employeeAffiliations.length ?? 0) > 0,
    queryFn: async () => {
      const items = await Promise.all((identityQuery.data?.employeeAffiliations ?? []).map(async (affiliation: IdentityAffiliationSummaryResponse) => {
        const { data, error } = await api.GET('/api/employees/employees/{id}', { params: { path: { id: affiliation.sourceId } } });
        if (error || !data) {
          throw new Error('Could not load employee details.');
        }
        return data;
      }));

      return items;
    },
  });

  const visitorDetailsQuery = useQuery({
    queryKey: ['security-officer', 'identity-360', identityId, 'visitor-details', identityQuery.data?.visitorAffiliations.map((item) => item.sourceId).join(',') ?? ''],
    enabled: section === 'overview' && (identityQuery.data?.visitorAffiliations.length ?? 0) > 0,
    queryFn: async () => {
      const items = await Promise.all((identityQuery.data?.visitorAffiliations ?? []).map(async (affiliation: IdentityAffiliationSummaryResponse) => {
        const { data, error } = await api.GET('/api/visitors/visitors/{id}', { params: { path: { id: affiliation.sourceId } } });
        if (error || !data) {
          throw new Error('Could not load visitor details.');
        }
        return data;
      }));

      return items;
    },
  });

  const employeeWorkLocationLabelsQuery = useQuery({
    queryKey: ['security-officer', 'identity-360', identityId, 'employee-work-location-labels', employeeDetailsQuery.data?.map((item) => item.id).join(',') ?? ''],
    enabled: section === 'overview' && (employeeDetailsQuery.data?.length ?? 0) > 0,
    queryFn: async () => {
      const locationIds = Array.from(new Set((employeeDetailsQuery.data ?? []).flatMap((employee) => employee.workLocations.map((item) => item.locationId))));
      const locations = await Promise.all(locationIds.map(async (locationId) => {
        const { data, error } = await api.GET('/api/locations/locations/{id}', { params: { path: { id: locationId } } });
        if (error || !data) {
          return null;
        }
        return data;
      }));

      return new Map(locations.filter((item): item is LocationResponse => item !== null).map((item) => [item.id, getLocationLabel(item)]));
    },
  });

  const assignmentsQuery = useQuery({
    queryKey: ['security-officer', 'identity-360', identityId, 'assignments'],
    enabled: section === 'assignments',
    queryFn: async () => {
      const [grantsResult, assignmentsResult, provisioningsResult] = await Promise.all([
        api.GET('/api/access-catalog/access-grants', { params: { query: { IdentityId: identityId, PackageId: undefined, Status: undefined, Page: 0, PageSize: 200 } as never } }),
        api.GET('/api/access-control/assignments', { params: { query: { SourceAssignmentId: undefined, IdentityId: identityId, AccessControlSystemId: undefined, Status: undefined, Page: 0, PageSize: 200 } as never } }),
        api.GET('/api/access-control/provisionings', { params: { query: { IdentityId: identityId, AccessControlSystemId: undefined, Status: undefined, Page: 0, PageSize: 200 } as never } }),
      ]);

      if (grantsResult.error || assignmentsResult.error || provisioningsResult.error) {
        throw new Error('Could not load assignments.');
      }

      const grants = grantsResult.data?.items ?? [];
      const assignments = assignmentsResult.data?.items ?? [];
      const provisionings = provisioningsResult.data?.items ?? [];

      const packageIds = Array.from(new Set(grants.map((item: AccessGrantResponse) => item.packageId)));
      const locationIds = Array.from(new Set(grants.flatMap((item: AccessGrantResponse) => item.locationIds ?? [])));

      const packageAccessItems = await Promise.all(packageIds.map(async (packageId) => {
        const { data, error } = await api.GET('/api/access-catalog/packages/{packageId}/access-items', { params: { path: { packageId }, query: { Page: 0, PageSize: 200 } } });
        if (error) {
          return [] as PackageAccessItemResponse[];
        }

        return (data?.items ?? []) as PackageAccessItemResponse[];
      }));

      const accessItemIds = Array.from(new Set([
        ...grants.map((item: AccessGrantResponse) => item.accessItemId).filter((item): item is string => Boolean(item)),
        ...packageAccessItems.flat().map((item) => item.accessItemId),
      ]));

      const [packages, accessItems, locations] = await Promise.all([
        Promise.all(packageIds.map(async (packageId) => {
          const { data, error } = await api.GET('/api/access-catalog/packages/{packageId}', { params: { path: { packageId } } });
          if (error || !data) return null;
          return data;
        })),
        Promise.all(accessItemIds.map(async (itemId) => {
          const { data, error } = await api.GET('/api/access-control/items/{itemId}', { params: { path: { itemId } } });
          if (error || !data) return null;
          return data;
        })),
        Promise.all(locationIds.map(async (locationId) => {
          const { data, error } = await api.GET('/api/locations/locations/{id}', { params: { path: { id: locationId } } });
          if (error || !data) return null;
          return data;
        })),
      ]);

      const targets = await Promise.all(accessItemIds.map(async (itemId) => {
        const { data, error } = await api.GET('/api/access-control/items/{itemId}/targets', { params: { path: { itemId }, query: { Page: 0, PageSize: 200 } } });
        if (error) {
          return [] as AccessLevelTargetResponse[];
        }

        return (data?.items ?? []) as AccessLevelTargetResponse[];
      }));

      const requestIds = Array.from(new Set(grants.filter((item: AccessGrantResponse) => item.sourceKind === 'CatalogRequest').map((item: AccessGrantResponse) => item.sourceId)));
      const requestDetails = await Promise.all(requestIds.map(async (requestId) => {
        const { data, error } = await api.GET('/api/access-catalog/package-requests/{requestId}/details', { params: { path: { requestId } } });
        if (error || !data) {
          return null;
        }

        return data;
      }));

      const accessItemsById = new Map(accessItems.filter((item): item is AccessItemResponse => item !== null).map((item) => [item.id, item]));
      const packageAccessItemsByPackageId = new Map<string, AccessItemResponse[]>();

      packageIds.forEach((packageId, index) => {
        const items = (packageAccessItems[index] ?? [])
          .map((link) => accessItemsById.get(link.accessItemId))
          .filter((item): item is AccessItemResponse => item !== undefined);
        packageAccessItemsByPackageId.set(packageId, items);
      });

      return {
        grants,
        assignments,
        provisionings,
        packagesById: new Map(packages.filter((item): item is PackageResponse => item !== null).map((item) => [item.id, item])),
        accessItemsById,
        packageAccessItemsByPackageId,
        locationsById: new Map(locations.filter((item): item is LocationResponse => item !== null).map((item) => [item.id, item])),
        targetsById: new Map(targets.flat().map((item) => [item.id, item])),
        requestDetailsById: new Map(requestDetails.filter((item): item is PackageRequestDetailResponse => item !== null).map((item) => [item.request.id, item])),
      };
    },
  });

  const subjectsQuery = useQuery({
    queryKey: ['security-officer', 'identity-360', identityId, 'subjects'],
    enabled: section === 'known-in',
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/subjects', { params: { query: { IdentityId: identityId, AccessControlSystemId: undefined, Page: 0, PageSize: 200 } as never } });
      if (error) {
        throw new Error('Could not load PACS subjects.');
      }
      return data?.items ?? [];
    },
  });

  const credentialsQuery = useQuery({
    queryKey: ['security-officer', 'identity-360', identityId, 'credentials'],
    enabled: section === 'credentials',
    queryFn: async () => {
      const credentialsResult = await api.GET('/api/credential-management/credentials', {
        params: { query: { CredentialTypeId: undefined, IdentityId: identityId, Status: undefined, Page: 0, PageSize: 200 } as never },
      });

      if (credentialsResult.error) {
        throw new Error('Could not load credentials.');
      }

      const credentials = credentialsResult.data?.items ?? [];
      const credentialIds = credentials.map((item: CredentialResponse) => item.id);
      const credentialTypeIds = Array.from(new Set(credentials.map((item: CredentialResponse) => item.credentialTypeId)));

      const [assignmentsResult, credentialTypesResult] = await Promise.all([
        credentialIds.length > 0
          ? api.GET('/api/access-control/credential-pacs-assignments', {
            params: { query: { CredentialId: undefined, CredentialIds: credentialIds, AccessControlSystemId: undefined, Status: undefined, Page: 0, PageSize: 500 } as never },
          })
          : Promise.resolve({ data: { items: [] }, error: undefined }),
        credentialTypeIds.length > 0
          ? api.GET('/api/credential-management/credential-types', {
            params: { query: { Query: undefined, Technology: undefined, Status: undefined, Page: 0, PageSize: 200 } as never },
          })
          : Promise.resolve({ data: { items: [] }, error: undefined }),
      ]);

      if (assignmentsResult.error || credentialTypesResult.error) {
        throw new Error('Could not load credential provisioning state.');
      }

      const assignments = assignmentsResult.data?.items ?? [];
      const credentialTypes = credentialTypesResult.data?.items ?? [];

      return {
        credentials,
        assignmentsByCredentialId: new Map(groupBy(assignments, (item) => item.credentialId)),
        credentialTypesById: new Map(credentialTypes.filter((item: CredentialTypeResponse) => credentialTypeIds.includes(item.id)).map((item: CredentialTypeResponse) => [item.id, item])),
      };
    },
  });

  const requestsQuery = useQuery({
    queryKey: ['security-officer', 'identity-360', identityId, 'requests'],
    enabled: section === 'requests',
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/package-requests', { params: { query: { RequesterIdentityId: undefined, BeneficiaryIdentityId: identityId, Status: undefined, ids: [] } as never } });
      if (error) {
        throw new Error('Could not load requests.');
      }

      const requests = data?.items ?? [];
      const packageIds = Array.from(new Set(requests.map((item: PackageRequestResponse) => item.packageId)));
      const packages = await Promise.all(packageIds.map(async (packageId) => {
        const { data: packageData, error: packageError } = await api.GET('/api/access-catalog/packages/{packageId}', { params: { path: { packageId } } });
        if (packageError || !packageData) {
          return null;
        }
        return packageData;
      }));

      return {
        requests,
        packagesById: new Map(packages.filter((item): item is PackageResponse => item !== null).map((item) => [item.id, item])),
      };
    },
  });

  const identity = identityQuery.data;

  return (
    <section className="grid gap-6">
      <Link to="/security-officer/identities" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('identities.detail.back')}
      </Link>

      {identityQuery.isLoading ? <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('identities.detail.loading')}</p> : null}
      {identityQuery.isError || !identity ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{t('identities.detail.couldNotLoad')}</p> : null}

      {identity ? (
        <>
          <IdentityHeader identity={identity} />

          <div className="grid gap-6 lg:grid-cols-[16rem_minmax(0,1fr)]">
            <Card className="p-3">
              <nav className="grid gap-2" aria-label={t('identities.detail.navigation')}>
                {sections.map((item) => (
                  <button key={item.id} type="button" className={section === item.id ? 'rounded-interactive bg-active-blue px-3 py-3 text-left text-foreground' : 'rounded-interactive px-3 py-3 text-left text-foreground transition hover:bg-hover-blue'} onClick={() => setSection(item.id)}>
                    <span className="block font-semibold">{item.label}</span>
                    <span className="mt-1 block text-[13px] leading-5 text-muted-foreground">{item.description}</span>
                  </button>
                ))}
              </nav>
            </Card>

            <div className="min-w-0 grid gap-4">
              {section === 'overview' ? <OverviewSection employeeDetails={employeeDetailsQuery.data ?? []} employeeWorkLocationLabels={employeeWorkLocationLabelsQuery.data ?? new Map<string, string>()} employeeLoading={employeeDetailsQuery.isLoading || employeeWorkLocationLabelsQuery.isLoading} employeeError={employeeDetailsQuery.isError || employeeWorkLocationLabelsQuery.isError} visitorDetails={visitorDetailsQuery.data ?? []} visitorLoading={visitorDetailsQuery.isLoading} visitorError={visitorDetailsQuery.isError} /> : null}
              {section === 'assignments' ? <AssignmentsSection identityId={identityId} data={assignmentsQuery.data} isLoading={assignmentsQuery.isLoading} isError={assignmentsQuery.isError} systemsById={systemsQuery.data ?? new Map<string, AccessControlSystemResponse>()} /> : null}
              {section === 'credentials' ? <CredentialsSection data={credentialsQuery.data} isLoading={credentialsQuery.isLoading} isError={credentialsQuery.isError} systemsById={systemsQuery.data ?? new Map<string, AccessControlSystemResponse>()} /> : null}
              {section === 'known-in' ? <KnownInSection subjects={subjectsQuery.data ?? []} isLoading={subjectsQuery.isLoading} isError={subjectsQuery.isError} systemsById={systemsQuery.data ?? new Map<string, AccessControlSystemResponse>()} /> : null}
              {section === 'requests' ? <RequestsSection data={requestsQuery.data} isLoading={requestsQuery.isLoading} isError={requestsQuery.isError} onOpenRequest={(requestId) => void navigate({ to: '/security-officer/identities/$identityId/requests/$requestId', params: { identityId, requestId } })} /> : null}
            </div>
          </div>
        </>
      ) : null}
    </section>
  );
}

function IdentityHeader({ identity }: { readonly identity: IdentityResponse }) {
  const { t } = useTranslation();
  const affiliationLabels = [
    identity.employeeAffiliations.length > 0 ? t('identities.list.employee') : null,
    identity.contractorAffiliations.length > 0 ? t('identities.list.contractor') : null,
    identity.visitorAffiliations.length > 0 ? t('identities.list.visitor') : null,
  ].filter((item): item is string => item !== null);

  return (
    <Card className="p-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-[28px] font-semibold tracking-tight">{identity.displayName}</h1>
            <Badge variant={getIdentityStatusVariant(identity.status)}>{identity.status}</Badge>
          </div>
          <p className="mt-2 text-[14px] text-muted-foreground">{identity.email ?? t('identities.detail.noEmail')}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          {affiliationLabels.length > 0 ? affiliationLabels.map((label) => <Badge key={label} variant="secondary">{label}</Badge>) : <Badge variant="outline">{t('identities.detail.noAffiliations')}</Badge>}
        </div>
      </div>
    </Card>
  );
}

function OverviewSection({ employeeDetails, employeeWorkLocationLabels, employeeLoading, employeeError, visitorDetails, visitorLoading, visitorError }: { readonly employeeDetails: EmployeeResponse[]; readonly employeeWorkLocationLabels: Map<string, string>; readonly employeeLoading: boolean; readonly employeeError: boolean; readonly visitorDetails: VisitorResponse[]; readonly visitorLoading: boolean; readonly visitorError: boolean; }) {
  const { t } = useTranslation();
  return (
    <>
      {employeeError || visitorError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{t('identities.detail.couldNotLoadOverview')}</p> : null}
      {employeeLoading || visitorLoading ? <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('identities.detail.loadingOverview')}</p> : null}
      {!employeeLoading && !visitorLoading && employeeDetails.length === 0 && visitorDetails.length === 0 ? <Card className="p-6 text-[14px] text-muted-foreground">{t('identities.detail.noOverview')}</Card> : null}

      {employeeDetails.map((employee) => (
        <Card key={employee.id} className="p-6">
          <h2 className="text-[18px] font-semibold tracking-tight">{t('identities.list.employee')}</h2>
          <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <Info label={t('identities.detail.employeeNumber')} value={employee.employeeNumber ?? '-'} />
            <Info label={t('identities.detail.organizationUnit')} value={employee.organizationUnit.name} />
            <Info label={t('identities.detail.jobTitle')} value={employee.jobTitle ?? '-'} />
            <Info label={t('identities.detail.status')} value={employee.status} />
            <Info label={t('identities.detail.contract')} value={employee.contractStartDate ? `${employee.contractStartDate}${employee.contractEndDate ? ` to ${employee.contractEndDate}` : ''}` : '-'} />
            <Info label={t('identities.detail.personas')} value={employee.personas.length > 0 ? employee.personas.map((item) => item.name).join(', ') : t('identities.list.none')} />
            <Info label={t('identities.detail.workLocations')} value={employee.workLocations.length > 0 ? employee.workLocations.map((item) => employeeWorkLocationLabels.get(item.locationId) ? `${employeeWorkLocationLabels.get(item.locationId)}${item.isPrimary ? ` (${t('identities.detail.primary')})` : ''}` : item.isPrimary ? t('identities.detail.primaryLocation') : t('identities.detail.location')).join(', ') : t('identities.list.none')} />
          </div>
        </Card>
      ))}

      {visitorDetails.map((visitor) => (
        <Card key={visitor.id} className="p-6">
          <h2 className="text-[18px] font-semibold tracking-tight">{t('identities.list.visitor')}</h2>
          <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <Info label={t('visitorsManagement.invitationDetail.name')} value={`${visitor.firstName} ${visitor.lastName}`} />
            <Info label={t('identities.list.email')} value={visitor.email} />
            <Info label={t('identities.detail.company')} value={visitor.company ?? '-'} />
            <Info label={t('identities.detail.licensePlate')} value={visitor.licensePlate ?? '-'} />
          </div>
        </Card>
      ))}
    </>
  );
}

function AssignmentsSection({ identityId, data, isLoading, isError, systemsById }: { readonly identityId: string; readonly data: AssignmentData | undefined; readonly isLoading: boolean; readonly isError: boolean; readonly systemsById: Map<string, AccessControlSystemResponse>; }) {
  const { t } = useTranslation();

  if (isError) {
    return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load assignments.</p>;
  }

  if (isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">Loading {t('identities.detail.assignments.label').toLowerCase()}...</p>;
  }

  const grants = data?.grants ?? [];
  if (grants.length === 0) {
    return <Card className="p-6 text-[14px] text-muted-foreground">No granted access for this identity.</Card>;
  }

  const views = grants.map((grant) => buildGrantView(grant, data, systemsById));
  const catalogViews = views.filter((item) => item.grant.sourceKind === 'CatalogRequest');
  const automaticViews = views.filter((item) => item.grant.sourceKind !== 'CatalogRequest');

  return (
    <div className="grid gap-4">
      <CatalogueAssignmentGroups views={catalogViews} requestDetailsById={data?.requestDetailsById ?? new Map<string, PackageRequestDetailResponse>()} />
      <AutomatedAssignmentGroups identityId={identityId} views={automaticViews} packageAccessItemsByPackageId={data?.packageAccessItemsByPackageId ?? new Map<string, AccessItemResponse[]>()} targetsById={data?.targetsById ?? new Map<string, AccessLevelTargetResponse>()} />
    </div>
  );
}

function CatalogueAssignmentGroups({ views, requestDetailsById }: { readonly views: readonly GrantView[]; readonly requestDetailsById: Map<string, PackageRequestDetailResponse>; }) {
  const requestGroups = groupCatalogViews([...views], requestDetailsById);
  if (requestGroups.length === 0) {
    return null;
  }

  return <CatalogAssignmentGroupsTable groups={requestGroups} />;
}

function AutomatedAssignmentGroups({ identityId, views, packageAccessItemsByPackageId, targetsById }: { readonly identityId: string; readonly views: readonly GrantView[]; readonly packageAccessItemsByPackageId: Map<string, AccessItemResponse[]>; readonly targetsById: Map<string, AccessLevelTargetResponse>; }) {
  const policyGroups = groupAutomaticViews([...views], packageAccessItemsByPackageId, targetsById);
  if (policyGroups.length === 0) {
    return null;
  }

  return <AutomatedAssignmentGroupsTable identityId={identityId} groups={policyGroups} />;
}

function AssignmentsTreeSection({ title, description, groups }: { readonly title: string; readonly description: string; readonly groups: React.ReactNode[] }) {
  return (
    <section className="grid gap-4">
      <div>
        <h2 className="text-[20px] font-semibold tracking-tight">{title}</h2>
        <p className="mt-2 text-[14px] text-muted-foreground">{description}</p>
      </div>
      {groups}
    </section>
  );
}

function CatalogAssignmentGroupsTable({ groups }: { readonly groups: readonly PackageAssignmentGroupView[] }) {
  const { t } = useTranslation();
  const [expandedGroupKeys, setExpandedGroupKeys] = useState<string[]>([]);

  return (
    <AssignmentGroupTableSection title="Catalog requests" description="Access granted from catalog requests, grouped by access package.">
      {groups.map((group) => {
        const groupKey = `${group.sourceType}-${group.sourceId}-${group.packageId}`;
        const isExpanded = expandedGroupKeys.includes(groupKey);
        const detailsId = `assignment-group-details-${groupKey}`;
        const provisionStatus = getCatalogAssignmentGroupProvisionStatus(group);
        const grantStatus = getCatalogAssignmentGroupStatus(group);

        return (
          <Fragment key={groupKey}>
            <tr className={isExpanded ? 'bg-hover-blue/50' : 'transition hover:bg-hover-blue'}>
              <td className="px-3 py-3 font-medium text-foreground">{group.packageName}</td>
              <td className="px-3 py-3 text-muted-foreground">{group.sourceLabel}</td>
              <td className="px-3 py-3 text-muted-foreground">{group.sourceReason}</td>
              <td className="px-3 py-3 text-muted-foreground">{group.validityLabel}</td>
              <td className="px-3 py-3"><Badge variant={getAccessGrantStatusVariant(grantStatus)}>{grantStatus}</Badge></td>
              <td className="px-3 py-3"><Badge variant={provisionStatus.variant}>{provisionStatus.label}</Badge></td>
              <td className="px-3 py-3 text-right">
                <button
                  type="button"
                  className="inline-flex items-center gap-2 rounded-interactive border border-border px-3 py-2 text-[13px] font-medium text-foreground transition hover:bg-hover-blue"
                  aria-expanded={isExpanded}
                  aria-controls={detailsId}
                  onClick={() => setExpandedGroupKeys((current) => current.includes(groupKey) ? current.filter((item) => item !== groupKey) : [...current, groupKey])}
                >
                  {isExpanded ? t('identities.detail.hide') : t('identities.detail.show')}
                  <ChevronRight className={isExpanded ? 'size-4 shrink-0 rotate-90 text-muted-foreground transition' : 'size-4 shrink-0 text-muted-foreground transition'} aria-hidden="true" />
                </button>
              </td>
            </tr>
            {isExpanded ? (
              <tr id={detailsId} className="bg-background">
                      <td colSpan={7} className="px-3 py-3">
                  <div className="grid gap-3">
                    <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
                      <Info label="Grant status" value={grantStatus} />
                      <Info label="Request status" value={group.requestStatus ? formatRequestStatus(group.requestStatus, group.requestSubStatus ?? null) : '-'} />
                      <Info label="Approved by" value={group.approvalSummary} />
                      <Info label={t('identities.detail.provisionings')} value={String(group.provisioningCount)} />
                      <Info label={t('identities.detail.created')} value={group.requestCreatedAt ? formatDateTimeLabel(group.requestCreatedAt) : '-'} />
                    </div>
                    <div className="grid gap-3">
                      {group.accessItems.map((item) => <AccessItemGroup key={`${group.packageId}-${item.accessItemId}`} group={item} />)}
                    </div>
                  </div>
                </td>
              </tr>
            ) : null}
          </Fragment>
        );
      })}
    </AssignmentGroupTableSection>
  );
}

function AutomatedAssignmentGroupsTable({ identityId, groups }: { readonly identityId: string; readonly groups: readonly AutomatedPackageAssignmentGroupView[] }) {
  const [expandedGroupKeys, setExpandedGroupKeys] = useState<string[]>([]);

  return (
    <AssignmentGroupTableSection title="Automated grants" description="Access granted from policy automation, grouped by access package.">
      {groups.map((group) => {
        const groupKey = `${group.sourceType}-${group.sourceId}-${group.packageId}`;
        const isExpanded = expandedGroupKeys.includes(groupKey);
        const detailsId = `assignment-group-details-${groupKey}`;
        const provisionStatus = getAutomatedAssignmentGroupProvisionStatus(group);

        return (
          <Fragment key={groupKey}>
            <tr className={isExpanded ? 'bg-hover-blue/50' : 'transition hover:bg-hover-blue'}>
              <td className="px-3 py-3 font-medium text-foreground">{group.packageName}</td>
              <td className="px-3 py-3 text-muted-foreground">{group.sourceLabel}</td>
              <td className="px-3 py-3 text-muted-foreground">{group.sourceReason}</td>
              <td className="px-3 py-3 text-muted-foreground">{group.validityLabel}</td>
              <td className="px-3 py-3"><Badge variant={getAccessGrantStatusVariant(group.status)}>{group.status}</Badge></td>
              <td className="px-3 py-3"><Badge variant={provisionStatus.variant}>{provisionStatus.label}</Badge></td>
              <td className="px-3 py-3 text-right">
                <button
                  type="button"
                  className="inline-flex items-center gap-2 rounded-interactive border border-border px-3 py-2 text-[13px] font-medium text-foreground transition hover:bg-hover-blue"
                  aria-expanded={isExpanded}
                  aria-controls={detailsId}
                  onClick={() => setExpandedGroupKeys((current) => current.includes(groupKey) ? current.filter((item) => item !== groupKey) : [...current, groupKey])}
                >
                  {isExpanded ? 'Hide' : 'Show'}
                  <ChevronRight className={isExpanded ? 'size-4 shrink-0 rotate-90 text-muted-foreground transition' : 'size-4 shrink-0 text-muted-foreground transition'} aria-hidden="true" />
                </button>
              </td>
            </tr>
            {isExpanded ? (
              <tr id={detailsId} className="bg-background">
                <td colSpan={7} className="px-3 py-3">
                  <div className="grid gap-3">
                    <div className="flex justify-end pb-1">
                      <AutomatedGrantReconcileButton identityId={identityId} group={group} />
                    </div>
                    <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                      <Info label="Grant status" value={group.status} />
                      <Info label="Approved by" value={group.approvalSummary} />
                      <Info label="Provisionings" value={String(group.provisioningCount)} />
                      <Info label="Locations" value={group.locationSummary || '-'} />
                      {group.revokeCause ? <Info label="Revoke cause" value={formatAccessGrantRevokeCause(group.revokeCause)} /> : null}
                      {group.revokedBy ? <Info label="Revoked by" value={group.revokedBy} /> : null}
                    </div>
                    <div className="grid gap-3">
                      {group.accessItems.map((item) => <AutomatedAccessItemGroup key={`${group.packageId}-${item.accessItemId}`} group={item} />)}
                    </div>
                  </div>
                </td>
              </tr>
            ) : null}
          </Fragment>
        );
      })}
    </AssignmentGroupTableSection>
  );
}

function AssignmentGroupTableSection({ title, description, children }: { readonly title: string; readonly description: string; readonly children: React.ReactNode; }) {
  return (
    <section className="grid gap-3">
      <div>
        <h2 className="text-[20px] font-semibold tracking-tight">{title}</h2>
        <p className="mt-2 text-[14px] text-muted-foreground">{description}</p>
      </div>
      <Card className="p-3 sm:p-4">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[62rem] border-collapse text-left text-[14px]">
            <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
              <tr>
                <th className="px-3 py-3 font-semibold">Package</th>
                <th className="px-3 py-3 font-semibold">Source</th>
                <th className="px-3 py-3 font-semibold">Reason</th>
                <th className="px-3 py-3 font-semibold">Validity</th>
                <th className="px-3 py-3 font-semibold">Status</th>
                <th className="px-3 py-3 font-semibold">Provision Status</th>
                <th className="px-3 py-3 text-right font-semibold">Details</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">{children}</tbody>
          </table>
        </div>
      </Card>
    </section>
  );
}

function AutomatedGrantReconcileButton({ identityId, group }: { readonly identityId: string; readonly group: AutomatedPackageAssignmentGroupView }) {
  const queryClient = useQueryClient();
  const { t } = useTranslation();
  const reconcileGrant = useMutation({
    mutationFn: async () => {
      await Promise.all(group.grantIds.map(async (accessGrantId) => {
        const { error } = await api.POST('/api/access-catalog/access-grants/{accessGrantId}/reconcile', { params: { path: { accessGrantId } } });
        if (error) {
          throw new Error(t('identities.detail.couldNotQueueGrantReconciliation'));
        }
      }));
    },
    onSuccess: async () => {
      toast.success(t('identities.detail.grantReconciliationQueued'));
      await queryClient.invalidateQueries({ queryKey: ['security-officer', 'identity-360', identityId, 'assignments'] });
    },
    onError: () => {
      toast.error(t('identities.detail.couldNotQueueGrantReconciliation'));
    },
  });

  return (
    <Button type="button" variant="outline" size="sm" disabled={reconcileGrant.isPending} onClick={() => reconcileGrant.mutate()}>
      {reconcileGrant.isPending ? t('identities.detail.reconciling') : t('identities.detail.reconcileGrant')}
    </Button>
  );
}

function PackageAssignmentGroup({ group }: { readonly group: PackageAssignmentGroupView }) {
  const { t } = useTranslation();
  return (
    <details className="rounded-structural border border-border bg-content">
      <summary className="cursor-pointer list-none p-6">
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-[18px] font-semibold tracking-tight text-foreground">{group.packageName}</h2>
              {group.requestStatus ? <Badge variant={getRequestStatusVariant(group.requestStatus, group.requestSubStatus ?? null)}>{formatRequestStatus(group.requestStatus, group.requestSubStatus ?? null)}</Badge> : null}
              <Badge variant={getProvisioningSummaryVariant(group.provisioningSummary.variant)}>{group.provisioningSummary.label}</Badge>
            </div>
            <div className="mt-3 grid gap-1 text-[13px] text-muted-foreground sm:grid-cols-2 xl:grid-cols-4">
              <p><span className="font-medium text-foreground">{t('identities.detail.source')}:</span> {group.sourceLabel}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.reason')}:</span> {group.sourceReason}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.validity')}:</span> {group.validityLabel}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.locations')}:</span> {group.locationSummary || '-'}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.approvedBy')}:</span> {group.approvalSummary}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.items')}:</span> {group.accessItems.length}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.provisionings')}:</span> {group.provisioningCount}</p>
              {group.requestCreatedAt ? <p><span className="font-medium text-foreground">{t('identities.detail.created')}:</span> {formatDateTimeLabel(group.requestCreatedAt)}</p> : null}
            </div>
          </div>
          <ChevronRight className="mt-1 size-5 shrink-0 text-muted-foreground transition group-open:rotate-90" aria-hidden="true" />
        </div>
      </summary>
      <div className="border-t border-border px-6 pb-6 pt-4 grid gap-4">
        {group.accessItems.map((item) => <AccessItemGroup key={`${group.packageId}-${item.accessItemId}`} group={item} />)}
      </div>
    </details>
  );
}

function AccessItemGroup({ group }: { readonly group: AccessItemGroupView }) {
  const { t } = useTranslation();
  const hasSkippedOutcome = group.materializationOutcomes.some((item) => item.status === 'SkippedNoTarget');
  const hasFailedOutcome = group.materializationOutcomes.some((item) => item.status === 'Failed');

  return (
    <details className="rounded-interactive border border-border bg-background">
      <summary className="cursor-pointer list-none p-4">
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="text-[16px] font-semibold text-foreground">{group.accessItemName}</h3>
              <Badge variant={getProvisioningSummaryVariant(group.provisioningSummary.variant)}>{group.provisioningSummary.label}</Badge>
              {hasSkippedOutcome ? <Badge variant="error">{t('identities.detail.noConfiguredTarget')}</Badge> : null}
              {hasFailedOutcome ? <Badge variant="error">{t('identities.detail.assignmentCreationFailed')}</Badge> : null}
            </div>
          </div>
          <ChevronRight className="mt-1 size-4 shrink-0 text-muted-foreground transition group-open:rotate-90" aria-hidden="true" />
        </div>
      </summary>
      <div className="border-t border-border p-4 grid gap-4">
        <MaterializationOutcomeNotice views={group.leafViews} outcomes={group.materializationOutcomes} />
        <PacsAssignmentList views={group.leafViews} title={t('identities.detail.pacsAssignments')} emptyLabel={t('identities.detail.noPacsAssignmentsYet')} provisioningTitle={t('identities.detail.actualProvisionedAccess')} />
      </div>
    </details>
  );
}

function AutomatedPackageAssignmentGroup({ identityId, group }: { readonly identityId: string; readonly group: AutomatedPackageAssignmentGroupView }) {
  const { t } = useTranslation();
  return (
    <details className="rounded-structural border border-border bg-content">
      <summary className="cursor-pointer list-none p-6">
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-[18px] font-semibold tracking-tight text-foreground">{group.packageName}</h2>
              <Badge variant={getProvisioningSummaryVariant(group.provisioningSummary.variant)}>{group.provisioningSummary.label}</Badge>
            </div>
            <div className="mt-3 grid gap-1 text-[13px] text-muted-foreground sm:grid-cols-2 xl:grid-cols-4">
              <p><span className="font-medium text-foreground">{t('identities.detail.source')}:</span> {group.sourceLabel}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.reason')}:</span> {group.sourceReason}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.validity')}:</span> {group.validityLabel}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.locations')}:</span> {group.locationSummary || '-'}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.approvedBy')}:</span> {group.approvalSummary}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.items')}:</span> {group.accessItems.length}</p>
              <p><span className="font-medium text-foreground">{t('identities.detail.provisionings')}:</span> {group.provisioningCount}</p>
            </div>
          </div>
          <div className="flex items-start gap-3">
            <div onClick={(event) => {
              event.preventDefault();
              event.stopPropagation();
            }}>
              <AutomatedGrantReconcileButton identityId={identityId} group={group} />
            </div>
            <ChevronRight className="mt-1 size-5 shrink-0 text-muted-foreground transition group-open:rotate-90" aria-hidden="true" />
          </div>
        </div>
      </summary>
      <div className="grid gap-4 border-t border-border px-6 pb-6 pt-4">
        {group.accessItems.map((item) => <AutomatedAccessItemGroup key={`${group.packageId}-${item.accessItemId}`} group={item} />)}
      </div>
    </details>
  );
}

function AutomatedAccessItemGroup({ group }: { readonly group: AutomatedAccessItemGroupView }) {
  const { t } = useTranslation();
  const hasSkippedOutcome = group.materializationOutcomes.some((item) => item.status === 'SkippedNoTarget');
  const hasFailedOutcome = group.materializationOutcomes.some((item) => item.status === 'Failed');

  return (
    <details className="rounded-interactive border border-border bg-background">
      <summary className="cursor-pointer list-none p-4">
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="text-[16px] font-semibold text-foreground">{group.accessItemName}</h3>
              <Badge variant={getProvisioningSummaryVariant(group.provisioningSummary.variant)}>{group.provisioningSummary.label}</Badge>
              {hasSkippedOutcome || !group.hasTargets ? <Badge variant="error">{t('identities.detail.noConfiguredTarget')}</Badge> : null}
              {hasFailedOutcome ? <Badge variant="error">{t('identities.detail.assignmentCreationFailed')}</Badge> : null}
            </div>
            <p className="mt-2 text-[13px] text-muted-foreground">{group.locationLabels || '-'}</p>
          </div>
          <ChevronRight className="mt-1 size-4 shrink-0 text-muted-foreground transition group-open:rotate-90" aria-hidden="true" />
        </div>
      </summary>
      <div className="grid gap-4 border-t border-border p-4">
        <MaterializationOutcomeNotice views={[group.view]} outcomes={group.materializationOutcomes} />
        <PacsAssignmentList views={[group.view]} title={t('identities.detail.pacsAssignments')} emptyLabel={t('identities.detail.noPacsAssignmentsYet')} provisioningTitle={t('identities.detail.actualProvisionedAccess')} assignments={group.assignments} provisionings={group.provisionings} />
      </div>
    </details>
  );
}

function PacsAssignmentList({ views, title, emptyLabel, provisioningTitle, assignments, provisionings }: { readonly views: readonly GrantView[]; readonly title: string; readonly emptyLabel: string; readonly provisioningTitle: string; readonly assignments?: readonly PACSAssignmentResponse[]; readonly provisionings?: readonly PACSProvisioningResponse[]; }) {
  const sourceAssignments = assignments ?? views.flatMap((view) => view.grantAssignments);
  const sourceProvisionings = provisionings ?? views.flatMap((view) => view.grantProvisionings);
  const assignmentRows = buildPacsAssignmentRows(views, sourceAssignments, sourceProvisionings);

  return (
    <div className="rounded-interactive border border-border p-4">
      <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{title}</p>
      <div className="mt-3 grid gap-2">
        {assignmentRows.length === 0 ? <p className="text-[14px] text-muted-foreground">{emptyLabel}</p> : assignmentRows.map((row) => <PacsAssignmentRow key={row.assignment.id} row={row} provisioningTitle={provisioningTitle} />)}
      </div>
    </div>
  );
}

function MaterializationOutcomeNotice({ views, outcomes }: { readonly views: readonly GrantView[]; readonly outcomes: readonly AccessGrantMaterializationOutcomeResponse[]; }) {
  const notices = outcomes
    .filter((item) => item.status === 'SkippedNoTarget' || item.status === 'Failed')
    .map((item) => ({
      key: item.id,
      variant: item.status === 'Failed' ? 'error' : 'secondary',
      message: item.status === 'Failed'
        ? `${getLocationLabelForOutcome(views, item.locationId)}: ${item.failureReason ?? i18n.t('identities.detail.failedToCreatePacsAssignments')}`
        : `${getLocationLabelForOutcome(views, item.locationId)}: ${item.failureReason ?? i18n.t('identities.detail.noEnabledTargetConfigured')}`,
    }));

  if (notices.length === 0) {
    return null;
  }

  return (
    <div className="grid gap-2">
      {notices.map((notice) => <p key={notice.key} className={notice.variant === 'error' ? 'rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error' : 'rounded-interactive border border-border bg-background px-4 py-3 text-[14px] text-muted-foreground'}>{notice.message}</p>)}
    </div>
  );
}

function PacsAssignmentRow({ row, provisioningTitle }: { readonly row: PacsAssignmentRowView; readonly provisioningTitle: string; }) {
  const { t } = useTranslation();
  const title = `${row.systemName} - ${row.locationLabel || t('identities.detail.unscoped')}`;
  const validityLabel = getValidityWindowLabel(row.assignment.validFrom, row.assignment.validUntil);

  return (
    <details className="rounded-interactive border border-border bg-background">
      <summary className="cursor-pointer list-none p-3">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0 flex-1">
            <p className="font-medium text-foreground">{title}</p>
            <p className="mt-1 text-[13px] text-muted-foreground">{row.targetName}</p>
            {validityLabel ? <p className="mt-1 text-[13px] text-muted-foreground">{validityLabel}</p> : null}
          </div>
          <div className="flex items-center gap-3">
            <Badge variant={getInfraStatusVariant(row.assignment.status)}>{row.assignment.status}</Badge>
            <ChevronRight className="size-4 shrink-0 text-muted-foreground transition group-open:rotate-90" aria-hidden="true" />
          </div>
        </div>
      </summary>
      <div className="grid gap-4 border-t border-border p-3">
        <dl className="grid gap-2 text-[13px] text-muted-foreground">
          <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.scheduled')}</dt><dd className="text-right">{formatDateTimeLabel(row.assignment.scheduledFor)}</dd></div>
          <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.completed')}</dt><dd className="text-right">{row.assignment.completedAt ? formatDateTimeLabel(row.assignment.completedAt) : '-'}</dd></div>
          {row.assignment.failureReason ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.failure')}</dt><dd className="text-right text-error">{row.assignment.failureReason}</dd></div> : null}
          {row.assignment.nativeAssignmentId ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.nativeAssignment')}</dt><dd className="text-right">{row.assignment.nativeAssignmentId}</dd></div> : null}
        </dl>
        <TranslatedProvisioningInsightPanel title={provisioningTitle} emptyLabel={t('identities.detail.noEffectiveProvisioningYet')} items={row.provisionings.map((item) => ({ key: item.id, systemName: row.systemName, targetName: row.targetName, status: item.status, scheduledFor: item.scheduledFor, provisionedAt: item.provisionedAt, completedAt: item.completedAt, failureReason: item.failureReason, nativeAssignmentId: item.nativeAssignmentId, validFrom: item.validFrom, validUntil: item.validUntil, provisioningTiming: item.provisioningTiming }))} />
      </div>
    </details>
  );
}

function KnownInSection({ subjects, isLoading, isError, systemsById }: { readonly subjects: PACSSubjectResponse[]; readonly isLoading: boolean; readonly isError: boolean; readonly systemsById: Map<string, AccessControlSystemResponse>; }) {
  const { t } = useTranslation();

  if (isError) {
    return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{t('identities.detail.couldNotLoadPacsSubjects')}</p>;
  }

  if (isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('identities.detail.loadingPacsSubjects')}</p>;
  }

  if (subjects.length === 0) {
    return <Card className="p-6 text-[14px] text-muted-foreground">{t('identities.detail.notKnownInAnyPacs')}</Card>;
  }

  return (
    <div className="grid gap-4">
      {subjects.map((subject) => (
        <Card key={subject.id} className="p-6">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h2 className="text-[18px] font-semibold tracking-tight">{systemsById.get(subject.accessControlSystemId)?.name ?? subject.accessControlSystemId}</h2>
              <p className="mt-1 text-[14px] text-muted-foreground">{t('identities.detail.nativeSubjectId', { value: subject.nativeSubjectId })}</p>
            </div>
            <Badge variant={subject.state === 'Active' ? 'success' : subject.state === 'Blocked' ? 'secondary' : 'error'}>{subject.state}</Badge>
          </div>
          <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <Info label={t('identities.detail.firstName')} value={subject.firstName} />
            <Info label={t('identities.detail.lastName')} value={subject.lastName} />
            <Info label={t('identities.list.email')} value={subject.email ?? '-'} />
            <Info label={t('identities.detail.lastSynchronized')} value={formatDateTimeLabel(subject.lastSynchronizedAt)} />
          </div>
        </Card>
      ))}
    </div>
  );
}

function CredentialsSection({ data, isLoading, isError, systemsById }: { readonly data: CredentialsData | undefined; readonly isLoading: boolean; readonly isError: boolean; readonly systemsById: Map<string, AccessControlSystemResponse>; }) {
  const { t } = useTranslation();
  const [expandedCredentialIds, setExpandedCredentialIds] = useState<string[]>([]);

  if (isError) {
    return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{t('identities.detail.couldNotLoadCredentials')}</p>;
  }

  if (isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('identities.detail.loadingCredentials')}</p>;
  }

  const credentials = data?.credentials ?? [];
  if (credentials.length === 0) {
    return <Card className="p-6 text-[14px] text-muted-foreground">{t('identities.detail.noIssuedCredentials')}</Card>;
  }

  return (
    <Card className="p-4 sm:p-5">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">{t('identities.detail.credential')}</th>
              <th className="px-4 py-3 font-semibold">{t('identities.detail.duration')}</th>
              <th className="px-4 py-3 font-semibold">{t('identities.detail.status')}</th>
              <th className="px-4 py-3 font-semibold">{t('identities.detail.provisionStatus')}</th>
              <th className="px-4 py-3 text-right font-semibold">{t('identities.detail.details')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {credentials.map((credential) => {
              const credentialType = data?.credentialTypesById.get(credential.credentialTypeId);
              const assignments = data?.assignmentsByCredentialId.get(credential.id) ?? [];
              const provisionStatus = getCredentialProvisionStatus(credential, assignments);
              const isExpanded = expandedCredentialIds.includes(credential.id);
              const detailsId = `credential-details-${credential.id}`;

              return (
                <Fragment key={credential.id}>
                  <tr className={isExpanded ? 'bg-hover-blue/50' : 'transition hover:bg-hover-blue'}>
                    <td className="px-4 py-4">
                      <div className="font-medium text-foreground">{credentialType?.name ?? credential.credentialTypeId}</div>
                      <div className="mt-1 text-muted-foreground">{credential.identifier}{credential.purpose ? ` - ${credential.purpose}` : ''}</div>
                    </td>
                    <td className="px-4 py-4 text-muted-foreground">
                      <div>{credential.durationKind}</div>
                      {credential.durationKind === 'Temporary' && credential.validUntil ? <div className="mt-1">{formatDateLabel(credential.validFrom)} - {formatDateLabel(credential.validUntil)}</div> : null}
                    </td>
                    <td className="px-4 py-4"><Badge variant={getCredentialStatusVariant(credential.status)}>{credential.status}</Badge></td>
                    <td className="px-4 py-4"><Badge variant={provisionStatus.variant}>{provisionStatus.label}</Badge></td>
                    <td className="px-4 py-4 text-right">
                      <button
                        type="button"
                        className="inline-flex items-center gap-2 rounded-interactive border border-border px-3 py-2 text-[13px] font-medium text-foreground transition hover:bg-hover-blue"
                        aria-expanded={isExpanded}
                        aria-controls={detailsId}
                        onClick={() => setExpandedCredentialIds((current) => current.includes(credential.id) ? current.filter((item) => item !== credential.id) : [...current, credential.id])}
                      >
                        {isExpanded ? t('identities.detail.hide') : t('identities.detail.show')}
                        <ChevronRight className={isExpanded ? 'size-4 shrink-0 rotate-90 text-muted-foreground transition' : 'size-4 shrink-0 text-muted-foreground transition'} aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                  {isExpanded ? (
                    <tr id={detailsId} className="bg-background">
                      <td colSpan={5} className="px-4 py-4">
                        <div className="grid gap-4">
                          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
                            <Info label={t('identities.detail.sourceKind')} value={credential.sourceKind} />
                            <Info label={t('identities.detail.validFrom')} value={formatDateTimeLabel(credential.validFrom)} />
                            <Info label={t('identities.detail.validUntil')} value={credential.validUntil ? formatDateTimeLabel(credential.validUntil) : t('visitorsManagement.invitationDetail.noEndDate')} />
                            <Info label={t('identities.detail.issuedAt')} value={formatDateTimeLabel(credential.createdAt)} />
                            <Info label={t('identities.detail.reasonText')} value={credential.reasonText} />
                          </div>
                          <TranslatedCredentialProvisioningPanel assignments={assignments} systemsById={systemsById} />
                        </div>
                      </td>
                    </tr>
                  ) : null}
                </Fragment>
              );
            })}
          </tbody>
        </table>
      </div>
    </Card>
  );
}

function RequestsSection({ data, isLoading, isError, onOpenRequest }: { readonly data: { readonly requests: PackageRequestResponse[]; readonly packagesById: Map<string, PackageResponse>; } | undefined; readonly isLoading: boolean; readonly isError: boolean; readonly onOpenRequest: (requestId: string) => void; }) {
  const { t } = useTranslation();

  if (isError) {
    return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{t('identities.detail.couldNotLoadRequests')}</p>;
  }

  if (isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('identities.detail.loadingRequests')}</p>;
  }

  const requests = data?.requests ?? [];
  if (requests.length === 0) {
    return <Card className="p-6 text-[14px] text-muted-foreground">{t('identities.detail.noCatalogRequests')}</Card>;
  }

  return (
    <Card className="p-4 sm:p-5">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">{t('identities.detail.package')}</th>
              <th className="px-4 py-3 font-semibold">{t('identities.detail.status')}</th>
              <th className="px-4 py-3 font-semibold">{t('identities.detail.created')}</th>
              <th className="px-4 py-3 font-semibold">{t('identities.detail.validFrom')}</th>
              <th className="px-4 py-3 font-semibold">{t('identities.detail.validUntil')}</th>
              <th className="px-4 py-3 text-right font-semibold">{t('identities.detail.open')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {requests.map((request) => (
              <tr key={request.id} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => onOpenRequest(request.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); onOpenRequest(request.id); } }}>
                <td className="px-4 py-4 font-medium text-foreground">{data?.packagesById.get(request.packageId)?.name ?? request.packageId}</td>
                <td className="px-4 py-4"><Badge variant={getRequestStatusVariant(request.status, request.subStatus)}>{formatRequestStatus(request.status, request.subStatus)}</Badge></td>
                <td className="px-4 py-4 text-muted-foreground">{formatDateTimeLabel(request.createdAt)}</td>
                <td className="px-4 py-4 text-muted-foreground">{formatDateTimeLabel(request.validFrom)}</td>
                <td className="px-4 py-4 text-muted-foreground">{request.validUntil ? formatDateTimeLabel(request.validUntil) : t('visitorsManagement.invitationDetail.noEndDate')}</td>
                <td className="px-4 py-4 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Card>
  );
}

function formatAccessGrantRevokeCause(cause: AccessGrantResponse['revokeCause']) {
  switch (cause) {
    case 'Manual':
      return i18n.t('identities.detail.manuallyRevoked');
    case 'VisitRescheduled':
      return i18n.t('identities.detail.visitRescheduled');
    case 'ArrivalRelocated':
      return i18n.t('identities.detail.arrivalRelocated');
    case 'VisitCancelled':
      return i18n.t('identities.detail.visitCancelled');
    case 'VisitOffboarded':
      return i18n.t('identities.detail.visitOffboarded');
    case 'EmployeeLifecycleAutomation':
      return i18n.t('identities.detail.employeeLifecycleAutomation');
    default:
      return '-';
  }
}

function ProvisioningInsightPanel({ title, emptyLabel, items }: { readonly title: string; readonly emptyLabel: string; readonly items: readonly { key: string; systemName: string; targetName: string; status: string; scheduledFor: string; provisionedAt: string | null; completedAt: string | null; failureReason: string | null; nativeAssignmentId: string | null; validFrom: string; validUntil: string | null; provisioningTiming: string; }[]; }) {
  const { t } = useTranslation();

  return (
    <div className="rounded-interactive border border-border p-4">
      <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{title}</p>
      <div className="mt-3 grid gap-2">
        {items.length === 0 ? <p className="text-[14px] text-muted-foreground">{emptyLabel}</p> : items.map((item) => (
          <div key={item.key} className="rounded-interactive border border-border bg-background p-3">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="font-medium text-foreground">{item.systemName}</p>
                <p className="mt-1 text-[13px] text-muted-foreground">{item.targetName}</p>
              </div>
              <Badge variant={getInfraStatusVariant(item.status)}>{item.status}</Badge>
            </div>
            <dl className="mt-3 grid gap-2 text-[13px] text-muted-foreground">
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.timing')}</dt><dd className="text-right">{item.provisioningTiming}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.window')}</dt><dd className="text-right">{formatDateTimeLabel(item.validFrom)}{item.validUntil ? ` to ${formatDateTimeLabel(item.validUntil)}` : ''}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.scheduled')}</dt><dd className="text-right">{formatDateTimeLabel(item.scheduledFor)}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.provisioned')}</dt><dd className="text-right">{item.provisionedAt ? formatDateTimeLabel(item.provisionedAt) : '-'}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.completed')}</dt><dd className="text-right">{item.completedAt ? formatDateTimeLabel(item.completedAt) : '-'}</dd></div>
              {item.failureReason ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.failure')}</dt><dd className="text-right text-error">{item.failureReason}</dd></div> : null}
              {item.nativeAssignmentId ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.nativeAssignment')}</dt><dd className="text-right">{item.nativeAssignmentId}</dd></div> : null}
            </dl>
          </div>
        ))}
      </div>
    </div>
  );
}

function CredentialProvisioningPanel({ assignments, systemsById }: { readonly assignments: readonly CredentialPACSAssignmentResponse[]; readonly systemsById: Map<string, AccessControlSystemResponse>; }) {
  const { t } = useTranslation();

  return (
    <div className="rounded-interactive border border-border p-4">
      <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{t('identities.detail.provisioning')}</p>
      <div className="mt-3 grid gap-2">
        {assignments.length === 0 ? <p className="text-[14px] text-muted-foreground">{t('identities.detail.noPacsProvisioningRows')}</p> : assignments.map((assignment) => (
          <div key={assignment.id} className="rounded-interactive border border-border bg-background p-3">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="font-medium text-foreground">{systemsById.get(assignment.accessControlSystemId)?.name ?? assignment.accessControlSystemId}</p>
                <p className="mt-1 text-[13px] text-muted-foreground">{t('identities.detail.credentialAssignment')}</p>
              </div>
              <Badge variant={getInfraStatusVariant(assignment.status)}>{assignment.status}</Badge>
            </div>
            <dl className="mt-3 grid gap-2 text-[13px] text-muted-foreground">
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.scheduled')}</dt><dd className="text-right">{formatDateTimeLabel(assignment.scheduledFor)}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.provisioned')}</dt><dd className="text-right">{assignment.provisionedAt ? formatDateTimeLabel(assignment.provisionedAt) : '-'}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.revoked')}</dt><dd className="text-right">{assignment.revokedAt ? formatDateTimeLabel(assignment.revokedAt) : '-'}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.attempts')}</dt><dd className="text-right">{assignment.attemptCount}</dd></div>
              {assignment.lastAttemptAt ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.lastAttempt')}</dt><dd className="text-right">{formatDateTimeLabel(assignment.lastAttemptAt)}</dd></div> : null}
              {assignment.failureReasonCode ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.failure')}</dt><dd className="text-right text-error">{assignment.failureReasonCode}</dd></div> : null}
              {assignment.errorMessage ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.error')}</dt><dd className="text-right text-error">{assignment.errorMessage}</dd></div> : null}
              {assignment.nativeAssignmentId ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.nativeAssignment')}</dt><dd className="text-right">{assignment.nativeAssignmentId}</dd></div> : null}
            </dl>
          </div>
        ))}
      </div>
    </div>
  );
}

function Info({ label, value }: { readonly label: string; readonly value: string }) {
  return <div className="rounded-interactive border border-border p-3"><div className="text-[12px] uppercase text-muted-foreground">{label}</div><div className="mt-1 break-all text-[14px] font-medium text-foreground">{value}</div></div>;
}

function formatDateTimeLabel(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function formatDateLabel(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(value));
}

function getIdentityStatusVariant(status: IdentityResponse['status']) {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Suspended':
      return 'secondary';
    default:
      return 'error';
  }
}

function getCredentialStatusVariant(status: CredentialResponse['status']) {
  switch (status) {
    case 'Active':
    case 'Issued':
      return 'success';
    case 'Suspended':
    case 'Expired':
      return 'secondary';
    default:
      return 'error';
  }
}

function getAccessGrantStatusVariant(status: AccessGrantResponse['status']) {
  return status === 'Active' ? 'success' : 'secondary';
}

function getCredentialProvisionStatus(
  credential: CredentialResponse,
  assignments: readonly CredentialPACSAssignmentResponse[],
) {
  const activeStatuses = new Set(['Provisioned', 'Active']);
  const inactiveStatuses = new Set(['Revoked', 'Archived']);
  const hasAssignments = assignments.length > 0;
  const hasActiveAssignment = assignments.some((assignment) => activeStatuses.has(assignment.status));
  const hasInactiveOnlyAssignments = hasAssignments && assignments.every((assignment) => inactiveStatuses.has(assignment.status));
  const hasPendingOrFailedAssignments = assignments.some((assignment) => !activeStatuses.has(assignment.status) && !inactiveStatuses.has(assignment.status));

  if (credential.status === 'Expired') {
    return hasAssignments ? { label: i18n.t('identities.detail.no'), variant: 'error' as const } : { label: i18n.t('identities.detail.yes'), variant: 'success' as const };
  }

  if (credential.status === 'Revoked') {
    return hasActiveAssignment || hasPendingOrFailedAssignments
      ? { label: i18n.t('identities.detail.no'), variant: 'error' as const }
      : { label: i18n.t('identities.detail.yes'), variant: 'success' as const };
  }

  if (!hasAssignments || hasInactiveOnlyAssignments || hasPendingOrFailedAssignments) {
    return { label: i18n.t('identities.detail.no'), variant: 'error' as const };
  }

  return { label: i18n.t('identities.detail.yes'), variant: 'success' as const };
}

function getCatalogAssignmentGroupProvisionStatus(group: PackageAssignmentGroupView) {
  const hasIssues = group.accessItems.some((item) => {
    const hasMaterializationIssue = item.materializationOutcomes.some((outcome) => outcome.status === 'SkippedNoTarget' || outcome.status === 'Failed');
    return hasMaterializationIssue || item.provisioningSummary.variant !== 'success';
  });

  return hasIssues ? { label: i18n.t('identities.detail.no'), variant: 'error' as const } : { label: i18n.t('identities.detail.yes'), variant: 'success' as const };
}

function getCatalogAssignmentGroupStatus(group: PackageAssignmentGroupView): AccessGrantResponse['status'] {
  return group.accessItems.some((item) => item.leafViews.some((view) => view.grant.status === 'Active')) ? 'Active' : 'Revoked';
}

function getAutomatedAssignmentGroupProvisionStatus(group: AutomatedPackageAssignmentGroupView) {
  const hasIssues = group.accessItems.some((item) => {
    const hasMaterializationIssue = item.materializationOutcomes.some((outcome) => outcome.status === 'SkippedNoTarget' || outcome.status === 'Failed');
    return hasMaterializationIssue || !item.hasTargets || item.provisioningSummary.variant !== 'success';
  });

  return hasIssues ? { label: i18n.t('identities.detail.no'), variant: 'error' as const } : { label: i18n.t('identities.detail.yes'), variant: 'success' as const };
}

function getRequestStatusVariant(status: PackageRequestStatus, subStatus: PackageRequestResponse['subStatus']) {
  if (status === 'InProgress') {
    return 'secondary';
  }

  switch (subStatus) {
    case 'Approved':
      return 'success';
    case 'Rejected':
    case 'Expired':
      return 'error';
    default:
      return 'secondary';
  }
}

function formatRequestStatus(status: PackageRequestStatus, subStatus: PackageRequestResponse['subStatus']) {
  if (status === 'InProgress') {
    return i18n.t('identities.detail.inProgress');
  }

  return subStatus === 'PartiallyApproved'
    ? i18n.t('identities.detail.completedPartiallyApproved')
    : subStatus === 'Approved'
      ? i18n.t('identities.detail.completedApproved')
      : subStatus === 'Rejected'
        ? i18n.t('identities.detail.completedRejected')
        : subStatus === 'Expired'
          ? i18n.t('identities.detail.completedExpired')
          : i18n.t('identities.detail.completedSimple');
}

function getInfraStatusVariant(status: string) {
  return status === 'Provisioned' || status === 'Active'
    ? 'success'
    : status === 'Failed' || status === 'Revoked' || status === 'Archived'
      ? 'error'
      : 'secondary';
}

function getProvisioningSummary(items: readonly PACSProvisioningResponse[]) {
  if (items.length === 0) {
    return { label: i18n.t('identities.detail.notMaterialized'), variant: 'secondary' as const };
  }

  if (items.some((item) => item.status === 'Failed')) {
    return { label: i18n.t('identities.detail.provisioningIssue'), variant: 'error' as const };
  }

  if (items.every((item) => item.status === 'Provisioned')) {
    return { label: i18n.t('identities.detail.provisioned'), variant: 'success' as const };
  }

  if (items.some((item) => item.status === 'Pending')) {
    return { label: i18n.t('identities.detail.provisioningPending'), variant: 'secondary' as const };
  }

  return { label: i18n.t('identities.detail.provisioningMixed'), variant: 'secondary' as const };
}

function getProvisioningSummaryVariant(variant: 'success' | 'secondary' | 'error') {
  return variant;
}

function TranslatedProvisioningInsightPanel({ title, emptyLabel, items }: { readonly title: string; readonly emptyLabel: string; readonly items: readonly { key: string; systemName: string; targetName: string; status: string; scheduledFor: string; provisionedAt: string | null; completedAt: string | null; failureReason: string | null; nativeAssignmentId: string | null; validFrom: string; validUntil: string | null; provisioningTiming: string; }[]; }) {
  const { t } = useTranslation();

  return (
    <div className="rounded-interactive border border-border p-4">
      <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{title}</p>
      <div className="mt-3 grid gap-2">
        {items.length === 0 ? <p className="text-[14px] text-muted-foreground">{emptyLabel}</p> : items.map((item) => (
          <div key={item.key} className="rounded-interactive border border-border bg-background p-3">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="font-medium text-foreground">{item.systemName}</p>
                <p className="mt-1 text-[13px] text-muted-foreground">{item.targetName}</p>
              </div>
              <Badge variant={getInfraStatusVariant(item.status)}>{item.status}</Badge>
            </div>
            <dl className="mt-3 grid gap-2 text-[13px] text-muted-foreground">
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.timing')}</dt><dd className="text-right">{item.provisioningTiming}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.window')}</dt><dd className="text-right">{formatDateTimeLabel(item.validFrom)}{item.validUntil ? ` to ${formatDateTimeLabel(item.validUntil)}` : ''}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.scheduled')}</dt><dd className="text-right">{formatDateTimeLabel(item.scheduledFor)}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.provisioned')}</dt><dd className="text-right">{item.provisionedAt ? formatDateTimeLabel(item.provisionedAt) : '-'}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.completed')}</dt><dd className="text-right">{item.completedAt ? formatDateTimeLabel(item.completedAt) : '-'}</dd></div>
              {item.failureReason ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.failure')}</dt><dd className="text-right text-error">{item.failureReason}</dd></div> : null}
              {item.nativeAssignmentId ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.nativeAssignment')}</dt><dd className="text-right">{item.nativeAssignmentId}</dd></div> : null}
            </dl>
          </div>
        ))}
      </div>
    </div>
  );
}

function TranslatedCredentialProvisioningPanel({ assignments, systemsById }: { readonly assignments: readonly CredentialPACSAssignmentResponse[]; readonly systemsById: Map<string, AccessControlSystemResponse>; }) {
  const { t } = useTranslation();

  return (
    <div className="rounded-interactive border border-border p-4">
      <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{t('identities.detail.provisioning')}</p>
      <div className="mt-3 grid gap-2">
        {assignments.length === 0 ? <p className="text-[14px] text-muted-foreground">{t('identities.detail.noPacsProvisioningRows')}</p> : assignments.map((assignment) => (
          <div key={assignment.id} className="rounded-interactive border border-border bg-background p-3">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="font-medium text-foreground">{systemsById.get(assignment.accessControlSystemId)?.name ?? assignment.accessControlSystemId}</p>
                <p className="mt-1 text-[13px] text-muted-foreground">{t('identities.detail.credentialAssignment')}</p>
              </div>
              <Badge variant={getInfraStatusVariant(assignment.status)}>{assignment.status}</Badge>
            </div>
            <dl className="mt-3 grid gap-2 text-[13px] text-muted-foreground">
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.scheduled')}</dt><dd className="text-right">{formatDateTimeLabel(assignment.scheduledFor)}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.provisioned')}</dt><dd className="text-right">{assignment.provisionedAt ? formatDateTimeLabel(assignment.provisionedAt) : '-'}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.revoked')}</dt><dd className="text-right">{assignment.revokedAt ? formatDateTimeLabel(assignment.revokedAt) : '-'}</dd></div>
              <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.attempts')}</dt><dd className="text-right">{assignment.attemptCount}</dd></div>
              {assignment.lastAttemptAt ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.lastAttempt')}</dt><dd className="text-right">{formatDateTimeLabel(assignment.lastAttemptAt)}</dd></div> : null}
              {assignment.failureReasonCode ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.failure')}</dt><dd className="text-right text-error">{assignment.failureReasonCode}</dd></div> : null}
              {assignment.errorMessage ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.error')}</dt><dd className="text-right text-error">{assignment.errorMessage}</dd></div> : null}
              {assignment.nativeAssignmentId ? <div className="flex items-center justify-between gap-3"><dt>{t('identities.detail.nativeAssignment')}</dt><dd className="text-right">{assignment.nativeAssignmentId}</dd></div> : null}
            </dl>
          </div>
        ))}
      </div>
    </div>
  );
}

type GrantView = {
  readonly grant: AccessGrantResponse;
  readonly grantAssignments: PACSAssignmentResponse[];
  readonly grantProvisionings: PACSProvisioningResponse[];
  readonly packageName: string;
  readonly accessItemName: string;
  readonly locationLabels: string;
  readonly provisioningSummary: { readonly label: string; readonly variant: 'success' | 'secondary' | 'error' };
  readonly hasStatusMismatch: boolean;
  readonly approvalHistory: ReturnType<typeof getApprovalHistory>;
  readonly systemApprovalReasons: string[];
  readonly shouldExpand: boolean;
  readonly approvalSummary: string;
  readonly locationLabelsById: Map<string, string>;
  readonly systemsById: Map<string, AccessControlSystemResponse>;
  readonly targetsById: Map<string, AccessLevelTargetResponse>;
};

type AssignmentData = {
  readonly grants: AccessGrantResponse[];
  readonly assignments: PACSAssignmentResponse[];
  readonly provisionings: PACSProvisioningResponse[];
  readonly packagesById: Map<string, PackageResponse>;
  readonly accessItemsById: Map<string, AccessItemResponse>;
  readonly packageAccessItemsByPackageId: Map<string, AccessItemResponse[]>;
  readonly locationsById: Map<string, LocationResponse>;
  readonly targetsById: Map<string, AccessLevelTargetResponse>;
  readonly requestDetailsById: Map<string, PackageRequestDetailResponse>;
};

type CredentialsData = {
  readonly credentials: CredentialResponse[];
  readonly assignmentsByCredentialId: Map<string, CredentialPACSAssignmentResponse[]>;
  readonly credentialTypesById: Map<string, CredentialTypeResponse>;
};

type PackageAssignmentGroupView = {
  readonly packageId: string;
  readonly sourceId: string;
  readonly sourceType: 'CatalogRequest' | 'Automated';
  readonly packageName: string;
  readonly sourceLabel: string;
  readonly sourceReason: string;
  readonly validityLabel: string;
  readonly locationSummary: string;
  readonly accessItems: AccessItemGroupView[];
  readonly provisioningSummary: { readonly label: string; readonly variant: 'success' | 'secondary' | 'error' };
  readonly provisioningCount: number;
  readonly approvalSummary: string;
  readonly shouldExpand: boolean;
  readonly requestStatus?: PackageRequestStatus;
  readonly requestSubStatus?: PackageRequestResponse['subStatus'];
  readonly requestCreatedAt?: string;
};

type AccessItemGroupView = {
  readonly accessItemId: string;
  readonly accessItemName: string;
  readonly leafViews: GrantView[];
  readonly materializationOutcomes: AccessGrantMaterializationOutcomeResponse[];
  readonly provisioningSummary: { readonly label: string; readonly variant: 'success' | 'secondary' | 'error' };
  readonly provisioningCount: number;
  readonly approvalSummary: string;
  readonly shouldExpand: boolean;
};

type AutomatedPackageAssignmentGroupView = {
  readonly packageId: string;
  readonly sourceId: string;
  readonly sourceType: 'Automated';
  readonly grantIds: string[];
  readonly status: AccessGrantResponse['status'];
  readonly packageName: string;
  readonly sourceLabel: string;
  readonly sourceReason: string;
  readonly validityLabel: string;
  readonly locationSummary: string;
  readonly accessItems: AutomatedAccessItemGroupView[];
  readonly provisioningSummary: { readonly label: string; readonly variant: 'success' | 'secondary' | 'error' };
  readonly provisioningCount: number;
  readonly approvalSummary: string;
  readonly shouldExpand: boolean;
  readonly revokedBy: string | null;
  readonly revokeCause: AccessGrantResponse['revokeCause'];
};

type AutomatedAccessItemGroupView = {
  readonly accessItemId: string;
  readonly accessItemName: string;
  readonly view: GrantView;
  readonly locationLabels: string;
  readonly assignments: PACSAssignmentResponse[];
  readonly provisionings: PACSProvisioningResponse[];
  readonly materializationOutcomes: AccessGrantMaterializationOutcomeResponse[];
  readonly hasTargets: boolean;
  readonly provisioningSummary: { readonly label: string; readonly variant: 'success' | 'secondary' | 'error' };
  readonly shouldExpand: boolean;
};

type PacsAssignmentRowView = {
  readonly assignment: PACSAssignmentResponse;
  readonly locationLabel: string;
  readonly systemName: string;
  readonly targetName: string;
  readonly provisionings: PACSProvisioningResponse[];
  readonly shouldExpand: boolean;
};

function buildGrantView(
  grant: AccessGrantResponse,
  data: AssignmentData | undefined,
  systemsById: Map<string, AccessControlSystemResponse>,
): GrantView {
  const grantAssignments = (data?.assignments ?? []).filter((item) => item.sourceAssignmentId === grant.id);
  const grantAssignmentIds = new Set(grantAssignments.map((item) => item.id));
  const grantProvisionings = (data?.provisionings ?? []).filter((item) => item.sourceAssignmentIds.some((assignmentId) => grantAssignmentIds.has(assignmentId)));
  const packageName = data?.packagesById.get(grant.packageId)?.name ?? grant.packageId;
  const accessItemName = grant.accessItemId ? data?.accessItemsById.get(grant.accessItemId)?.name ?? grant.accessItemId : packageName;
  const locationLabelsById = new Map((grant.locationIds ?? []).map((locationId) => [locationId, getLocationLabel(data?.locationsById.get(locationId))]));
  const locationLabels = Array.from(locationLabelsById.values()).join(', ');
  const provisioningSummary = getProvisioningSummary(grantProvisionings);
  const hasStatusMismatch = grantAssignments.some((assignment) => assignment.status === 'Pending') && grantProvisionings.some((provisioning) => provisioning.status === 'Provisioned');
  const approvalFlow = resolveApprovalFlow(grant, data?.requestDetailsById);
  const approvalHistory = approvalFlow ? getApprovalHistory(approvalFlow) : [];
  const systemApprovalReasons = approvalFlow ? getSystemApprovalReasons(approvalFlow) : [];
  const shouldExpand = hasStatusMismatch || provisioningSummary.variant !== 'success';
  const approvalSummary = getApprovalSummary(approvalHistory, systemApprovalReasons, grant.sourceKind);

  return {
    grant,
    grantAssignments,
    grantProvisionings,
    packageName,
    accessItemName,
    locationLabels,
    provisioningSummary,
    hasStatusMismatch,
    approvalHistory,
    systemApprovalReasons,
    shouldExpand,
    approvalSummary,
    locationLabelsById,
    systemsById,
    targetsById: data?.targetsById ?? new Map<string, AccessLevelTargetResponse>(),
  };
}

function groupCatalogViews(views: GrantView[], requestDetailsById: Map<string, PackageRequestDetailResponse>) {
  const groups = new Map<string, GrantView[]>();

  views.forEach((view) => {
    const key = view.grant.sourceId;
    const current = groups.get(key) ?? [];
    current.push(view);
    groups.set(key, current);
  });

  return Array.from(groups.entries()).map(([requestId, requestViews]): PackageAssignmentGroupView => {
    const request = requestDetailsById.get(requestId)?.request;
    const allProvisionings = requestViews.flatMap((item) => item.grantProvisionings);
    const allApprovalHistory = requestViews.flatMap((item) => item.approvalHistory);
    const allSystemReasons = requestViews.flatMap((item) => item.systemApprovalReasons);

    return {
      packageId: requestViews[0]?.grant.packageId ?? '',
      sourceId: requestId,
      sourceType: 'CatalogRequest',
      packageName: requestViews[0]?.packageName ?? requestViews[0]?.grant.packageId ?? '',
      sourceLabel: 'Catalogue Request',
      sourceReason: request?.requestReason ?? 'Catalogue request',
      validityLabel: getValidityLabel(requestViews),
      locationSummary: getLocationSummary(requestViews),
      accessItems: groupAccessItems(requestViews),
      provisioningSummary: getProvisioningSummary(allProvisionings),
      provisioningCount: allProvisionings.length,
      approvalSummary: getApprovalSummary(allApprovalHistory, allSystemReasons, 'CatalogRequest'),
      shouldExpand: requestViews.some((item) => item.shouldExpand),
      requestStatus: request?.status,
      requestSubStatus: request?.subStatus ?? null,
      requestCreatedAt: request?.createdAt,
    };
  });
}

function groupAutomaticViews(
  views: GrantView[],
  packageAccessItemsByPackageId: Map<string, AccessItemResponse[]>,
  targetsById: Map<string, AccessLevelTargetResponse>,
) {
  return views.map((view): AutomatedPackageAssignmentGroupView => {
    const packageId = view.grant.packageId;
    const sourceId = view.grant.sourceId;

    return {
      packageId,
      sourceId,
      sourceType: 'Automated',
      grantIds: [view.grant.id],
      status: view.grant.status,
      packageName: view.packageName,
      sourceLabel: formatSourceLabel(view.grant.sourceKind),
      sourceReason: getAutomaticReason(view),
      validityLabel: getValidityLabel([view]),
      locationSummary: getLocationSummary([view]),
      accessItems: groupAutomaticAccessItems([view], packageAccessItemsByPackageId.get(packageId) ?? [], targetsById),
      provisioningSummary: getProvisioningSummary(view.grantProvisionings),
      provisioningCount: view.grantProvisionings.length,
      approvalSummary: getApprovalSummary([], [], view.grant.sourceKind),
      shouldExpand: view.shouldExpand,
      revokedBy: view.grant.revokedBy,
      revokeCause: view.grant.revokeCause ?? null,
    };
  });
}

function groupAutomaticAccessItems(views: readonly GrantView[], accessItems: readonly AccessItemResponse[], targetsById: Map<string, AccessLevelTargetResponse>) {
  const uniqueAccessItems = Array.from(new Map(accessItems.map((item) => [item.id, item])).values());

  return uniqueAccessItems.map((accessItem): AutomatedAccessItemGroupView => {
    const targetIds = new Set(Array.from(targetsById.values()).filter((target) => target.accessItemId === accessItem.id).map((target) => target.id));
    const assignments = views.flatMap((view) => view.grantAssignments.filter((item) => targetIds.has(item.accessLevelTargetId)));
    const provisionings = views.flatMap((view) => view.grantProvisionings.filter((item) => targetIds.has(item.accessLevelTargetId)));
    const materializationOutcomes = views.flatMap((view) => view.grant.materializationOutcomes.filter((item) => item.accessItemId === accessItem.id));
    const hasMaterializationIssue = materializationOutcomes.some((item) => item.status === 'SkippedNoTarget' || item.status === 'Failed');
    const locationLabels = Array.from(new Set(views.map((view) => view.locationLabels).filter(Boolean))).join(', ');
    const view = views[0]!;

    return {
      accessItemId: accessItem.id,
      accessItemName: accessItem.name,
      view,
      locationLabels,
      assignments,
      provisionings,
      materializationOutcomes,
      hasTargets: targetIds.size > 0,
      provisioningSummary: getProvisioningSummary(provisionings),
      shouldExpand: hasMaterializationIssue || targetIds.size === 0 || provisionings.some((item) => item.status !== 'Provisioned') || assignments.some((item) => item.status === 'Pending') || provisionings.length === 0,
    };
  });
}

function groupAccessItems(views: GrantView[]) {
  return groupBy(views, (item) => item.grant.accessItemId ?? item.grant.id).map(([accessItemId, accessItemViews]): AccessItemGroupView => ({
    accessItemId,
    accessItemName: accessItemViews[0]?.accessItemName ?? accessItemId,
    leafViews: accessItemViews,
    materializationOutcomes: accessItemViews.flatMap((item) => item.grant.materializationOutcomes.filter((outcome) => outcome.accessItemId === accessItemId)),
    provisioningSummary: getProvisioningSummary(accessItemViews.flatMap((item) => item.grantProvisionings)),
    provisioningCount: accessItemViews.flatMap((item) => item.grantProvisionings).length,
    approvalSummary: getApprovalSummary(accessItemViews.flatMap((item) => item.approvalHistory), accessItemViews.flatMap((item) => item.systemApprovalReasons), accessItemViews[0]?.grant.sourceKind ?? 'Manual'),
    shouldExpand: accessItemViews.some((item) => item.shouldExpand) || accessItemViews.flatMap((item) => item.grant.materializationOutcomes.filter((outcome) => outcome.accessItemId === accessItemId)).some((outcome) => outcome.status === 'SkippedNoTarget' || outcome.status === 'Failed'),
  }));
}

function buildPacsAssignmentRows(views: readonly GrantView[], assignments: readonly PACSAssignmentResponse[], provisionings: readonly PACSProvisioningResponse[]) {
  const locationLabelByAssignmentId = new Map<string, string>();

  views.forEach((view) => {
    view.grantAssignments.forEach((assignment) => {
      locationLabelByAssignmentId.set(assignment.id, view.locationLabels || 'Unscoped');
    });
  });

  const systemsById = views[0]?.systemsById ?? new Map<string, AccessControlSystemResponse>();
  const targetsById = views[0]?.targetsById ?? new Map<string, AccessLevelTargetResponse>();

  return [...assignments]
    .map((assignment): PacsAssignmentRowView => ({
      assignment,
      locationLabel: locationLabelByAssignmentId.get(assignment.id) ?? 'Unscoped',
      systemName: systemsById.get(assignment.accessControlSystemId)?.name ?? 'Unknown system',
      targetName: targetsById.get(assignment.accessLevelTargetId)?.name ?? 'Unknown target',
      provisionings: provisionings.filter((item) => item.sourceAssignmentIds.includes(assignment.id)),
      shouldExpand: assignment.status !== 'Provisioned' || provisionings.some((item) => item.sourceAssignmentIds.includes(assignment.id) && item.status !== 'Provisioned'),
    }))
    .sort((left, right) => left.systemName.localeCompare(right.systemName) || left.locationLabel.localeCompare(right.locationLabel) || left.targetName.localeCompare(right.targetName) || left.assignment.validFrom.localeCompare(right.assignment.validFrom));
}

function getLocationLabelForOutcome(views: readonly GrantView[], locationId: string) {
  return views.find((view) => view.locationLabelsById.has(locationId))?.locationLabelsById.get(locationId) ?? locationId;
}

function groupBy<T>(items: readonly T[], keySelector: (item: T) => string) {
  const map = new Map<string, T[]>();
  items.forEach((item) => {
    const key = keySelector(item);
    const current = map.get(key) ?? [];
    current.push(item);
    map.set(key, current);
  });
  return Array.from(map.entries());
}

function getLocationSummary(views: readonly GrantView[]) {
  return Array.from(new Set(views.flatMap((item) => item.locationLabels.split(', ').filter(Boolean)))).join(', ');
}

function getValidityLabel(views: readonly GrantView[]) {
  const validFrom = views[0]?.grant.validFrom;
  const validUntil = views[0]?.grant.validUntil;
  if (!validFrom) return '-';
  return `${formatDateTimeLabel(validFrom)}${validUntil ? ` to ${formatDateTimeLabel(validUntil)}` : ''}`;
}

function getValidityWindowLabel(validFrom: string, validUntil: string | null) {
  return validUntil ? `${formatDateTimeLabel(validFrom)} to ${formatDateTimeLabel(validUntil)}` : '';
}

function getAutomaticReason(view: GrantView | undefined) {
  if (!view) return 'Automatic grant';
  return view.grant.reasonText || formatSourceLabel(view.grant.sourceKind);
}

function getApprovalSummary(
  history: readonly { approverDisplayName: string }[],
  systemApprovalReasons: readonly string[],
  sourceKind: AccessGrantResponse['sourceKind'],
) {
  if (sourceKind !== 'CatalogRequest') {
    return 'Automatic grant';
  }

  if (history.length === 0 && systemApprovalReasons.length > 0) {
    return 'System approved';
  }

  if (history.length === 0) {
    return 'No approval history';
  }

  const uniqueApprovers = Array.from(new Set(history.map((item) => item.approverDisplayName)));
  return uniqueApprovers.length === 1
    ? uniqueApprovers[0]
    : `${uniqueApprovers[0]} + ${uniqueApprovers.length - 1} more`;
}

function resolveApprovalFlow(grant: AccessGrantResponse, requestDetailsById: Map<string, PackageRequestDetailResponse> | undefined) {
  if (!grant.approvalFlowId || grant.sourceKind !== 'CatalogRequest') {
    return null;
  }

  const requestDetail = requestDetailsById?.get(grant.sourceId);
  return requestDetail?.flows.find((flow) => flow.approvalFlowId === grant.approvalFlowId) ?? null;
}

function getApprovalHistory(flow: PackageRequestDetailFlowResponse) {
  return flow.requirements
    .flatMap((requirement) => requirement.decisions.map((decision) => ({
      key: decision.id,
      approverDisplayName: decision.approverDisplayName,
      role: formatDecisionRole(requirement.type, decision.role, requirement.approvalGroupName),
      decisionKind: decision.decisionKind,
      decidedAt: decision.decidedAt,
      note: decision.note,
    })))
    .sort((left, right) => new Date(left.decidedAt).getTime() - new Date(right.decidedAt).getTime());
}

function getSystemApprovalReasons(flow: PackageRequestDetailFlowResponse) {
  return flow.requirements
    .filter((requirement) => requirement.status === 'SystemApproved' && requirement.systemApprovalReason)
    .map((requirement) => requirement.systemApprovalReason as string);
}

function formatDecisionRole(type: PackageRequestDetailRequirementResponse['type'], role: PackageRequestDetailDecisionResponse['role'], approvalGroupName: string | null) {
  return type === 'Destination'
    ? approvalGroupName ?? 'Destination approval'
    : `${role} approval`;
}

function formatSourceLabel(sourceKind: AccessGrantResponse['sourceKind']) {
  switch (sourceKind) {
    case 'OrganizationalUnit':
    case 'Persona':
      return 'HR Policy';
    case 'ReceptionArrival':
    case 'VisitorLocation':
      return 'Visitor Policy';
    case 'Manual':
      return 'Manual grant';
    default:
      return 'Catalog request';
  }
}
