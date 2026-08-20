import { Link } from '@tanstack/react-router';
import { useQuery } from '@tanstack/react-query';
import { Building2, Plus } from 'lucide-react';
import { useMemo, useState } from 'react';

import { fetchPlatformTenants, platformTenantsQueryKey } from '@/features/platform/platform-tenants';
import { Badge } from '@/shared/components/ui/badge';
import { buttonVariants } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { cn } from '@/shared/utils/cn';

export default function PlatformTenantsPage() {
  const [search, setSearch] = useState('');
  const tenantsQuery = useQuery({
    queryKey: platformTenantsQueryKey,
    queryFn: fetchPlatformTenants,
  });

  const filteredTenants = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) {
      return tenantsQuery.data ?? [];
    }

    return (tenantsQuery.data ?? []).filter((tenant) =>
      tenant.displayName.toLowerCase().includes(query)
      || tenant.id.toLowerCase().includes(query)
      || tenant.oidc.clientId.toLowerCase().includes(query),
    );
  }, [search, tenantsQuery.data]);

  return (
    <div className="grid gap-6">
      <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-[12px] font-semibold uppercase tracking-[0.18em] text-primary">Platform</p>
          <h1 className="mt-3 text-[30px] font-semibold tracking-tight text-foreground">Tenant Directory</h1>
          <p className="mt-3 max-w-3xl text-[14px] text-muted-foreground">Create, activate, deactivate, and inspect tenant-wide identity settings from one platform shell.</p>
        </div>
        <Link to="/platform/tenants/new" className={cn(buttonVariants(), 'inline-flex')}><Plus className="size-4" />New tenant</Link>
      </header>

      <Card className="gap-4 p-5 sm:p-6">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-[18px] font-semibold tracking-tight">All tenants</h2>
            <p className="mt-1 text-[13px] text-muted-foreground">Cross-tenant overview with OIDC, activity state, and last update.</p>
          </div>
          <Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search by tenant, display name, or client id" className="md:max-w-sm" />
        </div>

        {tenantsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading tenants...</p> : null}
        {tenantsQuery.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">{tenantsQuery.error instanceof Error ? tenantsQuery.error.message : 'Could not load tenants.'}</p> : null}

        {!tenantsQuery.isLoading && !tenantsQuery.isError ? (
          <div className="grid gap-3">
            {filteredTenants.length === 0 ? <div className="rounded-structural border border-dashed border-border bg-background px-5 py-8 text-[14px] text-muted-foreground">No tenants match the current search.</div> : null}
            {filteredTenants.map((tenant) => (
              <Link key={tenant.id} to="/platform/tenants/$tenantId" params={{ tenantId: tenant.id }} className="rounded-structural border border-border bg-content px-5 py-4 transition hover:border-primary/30 hover:bg-hover-blue">
                <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-3">
                      <div className="inline-flex size-10 items-center justify-center rounded-[14px] bg-hover-blue text-primary"><Building2 className="size-5" /></div>
                      <div className="min-w-0">
                        <h3 className="truncate text-[16px] font-semibold tracking-tight text-foreground">{tenant.displayName}</h3>
                        <p className="truncate text-[13px] text-muted-foreground">{tenant.id}</p>
                      </div>
                    </div>
                    <div className="mt-4 flex flex-wrap gap-2">
                      <Badge variant={tenant.isActive ? 'success' : 'warning'}>{tenant.isActive ? 'Active' : 'Deactivated'}</Badge>
                      <Badge variant="secondary">Client {tenant.oidc.clientId}</Badge>
                      <Badge variant="outline">Host mode {tenant.host.assignmentMode}</Badge>
                    </div>
                  </div>
                  <div className="grid gap-2 text-[13px] text-muted-foreground lg:text-right">
                    <div>
                      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-faint-foreground">OIDC metadata</p>
                      <p className="mt-1 break-all text-foreground">{tenant.oidc.metadataUrl}</p>
                    </div>
                    <p>Updated {formatDateTime(tenant.updatedAtUtc)}</p>
                  </div>
                </div>
              </Link>
            ))}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString();
}
