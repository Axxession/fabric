import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { ChevronLeft, ChevronRight, ExternalLink } from 'lucide-react';
import { useState } from 'react';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { VisitStatusBadge } from '@/shared/components/visit-status-badge';
import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { getPerspectiveById, type PerspectiveId } from '@/shared/perspectives/app-perspectives';

type CredentialResponse = components['schemas']['CredentialResponse'];
type CredentialTypeResponse = components['schemas']['CredentialTypeResponse'];
type AccessGrantResponse = components['schemas']['AccessGrantResponse'];
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
        <p className="text-[14px] font-semibold uppercase text-primary">Perspective</p>
        <h1 className="mt-3 text-[30px] font-semibold tracking-tight">{perspective.label}</h1>
        <p className="mt-3 max-w-2xl text-[14px] leading-6 text-muted-foreground">{perspective.description}</p>
      </div>

      <div className="rounded-structural border border-dashed border-border bg-content p-6 text-[14px] text-muted-foreground">
        No pages moved into this perspective yet.
      </div>
    </section>
  );
}

function ManagerOverviewPage() {
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
        <p className="text-[14px] font-semibold uppercase text-primary">Perspective</p>
        <h1 className="mt-3 text-[30px] font-semibold tracking-tight">Manager Overview</h1>
        <p className="mt-3 max-w-2xl text-[14px] leading-6 text-muted-foreground">
          {actorQuery.data?.displayName ? `Signed in as ${actorQuery.data.displayName}. Review your team and act on approvals.` : 'Review your team and act on approvals.'}
        </p>
        <div className="mt-6 flex flex-wrap gap-3">
          <Link to="/manager/my-team" className={buttonVariants()}>Open My Team</Link>
          <Link to="/manager/approval-inbox" className={buttonVariants({ variant: 'outline' })}>Open Approval Inbox</Link>
        </div>
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>My Team</CardTitle>
            <CardDescription>Direct reports by default, with optional indirect reporting line view.</CardDescription>
          </CardHeader>
          <CardContent>
            {directReportsQuery.isError || totalReportsQuery.isError ? <ErrorText message="Could not load team summary." /> : null}
            {directReportsQuery.isLoading || totalReportsQuery.isLoading ? <MutedText message="Loading team summary..." /> : null}
            {!directReportsQuery.isLoading && !totalReportsQuery.isLoading ? (
              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-structural border border-border p-4">
                  <p className="text-[12px] uppercase tracking-[0.18em] text-muted-foreground">Direct reports</p>
                  <p className="mt-3 text-[28px] font-semibold tracking-tight text-foreground">{directReportsQuery.data?.length ?? 0}</p>
                </div>
                <div className="rounded-structural border border-border p-4">
                  <p className="text-[12px] uppercase tracking-[0.18em] text-muted-foreground">Direct + indirect</p>
                  <p className="mt-3 text-[28px] font-semibold tracking-tight text-foreground">{totalReportsQuery.data?.length ?? 0}</p>
                </div>
              </div>
            ) : null}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Approval Inbox</CardTitle>
            <CardDescription>Open pending approval work from the manager perspective.</CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-[14px] text-muted-foreground">Review requests waiting for your decision, then open the full request for context.</p>
            <Link to="/manager/approval-inbox" className={`${buttonVariants({ variant: 'outline' })} mt-4`}>
              Review Approvals
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

  const identityId = actorQuery.data?.identityId ?? null;
  const isHost = actorQuery.data?.isHost ?? false;
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

  const credentials = credentialsQuery.data ?? [];
  const credentialTypesById = credentialTypesQuery.data ?? new Map<string, CredentialTypeResponse>();
  const activeGrants = (grantsQuery.data ?? []).filter((item: AccessGrantResponse) => item.status === 'Active');
  const assignedPackages = Array.from(new Map(activeGrants.map((item: AccessGrantResponse) => [item.packageId, item])).values())
    .sort((left, right) => {
      const leftName = packagesQuery.data?.get(left.packageId)?.name ?? left.packageId;
      const rightName = packagesQuery.data?.get(right.packageId)?.name ?? right.packageId;
      return leftName.localeCompare(rightName);
    });
  const requests = mergeEmployeeRequests(requesterRequestsQuery.data ?? [], beneficiaryRequestsQuery.data ?? [], identityId);
  const expectedVisitors = flattenExpectedVisitors(visitorsQuery.data ?? []);
  const hasActorContext = Boolean(identityId);

  return (
    <section className="grid gap-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-[28px] font-semibold tracking-tight">Employee Overview</h1>
          <p className="mt-2 max-w-3xl text-[14px] text-muted-foreground">
            {actorQuery.data?.displayName ? `Signed in as ${actorQuery.data.displayName}.` : 'Your identity, access, requests, and visitors at a glance.'}
          </p>
        </div>
        <Link to="/employee/request-access" className={buttonVariants()}>
          Request Access
        </Link>
      </div>

      {!actorQuery.isLoading && !hasActorContext ? (
        <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
          Could not load current employee identity.
        </p>
      ) : null}

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Assigned Credentials</CardTitle>
            <CardDescription>Current credentials linked to your identity.</CardDescription>
          </CardHeader>
          <CardContent>
            {credentialsQuery.isError || credentialTypesQuery.isError ? <ErrorText message="Could not load assigned credentials." /> : null}
            {credentialsQuery.isLoading || credentialTypesQuery.isLoading ? <MutedText message="Loading assigned credentials..." /> : null}
            {!credentialsQuery.isLoading && !credentialTypesQuery.isLoading && credentials.length === 0 ? <EmptyText message="No credentials assigned yet." /> : null}
            {!credentialsQuery.isLoading && !credentialTypesQuery.isLoading && credentials.length > 0 ? (
              <div className="grid gap-3">
                {credentials.map((credential) => {
                  const credentialType = credentialTypesById.get(credential.credentialTypeId);

                  return (
                    <div key={credential.id} className="rounded-structural border border-border p-4">
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <p className="font-medium text-foreground">{credentialType?.name ?? credential.formattedIdentifier}</p>
                          <p className="mt-1 text-[13px] text-muted-foreground">{credential.formattedIdentifier}</p>
                        </div>
                        <Badge variant={getCredentialStatusVariant(credential.status)}>{credential.status}</Badge>
                      </div>
                      <dl className="mt-4 grid gap-2 text-[13px] text-muted-foreground sm:grid-cols-2">
                        <div className="flex items-center justify-between gap-3 sm:block">
                          <dt>Purpose</dt>
                          <dd className="text-right text-foreground sm:mt-1 sm:text-left">{credential.purpose}</dd>
                        </div>
                        <div className="flex items-center justify-between gap-3 sm:block">
                          <dt>Valid until</dt>
                          <dd className="text-right text-foreground sm:mt-1 sm:text-left">{credential.validUntil ? formatDateTimeLabel(credential.validUntil) : 'No end date'}</dd>
                        </div>
                      </dl>
                    </div>
                  );
                })}
              </div>
            ) : null}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Assigned Packages</CardTitle>
            <CardDescription>Current access packages from active grants.</CardDescription>
          </CardHeader>
          <CardContent>
            {grantsQuery.isError || packagesQuery.isError ? <ErrorText message="Could not load assigned packages." /> : null}
            {grantsQuery.isLoading || packagesQuery.isLoading ? <MutedText message="Loading assigned packages..." /> : null}
            {!grantsQuery.isLoading && !packagesQuery.isLoading && assignedPackages.length === 0 ? <EmptyText message="No active packages assigned." /> : null}
            {!grantsQuery.isLoading && !packagesQuery.isLoading && assignedPackages.length > 0 ? (
              <div className="grid gap-3">
                {assignedPackages.map((grant) => {
                  const pkg = packagesQuery.data?.get(grant.packageId);

                  return (
                    <div key={grant.id} className="rounded-structural border border-border p-4">
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <p className="font-medium text-foreground">{pkg?.name ?? grant.packageId}</p>
                          <p className="mt-1 text-[13px] text-muted-foreground">{pkg?.description ?? 'Current package assignment.'}</p>
                        </div>
                        <Badge variant="success">Active</Badge>
                      </div>
                      <dl className="mt-4 grid gap-2 text-[13px] text-muted-foreground sm:grid-cols-2">
                        <div className="flex items-center justify-between gap-3 sm:block">
                          <dt>Valid from</dt>
                          <dd className="text-right text-foreground sm:mt-1 sm:text-left">{formatDateTimeLabel(grant.validFrom)}</dd>
                        </div>
                        <div className="flex items-center justify-between gap-3 sm:block">
                          <dt>Valid until</dt>
                          <dd className="text-right text-foreground sm:mt-1 sm:text-left">{grant.validUntil ? formatDateTimeLabel(grant.validUntil) : 'No end date'}</dd>
                        </div>
                      </dl>
                    </div>
                  );
                })}
              </div>
            ) : null}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Requests In Progress</CardTitle>
          <CardDescription>Requests you submitted or requests created for you.</CardDescription>
        </CardHeader>
        <CardContent>
          {requesterRequestsQuery.isError || beneficiaryRequestsQuery.isError || packagesQuery.isError ? <ErrorText message="Could not load requests in progress." /> : null}
          {requesterRequestsQuery.isLoading || beneficiaryRequestsQuery.isLoading || packagesQuery.isLoading ? <MutedText message="Loading requests in progress..." /> : null}
          {!requesterRequestsQuery.isLoading && !beneficiaryRequestsQuery.isLoading && !packagesQuery.isLoading && requests.length === 0 ? <EmptyText message="No requests in progress." /> : null}
          {!requesterRequestsQuery.isLoading && !beneficiaryRequestsQuery.isLoading && !packagesQuery.isLoading && requests.length > 0 ? (
            <>
              <div className="hidden overflow-x-auto md:block">
                <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
                  <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                    <tr>
                      <th className="px-4 py-3 font-semibold">Package</th>
                      <th className="px-4 py-3 font-semibold">Source</th>
                      <th className="px-4 py-3 font-semibold">Created</th>
                      <th className="px-4 py-3 font-semibold">Valid from</th>
                      <th className="px-4 py-3 font-semibold">Valid until</th>
                      <th className="px-4 py-3 text-right font-semibold">Open</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {requests.map((item) => (
                      <tr
                        key={item.request.id}
                        className="cursor-pointer transition hover:bg-hover-blue"
                        role="link"
                        tabIndex={0}
                        onClick={() => void navigate({ to: '/employee/request-access/$requestId', params: { requestId: item.request.id } })}
                        onKeyDown={(event) => {
                          if (event.key === 'Enter' || event.key === ' ') {
                            event.preventDefault();
                            void navigate({ to: '/employee/request-access/$requestId', params: { requestId: item.request.id } });
                          }
                        }}
                      >
                        <td className="px-4 py-4 font-medium text-foreground">{packagesQuery.data?.get(item.request.packageId)?.name ?? item.request.packageId}</td>
                        <td className="px-4 py-4"><Badge variant="secondary">{item.sourceLabel}</Badge></td>
                        <td className="px-4 py-4 text-muted-foreground">{formatDateTimeLabel(item.request.createdAt)}</td>
                        <td className="px-4 py-4 text-muted-foreground">{formatDateTimeLabel(item.request.validFrom)}</td>
                        <td className="px-4 py-4 text-muted-foreground">{item.request.validUntil ? formatDateTimeLabel(item.request.validUntil) : 'No end date'}</td>
                        <td className="px-4 py-4 text-right text-muted-foreground"><ExternalLink className="ml-auto size-4" aria-hidden="true" /></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="grid gap-3 md:hidden">
                {requests.map((item) => (
                  <article
                    key={item.request.id}
                    className="rounded-structural border border-border p-4 transition hover:bg-hover-blue"
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
                        <p className="font-medium text-foreground">{packagesQuery.data?.get(item.request.packageId)?.name ?? item.request.packageId}</p>
                        <p className="mt-1 text-[13px] text-muted-foreground">Created {formatDateTimeLabel(item.request.createdAt)}</p>
                      </div>
                      <Badge variant="secondary">{item.sourceLabel}</Badge>
                    </div>
                    <dl className="mt-4 grid gap-2 text-[14px]">
                      <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Valid from</dt><dd className="text-right text-foreground">{formatDateTimeLabel(item.request.validFrom)}</dd></div>
                      <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Valid until</dt><dd className="text-right text-foreground">{item.request.validUntil ? formatDateTimeLabel(item.request.validUntil) : 'No end date'}</dd></div>
                    </dl>
                  </article>
                ))}
              </div>
            </>
          ) : null}
        </CardContent>
      </Card>

      {isHost ? (
        <Card>
          <CardHeader>
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <CardTitle>Expected Visitors</CardTitle>
                <CardDescription>Today's invitations with quick day-by-day navigation.</CardDescription>
              </div>
              <div className="flex items-center gap-2 self-start">
                <Button type="button" variant="outline" size="icon" aria-label="Previous day" onClick={() => setVisitorsDay(addDays(visitorsDay, -1))}>
                  <ChevronLeft className="size-4" aria-hidden="true" />
                </Button>
                <div className="min-w-36 text-center text-[13px] font-medium text-foreground">{formatDayLabel(visitorsDay)}</div>
                <Button type="button" variant="outline" size="icon" aria-label="Next day" onClick={() => setVisitorsDay(addDays(visitorsDay, 1))}>
                  <ChevronRight className="size-4" aria-hidden="true" />
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {visitorsQuery.isError ? <ErrorText message="Could not load expected visitors." /> : null}
            {visitorsQuery.isLoading ? <MutedText message="Loading expected visitors..." /> : null}
            {!visitorsQuery.isLoading && expectedVisitors.length === 0 ? <EmptyText message="No visitors expected for this day." /> : null}
            {!visitorsQuery.isLoading && expectedVisitors.length > 0 ? (
              <>
                <div className="hidden overflow-x-auto md:block">
                  <table className="w-full min-w-[64rem] border-collapse text-left text-[14px]">
                    <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                      <tr>
                        <th className="px-4 py-3 font-semibold">Visitor</th>
                        <th className="px-4 py-3 font-semibold">Visit</th>
                        <th className="px-4 py-3 font-semibold">Visit Status</th>
                        <th className="px-4 py-3 font-semibold">Time</th>
                        <th className="px-4 py-3 font-semibold">Confirmation</th>
                        <th className="px-4 py-3 text-right font-semibold">Open</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {expectedVisitors.map((item) => (
                        <tr
                          key={item.invitation.id}
                          className="cursor-pointer transition hover:bg-hover-blue"
                          role="link"
                          tabIndex={0}
                          onClick={() => void navigate({ to: '/employee/visitors/$visitId/invitations/$invitationId', params: { visitId: item.visit.id ?? '', invitationId: item.invitation.id } })}
                          onKeyDown={(event) => {
                            if (event.key === 'Enter' || event.key === ' ') {
                              event.preventDefault();
                              void navigate({ to: '/employee/visitors/$visitId/invitations/$invitationId', params: { visitId: item.visit.id ?? '', invitationId: item.invitation.id } });
                            }
                          }}
                        >
                          <td className="px-4 py-4">
                            <div>
                              <p className="font-medium text-foreground">{formatInvitationName(item.invitation)}</p>
                              <p className="mt-1 text-[13px] text-muted-foreground">{item.invitation.email}</p>
                            </div>
                          </td>
                          <td className="px-4 py-4 text-foreground">{item.visit.summary || 'Untitled visit'}</td>
                          <td className="px-4 py-4"><VisitStatusBadge status={item.visit.status} /></td>
                          <td className="px-4 py-4 text-muted-foreground">{formatVisitTime(item.visit.start, item.visit.stop)}</td>
                          <td className="px-4 py-4"><Badge variant={getConfirmationVariant(item.invitation.confirmationStatus)}>{formatConfirmationStatus(item.invitation.confirmationStatus)}</Badge></td>
                          <td className="px-4 py-4 text-right text-muted-foreground"><ExternalLink className="ml-auto size-4" aria-hidden="true" /></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="grid gap-3 md:hidden">
                  {expectedVisitors.map((item) => (
                    <article
                      key={item.invitation.id}
                      className="rounded-structural border border-border p-4 transition hover:bg-hover-blue"
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
                          <p className="font-medium text-foreground">{formatInvitationName(item.invitation)}</p>
                          <p className="mt-1 text-[13px] text-muted-foreground">{item.visit.summary || 'Untitled visit'}</p>
                        </div>
                        <Badge variant={getConfirmationVariant(item.invitation.confirmationStatus)}>{formatConfirmationStatus(item.invitation.confirmationStatus)}</Badge>
                      </div>
                      <dl className="mt-4 grid gap-2 text-[14px]">
                        <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Time</dt><dd className="text-right text-foreground">{formatVisitTime(item.visit.start, item.visit.stop)}</dd></div>
                        <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Visit status</dt><dd><VisitStatusBadge status={item.visit.status} /></dd></div>
                      </dl>
                    </article>
                  ))}
                </div>
              </>
            ) : null}
          </CardContent>
        </Card>
      ) : null}
    </section>
  );
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
