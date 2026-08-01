import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft, ChevronRight, Plus } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { AccessControlProviderBadge } from '@/shared/components/access-control-provider-badge';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { AccessItemForm, type AccessItemFormValues } from './access-item-form';

type AccessItemResponse = components['schemas']['AccessItemResponse'];
type AccessControlSystemResponse = components['schemas']['AccessControlSystemResponse'];
type AccessLevelTargetResponse = components['schemas']['AccessLevelTargetResponse'];
type ApprovalDefinitionResponse = components['schemas']['ApprovalDefinitionResponse'];
type ApprovalGroupResponse = components['schemas']['ApprovalGroupResponse'];
type CreateUnipassAccessLevelTargetRequest = components['schemas']['CreateUnipassAccessLevelTargetRequest'];
type CreateApprovalDefinitionRequest = components['schemas']['CreateApprovalDefinitionRequest'];
type OrganizationalApprovalMode = components['schemas']['OrganizationalApprovalMode'];
type ProvisioningTiming = components['schemas']['ProvisioningTiming'];
type SystemMetadata = components['schemas']['SystemMetadata'];
type UpdateAccessItemRequest = components['schemas']['UpdateAccessItemRequest'];
type UpdateApprovalDefinitionRequest = components['schemas']['UpdateApprovalDefinitionRequest'];

const accessItemsQueryKey = ['administration', 'access-control', 'items'] as const;
const accessControlSystemsQueryKey = ['administration', 'access-control', 'systems'] as const;
const accessLevelTargetsQueryKey = ['administration', 'access-control', 'targets'] as const;
const approvalDefinitionsQueryKey = ['administration', 'access-model', 'approval-definitions'] as const;
const approvalGroupsQueryKey = ['administration', 'access-model', 'approval-groups'] as const;

type ApprovalFormValues = {
  destinationApprovalGroupId: string;
  organizationalApprovalMode: OrganizationalApprovalMode;
  organizationalApprovalLevels: string;
};

export default function AccessItemEditPage() {
  const { itemId } = useParams({ from: '/main/administration/access-control/items/$itemId/edit' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [approvalValues, setApprovalValues] = useState<ApprovalFormValues>({
    destinationApprovalGroupId: '',
    organizationalApprovalMode: 'None',
    organizationalApprovalLevels: '1',
  });
  const [isAddTargetOpen, setIsAddTargetOpen] = useState(false);
  const [selectedSystemId, setSelectedSystemId] = useState('');
  const [targetName, setTargetName] = useState('');
  const [selectedSiteId, setSelectedSiteId] = useState('');
  const [selectedAccessRuleId, setSelectedAccessRuleId] = useState('');
  const [provisioningTiming, setProvisioningTiming] = useState<ProvisioningTiming>('Eager');

  const accessItemQuery = useQuery({
    queryKey: [...accessItemsQueryKey, itemId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/items/{itemId}', { params: { path: { itemId } } });
      if (error || !data) {
        throw new Error('Could not load access item.');
      }
      return data;
    },
  });

  const approvalDefinitionsQuery = useQuery({
    queryKey: [...approvalDefinitionsQueryKey, itemId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/approval-definitions', {
        params: { query: { Page: 0, PageSize: 200, ids: [] } },
      });
      if (error) {
        throw new Error('Could not load approval definitions.');
      }
      return data?.items ?? [];
    },
  });

  const approvalGroupsQuery = useQuery({
    queryKey: [...approvalGroupsQueryKey, 'options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/approval-groups', {
        params: { query: { Name: undefined, ids: [], Page: 0, PageSize: 200 } as never },
      });
      if (error) {
        throw new Error('Could not load approval groups.');
      }
      return data?.items ?? [];
    },
  });

  const systemsQuery = useQuery({
    queryKey: [...accessControlSystemsQueryKey, 'options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/systems', {
        params: { query: { Name: undefined, Page: 0, PageSize: 200 } as never },
      });
      if (error) {
        throw new Error('Could not load access control systems.');
      }
      return data?.items ?? [];
    },
  });

  const targetsQuery = useQuery({
    queryKey: [...accessLevelTargetsQueryKey, itemId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/items/{itemId}/targets', { params: { path: { itemId }, query: { Page: 0, PageSize: 200 } } });
      if (error) {
        throw new Error('Could not load access control targets.');
      }
      return data;
    },
  });

  const targetMetadataQuery = useQuery({
    queryKey: [...accessControlSystemsQueryKey, selectedSystemId, 'metadata'],
    enabled: isAddTargetOpen && selectedSystemId !== '',
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/systems/{systemId}/metadata', { params: { path: { systemId: selectedSystemId } } });
      if (error || !data) {
        throw new Error('Could not load system metadata.');
      }
      return data;
    },
  });

  const currentApprovalDefinition = (approvalDefinitionsQuery.data ?? []).find((item) => item.accessItemId === itemId);

  useEffect(() => {
    if (!currentApprovalDefinition) {
      setApprovalValues({ destinationApprovalGroupId: '', organizationalApprovalMode: 'None', organizationalApprovalLevels: '1' });
      return;
    }

    setApprovalValues({
      destinationApprovalGroupId: currentApprovalDefinition.destinationApprovalGroupId ?? '',
      organizationalApprovalMode: currentApprovalDefinition.organizationalApprovalMode,
      organizationalApprovalLevels: String(currentApprovalDefinition.organizationalApprovalLevels ?? 1),
    });
  }, [currentApprovalDefinition]);

  const updateAccessItem = useMutation({
    mutationFn: async (values: AccessItemFormValues) => {
      const request: UpdateAccessItemRequest = {
        name: values.name,
        description: values.description.trim() === '' ? null : values.description,
        status: values.status,
      };

      const { error } = await api.PUT('/api/access-control/items/{itemId}', { params: { path: { itemId } }, body: request });
      if (error) {
        throw new Error('Could not save access item.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: accessItemsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...accessItemsQueryKey, itemId] }),
      ]);
      toast.success('Access item saved.');
    },
    onError: () => {
      toast.error('Could not save access item.');
    },
  });

  const saveApprovalDefinition = useMutation({
    mutationFn: async () => {
      if (currentApprovalDefinition) {
        const updateRequest: UpdateApprovalDefinitionRequest = {
          destinationApprovalGroupId: nullIfEmpty(approvalValues.destinationApprovalGroupId),
          organizationalApprovalMode: approvalValues.organizationalApprovalMode,
          organizationalApprovalLevels: Number(approvalValues.organizationalApprovalLevels || '1'),
        };

        const { error } = await api.PUT('/api/access-catalog/approval-definitions/{approvalDefinitionId}', {
          params: { path: { approvalDefinitionId: currentApprovalDefinition.id } },
          body: updateRequest,
        });

        if (error) {
          throw new Error('Could not save approval definition.');
        }

        return;
      }

      const createRequest: CreateApprovalDefinitionRequest = {
        accessItemId: itemId,
        destinationApprovalGroupId: nullIfEmpty(approvalValues.destinationApprovalGroupId),
        organizationalApprovalMode: approvalValues.organizationalApprovalMode,
        organizationalApprovalLevels: Number(approvalValues.organizationalApprovalLevels || '1'),
      };

      const { error } = await api.POST('/api/access-catalog/approval-definitions', { body: createRequest });
      if (error) {
        throw new Error('Could not create approval definition.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: approvalDefinitionsQueryKey });
      toast.success('Approval settings saved.');
    },
    onError: () => {
      toast.error('Could not save approval settings.');
    },
  });

  const createTarget = useMutation({
    mutationFn: async (request: CreateUnipassAccessLevelTargetRequest) => {
      const { error } = await api.POST('/api/access-control/items/{itemId}/targets/unipass', { params: { path: { itemId } }, body: request });
      if (error) {
        throw new Error('Could not create access control target.');
      }
    },
    onSuccess: async () => {
      setIsAddTargetOpen(false);
      setSelectedSystemId('');
      setTargetName('');
      setSelectedSiteId('');
      setSelectedAccessRuleId('');
      setProvisioningTiming('Eager');
      await queryClient.invalidateQueries({ queryKey: [...accessLevelTargetsQueryKey, itemId] });
      toast.success('Access control target created.');
    },
    onError: () => {
      toast.error('Could not create access control target.');
    },
  });

  const systems = systemsQuery.data ?? [];
  const systemsById = new Map(systems.map((system) => [system.id, system]));
  const selectedSystem = systems.find((system) => system.id === selectedSystemId);
  const targets = targetsQuery.data?.items ?? [];
  const metadata = targetMetadataQuery.data;
  const isMetadataReady = isUnipassMetadata(metadata);

  useEffect(() => {
    setSelectedSiteId('');
    setSelectedAccessRuleId('');
  }, [selectedSystemId]);

  function handleCreateTarget() {
    if (!selectedSystemId || !targetName.trim() || !selectedSiteId || !selectedAccessRuleId) {
      return;
    }

    createTarget.mutate({
      accessControlSystemId: selectedSystemId,
      name: targetName,
      siteId: Number(selectedSiteId),
      accessRuleId: Number(selectedAccessRuleId),
      provisioningTiming,
    });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>

        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Edit access item</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update access item details and status.</p>
        </div>
      </header>

      <Card className="p-6">
        {accessItemQuery.isError || updateAccessItem.isError ? (
          <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {accessItemQuery.isError ? 'Could not load access item.' : 'Could not save access item.'}
          </p>
        ) : null}

        {accessItemQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading access item...</p> : null}

        {!accessItemQuery.isLoading && accessItemQuery.data && !accessItemQuery.isError ? (
          <AccessItemForm initialValues={toFormValues(accessItemQuery.data)} isSubmitting={updateAccessItem.isPending} submitLabel="Save" includeStatus onSubmit={(values) => updateAccessItem.mutate(values)} />
        ) : null}
      </Card>

      <Card className="p-6">
        {approvalDefinitionsQuery.isError || approvalGroupsQuery.isError || saveApprovalDefinition.isError ? (
          <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {approvalDefinitionsQuery.isError ? 'Could not load approval definitions.' : approvalGroupsQuery.isError ? 'Could not load approval groups.' : 'Could not save approval settings.'}
          </p>
        ) : null}

        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Approval</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Configure organizational approval and destination approval group for this access item.</p>
          </div>
          <Button type="button" disabled={saveApprovalDefinition.isPending || approvalDefinitionsQuery.isLoading || approvalGroupsQuery.isLoading} onClick={() => saveApprovalDefinition.mutate()}>
            {saveApprovalDefinition.isPending ? 'Saving...' : 'Save approval'}
          </Button>
        </div>

        {approvalDefinitionsQuery.isLoading || approvalGroupsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading approval settings...</p> : null}

        {!approvalDefinitionsQuery.isLoading && !approvalGroupsQuery.isLoading ? (
          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              Destination Approval Group
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={approvalValues.destinationApprovalGroupId} onChange={(event) => setApprovalValues((current) => ({ ...current, destinationApprovalGroupId: event.target.value }))}>
                <option value="">No destination approval</option>
                {(approvalGroupsQuery.data ?? []).map((group: ApprovalGroupResponse) => <option key={group.id} value={group.id}>{group.name}</option>)}
              </select>
            </label>

            <label className="grid gap-2 text-[14px] font-medium">
              Organizational Approval Mode
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={approvalValues.organizationalApprovalMode} onChange={(event) => setApprovalValues((current) => ({ ...current, organizationalApprovalMode: event.target.value as OrganizationalApprovalMode }))}>
                <option value="None">None</option>
                <option value="ManagerChain">Manager Chain</option>
              </select>
            </label>

            <label className="grid gap-2 text-[14px] font-medium">
              Organizational Approval Levels
              <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" type="number" min={1} value={approvalValues.organizationalApprovalLevels} onChange={(event) => setApprovalValues((current) => ({ ...current, organizationalApprovalLevels: event.target.value }))} disabled={approvalValues.organizationalApprovalMode === 'None'} />
            </label>
          </div>
        ) : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Access Control Targets</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Link access control targets for this access item.</p>
          </div>
          <Button type="button" variant="outline" disabled={createTarget.isPending || systemsQuery.isLoading || systems.length === 0} onClick={() => setIsAddTargetOpen((current) => !current)}>
            <Plus className="size-4" aria-hidden="true" />
            {isAddTargetOpen ? 'Cancel' : 'Add target'}
          </Button>
        </div>

        {isAddTargetOpen ? (
          <div className="grid gap-4 rounded-structural border border-border p-4">
            <label className="grid gap-2 text-[14px] font-medium md:max-w-md">
              <span>Access Control System</span>
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedSystemId} onChange={(event) => setSelectedSystemId(event.target.value)}>
                <option value="">Select system</option>
                {systems.map((system: AccessControlSystemResponse) => <option key={system.id} value={system.id}>{system.name}</option>)}
              </select>
            </label>

            {targetMetadataQuery.isError ? (
              <p className="rounded-interactive border border-warning bg-background px-4 py-3 text-[14px] text-warning" role="alert">
                Could not load system metadata. Check the selected system before creating a target.
              </p>
            ) : null}

            {selectedSystem ? (
              selectedSystem.providerKind === 'Unipass' ? (
                <div className="grid gap-4 md:grid-cols-2">
                  <label className="grid gap-2 text-[14px] font-medium">
                    <span>Name</span>
                    <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={targetName} onChange={(event) => setTargetName(event.target.value)} />
                  </label>
                  <label className="grid gap-2 text-[14px] font-medium">
                    <span>Provisioning Timing</span>
                    <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={provisioningTiming} onChange={(event) => setProvisioningTiming(event.target.value as ProvisioningTiming)}>
                      <option value="Eager">Eager</option>
                      <option value="AtValidFrom">At Valid From</option>
                    </select>
                  </label>
                  <label className="grid gap-2 text-[14px] font-medium">
                    <span>Site</span>
                    <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={selectedSiteId} onChange={(event) => setSelectedSiteId(event.target.value)} disabled={targetMetadataQuery.isLoading || !isMetadataReady}>
                      <option value="">Select site</option>
                      {isMetadataReady ? metadata.sites.map((site) => <option key={site.id} value={String(site.id)}>{site.name}</option>) : null}
                    </select>
                    {targetMetadataQuery.isLoading ? <span className="text-[12px] text-muted-foreground">Loading system metadata...</span> : null}
                    {!targetMetadataQuery.isLoading && !isMetadataReady && !targetMetadataQuery.isError ? <span className="text-[12px] text-muted-foreground">Select a supported system to load available sites.</span> : null}
                  </label>
                  <label className="grid gap-2 text-[14px] font-medium">
                    <span>Access Rule</span>
                    <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={selectedAccessRuleId} onChange={(event) => setSelectedAccessRuleId(event.target.value)} disabled={targetMetadataQuery.isLoading || !isMetadataReady}>
                      <option value="">Select access rule</option>
                      {isMetadataReady ? metadata.accessRules.map((rule) => <option key={rule.id} value={String(rule.id)}>{rule.name}</option>) : null}
                    </select>
                    {targetMetadataQuery.isLoading ? <span className="text-[12px] text-muted-foreground">Loading system metadata...</span> : null}
                    {!targetMetadataQuery.isLoading && !isMetadataReady && !targetMetadataQuery.isError ? <span className="text-[12px] text-muted-foreground">Select a supported system to load available access rules.</span> : null}
                  </label>
                  <div className="md:col-span-2 flex justify-end">
                    <Button type="button" disabled={createTarget.isPending || targetMetadataQuery.isLoading || !isMetadataReady || !selectedSystemId || !targetName.trim() || !selectedSiteId || !selectedAccessRuleId} onClick={handleCreateTarget}>{createTarget.isPending ? 'Creating...' : targetMetadataQuery.isLoading ? 'Loading metadata...' : 'Create target'}</Button>
                  </div>
                </div>
              ) : (
                <div className="rounded-structural border border-border bg-background p-4 text-[14px] text-muted-foreground">This system type is not supported yet.</div>
              )
            ) : null}
          </div>
        ) : null}

        {targetsQuery.isError || systemsQuery.isError || targetMetadataQuery.isError || createTarget.isError ? (
          <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {targetsQuery.isError ? 'Could not load access control targets.' : systemsQuery.isError ? 'Could not load access control systems.' : targetMetadataQuery.isError ? 'Could not load system metadata.' : 'Could not create access control target.'}
          </p>
        ) : null}

        {targetsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading access control targets...</p> : null}

        {!targetsQuery.isLoading && targets.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No access control targets linked yet.</p> : null}

        {targets.length > 0 ? (
          <div className="grid gap-3">
            {targets.map((target: AccessLevelTargetResponse) => (
              <article key={target.id} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => void navigate({ to: '/administration/access-control/items/$itemId/targets/$targetId/edit', params: { itemId, targetId: target.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/access-control/items/$itemId/targets/$targetId/edit', params: { itemId, targetId: target.id } }); } }}>
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <p className="font-medium text-foreground">{target.name}</p>
                    <p className="mt-1 text-[14px] text-muted-foreground">{systemsById.get(target.accessControlSystemId)?.name ?? target.accessControlSystemId}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <AccessControlProviderBadge providerKind={systemsById.get(target.accessControlSystemId)?.providerKind ?? 'Unipass'} />
                    <Badge variant={target.isEnabled ? 'success' : 'secondary'}>{target.isEnabled ? 'Enabled' : 'Disabled'}</Badge>
                    <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
                  </div>
                </div>
                <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground md:grid-cols-3">
                  <div><dt className="font-medium text-foreground">Site</dt><dd>{target.siteName}</dd></div>
                  <div><dt className="font-medium text-foreground">Access Rule</dt><dd>{target.accessRuleName}</dd></div>
                  <div><dt className="font-medium text-foreground">Provisioning Timing</dt><dd>{target.provisioningTiming}</dd></div>
                </dl>
              </article>
            ))}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function toFormValues(accessItem: AccessItemResponse): AccessItemFormValues {
  return {
    name: accessItem.name,
    description: accessItem.description ?? '',
    status: accessItem.status,
  };
}

function nullIfEmpty(value: string) {
  return value.trim() === '' ? null : value;
}

function isUnipassMetadata(metadata: SystemMetadata | undefined): metadata is components['schemas']['SystemMetadataUnipassMetadata'] {
  return metadata?.type === 'unipass';
}
