import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { ChevronLeft, ChevronRight, ExternalLink } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { getGrantApprovalLabel, getGrantApprovalVariant, getGrantBusinessSummary, getGrantComplianceLabel, getGrantComplianceUntilLabel, getGrantComplianceVariant, getGrantStatusVariant } from '@/shared/access-grants/grant-status';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { VisitStatusBadge } from '@/shared/components/visit-status-badge';
import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { getPerspectiveById, type PerspectiveId } from '@/shared/perspectives/app-perspectives';
import { cn } from '@/shared/utils/cn';

type CredentialResponse = components['schemas']['CredentialResponse'];
type CredentialTypeResponse = components['schemas']['CredentialTypeResponse'];
type AccessGrantResponse = components['schemas']['AccessGrantResponse'];
type ContractorJobResponse = components['schemas']['ContractorJobResponse'];
type PackageRequestResponse = components['schemas']['PackageRequestResponse'];
type PackageResponse = components['schemas']['PackageResponse'];
type VisitResponse = components['schemas']['VisitResponse'];
type VisitInvitationResponse = components['schemas']['VisitInvitationResponse'];

type EmployeeRequestRow = {
  readonly request: PackageRequestResponse;
  readonly sourceLabel: string;
};

type ExpectedVisitorRow = {
  readonly invitation: VisitInvitationResponse;
  readonly visit: VisitResponse;
};

export function PerspectiveHomePage({ perspectiveId }: { perspectiveId: PerspectiveId }) {
  const { t } = useTranslation();
  const perspective = getPerspectiveById(perspectiveId);

  if (!perspective) {
    return null;
  }

  if (perspectiveId === 'employee') {
    return <EmployeeOverviewPage />;
  }

  if (perspectiveId === 'manager') {
    return <ManagerOverviewPage />;
  }

  return (
    <section className="grid gap-6">
      <div className="rounded-structural border border-border bg-content p-6 sm:p-8">
        <p className="text-[14px] font-semibold uppercase text-primary">{t('perspectives.placeholder.title')}</p>
        <h1 className="mt-3 text-[30px] font-semibold tracking-tight">{perspective.label}</h1>
        <p className="mt-3 max-w-2xl text-[14px] leading-6 text-muted-foreground">{perspective.description}</p>
      </div>

      <div className="rounded-structural border border-dashed border-border bg-content p-6 text-[14px] text-muted-foreground">
        {t('perspectives.placeholder.empty')}
      </div>
    </section>
  );
}

function ManagerOverviewPage() {
  const { t } = useTranslation();
  const actorQuery = useCurrentActor();
  const employeeId = actorQuery.data?.employeeId ?? null;

  const directReportsQuery = useQuery({
    queryKey: ['manager', 'overview', 'team', employeeId, 'direct'],
    enabled: Boolean(employeeId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees', {
        params: { query: { Query: undefined, Status: [], OrganizationUnitId: undefined, ManagerEmployeeId: employeeId ?? undefined, IncludeIndirectReports: false, IncludeDescendants: true, Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load direct reports.');
      }

      return data?.items ?? [];
    },
  });

  const totalReportsQuery = useQuery({
    queryKey: ['manager', 'overview', 'team', employeeId, 'all'],
    enabled: Boolean(employeeId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/employees/employees', {
        params: { query: { Query: undefined, Status: [], OrganizationUnitId: undefined, ManagerEmployeeId: employeeId ?? undefined, IncludeIndirectReports: true, IncludeDescendants: true, Page: 0, PageSize: 500 } as never },
      });

      if (error) {
        throw new Error('Could not load team.');
      }

      return data?.items ?? [];
    },
  });

  return (
    <section className="grid gap-6">
      <div className="rounded-structural border border-border bg-content p-6 sm:p-8">
        <p className="text-[14px] font-semibold uppercase text-primary">{t('perspectives.placeholder.title')}</p>
        <h1 className="mt-3 text-[30px] font-semibold tracking-tight">{t('perspectives.managerHome.title')}</h1>
        <p className="mt-3 max-w-2xl text-[14px] leading-6 text-muted-foreground">
          {actorQuery.data?.displayName ? t('perspectives.managerHome.signedInAs', { name: actorQuery.data.displayName }) : t('perspectives.managerHome.defaultDescription')}
        </p>
        <div className="mt-6 flex flex-wrap gap-3">
          <Link to="/manager/my-team" className={buttonVariants()}>{t('perspectives.managerHome.openMyTeam')}</Link>
          <Link to="/manager/approval-inbox" className={buttonVariants({ variant: 'outline' })}>{t('perspectives.managerHome.openApprovalInbox')}</Link>
        </div>
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('perspectives.managerHome.myTeam')}</CardTitle>
            <CardDescription>{t('perspectives.managerHome.myTeamDescription')}</CardDescription>
          </CardHeader>
          <CardContent>
            {directReportsQuery.isError || totalReportsQuery.isError ? <ErrorText message={t('perspectives.managerHome.couldNotLoadTeamSummary')} /> : null}
            {directReportsQuery.isLoading || totalReportsQuery.isLoading ? <MutedText message={t('perspectives.managerHome.loadingTeamSummary')} /> : null}
            {!directReportsQuery.isLoading && !totalReportsQuery.isLoading ? (
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-structural border border-border p-4">
                  <p className="text-[12px] uppercase tracking-[0.18em] text-muted-foreground">{t('perspectives.managerHome.directReports')}</p>
                  <p className="mt-3 text-[28px] font-semibold tracking-tight text-foreground">{directReportsQuery.data?.length ?? 0}</p>
                </div>
                <div className="rounded-structural border border-border p-4">
                  <p className="text-[12px] uppercase tracking-[0.18em] text-muted-foreground">{t('perspectives.managerHome.directAndIndirect')}</p>
                  <p className="mt-3 text-[28px] font-semibold tracking-tight text-foreground">{totalReportsQuery.data?.length ?? 0}</p>
                </div>
              </div>
            ) : null}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('perspectives.managerHome.approvalInbox')}</CardTitle>
            <CardDescription>{t('perspectives.managerHome.approvalInboxDescription')}</CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-[14px] text-muted-foreground">{t('perspectives.managerHome.approvalInboxBody')}</p>
            <Link to="/manager/approval-inbox" className={`${buttonVariants({ variant: 'outline' })} mt-4`}>
              {t('perspectives.managerHome.reviewApprovals')}
            </Link>
          </CardContent>
        </Card>
      </div>
    </section>
  );
}

function EmployeeOverviewPage() {
  const actorQuery = useCurrentActor();
  const navigate = useNavigate();
  const [visitorsDay, setVisitorsDay] = useState(() => startOfDay(new Date()));
  const [activeTab, setActiveTab] = useState<'credentials' | 'packages' | 'requests' | 'visitors' | 'contractor-jobs'>('requests');

  const identityId = actorQuery.data?.identityId ?? null;
  const isHost = actorQuery.data?.isHost ?? false;
  const canPlanContractors = (actorQuery.data?.roles ?? []).includes('contractor-planning') || (actorQuery.data?.roles ?? []).includes('contractor-enrollment');
  const visitorsInterval = getDayInterval(visitorsDay);

  const credentialsQuery = useQuery({
    queryKey: ['employee', 'overview', 'credentials', identityId],
    enabled: Boolean(identityId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/credential-management/credentials', {
        params: { query: { CredentialTypeId: undefined, IdentityId: identityId ?? undefined, Status: undefined, Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load credentials.');
      }

      return (data?.items ?? []).filter((credential: CredentialResponse) => !['Expired', 'Revoked', 'Archived'].includes(credential.status));
    },
  });

  const credentialTypesQuery = useQuery({
    queryKey: ['employee', 'overview', 'credential-types', (credentialsQuery.data ?? []).map((item) => item.credentialTypeId).join(',')],
    enabled: (credentialsQuery.data?.length ?? 0) > 0,
    queryFn: async () => {
      const ids = Array.from(new Set((credentialsQuery.data ?? []).map((item) => item.credentialTypeId)));
      const { data, error } = await api.GET('/api/credential-management/credential-types', {
        params: { query: { Query: undefined, Technology: undefined, Status: undefined, Page: 0, PageSize: 200, ids } as never },
      });

      if (error) {
        throw new Error('Could not load credential types.');
      }

      return new Map((data?.items ?? []).map((item: CredentialTypeResponse) => [item.id, item]));
    },
  });

  const grantsQuery = useQuery({
    queryKey: ['employee', 'overview', 'grants', identityId],
    enabled: Boolean(identityId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/access-grants', {
        params: { query: { IdentityId: identityId ?? undefined, PackageId: undefined, Status: 'Active', Page: 0, PageSize: 200 } as never },
      });

      if (error) {
        throw new Error('Could not load assigned packages.');
      }

      return data?.items ?? [];
    },
  });

  const requesterRequestsQuery = useQuery({
    queryKey: ['employee', 'overview', 'requests', 'requester', identityId],
    enabled: Boolean(identityId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/package-requests', {
        params: { query: { RequesterIdentityId: identityId ?? undefined, BeneficiaryIdentityId: undefined, Status: 'InProgress', ids: [] } as never },
      });

      if (error) {
        throw new Error('Could not load requests.');
      }

      return data?.items ?? [];
    },
  });

  const beneficiaryRequestsQuery = useQuery({
    queryKey: ['employee', 'overview', 'requests', 'beneficiary', identityId],
    enabled: Boolean(identityId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/package-requests', {
        params: { query: { RequesterIdentityId: undefined, BeneficiaryIdentityId: identityId ?? undefined, Status: 'InProgress', ids: [] } as never },
      });

      if (error) {
        throw new Error('Could not load requests.');
      }

      return data?.items ?? [];
    },
  });

  const packageIds = Array.from(new Set([
    ...(grantsQuery.data ?? []).map((item) => item.packageId),
    ...(requesterRequestsQuery.data ?? []).map((item) => item.packageId),
    ...(beneficiaryRequestsQuery.data ?? []).map((item) => item.packageId),
  ]));

  const packagesQuery = useQuery({
    queryKey: ['employee', 'overview', 'packages', packageIds.join(',')],
    enabled: packageIds.length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/packages', {
        params: { query: { Name: undefined, Page: 0, PageSize: 200, ids: packageIds } as never },
      });

      if (error) {
        throw new Error('Could not load packages.');
      }

      return new Map((data?.items ?? []).map((item: PackageResponse) => [item.id, item]));
    },
  });

  const visitorsQuery = useQuery({
    queryKey: ['employee', 'overview', 'expected-visitors', visitorsInterval.start.toISOString(), visitorsInterval.stop.toISOString()],
    enabled: isHost,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/visitors/visits', {
        params: {
          query: {
            after: visitorsInterval.start.toISOString(),
            before: visitorsInterval.stop.toISOString(),
            page: 0,
            pageSize: 250,
            withStatus: ['Scheduled'],
          },
        },
      });

      if (error) {
        throw new Error('Could not load expected visitors.');
      }

      return data?.items ?? [];
    },
  });

  const activeContractorJobsQuery = useQuery({
    queryKey: ['employee', 'overview', 'contractor-jobs', 'active'],
    enabled: canPlanContractors,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/jobs', {
        params: {
          query: {
            Query: undefined,
            CompanyId: undefined,
            JobTypeId: undefined,
            LocationId: undefined,
            PlannedStartAfter: undefined,
            PlannedEndBefore: undefined,
            Status: ['Active'],
            Page: 0,
            PageSize: 200,
          } as never,
        },
      });

      if (error) {
        throw new Error('Could not load active contractor jobs.');
      }

      return data?.items ?? [];
    },
  });

  const credentials = credentialsQuery.data ?? [];
  const credentialTypesById = credentialTypesQuery.data ?? new Map<string, CredentialTypeResponse>();
  const activeGrants = (grantsQuery.data ?? []).filter((item) => item.status === 'Active');
  const assignedPackages = Array.from(new Map(activeGrants.map((item) => [item.packageId, item])).values())
    .sort((left, right) => {
      const leftName = packagesQuery.data?.get(left.packageId)?.name ?? left.packageId;
      const rightName = packagesQuery.data?.get(right.packageId)?.name ?? right.packageId;
      return leftName.localeCompare(rightName);
    });
  const requests = mergeEmployeeRequests(requesterRequestsQuery.data ?? [], beneficiaryRequestsQuery.data ?? [], identityId);
  const expectedVisitors = flattenExpectedVisitors(visitorsQuery.data ?? []);
  const activeContractorJobs = activeContractorJobsQuery.data ?? [];
  const hasActorContext = Boolean(identityId);
  const activeCredentialsCount = credentials.filter((item) => item.status === 'Active').length;
  const todayVisitorsCount = isHost ? expectedVisitors.length : 0;

  return (
    <section className="grid gap-6">
      <div className="rounded-structural border border-border bg-content p-6 md:p-8">
        <div>
          <h1 className="text-[30px] font-semibold tracking-tight text-foreground">Employee Overview</h1>
          <p className="mt-3 max-w-3xl text-[14px] leading-6 text-muted-foreground">
            {actorQuery.data?.displayName ? `Signed in as ${actorQuery.data.displayName}. Review your access, pending requests, and today's activity.` : 'Your identity, access, requests, and visitors at a glance.'}
          </p>
        </div>

        <div className="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-4 2xl:grid-cols-5">
          <OverviewStat label="Active credentials" value={String(activeCredentialsCount)} detail={`${credentials.length} total assigned`} />
          <OverviewStat label="Active packages" value={String(assignedPackages.length)} detail={assignedPackages.length === 1 ? '1 package ready to use' : `${assignedPackages.length} packages ready to use`} />
          <OverviewStat label="Requests in progress" value={String(requests.length)} detail={requests.length === 0 ? 'No pending requests' : 'Submitted by you or for you'} />
          {canPlanContractors ? <OverviewStat label="Active jobs" value={String(activeContractorJobs.length)} detail={activeContractorJobs.length === 1 ? '1 contractor job active' : `${activeContractorJobs.length} contractor jobs active`} /> : null}
          {isHost ? <OverviewStat label="Expected visitors" value={String(todayVisitorsCount)} detail={formatDayLabel(visitorsDay)} /> : null}
        </div>
      </div>

      {!actorQuery.isLoading && !hasActorContext ? (
        <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
          Could not load current employee identity.
        </p>
      ) : null}

      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as 'credentials' | 'packages' | 'requests' | 'visitors' | 'contractor-jobs')}>
        <TabsList className="overflow-x-auto">
          <TabsTrigger value="credentials">Active Credentials <span className="text-[12px] font-medium text-muted-foreground">{credentials.length}</span></TabsTrigger>
          <TabsTrigger value="packages">Assigned Packages <span className="text-[12px] font-medium text-muted-foreground">{assignedPackages.length}</span></TabsTrigger>
          <TabsTrigger value="requests">Requests <span className="text-[12px] font-medium text-muted-foreground">{requests.length}</span></TabsTrigger>
          {canPlanContractors ? <TabsTrigger value="contractor-jobs">Contractor Jobs <span className="text-[12px] font-medium text-muted-foreground">{activeContractorJobs.length}</span></TabsTrigger> : null}
          {isHost ? <TabsTrigger value="visitors">Visitors <span className="text-[12px] font-medium text-muted-foreground">{todayVisitorsCount}</span></TabsTrigger> : null}
        </TabsList>

        <TabsContent value="credentials" className="mt-5">
              {credentialsQuery.isError || credentialTypesQuery.isError ? <ErrorText message="Could not load assigned credentials." /> : null}
              {credentialsQuery.isLoading || credentialTypesQuery.isLoading ? <MutedText message="Loading assigned credentials..." /> : null}
              {!credentialsQuery.isLoading && !credentialTypesQuery.isLoading && credentials.length === 0 ? <EmptyText message="No credentials assigned yet." /> : null}
              {!credentialsQuery.isLoading && !credentialTypesQuery.isLoading && credentials.length > 0 ? (
                <div className="grid gap-3">
                  {credentials.map((credential) => {
                    const credentialType = credentialTypesById.get(credential.credentialTypeId);

                    return (
                      <OverviewListItem key={credential.id}>
                        <div className="flex flex-wrap items-start justify-between gap-3">
                          <div>
                            <p className="text-[15px] font-semibold text-foreground">{credentialType?.name ?? credential.formattedIdentifier}</p>
                            <p className="mt-1 text-[14px] text-muted-foreground">{credential.formattedIdentifier}</p>
                          </div>
                          <Badge variant={getCredentialStatusVariant(credential.status)}>{credential.status}</Badge>
                        </div>
                        <div className="mt-4 flex flex-wrap gap-2">
                          <MetaChip>{credential.purpose}</MetaChip>
                          <MetaChip>{credential.validUntil ? `valid until ${formatDateTimeLabel(credential.validUntil)}` : 'no end date'}</MetaChip>
                        </div>
                      </OverviewListItem>
                    );
                  })}
                </div>
              ) : null}
        </TabsContent>

        <TabsContent value="packages" className="mt-5">
              {grantsQuery.isError || packagesQuery.isError ? <ErrorText message="Could not load assigned packages." /> : null}
              {grantsQuery.isLoading || packagesQuery.isLoading ? <MutedText message="Loading assigned packages..." /> : null}
              {!grantsQuery.isLoading && !packagesQuery.isLoading && assignedPackages.length === 0 ? <EmptyText message="No active packages assigned." /> : null}
              {!grantsQuery.isLoading && !packagesQuery.isLoading && assignedPackages.length > 0 ? (
                <div className="grid gap-3">
                  {assignedPackages.map((grant) => {
                    const pkg = packagesQuery.data?.get(grant.packageId);

                    return (
                      <OverviewListItem key={grant.id}>
                        <div className="flex flex-wrap items-start justify-between gap-3">
                          <div>
                            <p className="text-[15px] font-semibold text-foreground">{pkg?.name ?? grant.packageId}</p>
                            <p className="mt-1 text-[14px] leading-6 text-muted-foreground">{pkg?.description ?? 'Current package assignment.'}</p>
                          </div>
                          <Badge variant={getGrantStatusVariant(grant.status)}>{grant.status}</Badge>
                        </div>
                        <div className="mt-4 flex flex-wrap gap-2">
                          <Badge variant={getGrantApprovalVariant(grant.approvalStatus)}>{getGrantApprovalLabel(grant.approvalStatus)}</Badge>
                          <Badge variant={getGrantComplianceVariant(grant.complianceStatus)}>{getGrantComplianceLabel(grant.complianceStatus)}</Badge>
                          <MetaChip>{`from ${formatDateTimeLabel(grant.validFrom)}`}</MetaChip>
                          <MetaChip>{grant.validUntil ? `until ${formatDateTimeLabel(grant.validUntil)}` : 'no end date'}</MetaChip>
                          <MetaChip>{getGrantBusinessSummary(grant)}</MetaChip>
                          {getGrantComplianceUntilLabel(grant) ? <MetaChip>{`compliant until ${formatDateTimeLabel(getGrantComplianceUntilLabel(grant)!)}`}</MetaChip> : null}
                        </div>
                      </OverviewListItem>
                    );
                  })}
                </div>
              ) : null}
        </TabsContent>

        <TabsContent value="requests" className="mt-5">
              {requesterRequestsQuery.isError || beneficiaryRequestsQuery.isError || packagesQuery.isError ? <ErrorText message="Could not load requests in progress." /> : null}
              {requesterRequestsQuery.isLoading || beneficiaryRequestsQuery.isLoading || packagesQuery.isLoading ? <MutedText message="Loading requests in progress..." /> : null}
              {!requesterRequestsQuery.isLoading && !beneficiaryRequestsQuery.isLoading && !packagesQuery.isLoading && requests.length === 0 ? <EmptyText message="No requests in progress." /> : null}
              {!requesterRequestsQuery.isLoading && !beneficiaryRequestsQuery.isLoading && !packagesQuery.isLoading && requests.length > 0 ? (
                <div className="grid gap-3">
                  {requests.map((item) => (
                    <OverviewListItem
                      key={item.request.id}
                      role="button"
                      tabIndex={0}
                      onClick={() => void navigate({ to: '/employee/request-access/$requestId', params: { requestId: item.request.id } })}
                      onKeyDown={(event) => {
                        if (event.key === 'Enter' || event.key === ' ') {
                          event.preventDefault();
                          void navigate({ to: '/employee/request-access/$requestId', params: { requestId: item.request.id } });
                        }
                      }}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-[15px] font-semibold text-foreground">{packagesQuery.data?.get(item.request.packageId)?.name ?? item.request.packageId}</p>
                          <p className="mt-1 text-[14px] leading-6 text-muted-foreground">{item.sourceLabel}</p>
                        </div>
                        <Badge variant="secondary">Open</Badge>
                      </div>
                      <div className="mt-4 flex flex-wrap gap-2">
                        <MetaChip>{`created ${formatDateTimeLabel(item.request.createdAt)}`}</MetaChip>
                        <MetaChip>{`from ${formatDateTimeLabel(item.request.validFrom)}`}</MetaChip>
                        <MetaChip>{item.request.validUntil ? `until ${formatDateTimeLabel(item.request.validUntil)}` : 'no end date'}</MetaChip>
                      </div>
                    </OverviewListItem>
                  ))}
                </div>
              ) : null}
        </TabsContent>

        {canPlanContractors ? (
          <TabsContent value="contractor-jobs" className="mt-5">
            {activeContractorJobsQuery.isError ? <ErrorText message="Could not load active contractor jobs." /> : null}
            {activeContractorJobsQuery.isLoading ? <MutedText message="Loading active contractor jobs..." /> : null}
            {!activeContractorJobsQuery.isLoading && activeContractorJobs.length === 0 ? <EmptyText message="No active contractor jobs right now." /> : null}
            {!activeContractorJobsQuery.isLoading && activeContractorJobs.length > 0 ? (
              <div className="grid gap-3">
                {activeContractorJobs.map((job) => (
                  <OverviewListItem
                    key={job.id}
                    role="button"
                    tabIndex={0}
                    onClick={() => void navigate({ to: '/employee/contractors/jobs/$jobId', params: { jobId: job.id } })}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        void navigate({ to: '/employee/contractors/jobs/$jobId', params: { jobId: job.id } });
                      }
                    }}
                  >
                    <div className="flex flex-wrap items-start justify-between gap-3">
                      <div>
                        <p className="text-[15px] font-semibold text-foreground">{job.name}</p>
                        <p className="mt-1 text-[14px] leading-6 text-muted-foreground">{job.description || 'Contractor job currently in progress.'}</p>
                      </div>
                      <Badge variant="success">{job.status}</Badge>
                    </div>
                    <div className="mt-4 flex flex-wrap gap-2">
                      <MetaChip>{`from ${formatDateTimeLabel(job.plannedStart)}`}</MetaChip>
                      <MetaChip>{`until ${formatDateTimeLabel(job.plannedEnd)}`}</MetaChip>
                      <MetaChip>{`${job.assignmentCount} assignment${job.assignmentCount === 1 ? '' : 's'}`}</MetaChip>
                    </div>
                  </OverviewListItem>
                ))}
              </div>
            ) : null}
          </TabsContent>
        ) : null}

        {isHost ? (
          <TabsContent value="visitors" className="mt-5">
                <div className="mb-4 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <p className="text-[13px] text-muted-foreground">Today's invitations with quick day-by-day navigation.</p>
                  </div>
                  <div className="flex items-center gap-2 self-start">
                    <Button type="button" variant="outline" size="icon" aria-label="Previous day" onClick={() => setVisitorsDay(addDays(visitorsDay, -1))}>
                      <ChevronLeft className="size-4" aria-hidden="true" />
                    </Button>
                    <div className="min-w-36 text-center text-[13px] font-semibold text-foreground">{formatDayLabel(visitorsDay)}</div>
                    <Button type="button" variant="outline" size="icon" aria-label="Next day" onClick={() => setVisitorsDay(addDays(visitorsDay, 1))}>
                      <ChevronRight className="size-4" aria-hidden="true" />
                    </Button>
                  </div>
                </div>
                {visitorsQuery.isError ? <ErrorText message="Could not load expected visitors." /> : null}
                {visitorsQuery.isLoading ? <MutedText message="Loading expected visitors..." /> : null}
                {!visitorsQuery.isLoading && expectedVisitors.length === 0 ? <EmptyText message="No visitors expected for this day." /> : null}
                {!visitorsQuery.isLoading && expectedVisitors.length > 0 ? (
                  <div className="grid gap-3">
                    {expectedVisitors.map((item) => (
                      <OverviewListItem
                        key={item.invitation.id}
                        role="button"
                        tabIndex={0}
                        onClick={() => void navigate({ to: '/employee/visitors/$visitId/invitations/$invitationId', params: { visitId: item.visit.id ?? '', invitationId: item.invitation.id } })}
                        onKeyDown={(event) => {
                          if (event.key === 'Enter' || event.key === ' ') {
                            event.preventDefault();
                            void navigate({ to: '/employee/visitors/$visitId/invitations/$invitationId', params: { visitId: item.visit.id ?? '', invitationId: item.invitation.id } });
                          }
                        }}
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <p className="text-[15px] font-semibold text-foreground">{formatInvitationName(item.invitation)}</p>
                            <p className="mt-1 text-[14px] leading-6 text-muted-foreground">{item.visit.summary || 'Untitled visit'}</p>
                          </div>
                          <Badge variant={getConfirmationVariant(item.invitation.confirmationStatus)}>{formatConfirmationStatus(item.invitation.confirmationStatus)}</Badge>
                        </div>
                        <div className="mt-4 flex flex-wrap items-center gap-2">
                          <MetaChip>{formatVisitTime(item.visit.start, item.visit.stop)}</MetaChip>
                          <VisitStatusBadge status={item.visit.status} />
                          {item.invitation.email ? <MetaChip>{item.invitation.email}</MetaChip> : null}
                        </div>
                      </OverviewListItem>
                    ))}
                  </div>
                ) : null}
          </TabsContent>
        ) : null}

        {activeTab === 'requests' ? (
          <div className="fixed bottom-6 right-6 z-10">
            <Link to="/employee/request-access" className={buttonVariants({ className: 'h-12 rounded-fixed-action px-5 shadow-[0_18px_40px_rgba(29,66,104,0.22)]' })}>New Request</Link>
          </div>
        ) : null}

        {activeTab === 'contractor-jobs' && canPlanContractors ? (
          <div className="fixed bottom-6 right-6 z-10">
            <Link to="/employee/contractors" className={buttonVariants({ className: 'h-12 rounded-fixed-action px-5 shadow-[0_18px_40px_rgba(29,66,104,0.22)]' })}>Open Contractors Workspace</Link>
          </div>
        ) : null}

        {activeTab === 'visitors' && isHost ? (
          <div className="fixed bottom-6 right-6 z-10">
            <Link to="/employee/visitors" className={buttonVariants({ className: 'h-12 rounded-fixed-action px-5 shadow-[0_18px_40px_rgba(29,66,104,0.22)]' })}>Open Visitors</Link>
          </div>
        ) : null}
      </Tabs>
    </section>
  );
}

function OverviewStat({ label, value, detail }: { readonly label: string; readonly value: string; readonly detail: string }) {
  return (
    <div className="rounded-structural border border-border bg-content px-4 py-4 shadow-sm">
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{label}</p>
      <p className="mt-2 text-[32px] font-semibold tracking-tight text-primary">{value}</p>
      <p className="mt-1.5 text-[13px] leading-5 text-muted-foreground">{detail}</p>
    </div>
  );
}

function OverviewListItem({ className, ...props }: React.ComponentProps<'article'>) {
  return <article className={cn('rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)] transition hover:border-primary/20 hover:shadow-[0_14px_34px_rgba(17,24,39,0.1)]', className)} {...props} />;
}

function MetaChip({ children }: { readonly children: React.ReactNode }) {
  return <span className="inline-flex items-center rounded-[10px] border border-border bg-background px-3 py-1 text-[12px] font-medium text-muted-foreground">{children}</span>;
}

function ErrorText({ message }: { readonly message: string }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{message}</p>;
}

function MutedText({ message }: { readonly message: string }) {
  return <p className="text-[14px] text-muted-foreground">{message}</p>;
}

function EmptyText({ message }: { readonly message: string }) {
  return <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">{message}</p>;
}

function mergeEmployeeRequests(requesterRequests: readonly PackageRequestResponse[], beneficiaryRequests: readonly PackageRequestResponse[], identityId: string | null) {
  const rowsById = new Map<string, EmployeeRequestRow>();

  requesterRequests.forEach((request) => {
    rowsById.set(request.id, { request, sourceLabel: request.beneficiaryIdentityId === identityId ? 'Submitted by me / For me' : 'Submitted by me' });
  });

  beneficiaryRequests.forEach((request) => {
    const existing = rowsById.get(request.id);
    if (existing) {
      rowsById.set(request.id, { request, sourceLabel: 'Submitted by me / For me' });
      return;
    }

    rowsById.set(request.id, { request, sourceLabel: 'For me' });
  });

  return Array.from(rowsById.values()).sort((left, right) => new Date(right.request.createdAt).getTime() - new Date(left.request.createdAt).getTime());
}

function flattenExpectedVisitors(visits: readonly VisitResponse[]) {
  return visits
    .flatMap((visit) => (visit.invitations ?? []).map((invitation) => ({ invitation, visit })))
    .sort((left, right) => {
      const leftTime = left.visit.start ? new Date(left.visit.start).getTime() : 0;
      const rightTime = right.visit.start ? new Date(right.visit.start).getTime() : 0;
      if (leftTime !== rightTime) {
        return leftTime - rightTime;
      }

      return formatInvitationName(left.invitation).localeCompare(formatInvitationName(right.invitation));
    });
}

function getCredentialStatusVariant(status: CredentialResponse['status']) {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Suspended':
      return 'warning';
    case 'Issued':
      return 'secondary';
    default:
      return 'outline';
  }
}

function formatConfirmationStatus(status: VisitInvitationResponse['confirmationStatus']) {
  switch (status) {
    case 'Confirmed':
      return 'Confirmed';
    case 'Rejected':
      return 'Rejected';
    default:
      return 'Pending';
  }
}

function getConfirmationVariant(status: VisitInvitationResponse['confirmationStatus']) {
  switch (status) {
    case 'Confirmed':
      return 'success';
    case 'Rejected':
      return 'error';
    default:
      return 'secondary';
  }
}

function formatInvitationName(invitation: VisitInvitationResponse) {
  return `${invitation.firstName} ${invitation.lastName}`.trim();
}

function formatDateTimeLabel(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function formatVisitTime(start?: string | null, stop?: string | null) {
  if (!start || !stop) {
    return '-';
  }

  return `${new Intl.DateTimeFormat(undefined, { timeStyle: 'short' }).format(new Date(start))}-${new Intl.DateTimeFormat(undefined, { timeStyle: 'short' }).format(new Date(stop))}`;
}

function startOfDay(value: Date) {
  return new Date(value.getFullYear(), value.getMonth(), value.getDate());
}

function addDays(value: Date, days: number) {
  const next = new Date(value);
  next.setDate(next.getDate() + days);
  return startOfDay(next);
}

function getDayInterval(day: Date) {
  const start = startOfDay(day);
  const stop = addDays(start, 1);
  return { start, stop };
}

function formatDayLabel(value: Date) {
  return new Intl.DateTimeFormat(undefined, { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' }).format(value);
}
