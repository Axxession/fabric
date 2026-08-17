import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useLocation, useMatchRoute } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getGrantApprovalLabel, getGrantApprovalVariant, getGrantBusinessSummary, getGrantComplianceLabel, getGrantComplianceUntilLabel, getGrantComplianceVariant, getGrantStatusVariant } from '@/shared/access-grants/grant-status';
import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';

type PackageRequestDetailResponse = components['schemas']['PackageRequestDetailResponse'];
type PackageRequestDetailDecisionResponse = components['schemas']['PackageRequestDetailDecisionResponse'];
type PackageRequestDetailFlowResponse = components['schemas']['PackageRequestDetailFlowResponse'];
type PackageRequestDetailGrantResponse = components['schemas']['PackageRequestDetailGrantResponse'];
type PackageRequestDetailRequirementResponse = components['schemas']['PackageRequestDetailRequirementResponse'];
type PackageRequestResponse = components['schemas']['PackageRequestResponse'];

const requestDetailsQueryKey = ['employee', 'request-access', 'details'] as const;

export default function EmployeeRequestDetailPage() {
  const actorQuery = useCurrentActor();
  const queryClient = useQueryClient();
  const location = useLocation();
  const matchRoute = useMatchRoute();
  const employeeMatch = matchRoute({ to: '/employee/request-access/$requestId' });
  const managerMatch = matchRoute({ to: '/manager/approval-inbox/$requestId' });
  const securityMatch = matchRoute({ to: '/security-officer/identities/$identityId/requests/$requestId' });
  const requestId = (employeeMatch && 'requestId' in employeeMatch ? employeeMatch.requestId : undefined)
    ?? (managerMatch && 'requestId' in managerMatch ? managerMatch.requestId : undefined)
    ?? (securityMatch && 'requestId' in securityMatch ? securityMatch.requestId : undefined);
  const securityIdentityId = securityMatch && 'identityId' in securityMatch ? securityMatch.identityId : undefined;
  const isManagerContext = location.pathname.startsWith('/manager/approval-inbox');
  const isSecurityContext = location.pathname.startsWith('/security-officer/identities/');
  const backTo = isManagerContext ? '/manager/approval-inbox' : isSecurityContext && securityIdentityId ? `/security-officer/identities/${securityIdentityId}` : '/employee/request-access';
  const backLabel = isManagerContext ? 'Back to inbox' : isSecurityContext ? 'Back to Identity 360' : 'Back to requests';
  const approverIdentityId = actorQuery.data?.identityId ?? null;
  const approvableRequirementsQuery = useQuery({
    queryKey: ['manager', 'approval-inbox', 'approvable-requirements', approverIdentityId, requestId],
    enabled: isManagerContext && Boolean(approverIdentityId) && Boolean(requestId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/approval-inbox', {
        params: { query: { approverIdentityId: approverIdentityId ?? '', ids: [], Page: 0, PageSize: 100 } as never },
      });

      if (error || !data) {
        throw new Error('Could not load approvable requirements.');
      }

      return new Set((data.items ?? []).filter((item) => item.requestId === requestId).map((item) => item.approvalRequirementId));
    },
  });

  const detailQuery = useQuery({
    queryKey: [...requestDetailsQueryKey, requestId],
    enabled: Boolean(requestId),
    queryFn: async () => {
      if (!requestId) {
        throw new Error('Missing request id.');
      }

      const { data, error } = await api.GET('/api/access-catalog/package-requests/{requestId}/details', {
        params: { path: { requestId } },
      });

      if (error || !data) {
        throw new Error('Could not load request details.');
      }

      return data;
    },
  });

  const recordDecision = useMutation({
    mutationFn: async ({ approvalRequirementId, decisionKind }: { readonly approvalRequirementId: string; readonly decisionKind: 'Approve' | 'Reject' }) => {
      if (!approverIdentityId) {
        throw new Error('Missing approver identity.');
      }

      const { data, error } = await api.POST('/api/access-catalog/approval-requirements/{approvalRequirementId}/decisions', {
        params: { path: { approvalRequirementId } },
        body: {
          approverIdentityId,
          decisionKind,
          note: null,
        },
      });

      if (error || !data) {
        throw new Error('Could not record decision.');
      }

      return { decisionKind, data };
    },
    onSuccess: async ({ decisionKind }) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: requestDetailsQueryKey }),
        queryClient.invalidateQueries({ queryKey: ['manager', 'approval-inbox'] }),
        queryClient.invalidateQueries({ queryKey: ['employee', 'request-access', 'my-requests'] }),
      ]);

      toast.success(decisionKind === 'Approve' ? 'Approval recorded.' : 'Rejection recorded.');
    },
    onError: () => {
      toast.error('Could not record decision.');
    },
  });

  if (detailQuery.isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">Loading request details...</p>;
  }

  if (!detailQuery.data) {
    return <PanelError>Could not load request details.</PanelError>;
  }

  const detail = detailQuery.data;

  return (
    <section className="grid gap-6">
      <Link to={backTo} className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {backLabel}
      </Link>

      <SummaryCard detail={detail} />

      <Card>
        <CardHeader>
          <CardTitle>Requested Locations</CardTitle>
          <CardDescription>Original descendant locations included in this request.</CardDescription>
        </CardHeader>
        <CardContent>
          {detail.requestedLocations.length === 0 ? <p className="text-[14px] text-muted-foreground">No locations recorded.</p> : <LocationList locations={detail.requestedLocations.map((item) => item.label)} />}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Approval Progress</CardTitle>
          <CardDescription>Grouped by access item and normalized approval site.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-4">
          {detail.flows.length === 0 ? <p className="text-[14px] text-muted-foreground">No approval flows recorded.</p> : detail.flows.map((flow) => <FlowCard key={flow.approvalFlowId} flow={flow} isManagerContext={isManagerContext} approvableRequirementIds={approvableRequirementsQuery.data ?? null} pendingDecisionRequirementId={recordDecision.isPending ? recordDecision.variables?.approvalRequirementId ?? null : null} onDecide={(approvalRequirementId, decisionKind) => recordDecision.mutate({ approvalRequirementId, decisionKind })} />)}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Access Grants</CardTitle>
          <CardDescription>Grant records tied to this request, including approval and compliance state.</CardDescription>
        </CardHeader>
        <CardContent>
          {detail.grants.length === 0 ? <p className="text-[14px] text-muted-foreground">No granted access yet.</p> : <GrantList grants={detail.grants} />}
        </CardContent>
      </Card>
    </section>
  );
}

function SummaryCard({ detail }: { readonly detail: PackageRequestDetailResponse }) {
  const request = detail.request;

  return (
    <Card>
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>{detail.package.name}</CardTitle>
            <CardDescription>{detail.package.description ?? 'No package description.'}</CardDescription>
          </div>
          <Badge variant={getRequestStatusVariant(request)}>{formatRequestStatus(request)}</Badge>
        </div>
      </CardHeader>
      <CardContent className="grid gap-4">
        <div className="grid gap-3 md:grid-cols-3">
          <Info label="Created" value={formatDateTimeLabel(request.createdAt)} />
          <Info label="Valid from" value={formatDateTimeLabel(request.validFrom)} />
          <Info label="Valid until" value={request.validUntil ? formatDateTimeLabel(request.validUntil) : 'No end date'} />
          <Info label="Expires" value={formatDateTimeLabel(request.expiresAt)} />
          <Info label="Completed" value={request.decidedAt ? formatDateTimeLabel(request.decidedAt) : 'Not completed'} />
          <Info label="Requested locations" value={String(request.locationIds.length)} />
        </div>
        <div className="rounded-interactive border border-border p-4">
          <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Business justification</p>
          <p className="mt-3 whitespace-pre-wrap text-[14px] text-foreground">{request.requestReason}</p>
        </div>
      </CardContent>
    </Card>
  );
}

function FlowCard({ flow, isManagerContext, approvableRequirementIds, pendingDecisionRequirementId, onDecide }: { readonly flow: PackageRequestDetailFlowResponse; readonly isManagerContext: boolean; readonly approvableRequirementIds: ReadonlySet<string> | null; readonly pendingDecisionRequirementId: string | null; readonly onDecide: (approvalRequirementId: string, decisionKind: 'Approve' | 'Reject') => void; }) {
  return (
    <article className="rounded-structural border border-border p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h3 className="text-[16px] font-semibold text-foreground">{flow.accessItemName}</h3>
          <p className="mt-1 text-[14px] text-muted-foreground">Approval site: {flow.siteName}</p>
          {flow.accessItemDescription ? <p className="mt-2 text-[14px] text-muted-foreground">{flow.accessItemDescription}</p> : null}
        </div>
        <Badge variant={getFlowStatusVariant(flow.status)}>{formatFlowStatus(flow.status)}</Badge>
      </div>

      <div className="mt-4 grid gap-4 lg:grid-cols-3">
        <div className="rounded-interactive border border-border p-3">
          <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Requested locations</p>
          <div className="mt-3">
            <LocationList locations={flow.requestedLocations.map((item) => item.label)} />
          </div>
        </div>

        <div className="rounded-interactive border border-border p-3 lg:col-span-2">
          <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Requirements</p>
          <div className="mt-3 grid gap-2">
            {flow.requirements.length === 0 ? <p className="text-[14px] text-muted-foreground">No explicit requirements. Autoapproved at site level.</p> : flow.requirements.map((requirement) => <RequirementRow key={requirement.id} requirement={requirement} isManagerContext={isManagerContext} canDecide={approvableRequirementIds?.has(requirement.id) ?? false} isPendingDecision={pendingDecisionRequirementId === requirement.id} onDecide={onDecide} />)}
          </div>
        </div>
      </div>

      {flow.grants.length > 0 ? (
        <div className="mt-4 rounded-interactive border border-border p-3">
          <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Granted locations</p>
          <div className="mt-3 grid gap-2">
            {flow.grants.map((grant) => <GrantRow key={`${grant.id}-${grant.locationId}`} grant={grant} />)}
          </div>
        </div>
      ) : null}
    </article>
  );
}

function RequirementRow({ requirement, isManagerContext, canDecide, isPendingDecision, onDecide }: { readonly requirement: PackageRequestDetailRequirementResponse; readonly isManagerContext: boolean; readonly canDecide: boolean; readonly isPendingDecision: boolean; readonly onDecide: (approvalRequirementId: string, decisionKind: 'Approve' | 'Reject') => void; }) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-interactive border border-border bg-background p-3 text-[14px]">
      <div>
        <p className="font-medium text-foreground">{requirement.type === 'Destination' ? 'Destination approval' : `${requirement.role} approval`}</p>
        {getRequirementOwnerLabel(requirement) ? <p className="mt-1 text-[13px] text-muted-foreground">{getRequirementOwnerLabel(requirement)}</p> : null}
        {requirement.systemApprovalReason ? <p className="mt-1 text-[13px] text-muted-foreground">{requirement.systemApprovalReason}</p> : null}
      </div>
      <div className="flex items-center gap-2">
        {isManagerContext && canDecide && requirement.status === 'Pending' ? (
          <>
            <Button type="button" size="sm" variant="outline" disabled={isPendingDecision} onClick={() => onDecide(requirement.id, 'Reject')}>
              {isPendingDecision ? 'Saving...' : 'Reject'}
            </Button>
            <Button type="button" size="sm" disabled={isPendingDecision} onClick={() => onDecide(requirement.id, 'Approve')}>
              {isPendingDecision ? 'Saving...' : 'Approve'}
            </Button>
          </>
        ) : null}
        <Badge variant={getRequirementStatusVariant(requirement.status)}>{formatRequirementStatus(requirement.status)}</Badge>
      </div>
      {requirement.decisions.length > 0 ? <div className="mt-3 basis-full grid gap-2">{requirement.decisions.map((decision) => <p key={decision.id} className="text-[13px] text-muted-foreground">{formatDecision(decision)}</p>)}</div> : null}
    </div>
  );
}

function GrantList({ grants }: { readonly grants: PackageRequestDetailGrantResponse[] }) {
  return <div className="grid gap-2">{grants.map((grant) => <GrantRow key={`${grant.id}-${grant.locationId}`} grant={grant} />)}</div>;
}

function GrantRow({ grant }: { readonly grant: PackageRequestDetailGrantResponse }) {
  const complianceUntil = getGrantComplianceUntilLabel(grant);
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 rounded-interactive border border-border bg-background p-3 text-[14px]">
      <div>
        <p className="font-medium text-foreground">{grant.accessItemName}</p>
        <p className="mt-1 text-[13px] text-muted-foreground">{grant.locationLabel}</p>
        <p className="mt-1 text-[13px] text-muted-foreground">{getGrantBusinessSummary(grant)}</p>
        {complianceUntil ? <p className="mt-1 text-[13px] text-muted-foreground">Compliant until {formatDateTimeLabel(complianceUntil)}</p> : null}
      </div>
      <div className="flex flex-wrap items-center justify-end gap-2">
        <span className="text-[13px] text-muted-foreground">{formatDateTimeLabel(grant.validFrom)}</span>
        <Badge variant={getGrantStatusVariant(grant.status)}>{grant.status}</Badge>
        <Badge variant={getGrantApprovalVariant(grant.approvalStatus)}>{getGrantApprovalLabel(grant.approvalStatus)}</Badge>
        <Badge variant={getGrantComplianceVariant(grant.complianceStatus)}>{getGrantComplianceLabel(grant.complianceStatus)}</Badge>
      </div>
    </div>
  );
}

function LocationList({ locations }: { readonly locations: string[] }) {
  return <ul className="grid gap-2 text-[14px] text-foreground">{locations.map((location) => <li key={location} className="rounded-interactive border border-border bg-background px-3 py-2">{location}</li>)}</ul>;
}

function Info({ label, value }: { readonly label: string; readonly value: string }) {
  return <div className="rounded-interactive border border-border p-3"><div className="text-[12px] uppercase text-muted-foreground">{label}</div><div className="mt-1 text-[14px] font-medium text-foreground">{value}</div></div>;
}

function formatDateTimeLabel(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function formatRequestStatus(request: PackageRequestResponse) {
  if (request.status === 'InProgress') {
    return 'In Progress';
  }

  return request.subStatus === 'PartiallyApproved'
    ? 'Completed - Partially Approved'
    : request.subStatus === 'Approved'
      ? 'Completed - Approved'
      : request.subStatus === 'Rejected'
        ? 'Completed - Rejected'
        : request.subStatus === 'Expired'
          ? 'Completed - Expired'
          : 'Completed';
}

function getRequestStatusVariant(request: PackageRequestResponse) {
  if (request.status === 'InProgress') {
    return 'secondary';
  }

  switch (request.subStatus) {
    case 'Approved':
      return 'success';
    case 'Rejected':
    case 'Expired':
      return 'error';
    default:
      return 'secondary';
  }
}

function formatFlowStatus(status: PackageRequestDetailFlowResponse['status']) {
  switch (status) {
    case 'InProgress':
      return 'In Progress';
    case 'SystemApproved':
      return 'Approved';
    default:
      return status;
  }
}

function getFlowStatusVariant(status: PackageRequestDetailFlowResponse['status']) {
  switch (status) {
    case 'Approved':
    case 'SystemApproved':
      return 'success';
    case 'Rejected':
    case 'Expired':
      return 'error';
    default:
      return 'secondary';
  }
}

function formatRequirementStatus(status: PackageRequestDetailRequirementResponse['status']) {
  return status === 'SystemApproved' ? 'Approved' : status;
}

function getRequirementOwnerLabel(requirement: PackageRequestDetailRequirementResponse) {
  if (requirement.type === 'Destination') {
    return requirement.approvalGroupName ?? 'Unknown approval group';
  }

  return requirement.requiredApproverDisplayName ?? null;
}

function getRequirementStatusVariant(status: PackageRequestDetailRequirementResponse['status']) {
  switch (status) {
    case 'Approved':
    case 'SystemApproved':
      return 'success';
    case 'Rejected':
      return 'error';
    default:
      return 'secondary';
  }
}

function formatDecision(decision: PackageRequestDetailDecisionResponse) {
  const action = decision.decisionKind === 'Approve' ? 'Approved' : 'Rejected';
  return `${action} by ${decision.approverDisplayName} on ${formatDateTimeLabel(decision.decidedAt)}${decision.note ? ` • ${decision.note}` : ''}`;
}

function PanelError({ children }: { readonly children: React.ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
