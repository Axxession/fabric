import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { ArrowLeft, CalendarX, ChevronRight, Mail, Users } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { VisitStatusBadge } from '@/shared/components/visit-status-badge';
import { VisitForm, type VisitFormValues } from './visit-form';

type VisitResponse = components['schemas']['VisitResponse'];
type VisitInvitationResponse = components['schemas']['VisitInvitationResponse'];
type CredentialPACSAssignmentResponse = components['schemas']['CredentialPACSAssignmentResponse'];
type VisitorPreOnboardingSaga = components['schemas']['VisitorPreOnboardingSaga'] & { invitationSentAt?: string | null };

const visitsQueryKey = ['visitors-management', 'visits'] as const;
const onboardingSagaRefetchIntervalMs = 10_000;

function toDatetimeLocal(value: string): string {
  const date = new Date(value);
  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60_000);
  return local.toISOString().slice(0, 16);
}

function mapVisitToFormValues(visit: VisitResponse): VisitFormValues {
  return {
    summary: visit.summary ?? '',
    start: visit.start ? toDatetimeLocal(visit.start) : '',
    stop: visit.stop ? toDatetimeLocal(visit.stop) : '',
    locationId: visit.locationId ?? null,
  };
}

function formatInvitationName(invitation: VisitInvitationResponse) {
  return [invitation.firstName, invitation.lastName].filter(Boolean).join(' ') || invitation.email || 'Unnamed';
}

export function VisitEditPageContent({ visitId }: { readonly visitId: string }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'visit' | 'invitations'>('visit');
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);
  const [showInviteForm, setShowInviteForm] = useState(false);
  const [isInviteSuggestionsOpen, setIsInviteSuggestionsOpen] = useState(false);
  const [inviteFirstName, setInviteFirstName] = useState('');
  const [inviteLastName, setInviteLastName] = useState('');
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteCompany, setInviteCompany] = useState('');

  const visitQuery = useQuery({
    queryKey: [...visitsQueryKey, visitId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/visitors/visits/{id}', {
        params: { path: { id: visitId } },
      });

      if (error) {
        throw new Error('Could not load visit.');
      }

      return data;
    },
  });

  const visit = visitQuery.data;
  const isCancelledOrCompleted = visit?.status === 'Cancelled' || visit?.status === 'Completed';

  const reschedule = useMutation({
    mutationFn: async (values: { start: string; stop: string }) => {
      const { error } = await api.POST('/api/visitors/visits/{id}/reschedule', {
        params: { path: { id: visitId } },
        body: {
          start: new Date(values.start).toISOString(),
          stop: new Date(values.stop).toISOString(),
        },
      });

      if (error) {
        throw new Error('Could not reschedule visit.');
      }
    },
  });

  const updateSummary = useMutation({
    mutationFn: async (summary: string) => {
      const { error } = await api.PUT('/api/visitors/visits/{id}/summary', {
        params: { path: { id: visitId } },
        body: { summary },
      });

      if (error) {
        throw new Error('Could not update visit summary.');
      }
    },
  });

  const cancelVisit = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/visitors/visits/{id}/cancel', {
        params: { path: { id: visitId } },
      });

      if (error) {
        throw new Error('Could not cancel visit.');
      }
    },
  });

  const relocateVisit = useMutation({
    mutationFn: async (locationId: string | null) => {
      const { error } = await api.POST('/api/visitors/visits/{id}/relocate', {
        params: { path: { id: visitId } },
        body: { locationId },
      });

      if (error) {
        throw new Error('Could not relocate visit.');
      }
    },
  });

  const inviteVisitor = useMutation({
    mutationFn: async (values: { firstName: string; lastName: string; email: string; company: string }) => {
      const { error } = await api.POST('/api/visitors/visits/{id}/invitations', {
        params: { path: { id: visitId } },
        body: values,
      });

      if (error) {
        throw new Error('Could not send invitation.');
      }
    },
  });

  const visitorsQuery = useQuery({
    queryKey: ['visitors-management', 'visitors', 'search', inviteEmail],
    queryFn: async () => {
      if (!inviteEmail) {
        return [];
      }

      const { data, error } = await api.GET('/api/visitors/visitors', {
        params: { query: { Query: inviteEmail, ids: [] } },
      });

      if (error) {
        throw new Error('Could not search visitors.');
      }

      return data?.items ?? [];
    },
  });

  const sagasQuery = useQuery({
    queryKey: [...visitsQueryKey, visitId, 'onboarding-sagas'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/sagas/visitor-pre-onboarding/{visitId}', {
        params: { path: { visitId } },
      });

      if (error) {
        throw new Error('Could not load onboarding sagas.');
      }

      return data ?? [];
    },
    enabled: !!visit,
    refetchInterval: (query) => {
      const sagas = query.state.data ?? [];
      if (!Array.isArray(sagas) || sagas.length === 0) {
        return false;
      }

      return sagas.some(isSagaStillProcessing)
        ? onboardingSagaRefetchIntervalMs
        : false;
    },
  });

  const sagasByInvitationId = new Map(
    (sagasQuery.data ?? []).map((saga) => [saga.invitationId, saga]),
  );

  const credentialAssignmentsQuery = useQuery({
    queryKey: [...visitsQueryKey, visitId, 'credential-pacs-assignments', (sagasQuery.data ?? []).map((saga) => saga.credentialId ?? '').join(',')],
    queryFn: async () => {
      const credentialIds = Array.from(new Set((sagasQuery.data ?? []).map((saga) => saga.credentialId).filter((item): item is string => Boolean(item))));
      if (credentialIds.length === 0) {
        return [] as CredentialPACSAssignmentResponse[];
      }

      const { data, error } = await api.GET('/api/access-control/credential-pacs-assignments', {
        params: { query: { CredentialId: undefined, CredentialIds: credentialIds, AccessControlSystemId: undefined, Status: undefined, Page: 0, PageSize: 500 } as never },
      });

      if (error) {
        throw new Error('Could not load QR provisioning state.');
      }

      return data?.items ?? [];
    },
    enabled: !!visit && (sagasQuery.data?.length ?? 0) > 0,
  });

  const credentialAssignmentsByCredentialId = groupAssignmentsByCredentialId(credentialAssignmentsQuery.data ?? []);

  const isSaving = reschedule.isPending || updateSummary.isPending || relocateVisit.isPending;

  async function handleSubmit(formValues: VisitFormValues) {
    if (!visit) {
      return;
    }

    const summaryChanged = formValues.summary !== (visit.summary ?? '');
    const scheduleChanged = formValues.start !== toDatetimeLocal(visit.start ?? '') || formValues.stop !== toDatetimeLocal(visit.stop ?? '');
    const locationChanged = formValues.locationId !== (visit.locationId ?? null);

    try {
      if (summaryChanged) {
        await updateSummary.mutateAsync(formValues.summary);
      }

      if (scheduleChanged) {
        await reschedule.mutateAsync({ start: formValues.start, stop: formValues.stop });
      }

      if (locationChanged) {
        await relocateVisit.mutateAsync(formValues.locationId);
      }

      await queryClient.invalidateQueries({ queryKey: visitsQueryKey });
      await queryClient.invalidateQueries({ queryKey: [...visitsQueryKey, visitId] });
      toast.success('Visit updated.');
    } catch {
      toast.error('Could not save changes.');
    }
  }

  async function handleCancel() {
    try {
      await cancelVisit.mutateAsync();
      await queryClient.invalidateQueries({ queryKey: visitsQueryKey });
      await queryClient.invalidateQueries({ queryKey: [...visitsQueryKey, visitId] });
      toast.success('Visit cancelled.');
      setShowCancelConfirm(false);
    } catch {
      toast.error('Could not cancel visit.');
    }
  }

  async function handleInvite(e: React.FormEvent) {
    e.preventDefault();

    if (!inviteEmail) {
      return;
    }

    try {
      await inviteVisitor.mutateAsync({
        firstName: inviteFirstName,
        lastName: inviteLastName,
        email: inviteEmail,
        company: inviteCompany,
      });

      await queryClient.invalidateQueries({ queryKey: [...visitsQueryKey, visitId] });
      await queryClient.invalidateQueries({ queryKey: [...visitsQueryKey, visitId, 'onboarding-sagas'] });
      toast.success('Invitation sent.');
      setInviteFirstName('');
      setInviteLastName('');
      setInviteEmail('');
      setInviteCompany('');
      setShowInviteForm(false);
    } catch {
      toast.error('Could not send invitation.');
    }
  }

  const initialFormValues = visit ? mapVisitToFormValues(visit) : undefined;

  const errorMessage = visitQuery.isError
    ? 'Could not load visit.'
    : reschedule.isError
      ? 'Could not reschedule visit.'
      : updateSummary.isError
        ? 'Could not update visit summary.'
        : relocateVisit.isError
          ? 'Could not relocate visit.'
          : cancelVisit.isError
            ? 'Could not cancel visit.'
            : inviteVisitor.isError
              ? 'Could not send invitation.'
              : credentialAssignmentsQuery.isError
                ? 'Could not load QR provisioning state.'
              : null;

  return (
    <div className="grid gap-6">
      <button
        type="button"
        className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground"
        aria-label="Go back"
        onClick={() => window.history.back()}
      >
        <ArrowLeft className="size-4" aria-hidden="true" />
        Back
      </button>

      {visit ? (
        <header className="rounded-structural border border-border bg-content p-5 sm:p-6 md:p-8">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h1 className="text-[24px] font-semibold tracking-tight">{visit.summary || 'Untitled visit'}</h1>
              <p className="mt-2 text-[14px] text-muted-foreground">Update visit details and manage invitation progress from one workspace.</p>
            </div>
            <VisitStatusBadge status={visit.status} />
          </div>
          <div className="mt-4 flex flex-wrap gap-2">
            <SummaryChip icon={<Users className="size-3.5" />} label={`${visit.invitations?.length ?? 0} ${(visit.invitations?.length ?? 0) === 1 ? 'participant' : 'participants'}`} />
            {visit.host ? <SummaryChip icon={<Mail className="size-3.5" />} label={[visit.host.firstName, visit.host.lastName].filter(Boolean).join(' ') || visit.host.email || 'Host'} /> : null}
            {visit.host?.email ? <SummaryChip icon={<Mail className="size-3.5" />} label={visit.host.email} /> : null}
          </div>
        </header>
      ) : null}

      {errorMessage ? (
        <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
          {errorMessage}
        </p>
      ) : null}

      {visitQuery.isLoading ? (
        <p className="text-[14px] text-muted-foreground">Loading visit...</p>
      ) : null}

      {!visitQuery.isLoading && !visitQuery.isError && visit && initialFormValues ? (
        <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as 'visit' | 'invitations')}>
          <TabsList>
            <TabsTrigger value="visit">Visit</TabsTrigger>
            <TabsTrigger value="invitations">Invitations <span className="text-[12px] font-medium text-muted-foreground">{visit.invitations?.length ?? 0}</span></TabsTrigger>
          </TabsList>

          <TabsContent value="visit" className="mt-5">
            <Card className="p-5 sm:p-6">
              <h2 className="mb-4 text-[18px] font-semibold tracking-tight">Visit details</h2>
              <VisitForm
                initialValues={initialFormValues}
                isSubmitting={isSaving}
                disableSubmit={isCancelledOrCompleted}
                submitLabel="Save changes"
                onSubmit={handleSubmit}
                disabledFields={isCancelledOrCompleted ? ['summary', 'start', 'stop', 'location'] : undefined}
                footerLeft={
                  !isCancelledOrCompleted && !showCancelConfirm ? (
                    <Button
                      type="button"
                      variant="outline"
                      className="border-error text-error hover:bg-error-background"
                      onClick={() => setShowCancelConfirm(true)}
                    >
                      <CalendarX className="size-4" aria-hidden="true" />
                      Cancel visit
                    </Button>
                  ) : undefined
                }
              />

              {showCancelConfirm ? (
                <div className="-mx-5 -mb-5 mt-6 flex flex-col gap-3 border-t border-border bg-error-background px-5 py-4 sm:-mx-6 sm:-mb-6 sm:flex-row sm:items-center sm:px-6">
                  <p className="flex-1 text-[14px] font-medium text-error">Are you sure you want to cancel this visit?</p>
                  <Button variant="outline" size="sm" onClick={() => setShowCancelConfirm(false)}>Keep</Button>
                  <Button size="sm" className="bg-error text-white hover:opacity-90" onClick={handleCancel} disabled={cancelVisit.isPending}>{cancelVisit.isPending ? 'Cancelling...' : 'Yes, cancel'}</Button>
                </div>
              ) : null}
            </Card>
          </TabsContent>

          <TabsContent value="invitations" className="mt-5">
            <Card className="p-5 sm:p-6">
              <div className="mb-4 flex items-center justify-between gap-3">
                <h2 className="text-[18px] font-semibold tracking-tight">Invitations</h2>
                {!isCancelledOrCompleted && !showInviteForm ? <Button size="sm" onClick={() => setShowInviteForm(true)}>Invite</Button> : null}
              </div>

              {showInviteForm && !isCancelledOrCompleted ? (
                <form onSubmit={handleInvite} className="mb-6 rounded-[18px] border border-border bg-background p-4">
                  <h3 className="mb-4 text-[14px] font-semibold tracking-tight">New invitation</h3>
                  <div className="mb-4 grid gap-3">
                    <div className="relative">
                      <label className="mb-1 block text-[13px] font-medium text-foreground">Email</label>
                      <Input required type="email" placeholder="Email" value={inviteEmail} onChange={(e) => { setInviteEmail(e.target.value); setIsInviteSuggestionsOpen(true); }} />
                      {isInviteSuggestionsOpen && inviteEmail && visitorsQuery.data && visitorsQuery.data.length > 0 ? (
                        <div className="absolute z-50 mt-1 w-full rounded-structural border border-border bg-content text-foreground shadow-md">
                          {visitorsQuery.data.map((visitor) => (
                            <button key={visitor.id} type="button" className="flex w-full items-center gap-2 px-3 py-2 text-left text-[14px] transition hover:bg-hover-blue" onClick={() => { setInviteEmail(visitor.email ?? ''); setInviteFirstName(visitor.firstName ?? ''); setInviteLastName(visitor.lastName ?? ''); setInviteCompany(visitor.company ?? ''); setIsInviteSuggestionsOpen(false); }}>
                              <div>
                                <p className="font-medium text-foreground">{visitor.email}</p>
                                {visitor.firstName || visitor.lastName ? <p className="text-[12px] text-muted-foreground">{[visitor.firstName, visitor.lastName].filter(Boolean).join(' ')}</p> : null}
                              </div>
                            </button>
                          ))}
                        </div>
                      ) : null}
                    </div>

                    <div className="grid gap-3 sm:grid-cols-2">
                      <div>
                        <label className="mb-1 block text-[13px] font-medium text-foreground">First name</label>
                        <Input placeholder="First name" value={inviteFirstName} onChange={(e) => setInviteFirstName(e.target.value)} />
                      </div>
                      <div>
                        <label className="mb-1 block text-[13px] font-medium text-foreground">Last name</label>
                        <Input placeholder="Last name" value={inviteLastName} onChange={(e) => setInviteLastName(e.target.value)} />
                      </div>
                      <div className="sm:col-span-2">
                        <label className="mb-1 block text-[13px] font-medium text-foreground">Company</label>
                        <Input placeholder="Company" value={inviteCompany} onChange={(e) => setInviteCompany(e.target.value)} />
                      </div>
                    </div>
                  </div>

                  <div className="flex flex-col-reverse gap-2 sm:flex-row sm:items-center sm:justify-between [&>*]:w-full sm:[&>*]:w-auto">
                    <Button type="button" variant="outline" size="sm" onClick={() => { setShowInviteForm(false); setIsInviteSuggestionsOpen(false); setInviteFirstName(''); setInviteLastName(''); setInviteEmail(''); setInviteCompany(''); }}>Cancel</Button>
                    <Button type="submit" size="sm" className="w-full sm:w-auto" disabled={inviteVisitor.isPending || !inviteEmail}>{inviteVisitor.isPending ? 'Sending...' : 'Send invitation'}</Button>
                  </div>
                </form>
              ) : null}

              {visit.invitations && visit.invitations.length > 0 ? (
                <>
                  <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
                    <table className="w-full min-w-[60rem] border-collapse text-left text-[14px]">
                      <thead className="border-b border-border bg-background/70 text-[11px] uppercase tracking-[0.18em] text-muted-foreground">
                        <tr>
                          <th className="px-5 py-4 font-semibold">Visitor</th>
                          <th className="px-5 py-4 font-semibold">Invitation</th>
                          <th className="px-5 py-4 font-semibold">Confirmation</th>
                          <th className="px-5 py-4 font-semibold">Credential</th>
                          <th className="px-5 py-4 font-semibold">Arrival</th>
                          <th className="px-5 py-4 text-right font-semibold">Open</th>
                        </tr>
                      </thead>
                      <tbody>
                        {visit.invitations.map((invitation) => {
                          const saga = sagasByInvitationId.get(invitation.id) ?? null;
                          const credentialAssignments = saga?.credentialId ? credentialAssignmentsByCredentialId.get(saga.credentialId) ?? [] : [];
                          const qrStatus = getQrGeneratedStatus(saga, credentialAssignments);
                          const arrivalStatus = saga?.arrivalId ? 'Arrival registered' : 'Awaiting arrival';
                          const invitationSentStatus = saga?.invitationSentAt ? 'Invitation sent' : 'Invitation pending';

                          return (
                            <tr key={invitation.id} className="cursor-pointer border-t border-border transition hover:bg-hover-blue/45" role="link" tabIndex={0} onClick={() => void navigate({ to: '/employee/visitors/$visitId/invitations/$invitationId', params: { visitId, invitationId: invitation.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/employee/visitors/$visitId/invitations/$invitationId', params: { visitId, invitationId: invitation.id } }); } }}>
                              <td className="px-5 py-5 align-top">
                                <div className="min-w-0">
                                  <p className="truncate font-semibold text-foreground">{formatInvitationName(invitation)}</p>
                                  {invitation.email ? <p className="truncate text-[13px] text-muted-foreground">{invitation.email}</p> : null}
                                </div>
                              </td>
                              <td className="px-5 py-5 align-top"><Badge variant={invitationSentStatus === 'Invitation sent' ? 'success' : 'secondary'}>{invitationSentStatus}</Badge></td>
                              <td className="px-5 py-5 align-top"><Badge variant={getConfirmationVariant(invitation.confirmationStatus)}>{formatConfirmationStatus(invitation.confirmationStatus)}</Badge></td>
                              <td className="px-5 py-5 align-top"><Badge variant={getQrGeneratedVariant(qrStatus)}>{qrStatus}</Badge></td>
                              <td className="px-5 py-5 align-top"><Badge variant={arrivalStatus === 'Arrival registered' ? 'success' : 'secondary'}>{arrivalStatus}</Badge></td>
                              <td className="px-5 py-5 align-top text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>

                  <div className="grid gap-3 md:hidden">
                    {visit.invitations.map((invitation) => {
                      const saga = sagasByInvitationId.get(invitation.id) ?? null;
                      const credentialAssignments = saga?.credentialId ? credentialAssignmentsByCredentialId.get(saga.credentialId) ?? [] : [];
                      const qrStatus = getQrGeneratedStatus(saga, credentialAssignments);
                      const arrivalStatus = saga?.arrivalId ? 'Arrival registered' : 'Awaiting arrival';
                      const invitationSentStatus = saga?.invitationSentAt ? 'Invitation sent' : 'Invitation pending';

                      return (
                        <article key={invitation.id} className="rounded-[18px] border border-border bg-content p-5 shadow-[0_10px_28px_rgba(17,24,39,0.08)] transition hover:border-primary/20 hover:shadow-[0_14px_34px_rgba(17,24,39,0.1)]" role="link" tabIndex={0} onClick={() => void navigate({ to: '/employee/visitors/$visitId/invitations/$invitationId', params: { visitId, invitationId: invitation.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/employee/visitors/$visitId/invitations/$invitationId', params: { visitId, invitationId: invitation.id } }); } }}>
                          <div className="flex items-start justify-between gap-3">
                            <div className="flex min-w-0 items-start gap-3">
                              <div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-hover-blue"><Mail className="size-4 text-primary" /></div>
                              <div className="min-w-0">
                                <p className="truncate text-[14px] font-semibold text-foreground">{formatInvitationName(invitation)}</p>
                                {invitation.email ? <p className="truncate text-[13px] text-muted-foreground">{invitation.email}</p> : null}
                              </div>
                            </div>
                            <span className="inline-flex size-8 shrink-0 items-center justify-center rounded-interactive border border-border text-muted-foreground"><ChevronRight className="size-4" aria-hidden="true" /></span>
                          </div>
                          <div className="mt-4 flex flex-wrap gap-2">
                            <Badge variant={invitationSentStatus === 'Invitation sent' ? 'success' : 'secondary'}>{invitationSentStatus}</Badge>
                            <Badge variant={getConfirmationVariant(invitation.confirmationStatus)}>{formatConfirmationStatus(invitation.confirmationStatus)}</Badge>
                            <Badge variant={getQrGeneratedVariant(qrStatus)}>{qrStatus}</Badge>
                            <Badge variant={arrivalStatus === 'Arrival registered' ? 'success' : 'secondary'}>{arrivalStatus}</Badge>
                          </div>
                        </article>
                      );
                    })}
                  </div>
                </>
              ) : (
                <p className="text-[14px] text-muted-foreground">No invitations yet.</p>
              )}
            </Card>
          </TabsContent>
        </Tabs>
      ) : null}
    </div>
  );
}

function getQrGeneratedStatus(saga: VisitorPreOnboardingSaga | null, assignments: readonly CredentialPACSAssignmentResponse[]) {
  if (!saga?.credentialId) {
    return 'QR pending';
  }

  if (assignments.some((assignment) => assignment.status === 'Provisioned')) {
    return 'QR provisioned';
  }

  return 'QR generated';
}

function getQrGeneratedVariant(status: ReturnType<typeof getQrGeneratedStatus>) {
  return status === 'QR provisioned' ? 'success' : status === 'QR generated' ? 'outline' : 'secondary';
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

function SummaryChip({ icon, label }: { readonly icon: React.ReactNode; readonly label: string }) {
  return <span className="inline-flex items-center gap-1.5 rounded-[10px] border border-border bg-background px-3 py-1 text-[12px] font-medium text-muted-foreground">{icon}{label}</span>;
}

function groupAssignmentsByCredentialId(assignments: readonly CredentialPACSAssignmentResponse[]) {
  return assignments.reduce((map, assignment) => {
    const current = map.get(assignment.credentialId) ?? [];
    current.push(assignment);
    map.set(assignment.credentialId, current);
    return map;
  }, new Map<string, CredentialPACSAssignmentResponse[]>());
}

function isSagaStillProcessing(saga: VisitorPreOnboardingSaga) {
  const cancelling = Boolean(saga.cancellationRequestedAt && !saga.cancelledAt);
  const activeRegistration = !saga.cancelledAt && !saga.expiredAt && !saga.isCompleteOnOurEnd;
  return cancelling || activeRegistration;
}
