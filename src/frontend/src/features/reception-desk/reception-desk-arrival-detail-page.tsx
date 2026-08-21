import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { getContextComplianceLabel, getContextComplianceVariant, getGrantApprovalLabel, getGrantApprovalVariant, getGrantBusinessSummary, getGrantComplianceLabel, getGrantComplianceUntilLabel, getGrantComplianceVariant, getGrantStatusVariant } from '@/shared/access-grants/grant-status';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { Textarea } from '@/shared/components/ui/textarea';

import { getReceptionDeskWorkstationHeaders, getReceptionDeskWorkstationSettings } from './reception-desk-workstation-settings';

type Arrival = components['schemas']['ArrivalResponse'];
type AccessGrantResponse = components['schemas']['AccessGrantResponse'];
type ContextAssignedPackageResponse = components['schemas']['ContextAssignedPackageResponse'];
type ContextComplianceResponse = components['schemas']['ContextComplianceResponse'];
type ContractorJobAssignmentResponse = components['schemas']['ContractorJobAssignmentResponse'];
type ContractorJobResponse = components['schemas']['ContractorJobResponse'];
type ContractorResponse = components['schemas']['ContractorResponse'];
type RequirementComplianceResponse = components['schemas']['RequirementComplianceResponse'];
type RequirementEvidenceKind = Exclude<components['schemas']['RequirementEvidenceKind'], null>;
type VisitResponse = components['schemas']['VisitResponse'];
type VisitInvitationResponse = components['schemas']['VisitInvitationResponse'];

export default function ReceptionDeskArrivalDetailPage() {
  const { t } = useTranslation();
  const { arrivalId } = useParams({ from: '/reception-desk-workstation/arrivals/$arrivalId' });
  const queryClient = useQueryClient();
  const currentActorQuery = useCurrentActor();
  const [activeTab, setActiveTab] = useState<'reason' | 'grants' | 'compliance'>('reason');
  const [waiverRequirement, setWaiverRequirement] = useState<RequirementComplianceResponse | null>(null);
  const [waiverReason, setWaiverReason] = useState('');
  const [waiverValidUntil, setWaiverValidUntil] = useState('');

  const arrivalQuery = useQuery({
    queryKey: ['reception-desk', 'arrival', arrivalId, 'detail-page'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/arrivals/{id}', {
        headers: getReceptionDeskWorkstationHeaders(),
        params: { path: { id: arrivalId } },
      });
      if (error || !data) {
        throw new Error('Could not load arrival.');
      }

      return data;
    },
  });

  const visitorContextQuery = useQuery({
    queryKey: ['reception-desk', 'arrival', arrivalId, 'visitor-context', arrivalQuery.data?.invitationId],
    enabled: arrivalQuery.data?.type === 'Visitor' && Boolean(arrivalQuery.data?.invitationId),
    queryFn: async () => {
      const invitationId = arrivalQuery.data?.invitationId ?? '';
      const { data: visit, error } = await api.GET('/api/visitors/invitations/{invitationId}/visit', { params: { path: { invitationId } } });
      if (error || !visit) {
        throw new Error('Could not load visit details.');
      }

      const invitation = visit.invitations.find((item) => item.id === invitationId) ?? null;
      if (!invitation) {
        throw new Error('Could not load invitation details.');
      }

      const [packagesResult, complianceResult] = await Promise.all([
        api.POST('/api/access-catalog/access-grants/assigned-packages/by-source', { body: [{ sourceKind: 'VisitInvitation', sourceId: invitationId }] }),
        api.GET('/api/requirements/context-compliance/visits/{visitId}/invitations/{invitationId}', { params: { path: { visitId: visit.id, invitationId } } }),
      ]);

      if (packagesResult.error || complianceResult.error || !complianceResult.data) {
        throw new Error('Could not load visit context.');
      }

      return {
        visit,
        invitation,
        packages: (packagesResult.data?.[0]?.packages ?? []) as ContextAssignedPackageResponse[],
        compliance: complianceResult.data,
      };
    },
  });

  const contractorContextQuery = useQuery({
    queryKey: ['reception-desk', 'arrival', arrivalId, 'contractor-context', arrivalQuery.data?.jobAssignmentId],
    enabled: arrivalQuery.data?.type === 'Contractor' && Boolean(arrivalQuery.data?.jobAssignmentId),
    queryFn: async () => {
      const assignmentId = arrivalQuery.data?.jobAssignmentId ?? '';
      const { data: assignment, error: assignmentError } = await api.GET('/api/contractors/assignments/{assignmentId}', {
        params: { path: { assignmentId } },
      });
      if (assignmentError || !assignment) {
        throw new Error('Could not load contractor assignment.');
      }

      const [jobResult, contractorResult, packagesResult, complianceResult] = await Promise.all([
        api.GET('/api/contractors/jobs/{id}', { params: { path: { id: assignment.contractorJobId } } }),
        api.GET('/api/contractors/contractors/{id}', { params: { path: { id: assignment.contractorId } } }),
        api.POST('/api/access-catalog/access-grants/assigned-packages/by-source', { body: [{ sourceKind: 'ContractorAssignment', sourceId: assignmentId }] }),
        api.GET('/api/requirements/context-compliance/contractor-assignments/{assignmentId}', { params: { path: { assignmentId } } }),
      ]);

      if (jobResult.error || contractorResult.error || !jobResult.data || !contractorResult.data || packagesResult.error || complianceResult.error || !complianceResult.data) {
        throw new Error('Could not load contractor context.');
      }

      return {
        assignment,
        job: jobResult.data,
        contractor: contractorResult.data,
        packages: (packagesResult.data?.[0]?.packages ?? []) as ContextAssignedPackageResponse[],
        compliance: complianceResult.data,
      };
    },
  });

  const detail = arrivalQuery.data;
  const context = detail?.type === 'Visitor' ? visitorContextQuery.data : contractorContextQuery.data;
  const compliance = context?.compliance ?? null;
  const packages = context?.packages ?? [];

  const waiverMutation = useMutation({
    mutationFn: async () => {
      if (!detail || !waiverRequirement || !waiverValidUntil) {
        throw new Error('Waiver details missing.');
      }

      const actorLabel = currentActorQuery.data?.displayName ?? currentActorQuery.data?.email ?? 'Guard';
      const workstationId = getReceptionDeskWorkstationSettings()?.workstationId ?? 'unknown-workstation';
      const body = {
        requirementDefinitionId: waiverRequirement.requirementDefinitionId,
        validUntil: new Date(waiverValidUntil).toISOString(),
        reason: waiverReason,
        sourceReference: `${actorLabel} @ ${workstationId}`,
      };

      const result = detail.type === 'Visitor' && visitorContextQuery.data
        ? await api.POST('/api/requirements/context-compliance/visits/{visitId}/invitations/{invitationId}/waivers', { params: { path: { visitId: visitorContextQuery.data.visit.id, invitationId: visitorContextQuery.data.invitation.id } }, body })
        : await api.POST('/api/requirements/context-compliance/contractor-assignments/{assignmentId}/waivers', { params: { path: { assignmentId: contractorContextQuery.data?.assignment.id ?? '' } }, body });

      if (result.error) {
        throw new Error('Could not create waiver.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['reception-desk', 'arrival', arrivalId] });
      await queryClient.invalidateQueries({ queryKey: ['reception-desk', 'arrival', arrivalId, 'visitor-context'] });
      await queryClient.invalidateQueries({ queryKey: ['reception-desk', 'arrival', arrivalId, 'contractor-context'] });
      setWaiverRequirement(null);
      setWaiverReason('');
      setWaiverValidUntil('');
      toast.success('Waiver created.');
    },
    onError: () => toast.error('Could not create waiver.'),
  });

  const title = useMemo(() => {
    if (!detail) {
      return 'Loading...';
    }

    return [detail.firstName, detail.lastName].filter(Boolean).join(' ');
  }, [detail]);

  function openWaiver(requirement: RequirementComplianceResponse) {
    if (!detail) {
      return;
    }

    setWaiverRequirement(requirement);
    setWaiverReason('');
    setWaiverValidUntil(toLocalDateTimeValue(detail.expectedOffboardTime));
  }

  if (arrivalQuery.isLoading) {
    return <p className="text-[14px] text-muted-foreground">Loading arrival details...</p>;
  }

  if (arrivalQuery.isError || !detail) {
    return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load arrival details.</p>;
  }

  return (
    <div className="grid gap-6">
      <Link to="/reception-desk-workstation/expected-arrivals" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        Back to expected arrivals
      </Link>

      <header className="rounded-structural border border-border bg-content p-5 sm:p-6 md:p-8">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="text-[24px] font-semibold tracking-tight">{title}</h1>
            <p className="mt-2 text-[14px] text-muted-foreground">{detail.company ?? '-'} · {detail.type}</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Badge variant="secondary">{detail.type}</Badge>
            <Badge variant="outline">{detail.status}</Badge>
          </div>
        </div>
        <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          <Info label="Expected arrival" value={formatDateTime(detail.expectedArrivalTime)} />
          <Info label="Expected leave" value={formatDateTime(detail.expectedOffboardTime)} />
          <Info label="Company" value={detail.company ?? '-'} />
          <Info label="Identity" value={detail.identityId ?? '-'} />
        </div>
      </header>

      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as 'reason' | 'grants' | 'compliance')}>
        <TabsList>
          <TabsTrigger value="reason">Reason Of Visit</TabsTrigger>
          <TabsTrigger value="grants">Grants <span className="text-[12px] font-medium text-muted-foreground">{packages.reduce((total, item) => total + item.grants.length, 0)}</span></TabsTrigger>
          <TabsTrigger value="compliance">Compliance <span className="text-[12px] font-medium text-muted-foreground">{compliance?.requirements.length ?? 0}</span></TabsTrigger>
        </TabsList>

        <TabsContent value="reason">
          <Card className="p-5 sm:p-6">
            {detail.type === 'Visitor' && visitorContextQuery.data ? <VisitorReason visit={visitorContextQuery.data.visit} invitation={visitorContextQuery.data.invitation} /> : null}
            {detail.type === 'Contractor' && contractorContextQuery.data ? <ContractorReason assignment={contractorContextQuery.data.assignment} job={contractorContextQuery.data.job} contractor={contractorContextQuery.data.contractor} /> : null}
            {((detail.type === 'Visitor' && visitorContextQuery.isLoading) || (detail.type === 'Contractor' && contractorContextQuery.isLoading)) ? <p className="text-[14px] text-muted-foreground">Loading context...</p> : null}
          </Card>
        </TabsContent>

        <TabsContent value="grants">
          <Card className="p-5 sm:p-6">
            {packages.length === 0 ? <p className="text-[14px] text-muted-foreground">No grants assigned for this context.</p> : <div className="grid gap-4">{packages.map((item) => <AssignedPackageCard key={item.packageId} item={item} />)}</div>}
          </Card>
        </TabsContent>

        <TabsContent value="compliance">
          <Card className="p-5 sm:p-6">
            {!compliance ? <p className="text-[14px] text-muted-foreground">No compliance preview available.</p> : (
              <div className="grid gap-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <h2 className="text-[18px] font-semibold tracking-tight">Context compliance</h2>
                    <p className="mt-1 text-[14px] text-muted-foreground">Live requirement evaluation for this arrival context.</p>
                  </div>
                  <Badge variant={getContextComplianceVariant(compliance.status)}>{getContextComplianceLabel(compliance.status)}</Badge>
                </div>
                {compliance.compliantUntil ? <p className="text-[13px] text-muted-foreground">Compliant until {formatDateTime(compliance.compliantUntil)}</p> : null}
                {compliance.unavailableReason ? <p className="rounded-interactive border border-border bg-background px-4 py-3 text-[14px] text-muted-foreground">{compliance.unavailableReason}</p> : null}
                {compliance.requirements.length === 0 ? <p className="text-[14px] text-muted-foreground">No compliance requirements found.</p> : compliance.requirements.map((requirement) => <ComplianceRequirementCard key={requirement.requirementDefinitionId} requirement={requirement} onWaive={requirement.allowedEvidenceKinds.includes('RequirementWaiver' as RequirementEvidenceKind) ? () => openWaiver(requirement) : undefined} />)}
              </div>
            )}
          </Card>
        </TabsContent>
      </Tabs>

      {waiverRequirement ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/35 p-4">
          <div className="w-full max-w-lg rounded-structural border border-border bg-content p-6 shadow-sm">
            <h2 className="text-[18px] font-semibold tracking-tight">Create requirement waiver</h2>
            <p className="mt-1 text-[14px] text-muted-foreground">{waiverRequirement.name}</p>
            <div className="mt-5 grid gap-4">
              <label className="grid gap-2 text-[14px] font-medium">
                <span>Reason</span>
                <Textarea value={waiverReason} onChange={(event) => setWaiverReason(event.target.value)} />
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                <span>Valid until</span>
                <Input type="datetime-local" value={waiverValidUntil} onChange={(event) => setWaiverValidUntil(event.target.value)} />
              </label>
            </div>
            <div className="mt-6 flex justify-end gap-2">
              <Button type="button" variant="outline" onClick={() => setWaiverRequirement(null)}>Cancel</Button>
              <Button type="button" disabled={waiverMutation.isPending || !waiverReason.trim() || !waiverValidUntil} onClick={() => waiverMutation.mutate()}>{waiverMutation.isPending ? 'Saving...' : 'Create waiver'}</Button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function VisitorReason({ visit, invitation }: { readonly visit: VisitResponse; readonly invitation: VisitInvitationResponse }) {
  return (
    <div className="grid gap-4">
      <div>
        <h2 className="text-[18px] font-semibold tracking-tight">Visit details</h2>
        <p className="mt-1 text-[14px] text-muted-foreground">{visit.summary || '-'}</p>
      </div>
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <Info label="Host" value={[visit.host.firstName, visit.host.lastName].filter(Boolean).join(' ') || visit.host.email || '-'} />
        <Info label="Visit starts" value={visit.start ? formatDateTime(visit.start) : '-'} />
        <Info label="Visit ends" value={visit.stop ? formatDateTime(visit.stop) : '-'} />
        <Info label="Invitation" value={invitation.confirmationStatus} />
        <Info label="Email" value={invitation.email} />
        <Info label="Transport" value={invitation.transport ?? '-'} />
        <Info label="License plate" value={invitation.licensePlate ?? '-'} />
        <Info label="Confirmed at" value={invitation.confirmedAt ? formatDateTime(invitation.confirmedAt) : '-'} />
      </div>
    </div>
  );
}

function ContractorReason({ assignment, job, contractor }: { readonly assignment: ContractorJobAssignmentResponse; readonly job: ContractorJobResponse; readonly contractor: ContractorResponse }) {
  return (
    <div className="grid gap-4">
      <div>
        <h2 className="text-[18px] font-semibold tracking-tight">Job details</h2>
        <p className="mt-1 text-[14px] text-muted-foreground">{job.name}</p>
      </div>
      <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <Info label="Contractor" value={`${contractor.firstName} ${contractor.lastName}`.trim() || contractor.id} />
        <Info label="Email" value={contractor.email ?? '-'} />
        <Info label="Job window" value={`${formatDateTime(job.plannedStart)} - ${formatDateTime(job.plannedEnd)}`} />
        <Info label="Assignment window" value={`${formatDateTime(assignment.assignedFrom)} - ${formatDateTime(assignment.assignedUntil)}`} />
      </div>
    </div>
  );
}

function AssignedPackageCard({ item }: { readonly item: ContextAssignedPackageResponse }) {
  return (
    <article className="rounded-structural border border-border bg-background p-4">
      <h3 className="font-semibold text-foreground">{item.packageName}</h3>
      <div className="mt-4 grid gap-3">
        {item.grants.map((grant) => <GrantCard key={grant.grantId} grant={grant} />)}
      </div>
    </article>
  );
}

function GrantCard({ grant }: { readonly grant: ContextAssignedPackageResponse['grants'][number] }) {
  return (
    <div className="rounded-interactive border border-border bg-content p-3">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="font-medium text-foreground">{grant.accessItemName}</p>
          <p className="mt-1 text-[13px] text-muted-foreground">{formatDateTime(grant.validFrom)} - {grant.validUntil ? formatDateTime(grant.validUntil) : 'No end date'}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Badge variant={getGrantStatusVariant(grant.status)}>{grant.status}</Badge>
          <Badge variant={getGrantApprovalVariant(grant.approvalStatus)}>{getGrantApprovalLabel(grant.approvalStatus)}</Badge>
          <Badge variant={getGrantComplianceVariant(grant.complianceStatus)}>{getGrantComplianceLabel(grant.complianceStatus)}</Badge>
          <Badge variant={grant.provisioningStatus === 'Provisioned' ? 'success' : 'secondary'}>{grant.provisioningStatus === 'Provisioned' ? 'Provisioned' : grant.provisioningStatus}</Badge>
        </div>
      </div>
      <p className="mt-3 text-[13px] text-muted-foreground">{getGrantBusinessSummary(grant as unknown as AccessGrantResponse)}</p>
      {grant.compliantUntil ? <p className="mt-1 text-[13px] text-muted-foreground">Compliant until {formatDateTime(grant.compliantUntil)}</p> : null}
    </div>
  );
}

function ComplianceRequirementCard({ requirement, onWaive }: { readonly requirement: RequirementComplianceResponse; readonly onWaive?: () => void }) {
  return (
    <div className="rounded-structural border border-border bg-background p-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <p className="font-medium text-foreground">{requirement.name}</p>
            {requirement.isBlocking ? <Badge variant="secondary">Blocking</Badge> : <Badge variant="outline">Non-blocking</Badge>}
            <Badge variant={getRequirementComplianceVariant(requirement.status)}>{formatRequirementComplianceStatus(requirement.status)}</Badge>
          </div>
          <p className="mt-1 text-[14px] text-muted-foreground">{requirement.code}</p>
        </div>
        {onWaive ? <Button type="button" variant="outline" onClick={onWaive}>Add waiver</Button> : null}
      </div>
      <p className="mt-3 text-[14px] text-muted-foreground">{requirement.reason}</p>
      {requirement.validUntil ? <p className="mt-2 text-[13px] text-muted-foreground">Valid until {formatDateTime(requirement.validUntil)}</p> : null}
    </div>
  );
}

function formatRequirementComplianceStatus(status: RequirementComplianceResponse['status']) {
  switch (status) {
    case 'Fulfilled':
      return 'Compliant';
    case 'Missing':
      return 'Missing';
    case 'Failed':
      return 'Failed';
    case 'Expired':
      return 'Expired';
    default:
      return status;
  }
}

function getRequirementComplianceVariant(status: RequirementComplianceResponse['status']): 'success' | 'secondary' | 'error' {
  switch (status) {
    case 'Fulfilled':
      return 'success';
    case 'Expired':
      return 'secondary';
    default:
      return 'error';
  }
}

function Info({ label, value }: { readonly label: string; readonly value: string }) {
  return <div className="rounded-interactive border border-border p-3"><div className="text-[12px] uppercase text-muted-foreground">{label}</div><div className="mt-1 break-all text-[14px] font-medium text-foreground">{value}</div></div>;
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function toLocalDateTimeValue(value: string) {
  const date = new Date(value);
  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60_000);
  return local.toISOString().slice(0, 16);
}
