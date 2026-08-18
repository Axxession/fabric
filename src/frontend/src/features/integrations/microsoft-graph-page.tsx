import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Mail } from 'lucide-react';
import { useEffect, useState, type ReactNode, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { adminTenantSettingsQueryKey, buildUpdateTenantSettingsRequest } from '@/features/integrations/integration-settings';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { fetchAdminTenantSettings, updateAdminTenantSettings } from '@/shared/tenant/tenant-settings';

type GraphFormValues = {
  fromEmail: string;
  fromName: string;
  azureTenantId: string;
  applicationId: string;
  secret: string;
  saveSentItems: boolean;
};

const emptyValues: GraphFormValues = {
  fromEmail: '',
  fromName: '',
  azureTenantId: '',
  applicationId: '',
  secret: '',
  saveSentItems: false,
};

export default function MicrosoftGraphPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const settingsQuery = useQuery({
    queryKey: adminTenantSettingsQueryKey,
    queryFn: fetchAdminTenantSettings,
  });
  const [values, setValues] = useState<GraphFormValues>(emptyValues);

  useEffect(() => {
    if (!settingsQuery.data) {
      return;
    }

    setValues({
      fromEmail: settingsQuery.data.email?.fromEmail ?? '',
      fromName: settingsQuery.data.email?.fromName ?? '',
      azureTenantId: settingsQuery.data.email?.azureTenantId ?? '',
      applicationId: settingsQuery.data.email?.applicationId ?? '',
      secret: '',
      saveSentItems: settingsQuery.data.email?.saveSentItems ?? false,
    });
  }, [settingsQuery.data]);

  const saveSettings = useMutation({
    mutationFn: async () => {
      if (!settingsQuery.data) {
        throw new Error('Tenant settings are not loaded.');
      }

      return await updateAdminTenantSettings(buildUpdateTenantSettingsRequest(settingsQuery.data, {
        email: {
          fromEmail: values.fromEmail,
          fromName: values.fromName,
          azureTenantId: values.azureTenantId,
          applicationId: values.applicationId,
          secret: values.secret,
          saveSentItems: values.saveSentItems,
        },
      }));
    },
    onSuccess: async (data) => {
      queryClient.setQueryData(adminTenantSettingsQueryKey, data);
      await queryClient.invalidateQueries({ queryKey: adminTenantSettingsQueryKey });
      toast.success(t('integrationsSettings.microsoftGraph.saveSuccess'));
      setValues((current) => ({ ...current, secret: '' }));
    },
    onError: () => {
      toast.error(t('integrationsSettings.microsoftGraph.saveError'));
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    saveSettings.mutate();
  }

  const hasSecret = settingsQuery.data?.email?.hasSecret ?? false;

  return (
    <section className="grid gap-6 rounded-structural border border-border bg-content p-4 sm:p-6">
      <div>
        <p className="text-[13px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Integrations</p>
        <h1 className="mt-3 flex items-center gap-3 text-[32px] font-semibold tracking-tight"><Mail className="size-8 text-primary" />{t('integrationsSettings.microsoftGraph.title')}</h1>
        <p className="mt-3 max-w-2xl text-[14px] text-muted-foreground">{t('integrationsSettings.microsoftGraph.description')}</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('integrationsSettings.microsoftGraph.cardTitle')}</CardTitle>
          <CardDescription>{t('integrationsSettings.microsoftGraph.cardDescription')}</CardDescription>
        </CardHeader>
        <CardContent>
          {settingsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">{t('integrationsSettings.common.loading')}</p> : null}
          {settingsQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">{t('integrationsSettings.microsoftGraph.loadError')}</p> : null}

          {!settingsQuery.isLoading && !settingsQuery.isError ? (
            <form className="grid gap-6" onSubmit={handleSubmit}>
              <div className="grid gap-4 md:grid-cols-2">
                <Field label={t('integrationsSettings.microsoftGraph.fields.fromEmail')}>
                  <Input type="email" value={values.fromEmail} onChange={(event) => setValues((current) => ({ ...current, fromEmail: event.target.value }))} />
                </Field>
                <Field label={t('integrationsSettings.microsoftGraph.fields.fromName')}>
                  <Input value={values.fromName} onChange={(event) => setValues((current) => ({ ...current, fromName: event.target.value }))} />
                </Field>
                <Field label={t('integrationsSettings.microsoftGraph.fields.azureTenantId')}>
                  <Input value={values.azureTenantId} onChange={(event) => setValues((current) => ({ ...current, azureTenantId: event.target.value }))} />
                </Field>
                <Field label={t('integrationsSettings.microsoftGraph.fields.applicationId')}>
                  <Input value={values.applicationId} onChange={(event) => setValues((current) => ({ ...current, applicationId: event.target.value }))} />
                </Field>
                <Field label={t('integrationsSettings.microsoftGraph.fields.secret')} className="md:col-span-2">
                  <Input type="password" value={values.secret} onChange={(event) => setValues((current) => ({ ...current, secret: event.target.value }))} />
                  <p className="mt-2 text-[12px] text-muted-foreground">{hasSecret ? t('integrationsSettings.microsoftGraph.hints.secretConfigured') : t('integrationsSettings.microsoftGraph.hints.secretRequired')}</p>
                </Field>
              </div>

              <label className="flex items-center gap-3 text-[14px] font-medium text-foreground">
                <input type="checkbox" className="size-4 rounded border border-border" checked={values.saveSentItems} onChange={(event) => setValues((current) => ({ ...current, saveSentItems: event.target.checked }))} />
                {t('integrationsSettings.microsoftGraph.fields.saveSentItems')}
              </label>

              <div className="flex justify-end border-t border-border pt-6">
                <Button type="submit" className="w-full sm:w-auto" disabled={saveSettings.isPending}>
                  {saveSettings.isPending ? t('integrationsSettings.common.saving') : t('integrationsSettings.microsoftGraph.save')}
                </Button>
              </div>
            </form>
          ) : null}
        </CardContent>
      </Card>
    </section>
  );
}

function Field({ label, className, children }: { readonly label: string; readonly className?: string; readonly children: ReactNode }) {
  return <label className={`grid gap-2 text-[14px] font-medium text-foreground ${className ?? ''}`}><span>{label}</span>{children}</label>;
}
