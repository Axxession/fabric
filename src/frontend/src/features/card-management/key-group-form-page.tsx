import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

import { keyGroupsQueryKey, keyTypes, strategiesQueryKey, type CreateKeyGroupRequest, type KeyGroupKeySetRequest, type KeyType, type UpdateKeyGroupRequest } from './card-management-types';

type KeyRow = { readonly keyId: string; readonly value: string; readonly isDiversified: boolean };
type KeySetRow = { readonly keySetId: string; readonly keys: KeyRow[] };
type FormValues = { readonly name: string; readonly keyType: KeyType; readonly diversificationStrategyId: string; readonly keySets: KeySetRow[] };
type CreateFormValues = { readonly name: string; readonly keyType: KeyType; readonly numberOfKeySets: string; readonly numberOfKeys: string };

const emptyValues: FormValues = {
  name: '',
  keyType: 'Aes',
  diversificationStrategyId: '',
  keySets: [{ keySetId: '0', keys: [{ keyId: '0', value: '', isDiversified: false }] }],
};

const emptyCreateValues: CreateFormValues = {
  name: '',
  keyType: 'Aes',
  numberOfKeySets: '1',
  numberOfKeys: '5',
};

export function KeyGroupCreatePage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [values, setValues] = useState<CreateFormValues>(emptyCreateValues);

  const createKeyGroup = useMutation({
    mutationFn: async (request: CreateKeyGroupRequest) => {
      const { data, error } = await api.POST('/api/desfire/key-groups', { body: request });
      if (error || !data) {
        throw new Error('Could not generate key group.');
      }
      return data;
    },
    onSuccess: async (keyGroup) => {
      await queryClient.invalidateQueries({ queryKey: keyGroupsQueryKey });
      toast.success('Key group generated.');
      await navigate({ to: '/desfire-studio/key-groups/$keyGroupId/edit', params: { keyGroupId: keyGroup.id } });
    },
    onError: () => toast.error('Could not generate key group.'),
  });

  function updateValue<TKey extends keyof CreateFormValues>(key: TKey, value: CreateFormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    createKeyGroup.mutate({
      name: values.name,
      keyType: values.keyType,
      numberOfKeySets: Number(values.numberOfKeySets),
      numberOfKeys: Number(values.numberOfKeys),
    });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Generate key group</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Fabric will generate random keys for every key slot. You can review and adjust them on the next page before locking.</p>
        </div>
      </header>

      <Card className="p-4 sm:p-6">
        <form className="grid gap-5" onSubmit={handleSubmit}>
          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              Name
              <Input value={values.name} onChange={(event) => updateValue('name', event.target.value)} required />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              Key type
              <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={values.keyType} onChange={(event) => updateValue('keyType', event.target.value as KeyType)}>
                {keyTypes.map((keyType) => <option key={keyType} value={keyType}>{keyType}</option>)}
              </select>
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              Number of key sets
              <Input value={values.numberOfKeySets} type="number" min={1} max={16} onChange={(event) => updateValue('numberOfKeySets', event.target.value)} required />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              Number of keys
              <Input value={values.numberOfKeys} type="number" min={1} max={16} onChange={(event) => updateValue('numberOfKeys', event.target.value)} required />
            </label>
          </div>

          <div className="rounded-structural border border-border bg-hover-gray p-4 text-[14px] text-muted-foreground">
            This creates {values.numberOfKeySets || 0} key set(s) with {values.numberOfKeys || 0} random key(s) each. Keys are protected at rest immediately.
          </div>

          <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
            <Button type="button" variant="outline" onClick={() => window.history.back()}>Cancel</Button>
            <Button type="submit" disabled={createKeyGroup.isPending}>{createKeyGroup.isPending ? 'Generating...' : 'Generate keys'}</Button>
          </div>
        </form>
      </Card>
    </div>
  );
}

export default function KeyGroupEditPage() {
  const { keyGroupId } = useParams({ from: '/desfire-studio/key-groups/$keyGroupId/edit' });
  return <KeyGroupEditPageContent keyGroupId={keyGroupId} />;
}

export function KeyGroupEditPageContent({ keyGroupId }: { readonly keyGroupId: string }) {
  return <KeyGroupFormPage mode="edit" keyGroupId={keyGroupId} />;
}

function KeyGroupFormPage({ mode, keyGroupId }: { readonly mode: 'edit'; readonly keyGroupId?: string }) {
  const queryClient = useQueryClient();
  const [values, setValues] = useState<FormValues>(emptyValues);

  const keyGroupQuery = useQuery({
    queryKey: [...keyGroupsQueryKey, keyGroupId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/key-groups/{id}', { params: { path: { id: keyGroupId ?? '' } } });
      if (error || !data) {
        throw new Error('Could not load key group.');
      }
      return data;
    },
    enabled: mode === 'edit' && !!keyGroupId,
  });

  const strategiesQuery = useQuery({
    queryKey: strategiesQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/key-diversification-strategies', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error) {
        throw new Error('Could not load diversification strategies.');
      }
      return data;
    },
  });

  useEffect(() => {
    if (!keyGroupQuery.data) {
      return;
    }

    setValues({
      name: keyGroupQuery.data.name,
      keyType: keyGroupQuery.data.keyType,
      diversificationStrategyId: keyGroupQuery.data.diversificationStrategyId ?? '',
      keySets: keyGroupQuery.data.keySets.map((keySet) => ({
        keySetId: String(keySet.keySetId),
        keys: keySet.keys.map((key) => ({ keyId: String(key.keyId), value: key.value ?? '', isDiversified: key.isDiversified })),
      })),
    });
  }, [keyGroupQuery.data]);

  const saveKeyGroup = useMutation({
    mutationFn: async (request: UpdateKeyGroupRequest) => {
      const { error } = await api.PUT('/api/desfire/key-groups/{id}', { params: { path: { id: keyGroupId ?? '' } }, body: request });
      if (error) {
        throw new Error('Could not update key group.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: keyGroupsQueryKey });
      if (keyGroupId) {
        await queryClient.invalidateQueries({ queryKey: [...keyGroupsQueryKey, keyGroupId] });
      }
      toast.success('Key group updated.');
      window.history.back();
    },
    onError: () => toast.error('Could not update key group.'),
  });

  const locked = keyGroupQuery.data?.locked ?? false;
  const strategies = strategiesQuery.data?.items ?? [];

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    saveKeyGroup.mutate(toRequest(values));
  }

  function updateValue<TKey extends keyof FormValues>(key: TKey, value: FormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-[20px] font-semibold tracking-tight">{values.name || 'Edit key group'}</h2>
            {locked ? <Badge variant="warning">Locked</Badge> : null}
          </div>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update key values and diversification settings. Generated key set and key IDs are fixed after creation.</p>
        </div>
      </header>

      {keyGroupQuery.isError ? <PanelError>Could not load key group.</PanelError> : null}
      {locked ? <PanelError>Locked key groups cannot be edited from the API. Backend encoding can still use the stored keys.</PanelError> : null}

      <Card className="p-4 sm:p-6">
        {keyGroupQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading key group...</p> : null}
        {keyGroupQuery.data ? (
          <form className="grid gap-5" onSubmit={handleSubmit}>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium">
                Name
                <Input value={values.name} disabled={locked} onChange={(event) => updateValue('name', event.target.value)} required />
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                Key type
                <span className="flex h-9 items-center rounded-interactive border border-border bg-hover-gray px-3 text-[14px] text-foreground">
                  {values.keyType}
                </span>
              </label>
              <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
                Diversification strategy
                <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={values.diversificationStrategyId} disabled={locked} onChange={(event) => updateValue('diversificationStrategyId', event.target.value)}>
                  <option value="">None</option>
                  {strategies.map((strategy) => <option key={strategy.id} value={strategy.id}>{strategy.name}</option>)}
                </select>
              </label>
            </div>

            <KeySetsEditor values={values.keySets} disabled={locked} onChange={(keySets) => updateValue('keySets', keySets)} />

            <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <Button type="button" variant="outline" onClick={() => window.history.back()}>Cancel</Button>
              <Button type="submit" disabled={locked || saveKeyGroup.isPending}>{saveKeyGroup.isPending ? 'Saving...' : 'Save key group'}</Button>
            </div>
          </form>
        ) : null}
      </Card>
    </div>
  );
}

function KeySetsEditor({ values, disabled, onChange }: { readonly values: KeySetRow[]; readonly disabled: boolean; readonly onChange: (values: KeySetRow[]) => void }) {
  function updateKeySet(index: number, keySet: KeySetRow) {
    onChange(values.map((current, currentIndex) => currentIndex === index ? keySet : current));
  }

  return (
    <section className="grid gap-4">
      <div className="flex items-center justify-between gap-3">
        <h3 className="text-[16px] font-semibold tracking-tight">Key sets</h3>
        <span className="text-[13px] text-muted-foreground">Structure fixed after generation</span>
      </div>

      {values.map((keySet, index) => (
        <div key={index} className="grid gap-4 rounded-structural border border-border p-4">
          <div className="flex items-center justify-between gap-3">
            <h4 className="text-[14px] font-semibold tracking-tight">Key set {keySet.keySetId}</h4>
            <Badge variant="secondary">ID {keySet.keySetId}</Badge>
          </div>

          <div className="grid gap-3">
            {keySet.keys.map((key, keyIndex) => (
              <div key={keyIndex} className="grid gap-3 rounded-interactive border border-border p-3 md:grid-cols-[5rem_1fr_auto] md:items-end">
                <div className="grid gap-2 text-[14px] font-medium">
                  Key id
                  <Badge variant="secondary" className="h-9 w-fit px-3">{key.keyId}</Badge>
                </div>
                <label className="grid gap-2 text-[14px] font-medium">
                  Key value hex
                  <Input value={key.value} disabled={disabled} onChange={(event) => updateKeySet(index, { ...keySet, keys: keySet.keys.map((current, currentIndex) => currentIndex === keyIndex ? { ...current, value: event.target.value } : current) })} required />
                </label>
                <label className="flex items-center gap-2 rounded-interactive border border-border px-3 py-2 text-[14px] font-medium">
                  <input type="checkbox" checked={key.isDiversified} disabled={disabled} onChange={(event) => updateKeySet(index, { ...keySet, keys: keySet.keys.map((current, currentIndex) => currentIndex === keyIndex ? { ...current, isDiversified: event.target.checked } : current) })} />
                  Diversified
                </label>
              </div>
            ))}
          </div>
        </div>
      ))}
    </section>
  );
}

function toRequest(values: FormValues): UpdateKeyGroupRequest {
  return {
    name: values.name,
    diversificationStrategyId: values.diversificationStrategyId || null,
    keySets: values.keySets.map<KeyGroupKeySetRequest>((keySet) => ({
      keySetId: Number(keySet.keySetId),
      keys: keySet.keys.map((key) => ({ keyId: Number(key.keyId), value: key.value, isDiversified: key.isDiversified })),
    })),
  };
}

function PanelError({ children }: { readonly children: ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
