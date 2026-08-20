import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { type ReactNode, useState } from 'react';

import { createPlatformTenant, platformTenantsQueryKey, type PlatformTenantUpsertValues } from '@/features/platform/platform-tenants';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

const initialValues: PlatformTenantUpsertValues & { id: string } = {
  id: '',
  displayName: '',
  oidc: {
    metadataUrl: '',
    clientId: '',
    requireHttpsMetadata: true,
  },
};

export default function PlatformTenantCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [values, setValues] = useState(initialValues);
  const [error, setError] = useState<string | null>(null);
  const createTenant = useMutation({
    mutationFn: () => createPlatformTenant(values),
    onSuccess: async (tenant) => {
      await queryClient.invalidateQueries({ queryKey: platformTenantsQueryKey });
      await navigate({ to: '/platform/tenants/$tenantId', params: { tenantId: tenant.id } });
    },
    onError: (mutationError) => setError(mutationError instanceof Error ? mutationError.message : 'Could not create tenant.'),
  });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <button type="button" onClick={() => void navigate({ to: '/platform/tenants' })} className="inline-flex size-10 items-center justify-center rounded-interactive border border-border bg-content text-foreground transition hover:bg-hover-blue" aria-label="Go back">
          <ArrowLeft className="size-4" />
        </button>
        <div>
          <h1 className="text-[30px] font-semibold tracking-tight">Create tenant</h1>
          <p className="mt-3 max-w-2xl text-[14px] text-muted-foreground">Provision a tenant record, platform identity settings, and activation state.</p>
        </div>
      </header>

      <Card>
        <CardHeader>
          <CardTitle>Tenant registration</CardTitle>
          <CardDescription>Start with tenant identity and OIDC settings. Theme and logo stay out of scope for now.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-5">
          {error ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">{error}</p> : null}
          <div className="grid gap-5 md:grid-cols-2">
            <Field label="Tenant id"><Input value={values.id} onChange={(event) => setValues((current) => ({ ...current, id: event.target.value }))} placeholder="acme" /></Field>
            <Field label="Display name"><Input value={values.displayName} onChange={(event) => setValues((current) => ({ ...current, displayName: event.target.value }))} placeholder="Acme Industries" /></Field>
          </div>
          <div className="grid gap-5 md:grid-cols-[1fr_220px]">
            <Field label="OIDC metadata URL"><Input value={values.oidc.metadataUrl} onChange={(event) => setValues((current) => ({ ...current, oidc: { ...current.oidc, metadataUrl: event.target.value } }))} placeholder="https://id.example.com/realms/acme/.well-known/openid-configuration" /></Field>
            <Field label="OIDC client id"><Input value={values.oidc.clientId} onChange={(event) => setValues((current) => ({ ...current, oidc: { ...current.oidc, clientId: event.target.value } }))} placeholder="portal" /></Field>
          </div>
          <label className="inline-flex items-center gap-3 text-[14px] font-medium text-foreground">
            <input type="checkbox" checked={values.oidc.requireHttpsMetadata} onChange={(event) => setValues((current) => ({ ...current, oidc: { ...current.oidc, requireHttpsMetadata: event.target.checked } }))} className="size-4 rounded border border-border" />
            Require HTTPS metadata
          </label>
        </CardContent>
        <div className="flex justify-end gap-3 border-t border-border px-5 pt-5">
          <Button type="button" variant="outline" onClick={() => void navigate({ to: '/platform/tenants' })}>Cancel</Button>
          <Button type="button" onClick={() => { setError(null); createTenant.mutate(); }} disabled={createTenant.isPending}>{createTenant.isPending ? 'Creating...' : 'Create tenant'}</Button>
        </div>
      </Card>
    </div>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return <label className="grid gap-2 text-[13px] font-semibold text-foreground"><span>{label}</span>{children}</label>;
}
