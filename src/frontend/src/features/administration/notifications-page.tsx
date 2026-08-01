import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useLocation, useNavigate } from '@tanstack/react-router';
import { Bell, MailCheck, Send, UserCheck } from 'lucide-react';
import { useEffect, useId, useState, type FormEvent, type ReactNode } from 'react';
import { toast } from 'sonner';

import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { Textarea } from '@/shared/components/ui/textarea';

import {
  fetchVisitorPreOnboardingConfig,
  getDefaultVisitorPreOnboardingConfig,
  updateVisitorPreOnboardingConfig,
  visitorPreOnboardingConfigQueryKey,
  type CustomNotification,
  type VisitorPreOnboardingSagaConfigRequest,
} from '@/features/settings/visitor-pre-onboarding-config';

type NotificationsTab = 'visitors';

export default function NotificationsPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = getActiveTab(location.searchStr);

  function changeTab(nextTab: string) {
    if (!isNotificationsTab(nextTab)) {
      return;
    }

    void navigate({ to: '/administration/notifications', search: { tab: nextTab } as never, replace: true });
  }

  return (
    <section className="rounded-structural border border-border bg-content p-4 sm:p-6">
      <Tabs value={activeTab} onValueChange={changeTab}>
        <div className="grid gap-4 border-b border-border pb-6">
          <div>
            <p className="text-[13px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Administration</p>
            <h1 className="mt-3 text-[32px] font-semibold tracking-tight">Notifications</h1>
            <p className="mt-3 max-w-2xl text-[14px] text-muted-foreground">Manage notification templates and delivery toggles for operational workflows.</p>
          </div>

          <TabsList>
            <TabsTrigger value="visitors">Visitors</TabsTrigger>
          </TabsList>
        </div>

        <TabsContent value="visitors" className="pt-6">
          <VisitorNotificationsPanel />
        </TabsContent>
      </Tabs>
    </section>
  );
}

function VisitorNotificationsPanel() {
  const queryClient = useQueryClient();
  const configQuery = useQuery({
    queryKey: visitorPreOnboardingConfigQueryKey,
    queryFn: fetchVisitorPreOnboardingConfig,
  });
  const [values, setValues] = useState<VisitorPreOnboardingSagaConfigRequest>(getDefaultVisitorPreOnboardingConfig);

  useEffect(() => {
    if (configQuery.data) {
      setValues(configQuery.data);
    }
  }, [configQuery.data]);

  const updateConfig = useMutation({
    mutationFn: updateVisitorPreOnboardingConfig,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: visitorPreOnboardingConfigQueryKey });
      toast.success('Visitor notifications saved.');
    },
    onError: () => {
      toast.error('Could not save visitor notifications.');
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    updateConfig.mutate(normalize(values));
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-[20px]">Visitor notifications</CardTitle>
        <CardDescription>Configure invitation and host notification templates used by visitor pre-onboarding.</CardDescription>
      </CardHeader>
      <CardContent>
        {configQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading visitor notifications...</p> : null}
        {configQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load visitor notifications.</p> : null}

        {!configQuery.isLoading && !configQuery.isError ? (
          <form className="grid gap-6" onSubmit={handleSubmit}>
            <div className="grid gap-4 lg:grid-cols-2">
              <NotificationTemplateSection
                icon={<Send className="size-4" aria-hidden="true" />}
                title="Invitation"
                description="Sent to visitors after arrival registration and QR setup. Default template is used unless custom HTML is enabled."
                customEnabled={values.useCustomInviteNotification}
                customSubject={values.customInviteNotification?.subject ?? ''}
                customBody={values.customInviteNotification?.body ?? ''}
                onCustomEnabledChange={(checked) => setValues((current) => ({ ...current, useCustomInviteNotification: checked, customInviteNotification: checked ? current.customInviteNotification : null }))}
                onCustomSubjectChange={(subject) => setValues((current) => ({ ...current, customInviteNotification: updateCustomNotification(current.customInviteNotification, 'subject', subject) }))}
                onCustomBodyChange={(body) => setValues((current) => ({ ...current, customInviteNotification: updateCustomNotification(current.customInviteNotification, 'body', body) }))}
              />

              <NotificationTemplateSection
                icon={<MailCheck className="size-4" aria-hidden="true" />}
                title="Host confirmation"
                description="Optionally notify hosts when visitors confirm participation."
                sendEnabled={values.sendConfirmNotificationToHost}
                sendLabel="Send confirmation to host"
                customEnabled={values.useCustomConfirmNotification}
                customSubject={values.customConfirmNotification?.subject ?? ''}
                customBody={values.customConfirmNotification?.body ?? ''}
                onSendEnabledChange={(checked) => setValues((current) => ({ ...current, sendConfirmNotificationToHost: checked, useCustomConfirmNotification: checked ? current.useCustomConfirmNotification : false, customConfirmNotification: checked ? current.customConfirmNotification : null }))}
                onCustomEnabledChange={(checked) => setValues((current) => ({ ...current, useCustomConfirmNotification: checked, customConfirmNotification: checked ? current.customConfirmNotification : null }))}
                onCustomSubjectChange={(subject) => setValues((current) => ({ ...current, customConfirmNotification: updateCustomNotification(current.customConfirmNotification, 'subject', subject) }))}
                onCustomBodyChange={(body) => setValues((current) => ({ ...current, customConfirmNotification: updateCustomNotification(current.customConfirmNotification, 'body', body) }))}
              />

              <NotificationTemplateSection
                icon={<UserCheck className="size-4" aria-hidden="true" />}
                title="Host arrival"
                description="Optionally notify hosts when reception marks visitors as arrived."
                sendEnabled={values.sendArrivalNotificationToHost}
                sendLabel="Send arrival to host"
                customEnabled={values.useCustomArrivalNotification}
                customSubject={values.customArrivalNotification?.subject ?? ''}
                customBody={values.customArrivalNotification?.body ?? ''}
                onSendEnabledChange={(checked) => setValues((current) => ({ ...current, sendArrivalNotificationToHost: checked, useCustomArrivalNotification: checked ? current.useCustomArrivalNotification : false, customArrivalNotification: checked ? current.customArrivalNotification : null }))}
                onCustomEnabledChange={(checked) => setValues((current) => ({ ...current, useCustomArrivalNotification: checked, customArrivalNotification: checked ? current.customArrivalNotification : null }))}
                onCustomSubjectChange={(subject) => setValues((current) => ({ ...current, customArrivalNotification: updateCustomNotification(current.customArrivalNotification, 'subject', subject) }))}
                onCustomBodyChange={(body) => setValues((current) => ({ ...current, customArrivalNotification: updateCustomNotification(current.customArrivalNotification, 'body', body) }))}
              />

              <NotificationTemplateSection
                icon={<Bell className="size-4" aria-hidden="true" />}
                title="Cancellation"
                description="Notify visitors when visit cancellation moves onboarding saga into cancellation."
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
                description="Notify visitors when planned visit changes start time."
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
                description="Notify visitors when planned visit changes location."
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
              <Button type="submit" className="w-full sm:w-auto" disabled={updateConfig.isPending}>
                {updateConfig.isPending ? 'Saving...' : 'Save notifications'}
              </Button>
            </div>
          </form>
        ) : null}
      </CardContent>
    </Card>
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

      {sendLabel && onSendEnabledChange ? <CheckboxRow label={sendLabel} checked={sendEnabled ?? false} onChange={onSendEnabledChange} /> : null}

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

function normalize(config: VisitorPreOnboardingSagaConfigRequest): VisitorPreOnboardingSagaConfigRequest {
  return {
    ...config,
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

function getActiveTab(searchStr: string): NotificationsTab {
  const tab = new URLSearchParams(searchStr).get('tab');
  return isNotificationsTab(tab) ? tab : 'visitors';
}

function isNotificationsTab(value: string | null): value is NotificationsTab {
  return value === 'visitors';
}
