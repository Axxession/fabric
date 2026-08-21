import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, CalendarDays, Mail, MapPin, QrCode, UserRound } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { getContextComplianceLabel, getContextComplianceVariant, getGrantApprovalLabel, getGrantApprovalVariant, getGrantBusinessSummary, getGrantComplianceLabel, getGrantComplianceUntilLabel, getGrantComplianceVariant, getGrantStatusVariant } from '@/shared/access-grants/grant-status';
import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Card } from '@/shared/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

type AccessGrantResponse = components['schemas']['AccessGrantResponse'];
type ContextAssignedPackageResponse = components['schemas']['ContextAssignedPackageResponse'];
type CredentialPACSAssignmentResponse = components['schemas']['CredentialPACSAssignmentResponse'];
type CredentialResponse = components['schemas']['CredentialResponse'];
type CredentialTypeResponse = components['schemas']['CredentialTypeResponse'];
type RequirementComplianceResponse = components['schemas']['RequirementComplianceResponse'];
type VisitorPreOnboardingSaga = components['schemas']['VisitorPreOnboardingSaga'];
type VisitInvitationResponse = components['schemas']['VisitInvitationResponse'];
type VisitResponse = components['schemas']['VisitResponse'];
type VisitorResponse = components['schemas']['VisitorResponse'];

export default function VisitInvitationDetailPage() {
  const { t } = useTranslation();
  const { visitId, invitationId } = useParams({ from: '/main/employee/visitors/$visitId/invitations/$invitationId' });
  const [activeTab, setActiveTab] = useState<'overview' | 'access' | 'requirements' | 'credential'>('overview');

  const detailsQuery = useQuery({
    queryKey: ['visitors-management', 'visits', visitId, 'invitations', invitationId, 'details'],
    queryFn: async () => {
      const [visitResult, sagasResult] = await Promise.all([
        api.GET('/api/visitors/visits/{id}', { params: { path: { id: visitId } } }),
        api.GET('/api/sagas/visitor-pre-onboarding/{visitId}', { params: { path: { visitId } } }),
      ]);

      if (visitResult.error || !visitResult.data || sagasResult.error) {
        throw new Error(t('visitorsManagement.invitationDetail.couldNotLoad'));
      }

      const visit = visitResult.data;
      const invitation = visit.invitations.find((item) => item.id === invitationId);
      if (!invitation) {
        throw new Error(t('visitorsManagement.invitationDetail.couldNotLoad'));
      }

      const saga = (sagasResult.data ?? []).find((item) => item.invitationId === invitationId) ?? null;

      const [visitorResult, locationResult, credentialResult] = await Promise.all([
        api.GET('/api/visitors/visitors/{id}', { params: { path: { id: invitation.visitorId } } }),
        visit.locationId ? api.GET('/api/locations/locations/{id}', { params: { path: { id: visit.locationId } } }) : Promise.resolve({ data: null, error: undefined }),
        saga?.credentialId ? api.GET('/api/credential-management/credentials/{id}', { params: { path: { id: saga.credentialId } } }) : Promise.resolve({ data: null, error: undefined }),
      ]);

      if (visitorResult.error || !visitorResult.data) {
        throw new Error(t('visitorsManagement.invitationDetail.couldNotLoad'));
      }

      const visitor = visitorResult.data;
      const credential = credentialResult.data ?? null;

      const [packagesResult, credentialTypeResult, credentialAssignmentsResult, complianceResult] = await Promise.all([
        saga?.arrivalId
          ? api.POST('/api/access-catalog/access-grants/assigned-packages/by-source', { body: [{ sourceKind: 'VisitInvitation', sourceId: invitationId }] })
          : Promise.resolve({ data: [], error: undefined }),
        credential?.credentialTypeId
          ? api.GET('/api/credential-management/credential-types/{id}', { params: { path: { id: credential.credentialTypeId } } })
          : Promise.resolve({ data: null, error: undefined }),
        credential?.id
          ? api.GET('/api/access-control/credential-pacs-assignments', { params: { query: { CredentialId: credential.id, CredentialIds: [], AccessControlSystemId: undefined, Status: undefined, Page: 0, PageSize: 200 } as never } })
          : Promise.resolve({ data: { items: [] }, error: undefined }),
        api.GET('/api/requirements/context-compliance/visits/{visitId}/invitations/{invitationId}', { params: { path: { visitId, invitationId } } }),
      ]);

      if (packagesResult.error || credentialTypeResult.error || credentialAssignmentsResult.error || !visitorResult.data || !visitResult.data) {
        throw new Error(t('visitorsManagement.invitationDetail.couldNotLoad'));
      }

      if (complianceResult.error || !complianceResult.data) {
        throw new Error(t('visitorsManagement.invitationDetail.couldNotLoad'));
      }

      const assignedPackages = (packagesResult.data?.[0]?.packages ?? []) as ContextAssignedPackageResponse[];

      return {
        visit,
        invitation,
        visitor,
        saga,
        locationLabel: locationResult.data ? getLocationLabel(locationResult.data) : t('visitorsManagement.invitationDetail.notSpecified'),
        confirmationLink: `/visitor-confirmation/${visitId}/${invitationId}`,
        assignedPackages,
        credential,
        credentialType: credentialTypeResult.data ?? null,
        credentialAssignments: credentialAssignmentsResult.data?.items ?? [],
        compliance: complianceResult.data,
      };
    },
  });

  if (detailsQuery.isLoading) {
    return <p className="text-[14px] text-muted-foreground">{t('visitorsManagement.invitationDetail.loading')}</p>;
  }

  if (detailsQuery.isError || !detailsQuery.data) {
    return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{t('visitorsManagement.invitationDetail.couldNotLoad')}</p>;
  }

  const { visit, invitation, visitor, saga, locationLabel, confirmationLink, assignedPackages, credential, credentialType, credentialAssignments, compliance } = detailsQuery.data;
  const credentialProvisioningStatus = getCredentialProvisioningStatus(credential, credentialAssignments, t);
  const visitTimeLabel = `${formatDateTime(visit.start ?? '')} - ${formatDateTime(visit.stop ?? '')}`;
  const hostLabel = [visit.host.firstName, visit.host.lastName].filter(Boolean).join(' ') || visit.host.email || '-';

  return (
    <div className="grid gap-6">
      <button type="button" onClick={() => window.history.back()} className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        {t('visitorsManagement.invitationDetail.back')}
      </button>

      <header className="rounded-structural border border-border bg-content p-5 sm:p-6 md:p-8">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="text-[24px] font-semibold tracking-tight">{formatInvitationName(invitation, t)}</h1>
            <p className="mt-2 text-[14px] text-muted-foreground">{t('visitorsManagement.invitationDetail.summary', { name: formatInvitationName(invitation, t), visit: visit.summary })}</p>
          </div>
          <Badge variant={getConfirmationVariant(invitation.confirmationStatus)}>{formatConfirmationStatus(invitation.confirmationStatus, t)}</Badge>
        </div>
        <div className="mt-4 flex flex-wrap gap-2">
          <SummaryChip icon={<CalendarDays className="size-3.5" />} label={visitTimeLabel} />
          <SummaryChip icon={<MapPin className="size-3.5" />} label={locationLabel} />
          <SummaryChip icon={<UserRound className="size-3.5" />} label={hostLabel} />
          {(visitor.company ?? invitation.company) ? <SummaryChip icon={<Mail className="size-3.5" />} label={visitor.company ?? invitation.company ?? '-'} /> : null}
          {(visitor.licensePlate ?? invitation.licensePlate) ? <SummaryChip icon={<QrCode className="size-3.5" />} label={visitor.licensePlate ?? invitation.licensePlate ?? '-'} /> : null}
        </div>
      </header>

      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as 'overview' | 'access' | 'requirements' | 'credential')}>
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="access">Access <span className="text-[12px] font-medium text-muted-foreground">{assignedPackages.length}</span></TabsTrigger>
          <TabsTrigger value="requirements">Requirements <span className="text-[12px] font-medium text-muted-foreground">{compliance.requirements.length}</span></TabsTrigger>
          <TabsTrigger value="credential">Credential {credential ? <span className="text-[12px] font-medium text-muted-foreground">1</span> : null}</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="mt-5">
          <div className="grid gap-6 xl:grid-cols-3">
            <Card className="p-5 sm:p-6">
              <h2 className="text-[18px] font-semibold tracking-tight">{t('visitorsManagement.invitationDetail.visitDetails')}</h2>
              <dl className="mt-5 grid gap-4 text-[14px]">
                <Detail icon={<CalendarDays className="size-4" />} label={t('visitorsManagement.invitationDetail.starts')} value={formatDateTime(visit.start ?? '')} />
                <Detail icon={<CalendarDays className="size-4" />} label={t('visitorsManagement.invitationDetail.ends')} value={formatDateTime(visit.stop ?? '')} />
                <Detail icon={<MapPin className="size-4" />} label={t('visitorsManagement.invitationDetail.location')} value={locationLabel} />
                <Detail icon={<UserRound className="size-4" />} label={t('visitorsManagement.invitationDetail.host')} value={hostLabel} hint={visit.host.email ?? undefined} />
              </dl>
            </Card>

            <Card className="p-5 sm:p-6">
              <h2 className="text-[18px] font-semibold tracking-tight">{t('visitorsManagement.invitationDetail.visitorDetails')}</h2>
              <dl className="mt-5 grid gap-4 text-[14px]">
                <Detail icon={<Mail className="size-4" />} label={t('visitorsManagement.invitationDetail.name')} value={formatInvitationName(invitation, t)} hint={invitation.email} />
                <Detail icon={<Mail className="size-4" />} label={t('visitorsManagement.invitationDetail.company')} value={visitor.company ?? invitation.company ?? '-'} />
                <Detail icon={<Mail className="size-4" />} label={t('visitorsManagement.invitationDetail.licensePlate')} value={visitor.licensePlate ?? invitation.licensePlate ?? '-'} />
              </dl>
            </Card>

            <Card className="p-5 sm:p-6">
              <h2 className="text-[18px] font-semibold tracking-tight">{t('visitorsManagement.invitationDetail.confirmation')}</h2>
              <div className="mt-5 flex flex-wrap items-center gap-3">
                <Badge variant={getConfirmationVariant(invitation.confirmationStatus)}>{formatConfirmationStatus(invitation.confirmationStatus, t)}</Badge>
                <Link to={confirmationLink} className="inline-flex text-[14px] font-medium text-primary underline-offset-4 hover:underline">{t('visitorsManagement.invitationDetail.openConfirmationPage')}</Link>
              </div>
              <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-1">
                <Info label={t('visitorsManagement.invitationDetail.confirmedAt')} value={invitation.confirmedAt ? formatDateTime(invitation.confirmedAt) : '-'} />
                <Info label={t('visitorsManagement.invitationDetail.rejectedAt')} value={invitation.rejectedAt ? formatDateTime(invitation.rejectedAt) : '-'} />
                {invitation.confirmationStatus === 'Confirmed' ? <Info label={t('visitorsManagement.invitationDetail.transport')} value={invitation.transport ?? '-'} /> : null}
                {invitation.confirmationStatus === 'Confirmed' ? <Info label={t('visitorsManagement.invitationDetail.arrivedAt')} value={invitation.arrivedAt ? formatDateTime(invitation.arrivedAt) : '-'} /> : null}
              </div>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="access" className="mt-5">
          <Card className="p-5 sm:p-6">
            <h2 className="text-[18px] font-semibold tracking-tight">{t('visitorsManagement.invitationDetail.assignedPackages')}</h2>
            {assignedPackages.length === 0 ? <p className="mt-5 text-[14px] text-muted-foreground">{t('visitorsManagement.invitationDetail.noAssignedPackages')}</p> : (
              <div className="mt-5 grid gap-4">
                {assignedPackages.map((item) => (
                  <article key={item.packageId} className="rounded-structural border border-border bg-background p-4">
                    <h3 className="font-semibold text-foreground">{item.packageName}</h3>
                    <div className="mt-4 grid gap-3">
                      {item.grants.map((grant) => (
                        <div key={grant.grantId} className="rounded-interactive border border-border bg-content p-3">
                          <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                            <div>
                              <p className="font-medium text-foreground">{grant.accessItemName}</p>
                              <p className="mt-1 text-[13px] text-muted-foreground">{formatValidityRange(grant.validFrom, grant.validUntil)}</p>
                            </div>
                            <div className="flex flex-wrap gap-2">
                              <Badge variant={getGrantStatusVariant(grant.status)}>{grant.status}</Badge>
                              <Badge variant={getGrantApprovalVariant(grant.approvalStatus)}>{getGrantApprovalLabel(grant.approvalStatus)}</Badge>
                              <Badge variant={getGrantComplianceVariant(grant.complianceStatus)}>{getGrantComplianceLabel(grant.complianceStatus)}</Badge>
                              <Badge variant={grant.provisioningStatus === 'Provisioned' ? 'success' : 'secondary'}>{grant.provisioningStatus === 'Provisioned' ? t('visitorsManagement.invitationDetail.provisionedLabel') : grant.provisioningStatus}</Badge>
                            </div>
                          </div>
                          <p className="mt-3 text-[13px] text-muted-foreground">{getGrantBusinessSummary(grant as unknown as AccessGrantResponse)}</p>
                          {grant.compliantUntil ? <p className="mt-1 text-[13px] text-muted-foreground">{t('visitorsManagement.invitationDetail.compliantUntil')} {formatDateTime(grant.compliantUntil)}</p> : null}
                          {grant.revokeCause ? <p className="mt-2 text-[13px] text-muted-foreground">{formatAccessGrantRevokeCause(grant.revokeCause, t)}</p> : null}
                          {grant.revokedBy ? <p className="mt-1 text-[13px] text-muted-foreground">{grant.revokedBy}</p> : null}
                        </div>
                      ))}
                    </div>
                  </article>
                ))}
              </div>
            )}
          </Card>
        </TabsContent>

        <TabsContent value="requirements" className="mt-5">
          <Card className="p-5 sm:p-6">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <h2 className="text-[18px] font-semibold tracking-tight">{t('visitorsManagement.invitationDetail.contextCompliance')}</h2>
                <p className="mt-1 text-[14px] text-muted-foreground">{t('visitorsManagement.invitationDetail.contextComplianceDescription')}</p>
              </div>
              <Badge variant={getContextComplianceVariant(compliance.status)}>{getContextComplianceLabel(compliance.status)}</Badge>
            </div>

            {compliance.compliantUntil ? <p className="mt-4 text-[13px] text-muted-foreground">{t('visitorsManagement.invitationDetail.compliantUntil')} {formatDateTime(compliance.compliantUntil)}</p> : null}
            {compliance.unavailableReason ? <p className="mt-4 rounded-interactive border border-border bg-background px-4 py-3 text-[14px] text-muted-foreground">{compliance.unavailableReason}</p> : null}
            {compliance.requirements.length === 0 ? <p className="mt-5 text-[14px] text-muted-foreground">{t('visitorsManagement.invitationDetail.noRequirements')}</p> : (
              <div className="mt-5 grid gap-3">
                {compliance.requirements.map((requirement) => <RequirementCard key={requirement.requirementDefinitionId} requirement={requirement} />)}
              </div>
            )}
          </Card>
        </TabsContent>

        <TabsContent value="credential" className="mt-5">
          <Card className="p-5 sm:p-6">
            <h2 className="text-[18px] font-semibold tracking-tight">{t('visitorsManagement.invitationDetail.assignedCredential')}</h2>
            {!credential ? <p className="mt-5 text-[14px] text-muted-foreground">{t('visitorsManagement.invitationDetail.noCredential')}</p> : (
              <div className="mt-5 grid gap-6 lg:grid-cols-[18rem_minmax(0,1fr)]">
                <div className="rounded-structural border border-border bg-background p-4">
                  <img src={`/api/credential-management/credentials/${credential.id}/qr?size=220`} alt={t('visitorsManagement.invitationDetail.qrAlt', { name: formatInvitationName(invitation, t) })} className="mx-auto size-full max-w-[220px] rounded-structural border border-border bg-white p-3" />
                </div>
                <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                  <Info label={t('visitorsManagement.invitationDetail.credentialType')} value={credentialType?.name ?? credential.credentialTypeId} />
                  <Info label={t('visitorsManagement.invitationDetail.identifier')} value={credential.identifier} />
                  <Info label={t('visitorsManagement.invitationDetail.credentialStatus')} value={credential.status} />
                  <Info label={t('visitorsManagement.invitationDetail.provisioning')} value={credentialProvisioningStatus} />
                  <Info label={t('visitorsManagement.invitationDetail.validFrom')} value={formatDateTime(credential.validFrom)} />
                  <Info label={t('visitorsManagement.invitationDetail.validUntil')} value={credential.validUntil ? formatDateTime(credential.validUntil) : t('visitorsManagement.invitationDetail.noEndDate')} />
                </div>
              </div>
            )}
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

function SummaryChip({ icon, label }: { readonly icon: React.ReactNode; readonly label: string }) {
  return <span className="inline-flex items-center gap-1.5 rounded-[10px] border border-border bg-background px-3 py-1 text-[12px] font-medium text-muted-foreground">{icon}{label}</span>;
}

function formatInvitationName(invitation: VisitInvitationResponse, t: ReturnType<typeof useTranslation>['t']) {
  return [invitation.firstName, invitation.lastName].filter(Boolean).join(' ') || invitation.email || t('visitorsManagement.invitationDetail.unnamed');
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function formatValidityRange(validFrom: string, validUntil: string | null) {
  return validUntil ? `${formatDateTime(validFrom)} - ${formatDateTime(validUntil)}` : formatDateTime(validFrom);
}

function formatAccessGrantRevokeCause(cause: AccessGrantResponse['revokeCause'], t: ReturnType<typeof useTranslation>['t']) {
  switch (cause) {
    case 'Manual':
      return t('visitorsManagement.invitationDetail.manuallyRevoked');
    case 'VisitRescheduled':
      return t('visitorsManagement.invitationDetail.visitRescheduled');
    case 'ArrivalRelocated':
      return t('visitorsManagement.invitationDetail.arrivalRelocated');
    case 'VisitCancelled':
      return t('visitorsManagement.invitationDetail.visitCancelled');
    case 'VisitOffboarded':
      return t('visitorsManagement.invitationDetail.visitOffboarded');
    case 'EmployeeLifecycleAutomation':
      return t('visitorsManagement.invitationDetail.employeeLifecycleAutomation');
    default:
      return '-';
  }
}

function formatConfirmationStatus(status: VisitInvitationResponse['confirmationStatus'], t: ReturnType<typeof useTranslation>['t']) {
  switch (status) {
    case 'Confirmed':
      return t('visitorsManagement.invitationDetail.confirmed');
    case 'Rejected':
      return t('visitorsManagement.invitationDetail.rejected');
    default:
      return t('visitorsManagement.invitationDetail.pending');
  }
}

function getConfirmationVariant(status: VisitInvitationResponse['confirmationStatus']): 'success' | 'error' | 'secondary' {
  switch (status) {
    case 'Confirmed':
      return 'success';
    case 'Rejected':
      return 'error';
    default:
      return 'secondary';
  }
}

function getCredentialProvisioningStatus(credential: CredentialResponse | null, assignments: readonly CredentialPACSAssignmentResponse[], t: ReturnType<typeof useTranslation>['t']) {
  if (!credential) {
    return t('visitorsManagement.invitationDetail.notYet');
  }

  if (assignments.some((assignment) => assignment.status === 'Provisioned')) {
    return t('visitorsManagement.invitationDetail.provisionedLabel');
  }

  return t('visitorsManagement.invitationDetail.generated');
}

function Detail({ icon, label, value, hint }: { readonly icon: React.ReactNode; readonly label: string; readonly value: string; readonly hint?: string }) {
  return (
    <div className="flex gap-3">
      <div className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-full bg-hover-blue text-primary">{icon}</div>
      <div>
        <dt className="text-[12px] font-medium text-muted-foreground">{label}</dt>
        <dd className="mt-0.5 text-foreground">{value}</dd>
        {hint ? <dd className="text-[13px] text-muted-foreground">{hint}</dd> : null}
      </div>
    </div>
  );
}

function Info({ label, value }: { readonly label: string; readonly value: string }) {
  return <div className="rounded-interactive border border-border p-3"><div className="text-[12px] uppercase text-muted-foreground">{label}</div><div className="mt-1 break-all text-[14px] font-medium text-foreground">{value}</div></div>;
}

function RequirementCard({ requirement }: { readonly requirement: RequirementComplianceResponse }) {
  return (
    <div className="rounded-structural border border-border bg-background p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="font-medium text-foreground">{requirement.name}</p>
          <p className="mt-1 text-[14px] text-muted-foreground">{requirement.code}{requirement.isBlocking ? ' - blocking' : ' - non-blocking'}</p>
        </div>
        <Badge variant={getRequirementComplianceVariant(requirement.status)}>{formatRequirementComplianceStatus(requirement.status)}</Badge>
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

function getLocationLabel(location: { type: components['schemas']['LocationType']; site: { name: string }; building?: { name: string } | null; room?: { name: string } | null }) {
  switch (location.type) {
    case 'Site':
      return location.site.name;
    case 'Building':
      return [location.site.name, location.building?.name].filter(Boolean).join(' / ');
    case 'Room':
      return [location.site.name, location.building?.name, location.room?.name].filter(Boolean).join(' / ');
  }
}
