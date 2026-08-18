import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { KeyRound } from 'lucide-react';
import { useEffect, useState, type ReactNode, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { adminTenantSettingsQueryKey, buildUpdateTenantSettingsRequest } from '@/features/integrations/integration-settings';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { fetchAdminTenantSettings, updateAdminTenantSettings } from '@/shared/tenant/tenant-settings';

type KeycloakFormValues = {
  url: string;
  realm: string;
  clientId: string;
  clientSecret: string;
};

const emptyValues: KeycloakFormValues = {
  url: '',
  realm: '',
  clientId: '',
  clientSecret: '',
};

export default function KeycloakPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const settingsQuery = useQuery({
    queryKey: adminTenantSettingsQueryKey,
    queryFn: fetchAdminTenantSettings,
  });
  const [values, setValues] = useState<KeycloakFormValues>(emptyValues);

  useEffect(() => {
    if (!settingsQuery.data) {
      return;
    }

    setValues({
      url: settingsQuery.data.keycloak?.url ?? '',
      realm: settingsQuery.data.keycloak?.realm ?? '',
      clientId: settingsQuery.data.keycloak?.clientId ?? '',
      clientSecret: '',
    });
  }, [settingsQuery.data]);

  const saveSettings = useMutation({
    mutationFn: async () => {
      if (!settingsQuery.data) {
        throw new Error('Tenant settings are not loaded.');
      }

      return await updateAdminTenantSettings(buildUpdateTenantSettingsRequest(settingsQuery.data, {
        keycloak: {
          url: values.url,
          realm: values.realm,
          clientId: values.clientId,
          clientSecret: values.clientSecret,
        },
      }));
    },
    onSuccess: async (data) => {
      queryClient.setQueryData(adminTenantSettingsQueryKey, data);
      await queryClient.invalidateQueries({ queryKey: adminTenantSettingsQueryKey });
      toast.success(t('integrationsSettings.keycloak.saveSuccess'));
      setValues((current) => ({ ...current, clientSecret: '' }));
    },
    onError: () => {
      toast.error(t('integrationsSettings.keycloak.saveError'));
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    saveSettings.mutate();
  }

  const hasClientSecret = settingsQuery.data?.keycloak?.hasClientSecret ?? false;

  return (
    <section className="grid gap-6 rounded-structural border border-border bg-content p-4 sm:p-6">
      <div>
        <p className="text-[13px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Integrations</p>
        <h1 className="mt-3 flex items-center gap-3 text-[32px] font-semibold tracking-tight"><KeyRound className="size-8 text-primary" />{t('integrationsSettings.keycloak.title')}</h1>
        <p className="mt-3 max-w-2xl text-[14px] text-muted-foreground">{t('integrationsSettings.keycloak.description')}</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('integrationsSettings.keycloak.cardTitle')}</CardTitle>
          <CardDescription>{t('integrationsSettings.keycloak.cardDescription')}</CardDescription>
        </CardHeader>
        <CardContent>
          {settingsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">{t('integrationsSettings.common.loading')}</p> : null}
          {settingsQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">{t('integrationsSettings.keycloak.loadError')}</p> : null}

          {!settingsQuery.isLoading && !settingsQuery.isError ? (
            <form className="grid gap-6" onSubmit={handleSubmit}>
              <div className="grid gap-4 md:grid-cols-2">
                <Field label={t('integrationsSettings.keycloak.fields.url')} className="md:col-span-2">
                  <Input value={values.url} onChange={(event) => setValues((current) => ({ ...current, url: event.target.value }))} />
                </Field>
                <Field label={t('integrationsSettings.keycloak.fields.realm')}>
                  <Input value={values.realm} onChange={(event) => setValues((current) => ({ ...current, realm: event.target.value }))} />
                </Field>
                <Field label={t('integrationsSettings.keycloak.fields.clientId')}>
                  <Input value={values.clientId} onChange={(event) => setValues((current) => ({ ...current, clientId: event.target.value }))} />
                </Field>
                <Field label={t('integrationsSettings.keycloak.fields.clientSecret')} className="md:col-span-2">
                  <Input type="password" value={values.clientSecret} onChange={(event) => setValues((current) => ({ ...current, clientSecret: event.target.value }))} />
                  <p className="mt-2 text-[12px] text-muted-foreground">{hasClientSecret ? t('integrationsSettings.keycloak.hints.clientSecretConfigured') : t('integrationsSettings.keycloak.hints.clientSecretRequired')}</p>
                </Field>
              </div>

              <div className="flex justify-end border-t border-border pt-6">
                <Button type="submit" className="w-full sm:w-auto" disabled={saveSettings.isPending}>
                  {saveSettings.isPending ? t('integrationsSettings.common.saving') : t('integrationsSettings.keycloak.save')}
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
