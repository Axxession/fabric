import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft, Pencil, Plus } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { AccessControlProviderBadge } from '@/shared/components/access-control-provider-badge';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { fromTimeSpan, getDefaultDurationInputValue, toTimeSpan } from '@/shared/components/ui/duration-input';

import { CredentialTypeForm, type CredentialTypeFormValues } from './credential-type-form';

type CredentialRangeResponse = components['schemas']['CredentialRangeResponse'];
type CredentialTypeResponse = components['schemas']['CredentialTypeResponse'];
type CredentialTypeTargetResponse = components['schemas']['CredentialTypeTargetResponse'];
type CreateCredentialRangeRequest = components['schemas']['CreateCredentialRangeRequest'];
type CreateUnipassCredentialTypeTargetRequest = components['schemas']['CreateUnipassCredentialTypeTargetRequest'];
type AccessControlSystemResponse = components['schemas']['AccessControlSystemResponse'];
type UpdateCredentialRangeRequest = components['schemas']['UpdateCredentialRangeRequest'];
type UpdateCredentialTypeRequest = components['schemas']['UpdateCredentialTypeRequest'];
type UpdateUnipassCredentialTypeTargetRequest = components['schemas']['UpdateUnipassCredentialTypeTargetRequest'];

const credentialTypesQueryKey = ['administration', 'credential-types'] as const;

type RangeFormValues = {
  rangeStart: string;
  rangeStop: string;
  isActive: boolean;
};

type SupportedPacsFormValues = {
  accessControlSystemId: string;
  provisioningTiming: components['schemas']['ProvisioningTiming'];
  isEnabled: boolean;
};

const emptyRange: RangeFormValues = { rangeStart: '', rangeStop: '', isActive: true };
const emptySupportedPacs: SupportedPacsFormValues = { accessControlSystemId: '', provisioningTiming: 'Eager', isEnabled: true };

export default function CredentialTypeEditPage() {
  const { credentialTypeId } = useParams({ from: '/main/administration/credential-types/$credentialTypeId/edit' });
  const queryClient = useQueryClient();
  const [values, setValues] = useState<CredentialTypeFormValues>({
    name: '',
    technology: 'Qr',
    allocationMode: 'Range',
    recyclePolicy: 'NeverReuse',
    recycleGracePeriod: getDefaultDurationInputValue(),
    requiresConfirmedPacsRevocation: false,
    nearLimitThreshold: '',
    identifierPrefix: '',
    identifierSuffix: '',
    identifierNumberLength: '',
    identifierPaddingDirection: 'Left',
    identifierPaddingCharacter: '',
    status: 'Active',
  });
  const [isRangeEditorOpen, setIsRangeEditorOpen] = useState(false);
  const [editingRangeId, setEditingRangeId] = useState<string | null>(null);
  const [rangeValues, setRangeValues] = useState<RangeFormValues>(emptyRange);
  const [isTargetEditorOpen, setIsTargetEditorOpen] = useState(false);
  const [editingTargetId, setEditingTargetId] = useState<string | null>(null);
  const [targetValues, setTargetValues] = useState<SupportedPacsFormValues>(emptySupportedPacs);

  const credentialTypeQuery = useQuery({
    queryKey: [...credentialTypesQueryKey, credentialTypeId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/credential-management/credential-types/{id}', { params: { path: { id: credentialTypeId } } });
      if (error || !data) {
        throw new Error('Could not load credential type.');
      }

      return data;
    },
  });

  const credentialTargetsQuery = useQuery({
    queryKey: [...credentialTypesQueryKey, credentialTypeId, 'targets'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/credential-type-targets', {
        params: { query: { CredentialTypeId: credentialTypeId, AccessControlSystemId: undefined, Page: 0, PageSize: 200 } as never },
      });
      if (error) {
        throw new Error('Could not load supported PACS mappings.');
      }

      return data?.items ?? [];
    },
  });

  const systemsQuery = useQuery({
    queryKey: ['administration', 'access-control', 'systems', 'options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/systems', { params: { query: { Name: undefined, Page: 0, PageSize: 200 } as never } });
      if (error) {
        throw new Error('Could not load access control systems.');
      }

      return data?.items ?? [];
    },
  });

  useEffect(() => {
    if (!credentialTypeQuery.data) {
      return;
    }

    setValues(toFormValues(credentialTypeQuery.data));
  }, [credentialTypeQuery.data]);

  const updateCredentialType = useMutation({
    mutationFn: async (request: UpdateCredentialTypeRequest) => {
      const { error } = await api.PUT('/api/credential-management/credential-types/{id}', { params: { path: { id: credentialTypeId } }, body: request });
      if (error) {
        throw new Error('Could not save credential type.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: credentialTypesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...credentialTypesQueryKey, credentialTypeId] }),
      ]);
      toast.success('Credential type saved.');
    },
    onError: () => {
      toast.error('Could not save credential type.');
    },
  });

  const setCredentialTypeStatus = useMutation({
    mutationFn: async (status: components['schemas']['CredentialTypeStatus']) => {
      const request = status === 'Active'
        ? api.POST('/api/credential-management/credential-types/{id}/activate', { params: { path: { id: credentialTypeId } } })
        : api.POST('/api/credential-management/credential-types/{id}/disable', { params: { path: { id: credentialTypeId } } });

      const { error } = await request;
      if (error) {
        throw new Error('Could not update credential type status.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: credentialTypesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...credentialTypesQueryKey, credentialTypeId] }),
      ]);
    },
  });

  const createRange = useMutation({
    mutationFn: async (request: CreateCredentialRangeRequest) => {
      const { error } = await api.POST('/api/credential-management/credential-types/{id}/ranges', { params: { path: { id: credentialTypeId } }, body: request });
      if (error) {
        throw new Error('Could not create range.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...credentialTypesQueryKey, credentialTypeId] });
      setRangeValues(emptyRange);
      setIsRangeEditorOpen(false);
      toast.success('Range added.');
    },
    onError: () => toast.error('Could not add range.'),
  });

  const updateRange = useMutation({
    mutationFn: async ({ rangeId, request }: { readonly rangeId: string; readonly request: UpdateCredentialRangeRequest }) => {
      const { error } = await api.PUT('/api/credential-management/credential-types/ranges/{rangeId}', { params: { path: { rangeId } }, body: request });
      if (error) {
        throw new Error('Could not save range.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...credentialTypesQueryKey, credentialTypeId] });
      setRangeValues(emptyRange);
      setEditingRangeId(null);
      setIsRangeEditorOpen(false);
      toast.success('Range saved.');
    },
    onError: () => toast.error('Could not save range.'),
  });

  const createTarget = useMutation({
    mutationFn: async (request: CreateUnipassCredentialTypeTargetRequest) => {
      const { error } = await api.POST('/api/access-control/credential-type-targets/unipass', { body: request });
      if (error) {
        throw new Error('Could not add supported PACS mapping.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...credentialTypesQueryKey, credentialTypeId, 'targets'] });
      setTargetValues(emptySupportedPacs);
      setEditingTargetId(null);
      setIsTargetEditorOpen(false);
      toast.success('Supported PACS mapping added.');
    },
    onError: () => toast.error('Could not add supported PACS mapping.'),
  });

  const updateTarget = useMutation({
    mutationFn: async ({ targetId, request }: { readonly targetId: string; readonly request: UpdateUnipassCredentialTypeTargetRequest }) => {
      const { error } = await api.PUT('/api/access-control/credential-type-targets/unipass/{targetId}', { params: { path: { targetId } }, body: request });
      if (error) {
        throw new Error('Could not save supported PACS mapping.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...credentialTypesQueryKey, credentialTypeId, 'targets'] });
      setTargetValues(emptySupportedPacs);
      setEditingTargetId(null);
      setIsTargetEditorOpen(false);
      toast.success('Supported PACS mapping saved.');
    },
    onError: () => toast.error('Could not save supported PACS mapping.'),
  });

  async function handleSubmit(nextValues: CredentialTypeFormValues) {
    const currentStatus = credentialTypeQuery.data?.status;

    await updateCredentialType.mutateAsync({
      name: nextValues.name.trim(),
      technology: nextValues.technology,
      allocationMode: nextValues.allocationMode,
      recyclePolicy: nextValues.allocationMode === 'Provided' ? 'NeverReuse' : nextValues.recyclePolicy,
      recycleGracePeriod: toTimeSpan(nextValues.recycleGracePeriod),
      requiresConfirmedPacsRevocation: nextValues.allocationMode === 'Provided' ? false : nextValues.requiresConfirmedPacsRevocation,
      nearLimitThreshold: nextValues.nearLimitThreshold.trim() === '' ? null : Number(nextValues.nearLimitThreshold),
      identifierPrefix: nextValues.technology === 'Qr' ? nextValues.identifierPrefix.trim() || null : null,
      identifierSuffix: nextValues.technology === 'Qr' ? nextValues.identifierSuffix.trim() || null : null,
      identifierNumberLength: nextValues.technology === 'Qr' && nextValues.identifierNumberLength.trim() !== '' ? Number(nextValues.identifierNumberLength) : null,
      identifierPaddingDirection: nextValues.technology === 'Qr' && nextValues.identifierNumberLength.trim() !== '' ? nextValues.identifierPaddingDirection : null,
      identifierPaddingCharacter: nextValues.technology === 'Qr' && nextValues.identifierNumberLength.trim() !== '' ? nextValues.identifierPaddingCharacter || null : null,
    });

    if (currentStatus && currentStatus !== nextValues.status) {
      await setCredentialTypeStatus.mutateAsync(nextValues.status);
      toast.success(`Credential type ${nextValues.status === 'Active' ? 'activated' : 'disabled'}.`);
    }
  }

  function handleSaveRange() {
    const request = {
      rangeStart: Number(rangeValues.rangeStart),
      rangeStop: Number(rangeValues.rangeStop),
      isActive: rangeValues.isActive,
    } satisfies CreateCredentialRangeRequest;

    if (editingRangeId) {
      updateRange.mutate({ rangeId: editingRangeId, request });
      return;
    }

    createRange.mutate(request);
  }

  function startEditRange(range: CredentialRangeResponse) {
    setEditingRangeId(range.id);
    setRangeValues({ rangeStart: String(range.rangeStart), rangeStop: String(range.rangeStop), isActive: range.isActive });
    setIsRangeEditorOpen(true);
  }

  function handleSaveTarget() {
    if (!targetValues.accessControlSystemId) {
      return;
    }

    if (editingTargetId) {
      updateTarget.mutate({
        targetId: editingTargetId,
        request: {
          provisioningTiming: targetValues.provisioningTiming,
          isEnabled: targetValues.isEnabled,
        },
      });
      return;
    }

    createTarget.mutate({
      credentialTypeId,
      accessControlSystemId: targetValues.accessControlSystemId,
      provisioningTiming: targetValues.provisioningTiming,
    });
  }

  function startEditTarget(target: CredentialTypeTargetResponse) {
    setEditingTargetId(target.id);
    setTargetValues({
      accessControlSystemId: target.accessControlSystemId,
      provisioningTiming: target.provisioningTiming,
      isEnabled: target.isEnabled,
    });
    setIsTargetEditorOpen(true);
  }

  const credentialType = credentialTypeQuery.data;
  const ranges = credentialType?.ranges ?? [];
  const targets = credentialTargetsQuery.data ?? [];
  const systems = systemsQuery.data ?? [];
  const systemsById = new Map(systems.map((item) => [item.id, item]));
  const configuredSystemIds = new Set(targets.map((item) => item.accessControlSystemId));
  const availableSystems = systems.filter((system) => system.providerKind === 'Unipass' && (!configuredSystemIds.has(system.id) || system.id === targetValues.accessControlSystemId));
  const isSaving = updateCredentialType.isPending || setCredentialTypeStatus.isPending;
  const isRangeSaving = createRange.isPending || updateRange.isPending;
  const isTargetSaving = createTarget.isPending || updateTarget.isPending;

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Edit credential type</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update credential technology, allocation, recycle policy, and ranges.</p>
        </div>
      </header>

      <Card className="p-6">
        {credentialTypeQuery.isError || updateCredentialType.isError || setCredentialTypeStatus.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{credentialTypeQuery.isError ? 'Could not load credential type.' : setCredentialTypeStatus.isError ? 'Could not update credential type status.' : 'Could not save credential type.'}</p> : null}
        {credentialTypeQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading credential type...</p> : null}
        {!credentialTypeQuery.isLoading && credentialType ? <CredentialTypeForm values={values} onChange={setValues} onSubmit={() => void handleSubmit(values)} isSubmitting={isSaving} submitLabel="Save credential type" includeStatus /> : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Ranges</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Manage numeric ranges for range-allocated credential types.</p>
          </div>
          <Button type="button" variant="outline" disabled={values.allocationMode !== 'Range'} onClick={() => { setIsRangeEditorOpen((current) => !current); if (isRangeEditorOpen) { setEditingRangeId(null); setRangeValues(emptyRange); } }}>
            <Plus className="size-4" aria-hidden="true" />
            {isRangeEditorOpen ? 'Cancel' : 'Add range'}
          </Button>
        </div>

        {values.allocationMode !== 'Range' ? <p className="rounded-structural border border-dashed border-border p-4 text-[14px] text-muted-foreground">This credential type uses provided identifiers, so ranges do not apply.</p> : null}

        {isRangeEditorOpen && values.allocationMode === 'Range' ? (
          <div className="grid gap-4 rounded-structural border border-border p-4 md:grid-cols-4">
            <label className="grid gap-2 text-[14px] font-medium">
              Range Start
              <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" type="number" value={rangeValues.rangeStart} onChange={(event) => setRangeValues((current) => ({ ...current, rangeStart: event.target.value }))} />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              Range Stop
              <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" type="number" value={rangeValues.rangeStop} onChange={(event) => setRangeValues((current) => ({ ...current, rangeStop: event.target.value }))} />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              Status
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={rangeValues.isActive ? 'Active' : 'Inactive'} onChange={(event) => setRangeValues((current) => ({ ...current, isActive: event.target.value === 'Active' }))}>
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
              </select>
            </label>
            <div className="flex items-end justify-end">
              <Button type="button" disabled={isRangeSaving || !rangeValues.rangeStart || !rangeValues.rangeStop} onClick={handleSaveRange}>{editingRangeId ? 'Save range' : 'Add range'}</Button>
            </div>
          </div>
        ) : null}

        {createRange.isError || updateRange.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{createRange.isError ? 'Could not add range.' : 'Could not save range.'}</p> : null}

        {values.allocationMode === 'Range' && ranges.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No ranges configured yet.</p> : null}

        {ranges.length > 0 ? (
          <div className="grid gap-3">
            {ranges.map((range) => (
              <div key={range.id} className="flex flex-col gap-4 rounded-structural border border-border p-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="min-w-0">
                  <p className="font-medium text-foreground">{range.rangeStart} - {range.rangeStop}</p>
                  <div className="mt-2 flex flex-wrap gap-2 text-[14px] text-muted-foreground">
                    <Badge variant={range.isActive ? 'success' : 'secondary'}>{range.isActive ? 'Active' : 'Inactive'}</Badge>
                    <span>Range size: {Number(range.rangeStop) - Number(range.rangeStart) + 1}</span>
                  </div>
                </div>
                <Button type="button" variant="outline" size="sm" onClick={() => startEditRange(range)}>
                  <Pencil className="size-4" aria-hidden="true" />
                  Edit
                </Button>
              </div>
            ))}
          </div>
        ) : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Supported PACS</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Configure which PACS systems support this credential type and how provisioning should be scheduled.</p>
          </div>
          <Button type="button" variant="outline" disabled={availableSystems.length === 0 && !editingTargetId} onClick={() => {
            setIsTargetEditorOpen((current) => !current);
            if (isTargetEditorOpen) {
              setEditingTargetId(null);
              setTargetValues(emptySupportedPacs);
            }
          }}>
            <Plus className="size-4" aria-hidden="true" />
            {isTargetEditorOpen ? 'Cancel' : 'Add PACS mapping'}
          </Button>
        </div>

        {isTargetEditorOpen ? (
          <div className="grid gap-4 rounded-structural border border-border p-4 md:grid-cols-4">
            <label className="grid gap-2 text-[14px] font-medium">
              Access Control System
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={targetValues.accessControlSystemId} onChange={(event) => setTargetValues((current) => ({ ...current, accessControlSystemId: event.target.value }))} disabled={Boolean(editingTargetId)}>
                <option value="">Select system</option>
                {availableSystems.map((system) => <option key={system.id} value={system.id}>{system.name}</option>)}
              </select>
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              Provisioning Timing
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={targetValues.provisioningTiming} onChange={(event) => setTargetValues((current) => ({ ...current, provisioningTiming: event.target.value as components['schemas']['ProvisioningTiming'] }))}>
                <option value="Eager">Eager</option>
                <option value="AtValidFrom">At Valid From</option>
              </select>
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              Status
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={targetValues.isEnabled ? 'Enabled' : 'Disabled'} onChange={(event) => setTargetValues((current) => ({ ...current, isEnabled: event.target.value === 'Enabled' }))}>
                <option value="Enabled">Enabled</option>
                <option value="Disabled">Disabled</option>
              </select>
            </label>
            <div className="flex items-end justify-end">
              <Button type="button" disabled={isTargetSaving || !targetValues.accessControlSystemId} onClick={handleSaveTarget}>{editingTargetId ? 'Save mapping' : 'Add mapping'}</Button>
            </div>
          </div>
        ) : null}

        {credentialTargetsQuery.isError || systemsQuery.isError || createTarget.isError || updateTarget.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{credentialTargetsQuery.isError ? 'Could not load supported PACS mappings.' : systemsQuery.isError ? 'Could not load access control systems.' : createTarget.isError ? 'Could not add supported PACS mapping.' : 'Could not save supported PACS mapping.'}</p> : null}
        {credentialTargetsQuery.isLoading || systemsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading supported PACS mappings...</p> : null}
        {!credentialTargetsQuery.isLoading && targets.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No supported PACS mappings configured yet.</p> : null}

        {targets.length > 0 ? (
          <div className="grid gap-3">
            {targets.map((target) => {
              const system = systemsById.get(target.accessControlSystemId);
              return (
                <div key={target.id} className="flex flex-col gap-4 rounded-structural border border-border p-4 sm:flex-row sm:items-center sm:justify-between">
                  <div className="min-w-0">
                    <p className="font-medium text-foreground">{system?.name ?? target.accessControlSystemId}</p>
                    <div className="mt-2 flex flex-wrap items-center gap-2 text-[14px] text-muted-foreground">
                      {system ? <AccessControlProviderBadge providerKind={system.providerKind} /> : null}
                      <Badge variant={target.isEnabled ? 'success' : 'secondary'}>{target.isEnabled ? 'Enabled' : 'Disabled'}</Badge>
                      <span>{target.provisioningTiming === 'Eager' ? 'Eager provisioning' : 'Provision at valid from'}</span>
                    </div>
                  </div>
                  <Button type="button" variant="outline" size="sm" onClick={() => startEditTarget(target)}>
                    <Pencil className="size-4" aria-hidden="true" />
                    Edit
                  </Button>
                </div>
              );
            })}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function toFormValues(item: CredentialTypeResponse): CredentialTypeFormValues {
  return {
    name: item.name,
    technology: item.technology,
    allocationMode: item.allocationMode,
    recyclePolicy: item.recyclePolicy,
    recycleGracePeriod: fromTimeSpan(item.recycleGracePeriod),
    requiresConfirmedPacsRevocation: item.requiresConfirmedPacsRevocation,
    nearLimitThreshold: item.nearLimitThreshold == null ? '' : String(item.nearLimitThreshold),
    identifierPrefix: item.identifierPrefix ?? '',
    identifierSuffix: item.identifierSuffix ?? '',
    identifierNumberLength: item.identifierNumberLength == null ? '' : String(item.identifierNumberLength),
    identifierPaddingDirection: item.identifierPaddingDirection ?? 'Left',
    identifierPaddingCharacter: item.identifierPaddingCharacter ?? '',
    status: item.status,
  };
}
