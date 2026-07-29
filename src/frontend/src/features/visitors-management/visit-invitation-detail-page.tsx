import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, CalendarDays, Mail, MapPin, QrCode, UserRound } from 'lucide-react';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

type AccessGrantResponse = components['schemas']['AccessGrantResponse'];
type CredentialPACSAssignmentResponse = components['schemas']['CredentialPACSAssignmentResponse'];
type CredentialResponse = components['schemas']['CredentialResponse'];
type CredentialTypeResponse = components['schemas']['CredentialTypeResponse'];
type VisitorPreOnboardingSaga = components['schemas']['VisitorPreOnboardingSaga'];
type VisitInvitationResponse = components['schemas']['VisitInvitationResponse'];
type VisitResponse = components['schemas']['VisitResponse'];
type VisitorResponse = components['schemas']['VisitorResponse'];

export default function VisitInvitationDetailPage() {
  const { visitId, invitationId } = useParams({ from: '/main/employee/visitors/$visitId/invitations/$invitationId' });

  const detailsQuery = useQuery({
    queryKey: ['visitors-management', 'visits', visitId, 'invitations', invitationId, 'details'],
    queryFn: async () => {
      const [visitResult, sagasResult] = await Promise.all([
        api.GET('/api/visitors/visits/{id}', { params: { path: { id: visitId } } }),
        api.GET('/api/sagas/visitor-pre-onboarding/{visitId}', { params: { path: { visitId } } }),
      ]);

      if (visitResult.error || !visitResult.data || sagasResult.error) {
        throw new Error('Could not load invitation details.');
      }

      const visit = visitResult.data;
      const invitation = visit.invitations.find((item) => item.id === invitationId);
      if (!invitation) {
        throw new Error('Could not load invitation details.');
      }

      const saga = (sagasResult.data ?? []).find((item) => item.invitationId === invitationId) ?? null;

      const [visitorResult, locationResult, credentialResult] = await Promise.all([
        api.GET('/api/visitors/visitors/{id}', { params: { path: { id: invitation.visitorId } } }),
        visit.locationId ? api.GET('/api/locations/locations/{id}', { params: { path: { id: visit.locationId } } }) : Promise.resolve({ data: null, error: undefined }),
        saga?.credentialId ? api.GET('/api/credential-management/credentials/{id}', { params: { path: { id: saga.credentialId } } }) : Promise.resolve({ data: null, error: undefined }),
      ]);

      if (visitorResult.error || !visitorResult.data) {
        throw new Error('Could not load invitation details.');
      }

      const visitor = visitorResult.data;
      const credential = credentialResult.data ?? null;

      const [grantsResult, packageResults, credentialTypeResult, credentialAssignmentsResult] = await Promise.all([
        saga?.arrivalId
          ? api.GET('/api/access-catalog/access-grants', { params: { query: { IdentityId: visitor.identityId, PackageId: undefined, Status: undefined, Page: 0, PageSize: 200 } as never } })
          : Promise.resolve({ data: { items: [] }, error: undefined }),
        saga?.arrivalId
          ? api.GET('/api/access-catalog/packages', { params: { query: { Name: undefined, Page: 0, PageSize: 200 } as never } })
          : Promise.resolve({ data: { items: [] }, error: undefined }),
        credential?.credentialTypeId
          ? api.GET('/api/credential-management/credential-types/{id}', { params: { path: { id: credential.credentialTypeId } } })
          : Promise.resolve({ data: null, error: undefined }),
        credential?.id
          ? api.GET('/api/access-control/credential-pacs-assignments', { params: { query: { CredentialId: credential.id, CredentialIds: [], AccessControlSystemId: undefined, Status: undefined, Page: 0, PageSize: 200 } as never } })
          : Promise.resolve({ data: { items: [] }, error: undefined }),
      ]);

      if (grantsResult.error || packageResults.error || credentialTypeResult.error || credentialAssignmentsResult.error) {
        throw new Error('Could not load invitation details.');
      }

      const packageById = new Map((packageResults.data?.items ?? []).map((item) => [item.id, item]));
      const assignedPackages = (grantsResult.data?.items ?? [])
        .filter((grant: AccessGrantResponse) => grant.sourceKind === 'ReceptionArrival' && grant.sourceId === saga?.arrivalId)
        .map((grant: AccessGrantResponse) => ({
          grant,
          packageName: packageById.get(grant.packageId)?.name ?? grant.packageId,
          isProvisioned: grant.materializationOutcomes.length > 0 && grant.materializationOutcomes.every((outcome) => outcome.status === 'Created'),
        }));

      return {
        visit,
        invitation,
        visitor,
        saga,
        locationLabel: locationResult.data ? getLocationLabel(locationResult.data) : 'Not specified',
        confirmationLink: `/visitor-confirmation/${visitId}/${invitationId}`,
        assignedPackages,
        credential,
        credentialType: credentialTypeResult.data ?? null,
        credentialAssignments: credentialAssignmentsResult.data?.items ?? [],
      };
    },
  });

  if (detailsQuery.isLoading) {
    return <p className="text-[14px] text-muted-foreground">Loading invitation details...</p>;
  }

  if (detailsQuery.isError || !detailsQuery.data) {
    return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load invitation details.</p>;
  }

  const { visit, invitation, visitor, saga, locationLabel, confirmationLink, assignedPackages, credential, credentialType, credentialAssignments } = detailsQuery.data;
  const credentialProvisioningStatus = getCredentialProvisioningStatus(credential, credentialAssignments);

  return (
    <div className="grid gap-6">
      <Link to="/employee/visitors/$visitId/edit" params={{ visitId }} className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        Back to visit
      </Link>

      <header className="rounded-structural border border-border bg-content p-5 sm:p-6">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-[24px] font-semibold tracking-tight">Invitation details</h1>
          <Badge variant={getConfirmationVariant(invitation.confirmationStatus)}>{formatConfirmationStatus(invitation.confirmationStatus)}</Badge>
        </div>
        <p className="mt-2 text-[14px] text-muted-foreground">{formatInvitationName(invitation)} for {visit.summary}</p>
      </header>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card className="p-5 sm:p-6">
          <h2 className="text-[18px] font-semibold tracking-tight">Visit Details</h2>
          <dl className="mt-5 grid gap-4 text-[14px]">
            <Detail icon={<CalendarDays className="size-4" />} label="Starts" value={formatDateTime(visit.start ?? '')} />
            <Detail icon={<CalendarDays className="size-4" />} label="Ends" value={formatDateTime(visit.stop ?? '')} />
            <Detail icon={<MapPin className="size-4" />} label="Location" value={locationLabel} />
            <Detail icon={<UserRound className="size-4" />} label="Host" value={[visit.host.firstName, visit.host.lastName].filter(Boolean).join(' ') || visit.host.email || '-'} hint={visit.host.email ?? undefined} />
          </dl>
        </Card>

        <Card className="p-5 sm:p-6">
          <h2 className="text-[18px] font-semibold tracking-tight">Visitor Details</h2>
          <dl className="mt-5 grid gap-4 text-[14px]">
            <Detail icon={<Mail className="size-4" />} label="Name" value={formatInvitationName(invitation)} hint={invitation.email} />
            <Detail icon={<Mail className="size-4" />} label="Company" value={visitor.company ?? invitation.company ?? '-'} />
            <Detail icon={<Mail className="size-4" />} label="License plate" value={visitor.licensePlate ?? invitation.licensePlate ?? '-'} />
          </dl>
        </Card>

        <Card className="p-5 sm:p-6">
          <h2 className="text-[18px] font-semibold tracking-tight">Confirmation</h2>
          <div className="mt-5 flex flex-wrap items-center gap-3">
            <Badge variant={getConfirmationVariant(invitation.confirmationStatus)}>{formatConfirmationStatus(invitation.confirmationStatus)}</Badge>
            <Link to={confirmationLink} className="inline-flex text-[14px] font-medium text-primary underline-offset-4 hover:underline">Open confirmation page</Link>
          </div>
          <div className="mt-5 grid gap-3 md:grid-cols-2">
            <Info label="Confirmed at" value={invitation.confirmedAt ? formatDateTime(invitation.confirmedAt) : '-'} />
            <Info label="Rejected at" value={invitation.rejectedAt ? formatDateTime(invitation.rejectedAt) : '-'} />
            {invitation.confirmationStatus === 'Confirmed' ? <Info label="Transport" value={invitation.transport ?? '-'} /> : null}
            {invitation.confirmationStatus === 'Confirmed' ? <Info label="Arrived at" value={invitation.arrivedAt ? formatDateTime(invitation.arrivedAt) : '-'} /> : null}
          </div>
        </Card>

        <Card className="p-5 sm:p-6">
          <h2 className="text-[18px] font-semibold tracking-tight">Assigned Packages</h2>
          {assignedPackages.length === 0 ? <p className="mt-5 text-[14px] text-muted-foreground">No access packages assigned for this invitation yet.</p> : (
            <div className="mt-5 overflow-x-auto">
              <table className="w-full min-w-[32rem] border-collapse text-left text-[14px]">
                <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-semibold">Package</th>
                    <th className="px-4 py-3 font-semibold">Status</th>
                    <th className="px-4 py-3 font-semibold">Provisioned</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {assignedPackages.map((item) => (
                    <tr key={item.grant.id}>
                      <td className="px-4 py-4 font-medium text-foreground">{item.packageName}</td>
                      <td className="px-4 py-4"><Badge variant={getAccessGrantVariant(item.grant.status)}>{item.grant.status}</Badge></td>
                      <td className="px-4 py-4"><Badge variant={item.isProvisioned ? 'success' : 'secondary'}>{item.isProvisioned ? 'Provisioned' : 'Not yet'}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>

        <Card className="p-5 sm:p-6 xl:col-span-2">
          <h2 className="text-[18px] font-semibold tracking-tight">Assigned Credential</h2>
          {!credential ? <p className="mt-5 text-[14px] text-muted-foreground">No credential assigned yet.</p> : (
            <div className="mt-5 grid gap-6 lg:grid-cols-[18rem_minmax(0,1fr)]">
              <div className="rounded-structural border border-border bg-background p-4">
                <img src={`/api/credential-management/credentials/${credential.id}/qr?size=220`} alt={`QR code for ${formatInvitationName(invitation)}`} className="mx-auto size-full max-w-[220px] rounded-structural border border-border bg-white p-3" />
              </div>
              <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                <Info label="Credential type" value={credentialType?.name ?? credential.credentialTypeId} />
                <Info label="Identifier" value={credential.formattedIdentifier} />
                <Info label="Credential status" value={credential.status} />
                <Info label="Provisioning" value={credentialProvisioningStatus} />
                <Info label="Valid from" value={formatDateTime(credential.validFrom)} />
                <Info label="Valid until" value={credential.validUntil ? formatDateTime(credential.validUntil) : 'No end date'} />
              </div>
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}

function formatInvitationName(invitation: VisitInvitationResponse) {
  return [invitation.firstName, invitation.lastName].filter(Boolean).join(' ') || invitation.email || 'Unnamed';
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
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

function getCredentialProvisioningStatus(credential: CredentialResponse | null, assignments: readonly CredentialPACSAssignmentResponse[]) {
  if (!credential) {
    return 'Not yet';
  }

  if (assignments.some((assignment) => assignment.status === 'Provisioned')) {
    return 'Provisioned';
  }

  return 'Generated';
}

function getAccessGrantVariant(status: AccessGrantResponse['status']): 'success' | 'secondary' | 'error' {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Revoked':
      return 'error';
    default:
      return 'secondary';
  }
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
