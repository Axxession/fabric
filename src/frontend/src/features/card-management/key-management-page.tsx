import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { KeyRound, Lock, Pencil, Plus, ShieldCheck } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

import { formatDateTime, keyGroupsQueryKey, strategiesQueryKey, type KeyDiversificationStrategy, type KeyGroup } from './card-management-types';

export default function KeyManagementPage() {
  const queryClient = useQueryClient();

  const keyGroupsQuery = useQuery({
    queryKey: keyGroupsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/key-groups', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error) {
        throw new Error('Could not load key groups.');
      }
      return data;
    },
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

  const lockKeyGroup = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.POST('/api/desfire/key-groups/{id}/lock', { params: { path: { id } } });
      if (error) {
        throw new Error('Could not lock key group.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: keyGroupsQueryKey });
      toast.success('Key group locked.');
    },
    onError: () => toast.error('Could not lock key group.'),
  });

  const keyGroups = keyGroupsQuery.data?.items ?? [];
  const strategies = strategiesQuery.data?.items ?? [];
  const strategyById = new Map(strategies.map((strategy) => [strategy.id, strategy]));

  return (
    <section className="rounded-structural border border-border bg-content">
      <div className="border-b border-border p-4 sm:p-6">
        <h1 className="text-[20px] font-semibold tracking-tight">Key Management</h1>
        <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Manage DESFire key groups and diversification strategies used for card encoding.</p>
      </div>

      <div className="p-4 sm:p-6">
        <Tabs defaultValue="key-groups">
          <TabsList>
            <TabsTrigger value="key-groups">Key groups</TabsTrigger>
            <TabsTrigger value="strategies">Diversification strategies</TabsTrigger>
          </TabsList>

          <TabsContent value="key-groups">
            <KeyGroupsPanel
              keyGroups={keyGroups}
              strategyById={strategyById}
              isLoading={keyGroupsQuery.isLoading || strategiesQuery.isLoading}
              isError={keyGroupsQuery.isError}
              isLocking={lockKeyGroup.isPending}
              onLock={(group) => {
                if (window.confirm(`Lock key group "${group.name}"? Key values will no longer be readable or editable from the API.`)) {
                  lockKeyGroup.mutate(group.id);
                }
              }}
            />
          </TabsContent>

          <TabsContent value="strategies">
            <StrategiesPanel strategies={strategies} isLoading={strategiesQuery.isLoading} isError={strategiesQuery.isError} />
          </TabsContent>
        </Tabs>
      </div>
    </section>
  );
}

function KeyGroupsPanel({ keyGroups, strategyById, isLoading, isError, isLocking, onLock }: { readonly keyGroups: KeyGroup[]; readonly strategyById: Map<string, KeyDiversificationStrategy>; readonly isLoading: boolean; readonly isError: boolean; readonly isLocking: boolean; readonly onLock: (group: KeyGroup) => void }) {
  return (
    <div className="grid gap-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-[16px] font-semibold tracking-tight">Key groups</h2>
          <p className="mt-1 text-[14px] text-muted-foreground">Stored keysets. Locked groups remain usable by backend encoding but hide key material from API clients.</p>
        </div>
        <Link to="/desfire-studio/key-groups/new" className={buttonVariants({ className: 'w-full sm:w-fit' })}>
          <Plus className="size-4" aria-hidden="true" />
          Generate key group
        </Link>
      </div>

      {isError ? <PanelError>Could not load key groups.</PanelError> : null}
      {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading key groups...</p> : null}
      {!isLoading && !isError && keyGroups.length === 0 ? (
        <Empty>
          <EmptyHeader><EmptyTitle>No key groups yet</EmptyTitle><EmptyDescription>Add a key group before creating transformations that require secure card operations.</EmptyDescription></EmptyHeader>
        </Empty>
      ) : null}
      {keyGroups.length > 0 ? <KeyGroupsTable keyGroups={keyGroups} strategyById={strategyById} isLocking={isLocking} onLock={onLock} /> : null}
    </div>
  );
}

function KeyGroupsTable({ keyGroups, strategyById, isLocking, onLock }: { readonly keyGroups: KeyGroup[]; readonly strategyById: Map<string, KeyDiversificationStrategy>; readonly isLocking: boolean; readonly onLock: (group: KeyGroup) => void }) {
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Key type</th><th className="px-4 py-3 font-semibold">State</th><th className="px-4 py-3 font-semibold">Keysets</th><th className="px-4 py-3 font-semibold">Diversification</th><th className="px-4 py-3 text-right font-semibold">Actions</th></tr>
        </thead>
        <tbody className="divide-y divide-border">
          {keyGroups.map((group) => (
            <tr key={group.id}>
              <td className="px-4 py-4 font-medium text-foreground">{group.name}</td>
              <td className="px-4 py-4"><Badge variant="outline">{group.keyType}</Badge></td>
              <td className="px-4 py-4"><Badge variant={group.locked ? 'warning' : 'success'}>{group.locked ? 'Locked' : 'Editable'}</Badge></td>
              <td className="px-4 py-4 text-muted-foreground">{group.keySets.length}</td>
              <td className="px-4 py-4 text-muted-foreground">{group.diversificationStrategyId ? strategyById.get(group.diversificationStrategyId)?.name ?? group.diversificationStrategyId : 'None'}</td>
              <td className="px-4 py-4">
                <div className="flex justify-end gap-2">
                  {!group.locked ? <Button type="button" variant="outline" size="sm" disabled={isLocking} onClick={() => onLock(group)}><Lock className="size-4" aria-hidden="true" />Lock</Button> : null}
                   <Link to="/desfire-studio/key-groups/$keyGroupId/edit" params={{ keyGroupId: group.id }} className={buttonVariants({ variant: 'outline', size: 'sm' })}><Pencil className="size-4" aria-hidden="true" />Edit</Link>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function StrategiesPanel({ strategies, isLoading, isError }: { readonly strategies: KeyDiversificationStrategy[]; readonly isLoading: boolean; readonly isError: boolean }) {
  return (
    <div className="grid gap-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-[16px] font-semibold tracking-tight">Diversification strategies</h2>
          <p className="mt-1 text-[14px] text-muted-foreground">Reusable recipes for deriving card-specific keys.</p>
        </div>
        <Link to="/desfire-studio/diversification-strategies/new" className={buttonVariants({ className: 'w-full sm:w-fit' })}>
          <Plus className="size-4" aria-hidden="true" />
          Add strategy
        </Link>
      </div>
      {isError ? <PanelError>Could not load diversification strategies.</PanelError> : null}
      {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading diversification strategies...</p> : null}
      {!isLoading && !isError && strategies.length === 0 ? <Empty><EmptyHeader><EmptyTitle>No strategies yet</EmptyTitle><EmptyDescription>Add a diversification strategy to support diversified keys in key groups.</EmptyDescription></EmptyHeader></Empty> : null}
      {strategies.length > 0 ? <StrategiesTable strategies={strategies} /> : null}
    </div>
  );
}

function StrategiesTable({ strategies }: { readonly strategies: KeyDiversificationStrategy[] }) {
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[46rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Algorithm</th><th className="px-4 py-3 font-semibold">Inputs</th><th className="px-4 py-3 font-semibold">Updated</th><th className="px-4 py-3 text-right font-semibold">Actions</th></tr>
        </thead>
        <tbody className="divide-y divide-border">
          {strategies.map((strategy) => (
            <tr key={strategy.id}>
              <td className="px-4 py-4 font-medium text-foreground"><span className="inline-flex items-center gap-2"><ShieldCheck className="size-4 text-primary" aria-hidden="true" />{strategy.name}</span></td>
              <td className="px-4 py-4"><Badge variant="outline">{strategy.algorithm}</Badge></td>
              <td className="px-4 py-4 text-muted-foreground">{strategy.inputs.length}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatDateTime(strategy.updatedAt)}</td>
               <td className="px-4 py-4"><div className="flex justify-end"><Link to="/desfire-studio/diversification-strategies/$strategyId/edit" params={{ strategyId: strategy.id }} className={buttonVariants({ variant: 'outline', size: 'sm' })}><KeyRound className="size-4" aria-hidden="true" />Edit</Link></div></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function PanelError({ children }: { readonly children: React.ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
