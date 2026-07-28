import { useEffect, useId, useState, type FormEvent, type ReactNode } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bell, MailCheck, Route, Send, UserCheck } from 'lucide-react';
import { toast } from 'sonner';

import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Textarea } from '@/shared/components/ui/textarea';

import {
  fetchVisitorPreOnboardingConfig,
  getDefaultVisitorPreOnboardingConfig,
  updateVisitorPreOnboardingConfig,
  visitorPreOnboardingConfigQueryKey,
  type CredentialGenerationMode,
  type CustomNotification,
  type VisitorPreOnboardingSagaConfigRequest,
} from './visitor-pre-onboarding-config';

export default function VisitorsSettingsPage() {
  const queryClient = useQueryClient();
  const configQuery = useQuery({
    queryKey: visitorPreOnboardingConfigQueryKey,
    queryFn: fetchVisitorPreOnboardingConfig,
  });
  const [values, setValues] = useState<VisitorPreOnboardingSagaConfigRequest>(getDefaultVisitorPreOnboardingConfig);
  const accessControlQrUnavailable = values.qrGenerationMode === 'AccessControlQr';

  useEffect(() => {
    if (configQuery.data) {
      setValues(toRequest(configQuery.data));
    }
  }, [configQuery.data]);

  const updateConfig = useMutation({
    mutationFn: updateVisitorPreOnboardingConfig,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: visitorPreOnboardingConfigQueryKey });
      toast.success('Visitor journey settings saved.');
    },
    onError: () => {
      toast.error('Could not save visitor journey settings.');
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (accessControlQrUnavailable) {
      toast.error('Access control QR is temporarily unavailable during PACS migration.');
      return;
    }

    updateConfig.mutate(normalize(values));
  }

  function handleQrGenerationModeChange(mode: CredentialGenerationMode) {
    setValues((current) => ({ ...current, qrGenerationMode: mode }));
  }

  return (
    <section className="grid gap-6">
      <div className="rounded-structural border border-border bg-content p-4 sm:p-6">
        <p className="text-[13px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Settings</p>
        <h1 className="mt-3 text-[32px] font-semibold tracking-tight">Visitors</h1>
        <p className="mt-3 max-w-2xl text-[14px] text-muted-foreground">Configure visitor journey defaults and notification behavior for pre-onboarding.</p>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <CardTitle className="flex items-center gap-2 text-[20px]">
                <Route className="size-5 text-primary" aria-hidden="true" />
                Visitor journey
              </CardTitle>
              <CardDescription className="mt-2 max-w-3xl">
                Decide how visitor pre-onboarding issues credentials and which emails are sent during invitation, confirmation, cancellation, and rescheduling.
              </CardDescription>
            </div>
            <StatusPill mode={values.qrGenerationMode} />
          </div>
        </CardHeader>
        <CardContent>
          {configQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading visitor journey settings...</p> : null}
          {configQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load visitor journey settings.</p> : null}

          {!configQuery.isLoading && !configQuery.isError ? (
            <form className="grid gap-6" onSubmit={handleSubmit}>
              <div className="grid gap-2">
                <label className="text-[14px] font-medium" htmlFor="qr-generation-mode">Credential generation</label>
                <select
                  id="qr-generation-mode"
                  className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20 md:max-w-sm"
                  value={values.qrGenerationMode}
                  onChange={(event) => handleQrGenerationModeChange(event.target.value as CredentialGenerationMode)}
                >
                  <option value="PlatformQr">Platform QR</option>
                  <option value="AccessControlQr">Access control QR</option>
                </select>
                <p className="text-[13px] text-muted-foreground">Controls which system should generate visitor credentials during pre-onboarding.</p>
              </div>

              {values.qrGenerationMode === 'AccessControlQr' ? (
                <div className="grid gap-4 rounded-structural border border-border bg-background p-4 lg:grid-cols-2">
                  <div className="lg:col-span-2">
                    <h2 className="text-[15px] font-semibold">Access control credential</h2>
                    <p className="mt-1 text-[13px] text-muted-foreground">Access control QR is temporarily unavailable while legacy AccessPolicies flows are removed.</p>
                  </div>
                  <p className="rounded-interactive border border-border px-4 py-3 text-[14px] text-muted-foreground lg:col-span-2">
                    Save is disabled for this mode until PACS credential issuance is migrated to `AccessControl`.
                  </p>
                </div>
              ) : null}

              <div className="grid gap-4 lg:grid-cols-2">
                <NotificationTemplateSection
                  icon={<Send className="size-4" aria-hidden="true" />}
                  title="Invitation"
                  description="Sent to visitors after arrival registration and QR setup. The default template is used unless custom HTML is enabled."
                  customEnabled={values.useCustomInviteNotification}
                  customSubject={values.customInviteNotification?.subject ?? ''}
                  customBody={values.customInviteNotification?.body ?? ''}
                  onCustomEnabledChange={(checked) => setValues((current) => ({ ...current, useCustomInviteNotification: checked, customInviteNotification: checked ? current.customInviteNotification : null }))}
                  onCustomSubjectChange={(subject) => setValues((current) => ({ ...current, customInviteNotification: updateCustomNotification(current.customInviteNotification, 'subject', subject) }))}
                  onCustomBodyChange={(body) => setValues((current) => ({ ...current, customInviteNotification: updateCustomNotification(current.customInviteNotification, 'body', body) }))}
                />

                <NotificationTemplateSection
                  icon={<MailCheck className="size-4" aria-hidden="true" />}
                  title="Organizer confirmation"
                  description="Optionally notify organizers when a visitor confirms participation."
                  sendEnabled={values.sendConfirmNotificationToOrganizer}
                  sendLabel="Send confirmation to organizer"
                  customEnabled={values.useCustomConfirmNotification}
                  customSubject={values.customConfirmNotification?.subject ?? ''}
                  customBody={values.customConfirmNotification?.body ?? ''}
                  onSendEnabledChange={(checked) => setValues((current) => ({ ...current, sendConfirmNotificationToOrganizer: checked, useCustomConfirmNotification: checked ? current.useCustomConfirmNotification : false, customConfirmNotification: checked ? current.customConfirmNotification : null }))}
                  onCustomEnabledChange={(checked) => setValues((current) => ({ ...current, useCustomConfirmNotification: checked, customConfirmNotification: checked ? current.customConfirmNotification : null }))}
                  onCustomSubjectChange={(subject) => setValues((current) => ({ ...current, customConfirmNotification: updateCustomNotification(current.customConfirmNotification, 'subject', subject) }))}
                  onCustomBodyChange={(body) => setValues((current) => ({ ...current, customConfirmNotification: updateCustomNotification(current.customConfirmNotification, 'body', body) }))}
                />

                <NotificationTemplateSection
                  icon={<UserCheck className="size-4" aria-hidden="true" />}
                  title="Organizer arrival"
                  description="Optionally notify organizers when reception marks a visitor as arrived."
                  sendEnabled={values.sendArrivalNotificationToOrganizer}
                  sendLabel="Send arrival to organizer"
                  customEnabled={values.useCustomArrivalNotification}
                  customSubject={values.customArrivalNotification?.subject ?? ''}
                  customBody={values.customArrivalNotification?.body ?? ''}
                  onSendEnabledChange={(checked) => setValues((current) => ({ ...current, sendArrivalNotificationToOrganizer: checked, useCustomArrivalNotification: checked ? current.useCustomArrivalNotification : false, customArrivalNotification: checked ? current.customArrivalNotification : null }))}
                  onCustomEnabledChange={(checked) => setValues((current) => ({ ...current, useCustomArrivalNotification: checked, customArrivalNotification: checked ? current.customArrivalNotification : null }))}
                  onCustomSubjectChange={(subject) => setValues((current) => ({ ...current, customArrivalNotification: updateCustomNotification(current.customArrivalNotification, 'subject', subject) }))}
                  onCustomBodyChange={(body) => setValues((current) => ({ ...current, customArrivalNotification: updateCustomNotification(current.customArrivalNotification, 'body', body) }))}
                />

                <NotificationTemplateSection
                  icon={<Bell className="size-4" aria-hidden="true" />}
                  title="Cancellation"
                  description="Notify visitors when visit cancellation moves their onboarding saga into cancellation."
                  sendEnabled={values.sendCancellationNotification}
                  sendLabel="Send cancellation notification"
                  customEnabled={values.useCustomCancellationNotification}
                  customSubject={values.customCancellationNotification?.subject ?? ''}
                  customBody={values.customCancellationNotification?.body ?? ''}
                  onSendEnabledChange={(checked) => setValues((current) => ({ ...current, sendCancellationNotification: checked, useCustomCancellationNotification: checked ? current.useCustomCancellationNotification : false, customCancellationNotification: checked ? current.customCancellationNotification : null }))}
                  onCustomEnabledChange={(checked) => setValues((current) => ({ ...current, useCustomCancellationNotification: checked, customCancellationNotification: checked ? current.customCancellationNotification : null }))}
                  onCustomSubjectChange={(subject) => setValues((current) => ({ ...current, customCancellationNotification: updateCustomNotification(current.customCancellationNotification, 'subject', subject) }))}
                  onCustomBodyChange={(body) => setValues((current) => ({ ...current, customCancellationNotification: updateCustomNotification(current.customCancellationNotification, 'body', body) }))}
                />

                <NotificationTemplateSection
                  icon={<Bell className="size-4" aria-hidden="true" />}
                  title="Reschedule"
                  description="Notify visitors when a planned visit changes start time."
                  sendEnabled={values.sendRescheduleNotification}
                  sendLabel="Send reschedule notification"
                  customEnabled={values.useCustomRescheduleNotification}
                  customSubject={values.customRescheduleNotification?.subject ?? ''}
                  customBody={values.customRescheduleNotification?.body ?? ''}
                  onSendEnabledChange={(checked) => setValues((current) => ({ ...current, sendRescheduleNotification: checked, useCustomRescheduleNotification: checked ? current.useCustomRescheduleNotification : false, customRescheduleNotification: checked ? current.customRescheduleNotification : null }))}
                  onCustomEnabledChange={(checked) => setValues((current) => ({ ...current, useCustomRescheduleNotification: checked, customRescheduleNotification: checked ? current.customRescheduleNotification : null }))}
                  onCustomSubjectChange={(subject) => setValues((current) => ({ ...current, customRescheduleNotification: updateCustomNotification(current.customRescheduleNotification, 'subject', subject) }))}
                  onCustomBodyChange={(body) => setValues((current) => ({ ...current, customRescheduleNotification: updateCustomNotification(current.customRescheduleNotification, 'body', body) }))}
                />

                <NotificationTemplateSection
                  icon={<Bell className="size-4" aria-hidden="true" />}
                  title="Relocation"
                  description="Notify visitors when a planned visit changes location."
                  sendEnabled={values.sendRelocationNotification}
                  sendLabel="Send relocation notification"
                  customEnabled={values.useCustomRelocationNotification}
                  customSubject={values.customRelocationNotification?.subject ?? ''}
                  customBody={values.customRelocationNotification?.body ?? ''}
                  onSendEnabledChange={(checked) => setValues((current) => ({ ...current, sendRelocationNotification: checked, useCustomRelocationNotification: checked ? current.useCustomRelocationNotification : false, customRelocationNotification: checked ? current.customRelocationNotification : null }))}
                  onCustomEnabledChange={(checked) => setValues((current) => ({ ...current, useCustomRelocationNotification: checked, customRelocationNotification: checked ? current.customRelocationNotification : null }))}
                  onCustomSubjectChange={(subject) => setValues((current) => ({ ...current, customRelocationNotification: updateCustomNotification(current.customRelocationNotification, 'subject', subject) }))}
                  onCustomBodyChange={(body) => setValues((current) => ({ ...current, customRelocationNotification: updateCustomNotification(current.customRelocationNotification, 'body', body) }))}
                />
              </div>

              <div className="flex justify-end border-t border-border pt-6">
                <Button type="submit" className="w-full sm:w-auto" disabled={updateConfig.isPending || accessControlQrUnavailable}>
                  {updateConfig.isPending ? 'Saving...' : 'Save visitor journey'}
                </Button>
              </div>
            </form>
          ) : null}
        </CardContent>
      </Card>
    </section>
  );
}

function SelectField({ label, value, required, children, onChange }: { readonly label: string; readonly value: string; readonly required?: boolean; readonly children: ReactNode; readonly onChange: (value: string) => void }) {
  const id = useId();

  return (
    <div className="grid gap-2">
      <label className="text-[14px] font-medium" htmlFor={id}>{label}</label>
      <select
        id={id}
        className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20"
        value={value}
        required={required}
        onChange={(event) => onChange(event.target.value)}
      >
        {children}
      </select>
    </div>
  );
}

function NotificationTemplateSection({
  icon,
  title,
  description,
  sendEnabled,
  sendLabel,
  customEnabled,
  customSubject,
  customBody,
  onSendEnabledChange,
  onCustomEnabledChange,
  onCustomSubjectChange,
  onCustomBodyChange,
}: {
  readonly icon: ReactNode;
  readonly title: string;
  readonly description: string;
  readonly sendEnabled?: boolean;
  readonly sendLabel?: string;
  readonly customEnabled: boolean;
  readonly customSubject: string;
  readonly customBody: string;
  readonly onSendEnabledChange?: (checked: boolean) => void;
  readonly onCustomEnabledChange: (checked: boolean) => void;
  readonly onCustomSubjectChange: (value: string) => void;
  readonly onCustomBodyChange: (value: string) => void;
}) {
  const customTemplateId = useId();
  const customSubjectId = useId();
  const disabledBySendToggle = sendEnabled === false;
  const customFieldsDisabled = !customEnabled || disabledBySendToggle;

  return (
    <section className="grid gap-4 rounded-structural border border-border bg-background p-4">
      <div className="flex items-start gap-3">
        <div className="mt-0.5 rounded-interactive bg-hover-blue p-2 text-primary">{icon}</div>
        <div>
          <h2 className="text-[15px] font-semibold">{title}</h2>
          <p className="mt-1 text-[13px] leading-5 text-muted-foreground">{description}</p>
        </div>
      </div>

      {sendLabel && onSendEnabledChange ? (
        <CheckboxRow label={sendLabel} checked={sendEnabled ?? false} onChange={onSendEnabledChange} />
      ) : null}

      <CheckboxRow label="Use custom notification" checked={customEnabled} disabled={disabledBySendToggle} onChange={onCustomEnabledChange} />

      <div className="grid gap-2">
        <label className="text-[13px] font-medium" htmlFor={customSubjectId}>Custom subject</label>
        <Input
          id={customSubjectId}
          value={customSubject}
          onChange={(event) => onCustomSubjectChange(event.target.value)}
          disabled={customFieldsDisabled}
          required={!customFieldsDisabled}
          placeholder="Email subject"
        />
      </div>

      <div className="grid gap-2">
        <label className="text-[13px] font-medium" htmlFor={customTemplateId}>Custom HTML</label>
        <Textarea
          id={customTemplateId}
          value={customBody}
          onChange={(event) => onCustomBodyChange(event.target.value)}
          disabled={customFieldsDisabled}
          required={!customFieldsDisabled}
          placeholder="Paste full email HTML here."
          spellCheck={false}
          className="font-mono text-[13px]"
        />
      </div>
    </section>
  );
}

function CheckboxRow({ label, checked, disabled, onChange }: { readonly label: string; readonly checked: boolean; readonly disabled?: boolean; readonly onChange: (checked: boolean) => void }) {
  return (
    <label className="flex items-center gap-3 rounded-interactive border border-border bg-content px-3 py-2 text-[14px] font-medium">
      <input
        type="checkbox"
        className="size-4 accent-primary disabled:cursor-not-allowed disabled:opacity-50"
        checked={checked}
        disabled={disabled}
        onChange={(event) => onChange(event.target.checked)}
      />
      {label}
    </label>
  );
}

function StatusPill({ mode }: { readonly mode: CredentialGenerationMode }) {
  return (
    <div className="w-fit rounded-full border border-border bg-hover-blue px-3 py-1 text-[12px] font-semibold text-primary">
      {mode === 'PlatformQr' ? 'Platform QR' : 'Access control QR'}
    </div>
  );
}

function toRequest(config: VisitorPreOnboardingSagaConfigRequest): VisitorPreOnboardingSagaConfigRequest {
  return {
    useCustomInviteNotification: config.useCustomInviteNotification,
    customInviteNotification: config.customInviteNotification,
    qrGenerationMode: config.qrGenerationMode,
    systemId: config.systemId,
    badgeTypeId: config.badgeTypeId,
    sendConfirmNotificationToOrganizer: config.sendConfirmNotificationToOrganizer,
    useCustomConfirmNotification: config.useCustomConfirmNotification,
    customConfirmNotification: config.customConfirmNotification,
    sendCancellationNotification: config.sendCancellationNotification,
    useCustomCancellationNotification: config.useCustomCancellationNotification,
    customCancellationNotification: config.customCancellationNotification,
    sendRescheduleNotification: config.sendRescheduleNotification,
    useCustomRescheduleNotification: config.useCustomRescheduleNotification,
    customRescheduleNotification: config.customRescheduleNotification,
    sendRelocationNotification: config.sendRelocationNotification,
    useCustomRelocationNotification: config.useCustomRelocationNotification,
    customRelocationNotification: config.customRelocationNotification,
    sendArrivalNotificationToOrganizer: config.sendArrivalNotificationToOrganizer,
    useCustomArrivalNotification: config.useCustomArrivalNotification,
    customArrivalNotification: config.customArrivalNotification,
  };
}

function normalize(config: VisitorPreOnboardingSagaConfigRequest): VisitorPreOnboardingSagaConfigRequest {
  return {
    ...config,
    systemId: config.qrGenerationMode === 'AccessControlQr' ? config.systemId : null,
    badgeTypeId: config.qrGenerationMode === 'AccessControlQr' ? config.badgeTypeId : null,
    customInviteNotification: normalizeNotification(config.useCustomInviteNotification, config.customInviteNotification),
    customConfirmNotification: normalizeNotification(config.useCustomConfirmNotification, config.customConfirmNotification),
    customCancellationNotification: normalizeNotification(config.useCustomCancellationNotification, config.customCancellationNotification),
    customRescheduleNotification: normalizeNotification(config.useCustomRescheduleNotification, config.customRescheduleNotification),
    customRelocationNotification: normalizeNotification(config.useCustomRelocationNotification, config.customRelocationNotification),
    customArrivalNotification: normalizeNotification(config.useCustomArrivalNotification, config.customArrivalNotification),
  };
}

function updateCustomNotification(notification: CustomNotification | null, field: 'subject' | 'body', value: string): CustomNotification {
  return {
    subject: field === 'subject' ? value : notification?.subject ?? '',
    body: field === 'body' ? value : notification?.body ?? '',
  };
}

function normalizeNotification(enabled: boolean, notification: CustomNotification | null) {
  if (!enabled) {
    return null;
  }

  const subject = notification?.subject.trim() ?? '';
  const body = notification?.body.trim() ?? '';

  if (!subject || !body) {
    return null;
  }

  return { subject, body };
}
