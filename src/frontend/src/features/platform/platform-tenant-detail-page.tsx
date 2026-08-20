import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { type ReactNode, useEffect, useState } from 'react';

import { activatePlatformTenant, deactivatePlatformTenant, fetchPlatformTenant, platformTenantsQueryKey, updatePlatformTenant, type PlatformTenantUpsertValues } from '@/features/platform/platform-tenants';
import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { cn } from '@/shared/utils/cn';

export default function PlatformTenantDetailPage() {
  const { tenantId } = useParams({ from: '/platform/tenants/$tenantId' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [values, setValues] = useState<PlatformTenantUpsertValues | null>(null);
  const [error, setError] = useState<string | null>(null);
  const tenantQuery = useQuery({
    queryKey: [...platformTenantsQueryKey, tenantId],
    queryFn: () => fetchPlatformTenant(tenantId),
  });

  useEffect(() => {
    if (!tenantQuery.data) {
      return;
    }

    setValues({
      displayName: tenantQuery.data.displayName,
      oidc: tenantQuery.data.oidc,
    });
  }, [tenantQuery.data]);

  const saveTenant = useMutation({
    mutationFn: () => updatePlatformTenant(tenantId, values!),
    onSuccess: async (tenant) => {
      setValues({ displayName: tenant.displayName, oidc: tenant.oidc });
      await queryClient.invalidateQueries({ queryKey: platformTenantsQueryKey });
      await queryClient.invalidateQueries({ queryKey: [...platformTenantsQueryKey, tenantId] });
    },
    onError: (mutationError) => setError(mutationError instanceof Error ? mutationError.message : 'Could not save tenant.'),
  });

  const toggleActive = useMutation({
    mutationFn: () => tenantQuery.data?.isActive ? deactivatePlatformTenant(tenantId) : activatePlatformTenant(tenantId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: platformTenantsQueryKey });
      await queryClient.invalidateQueries({ queryKey: [...platformTenantsQueryKey, tenantId] });
    },
    onError: (mutationError) => setError(mutationError instanceof Error ? mutationError.message : 'Could not update tenant state.'),
  });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <button type="button" onClick={() => void navigate({ to: '/platform/tenants' })} className="inline-flex size-10 items-center justify-center rounded-interactive border border-border bg-content text-foreground transition hover:bg-hover-blue" aria-label="Go back">
          <ArrowLeft className="size-4" />
        </button>
        <div className="min-w-0">
          <h1 className="truncate text-[30px] font-semibold tracking-tight">{tenantQuery.data?.displayName ?? 'Tenant detail'}</h1>
          <p className="mt-3 max-w-3xl text-[14px] text-muted-foreground">Review tenant identity setup, current integrations, and activation state.</p>
        </div>
      </header>

      {tenantQuery.isLoading ? <Card className="p-6 text-[14px] text-muted-foreground">Loading tenant...</Card> : null}
      {tenantQuery.isError ? <Card className="border-error/40 p-6 text-[14px] text-error">{tenantQuery.error instanceof Error ? tenantQuery.error.message : 'Could not load tenant.'}</Card> : null}

      {tenantQuery.data && values ? (
        <>
          <Card>
            <CardHeader>
              <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div>
                  <CardTitle>{tenantQuery.data.id}</CardTitle>
                  <CardDescription>Created {formatDateTime(tenantQuery.data.createdAtUtc)}. Updated {formatDateTime(tenantQuery.data.updatedAtUtc)}.</CardDescription>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Badge variant={tenantQuery.data.isActive ? 'success' : 'warning'}>{tenantQuery.data.isActive ? 'Active' : 'Deactivated'}</Badge>
                  <Badge variant="secondary">Host mode {tenantQuery.data.host.assignmentMode}</Badge>
                </div>
              </div>
            </CardHeader>
            <CardContent className="grid gap-5">
              {error ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">{error}</p> : null}
              <div className="grid gap-5 md:grid-cols-2">
                <Field label="Display name"><Input value={values.displayName} onChange={(event) => setValues((current) => current ? { ...current, displayName: event.target.value } : current)} /></Field>
                <Field label="OIDC client id"><Input value={values.oidc.clientId} onChange={(event) => setValues((current) => current ? { ...current, oidc: { ...current.oidc, clientId: event.target.value } } : current)} /></Field>
              </div>
              <Field label="OIDC metadata URL"><Input value={values.oidc.metadataUrl} onChange={(event) => setValues((current) => current ? { ...current, oidc: { ...current.oidc, metadataUrl: event.target.value } } : current)} /></Field>
              <label className="inline-flex items-center gap-3 text-[14px] font-medium text-foreground">
                <input type="checkbox" checked={values.oidc.requireHttpsMetadata} onChange={(event) => setValues((current) => current ? { ...current, oidc: { ...current.oidc, requireHttpsMetadata: event.target.checked } } : current)} className="size-4 rounded border border-border" />
                Require HTTPS metadata
              </label>
            </CardContent>
            <div className="flex flex-col gap-3 border-t border-border px-5 pt-5 lg:flex-row lg:items-center lg:justify-between">
              <div className="flex flex-wrap gap-3">
                <Button type="button" variant={tenantQuery.data.isActive ? 'destructive' : 'secondary'} onClick={() => { setError(null); toggleActive.mutate(); }} disabled={toggleActive.isPending}>{toggleActive.isPending ? 'Updating...' : tenantQuery.data.isActive ? 'Deactivate tenant' : 'Activate tenant'}</Button>
                <Link to="/platform/tenants" className={cn(buttonVariants({ variant: 'outline' }), 'inline-flex')}>Back to tenants</Link>
              </div>
              <Button type="button" onClick={() => { setError(null); saveTenant.mutate(); }} disabled={saveTenant.isPending}>{saveTenant.isPending ? 'Saving...' : 'Save changes'}</Button>
            </div>
          </Card>

          <div className="grid gap-6 xl:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Integration summary</CardTitle>
                <CardDescription>Platform view of tenant-side external admin connections.</CardDescription>
              </CardHeader>
              <CardContent className="grid gap-4">
                <IntegrationSummary title="Keycloak" summary={tenantQuery.data.keycloak} />
                <IntegrationSummary title="Microsoft Graph" summary={tenantQuery.data.microsoftGraph} />
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Stored tenant config</CardTitle>
                <CardDescription>Read-only values outside the current editing scope.</CardDescription>
              </CardHeader>
              <CardContent className="grid gap-4 text-[14px] text-muted-foreground">
                <ReadOnlyRow label="Theme primary color" value={tenantQuery.data.theme.primaryColor} />
                <ReadOnlyRow label="Theme background color" value={tenantQuery.data.theme.backgroundColor} />
                <ReadOnlyRow label="Logo" value={tenantQuery.data.logo ? `Configured (${tenantQuery.data.logo.contentType})` : 'Not configured'} />
                <ReadOnlyRow label="Host assignment mode" value={tenantQuery.data.host.assignmentMode} />
              </CardContent>
            </Card>
          </div>
        </>
      ) : null}
    </div>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return <label className="grid gap-2 text-[13px] font-semibold text-foreground"><span>{label}</span>{children}</label>;
}

function IntegrationSummary({ title, summary }: { title: string; summary: { isConfigured: boolean; isEnabled: boolean; hasSecret: boolean; updatedAtUtc: string | null } }) {
  return (
    <div className="rounded-interactive border border-border bg-background px-4 py-4">
      <div className="flex flex-wrap items-center gap-2">
        <h3 className="text-[15px] font-semibold text-foreground">{title}</h3>
        <Badge variant={summary.isEnabled ? 'success' : 'outline'}>{summary.isEnabled ? 'Enabled' : 'Disabled'}</Badge>
        <Badge variant={summary.isConfigured ? 'secondary' : 'warning'}>{summary.isConfigured ? 'Configured' : 'Needs setup'}</Badge>
      </div>
      <div className="mt-3 grid gap-1 text-[13px] text-muted-foreground">
        <p>Secret stored: {summary.hasSecret ? 'Yes' : 'No'}</p>
        <p>Last updated: {summary.updatedAtUtc ? formatDateTime(summary.updatedAtUtc) : 'Never'}</p>
      </div>
    </div>
  );
}

function ReadOnlyRow({ label, value }: { label: string; value: string }) {
  return <div className="flex items-start justify-between gap-4"><span className="text-foreground">{label}</span><span className="text-right">{value}</span></div>;
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString();
}
