import { type ReactNode, useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { Bike, CalendarDays, CheckCircle2, MapPin, UserRound, XCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

type VisitConfirmationResponse = components['schemas']['VisitConfirmationResponse'];
type Transport = NonNullable<components['schemas']['ModeOfTransport']>;

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

function getHostName(visit: VisitConfirmationResponse, unnamedHostLabel: string) {
  return [visit.host.firstName, visit.host.lastName].filter(Boolean).join(' ') || visit.host.email || unnamedHostLabel;
}

function getVisitorName(visit: VisitConfirmationResponse) {
  return [visit.visitor.firstName, visit.visitor.lastName].filter(Boolean).join(' ') || visit.visitor.email;
}

export default function VisitorConfirmationPage() {
  const { visitId, invitationId } = useParams({ from: '/main/visitor-confirmation/$visitId/$invitationId' });
  const queryClient = useQueryClient();
  const { t } = useTranslation();
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [company, setCompany] = useState('');
  const [transport, setTransport] = useState<Transport>('Car');
  const [licensePlate, setLicensePlate] = useState('');
  const [message, setMessage] = useState<string | null>(null);

  const queryKey = ['visitor-confirmation', visitId, invitationId] as const;
  const transportOptions: { value: Transport; label: string }[] = [
    { value: 'Car', label: t('visitorConfirmation.car') },
    { value: 'PublicTransport', label: t('visitorConfirmation.publicTransport') },
    { value: 'Bike', label: t('visitorConfirmation.bike') },
    { value: 'Walk', label: t('visitorConfirmation.walk') },
  ];

  const confirmationQuery = useQuery({
    queryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/visitors/visits/{visitId}/invitations/{invitationId}/confirmation', {
        params: { path: { visitId, invitationId } },
      });

      if (error || !data) {
        throw new Error(t('visitorConfirmation.couldNotLoadInvitation'));
      }

      return data;
    },
  });

  const visit = confirmationQuery.data;
  const alreadyResponded = visit?.confirmationStatus === 'Confirmed' || visit?.confirmationStatus === 'Rejected';
  const licensePlateRequired = transport === 'Car';
  const canSubmit = !!firstName && !!lastName && !!email && !!company && (!licensePlateRequired || !!licensePlate.trim()) && !alreadyResponded;

  useEffect(() => {
    if (!visit) {
      return;
    }

    setFirstName(visit.visitor.firstName ?? '');
    setLastName(visit.visitor.lastName ?? '');
    setEmail(visit.visitor.email ?? '');
    setCompany(visit.visitor.company ?? '');
    setTransport((visit.transport ?? 'Car') as Transport);
    setLicensePlate(visit.visitor.licensePlate ?? '');
  }, [visit]);

  const acceptInvitation = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/visitors/visits/{visitId}/invitations/{invitationId}/confirm', {
        params: { path: { visitId, invitationId } },
        body: {
          firstName,
          lastName,
          email,
          company,
          transport,
          licensePlate: licensePlateRequired ? licensePlate : null,
        },
      });

      if (error) {
        throw new Error(t('visitorConfirmation.couldNotAcceptInvitation'));
      }
    },
    onSuccess: async () => {
      setMessage(t('visitorConfirmation.acceptedMessage'));
      await queryClient.invalidateQueries({ queryKey });
    },
    onError: async () => {
      setMessage(t('visitorConfirmation.acceptFailedMessage'));
      await queryClient.invalidateQueries({ queryKey });
    },
  });

  const rejectInvitation = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/visitors/visits/{visitId}/invitations/{invitationId}/reject', {
        params: { path: { visitId, invitationId } },
      });

      if (error) {
        throw new Error(t('visitorConfirmation.couldNotRejectInvitation'));
      }
    },
    onSuccess: async () => {
      setMessage(t('visitorConfirmation.rejectedMessage'));
      await queryClient.invalidateQueries({ queryKey });
    },
    onError: async () => {
      setMessage(t('visitorConfirmation.rejectFailedMessage'));
      await queryClient.invalidateQueries({ queryKey });
    },
  });

  if (confirmationQuery.isLoading) {
    return <p className="text-[14px] text-muted-foreground">{t('visitorConfirmation.loading')}</p>;
  }

  if (confirmationQuery.isError || !visit) {
    return (
      <Card className="mx-auto max-w-2xl p-6">
        <p className="text-[13px] font-semibold uppercase tracking-[0.18em] text-error">{t('visitorConfirmation.unavailableTag')}</p>
        <h1 className="mt-3 text-[28px] font-semibold tracking-tight">{t('visitorConfirmation.unavailableTitle')}</h1>
        <p className="mt-3 text-[14px] text-muted-foreground">{t('visitorConfirmation.unavailableDescription')}</p>
      </Card>
    );
  }

  return (
    <div className="mx-auto grid max-w-4xl gap-6">
      <header className="rounded-structural border border-border bg-content p-5 sm:p-7">
        <p className="text-[13px] font-semibold uppercase tracking-[0.18em] text-primary">{t('visitorConfirmation.headerTag')}</p>
        <h1 className="mt-3 text-[28px] font-semibold tracking-tight sm:text-[36px]">{visit.summary}</h1>
        <p className="mt-3 max-w-2xl text-[15px] text-muted-foreground">
          {alreadyResponded
            ? t('visitorConfirmation.alreadyAnswered')
            : t('visitorConfirmation.reviewPrompt')}
        </p>
      </header>

      {message ? (
        <p className="rounded-interactive border border-border bg-hover-blue px-4 py-3 text-[14px] text-foreground" role="status">
          {message}
        </p>
      ) : null}

      {alreadyResponded ? <ResponseState visit={visit} /> : null}

      <div className="grid gap-6 lg:grid-cols-[1fr_1.25fr]">
        <Card className="p-5 sm:p-6">
          <h2 className="text-[18px] font-semibold tracking-tight">{t('visitorConfirmation.detailsTitle')}</h2>
          <dl className="mt-5 grid gap-4 text-[14px]">
            <Detail icon={<CalendarDays className="size-4" />} label={t('visitorConfirmation.starts')} value={formatDateTime(visit.start)} />
            <Detail icon={<CalendarDays className="size-4" />} label={t('visitorConfirmation.ends')} value={formatDateTime(visit.stop)} />
            <Detail icon={<MapPin className="size-4" />} label={t('visitorConfirmation.location')} value={visit.locationLabel ?? t('visitorConfirmation.notSpecified')} />
            <Detail icon={<UserRound className="size-4" />} label={t('visitorConfirmation.host')} value={getHostName(visit, t('visitorConfirmation.unnamedHost'))} hint={visit.host.email ?? t('visitorConfirmation.noEmailProvided')} />
          </dl>
        </Card>

        <Card className="p-5 sm:p-6">
          <h2 className="text-[18px] font-semibold tracking-tight">{t('visitorConfirmation.responseTitle')}</h2>
          <p className="mt-2 text-[14px] text-muted-foreground">{t('visitorConfirmation.invitedAs', { name: getVisitorName(visit) })}</p>

          <form className="mt-5 grid gap-4" onSubmit={(event) => { event.preventDefault(); void acceptInvitation.mutateAsync(); }}>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label={t('visitorConfirmation.firstName')} value={firstName} onChange={setFirstName} disabled={alreadyResponded} />
              <Field label={t('visitorConfirmation.lastName')} value={lastName} onChange={setLastName} disabled={alreadyResponded} />
            </div>
            <Field label={t('visitorConfirmation.email')} type="email" value={email} onChange={setEmail} disabled={alreadyResponded} />
            <Field label={t('visitorConfirmation.company')} value={company} onChange={setCompany} disabled={alreadyResponded} />

            <div>
              <label className="mb-1 block text-[13px] font-medium text-foreground" htmlFor="transport">{t('visitorConfirmation.transportMode')}</label>
              <select
                id="transport"
                className="h-10 w-full rounded-interactive border border-border bg-content px-3 text-[14px] text-foreground outline-none focus:ring-[3px] focus:ring-primary/20 disabled:opacity-50"
                value={transport}
                disabled={alreadyResponded}
                onChange={(event) => setTransport(event.target.value as Transport)}
              >
                {transportOptions.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </div>

            {licensePlateRequired ? (
              <Field label={t('visitorConfirmation.licensePlate')} value={licensePlate} onChange={setLicensePlate} disabled={alreadyResponded} />
            ) : null}

            <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-between">
              <Button
                type="button"
                variant="outline"
                className="border-error text-error hover:bg-error-background"
                disabled={alreadyResponded || rejectInvitation.isPending || acceptInvitation.isPending}
                onClick={() => void rejectInvitation.mutateAsync()}
              >
                <XCircle className="size-4" aria-hidden="true" />
                {t('visitorConfirmation.rejectVisit')}
              </Button>
              <Button type="submit" disabled={!canSubmit || acceptInvitation.isPending || rejectInvitation.isPending}>
                <CheckCircle2 className="size-4" aria-hidden="true" />
                {acceptInvitation.isPending ? t('visitorConfirmation.accepting') : t('visitorConfirmation.acceptVisit')}
              </Button>
            </div>
          </form>
        </Card>
      </div>
    </div>
  );
}

function Detail({ icon, label, value, hint }: { readonly icon: ReactNode; readonly label: string; readonly value: string; readonly hint?: string }) {
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

function Field({ label, value, onChange, disabled, type = 'text' }: { readonly label: string; readonly value: string; readonly onChange: (value: string) => void; readonly disabled: boolean; readonly type?: string }) {
  const id = label.toLowerCase().replaceAll(' ', '-');

  return (
    <div>
      <label className="mb-1 block text-[13px] font-medium text-foreground" htmlFor={id}>{label}</label>
      <Input id={id} type={type} value={value} disabled={disabled} required onChange={(event) => onChange(event.target.value)} />
    </div>
  );
}

function ResponseState({ visit }: { readonly visit: VisitConfirmationResponse }) {
  const { t } = useTranslation();
  const accepted = visit.confirmationStatus === 'Confirmed';
  const transportLabel = visit.transport ? getTransportLabel(visit.transport, t) : null;

  return (
    <Card className="flex items-start gap-3 p-4">
      <div className={`flex size-9 shrink-0 items-center justify-center rounded-full ${accepted ? 'bg-success-background text-success' : 'bg-error-background text-error'}`}>
        {accepted ? <CheckCircle2 className="size-5" aria-hidden="true" /> : <XCircle className="size-5" aria-hidden="true" />}
      </div>
      <div>
        <p className="text-[15px] font-semibold text-foreground">{accepted ? t('visitorConfirmation.acceptedMessage') : t('visitorConfirmation.rejectedMessage')}</p>
        <p className="mt-1 text-[14px] text-muted-foreground">
          {t('visitorConfirmation.responseRecorded', {
            when: accepted && visit.confirmedAt ? formatDateTime(visit.confirmedAt) : visit.rejectedAt ? formatDateTime(visit.rejectedAt) : t('visitorConfirmation.forThisInvitation'),
          })}
        </p>
        {accepted && transportLabel ? (
          <p className="mt-2 inline-flex items-center gap-1.5 text-[13px] text-muted-foreground">
            <Bike className="size-3.5" aria-hidden="true" />
            {transportLabel}
          </p>
        ) : null}
      </div>
    </Card>
  );
}

function getTransportLabel(transport: Transport, t: ReturnType<typeof useTranslation>['t']) {
  switch (transport) {
    case 'Car':
      return t('visitorConfirmation.car');
    case 'PublicTransport':
      return t('visitorConfirmation.publicTransport');
    case 'Bike':
      return t('visitorConfirmation.bike');
    case 'Walk':
      return t('visitorConfirmation.walk');
    default:
      return transport;
  }
}
