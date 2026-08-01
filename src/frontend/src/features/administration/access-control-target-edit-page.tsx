import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { AccessControlProviderBadge } from '@/shared/components/access-control-provider-badge';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

type AccessControlSystemResponse = components['schemas']['AccessControlSystemResponse'];
type AccessLevelTargetResponse = components['schemas']['AccessLevelTargetResponse'];
type ProvisioningTiming = components['schemas']['ProvisioningTiming'];
type SystemMetadata = components['schemas']['SystemMetadata'];
type UpdateUnipassAccessLevelTargetRequest = components['schemas']['UpdateUnipassAccessLevelTargetRequest'];

type FormValues = {
  name: string;
  siteId: string;
  accessRuleId: string;
  isEnabled: boolean;
  provisioningTiming: ProvisioningTiming;
};

const accessLevelTargetsQueryKey = ['administration', 'access-control', 'targets'] as const;
const accessControlSystemsQueryKey = ['administration', 'access-control', 'systems'] as const;

export default function AccessControlTargetEditPage() {
  const { itemId, targetId } = useParams({ from: '/main/administration/access-control/items/$itemId/targets/$targetId/edit' });
  const queryClient = useQueryClient();
  const [values, setValues] = useState<FormValues>({ name: '', siteId: '', accessRuleId: '', isEnabled: true, provisioningTiming: 'Eager' });

  const targetsQuery = useQuery({
    queryKey: [...accessLevelTargetsQueryKey, itemId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/items/{itemId}/targets', { params: { path: { itemId }, query: { Page: 0, PageSize: 200 } } });
      if (error) throw new Error('Could not load access control targets.');
      return data;
    },
  });

  const systemsQuery = useQuery({
    queryKey: [...accessControlSystemsQueryKey, 'options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/systems', { params: { query: { Name: undefined, Page: 0, PageSize: 200 } as never } });
      if (error) throw new Error('Could not load access control systems.');
      return data?.items ?? [];
    },
  });

  const target = (targetsQuery.data?.items ?? []).find((item) => item.id === targetId);

  const metadataQuery = useQuery({
    queryKey: [...accessControlSystemsQueryKey, target?.accessControlSystemId ?? '', 'metadata'],
    enabled: Boolean(target?.accessControlSystemId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/systems/{systemId}/metadata', { params: { path: { systemId: target?.accessControlSystemId ?? '' } } });
      if (error || !data) throw new Error('Could not load system metadata.');
      return data;
    },
  });

  useEffect(() => {
    if (!target) {
      return;
    }

    setValues({
      name: target.name,
      siteId: String(target.siteId),
      accessRuleId: String(target.accessRuleId),
      isEnabled: target.isEnabled,
      provisioningTiming: target.provisioningTiming,
    });
  }, [target]);

  const updateTarget = useMutation({
    mutationFn: async (request: UpdateUnipassAccessLevelTargetRequest) => {
      const { error } = await api.PUT('/api/access-control/items/targets/unipass/{targetId}', { params: { path: { targetId } }, body: request });
      if (error) throw new Error('Could not save access control target.');
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...accessLevelTargetsQueryKey, itemId] });
      toast.success('Access control target saved.');
    },
    onError: () => toast.error('Could not save access control target.'),
  });

  const systemsById = new Map((systemsQuery.data ?? []).map((system) => [system.id, system]));
  const system = target ? systemsById.get(target.accessControlSystemId) : undefined;
  const isMetadataReady = isUnipassMetadata(metadataQuery.data);
  const unipassMetadata = isMetadataReady ? metadataQuery.data : undefined;

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!target || !values.name.trim() || !values.siteId || !values.accessRuleId) {
      return;
    }

    updateTarget.mutate({
      name: values.name,
      siteId: Number(values.siteId),
      accessRuleId: Number(values.accessRuleId),
      isEnabled: values.isEnabled,
      provisioningTiming: values.provisioningTiming,
    });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Edit access control target</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update target details for this access item. System type cannot be changed.</p>
        </div>
      </header>

      <Card className="p-6">
        {targetsQuery.isError || systemsQuery.isError || metadataQuery.isError || updateTarget.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{targetsQuery.isError ? 'Could not load access control target.' : systemsQuery.isError ? 'Could not load access control systems.' : metadataQuery.isError ? 'Could not load system metadata.' : 'Could not save access control target.'}</p> : null}
        {targetsQuery.isLoading || systemsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading access control target...</p> : null}

        {!targetsQuery.isLoading && !systemsQuery.isLoading && target ? (
          system?.providerKind === 'Unipass' ? (
            <form className="grid gap-5" onSubmit={handleSubmit}>
              <div className="grid gap-4 md:grid-cols-2">
                <label className="grid gap-2 text-[14px] font-medium">
                  System
                  <div className="h-10 rounded-interactive border border-border bg-background px-3 py-2 text-[14px] text-foreground">
                    <div className="flex items-center gap-2"><AccessControlProviderBadge providerKind={system.providerKind} /><span>{system.name}</span></div>
                  </div>
                </label>

                <label className="grid gap-2 text-[14px] font-medium">
                  Provisioning Timing
                  <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.provisioningTiming} onChange={(event) => setValues((current) => ({ ...current, provisioningTiming: event.target.value as ProvisioningTiming }))}>
                    <option value="Eager">Eager</option>
                    <option value="AtValidFrom">At Valid From</option>
                  </select>
                </label>

                <label className="grid gap-2 text-[14px] font-medium">
                  Name
                  <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.name} onChange={(event) => setValues((current) => ({ ...current, name: event.target.value }))} required />
                </label>

                <label className="grid gap-2 text-[14px] font-medium">
                  Status
                  <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.isEnabled ? 'Enabled' : 'Disabled'} onChange={(event) => setValues((current) => ({ ...current, isEnabled: event.target.value === 'Enabled' }))}>
                    <option value="Enabled">Enabled</option>
                    <option value="Disabled">Disabled</option>
                  </select>
                </label>

                <label className="grid gap-2 text-[14px] font-medium">
                  Site
                  <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={values.siteId} onChange={(event) => setValues((current) => ({ ...current, siteId: event.target.value }))} disabled={metadataQuery.isLoading || !isMetadataReady}>
                    <option value="">Select site</option>
                    {unipassMetadata ? unipassMetadata.sites.map((site) => <option key={site.id} value={String(site.id)}>{site.name}</option>) : null}
                  </select>
                  {metadataQuery.isLoading ? <span className="text-[12px] text-muted-foreground">Loading system metadata...</span> : null}
                </label>

                <label className="grid gap-2 text-[14px] font-medium">
                  Access Rule
                  <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={values.accessRuleId} onChange={(event) => setValues((current) => ({ ...current, accessRuleId: event.target.value }))} disabled={metadataQuery.isLoading || !isMetadataReady}>
                    <option value="">Select access rule</option>
                    {unipassMetadata ? unipassMetadata.accessRules.map((rule) => <option key={rule.id} value={String(rule.id)}>{rule.name}</option>) : null}
                  </select>
                  {metadataQuery.isLoading ? <span className="text-[12px] text-muted-foreground">Loading system metadata...</span> : null}
                </label>
              </div>

              <div className="flex justify-end">
                <Button type="submit" disabled={updateTarget.isPending || metadataQuery.isLoading || !isMetadataReady || !values.name.trim() || !values.siteId || !values.accessRuleId}>{updateTarget.isPending ? 'Saving...' : metadataQuery.isLoading ? 'Loading metadata...' : 'Save'}</Button>
              </div>
            </form>
          ) : (
            <div className="rounded-structural border border-border bg-background p-4 text-[14px] text-muted-foreground">Editing for this target type is not supported yet.</div>
          )
        ) : null}
      </Card>
    </div>
  );
}

function isUnipassMetadata(metadata: SystemMetadata | undefined): metadata is components['schemas']['SystemMetadataUnipassMetadata'] {
  return metadata?.type === 'unipass';
}
