import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Copy, Eye, KeyRound, Plus, RotateCcw, Trash2 } from 'lucide-react';
import { useState, type FormEvent } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Input } from '@/shared/components/ui/input';

type HardwareAgent = components['schemas']['HardwareAgentResponse'];
type CreateHardwareAgentRequest = components['schemas']['CreateHardwareAgentRequest'];
type HardwareAgentKeyResponse = components['schemas']['HardwareAgentKeyResponse'];
type HardwareConnectionStatus = components['schemas']['HardwareConnectionStatus'];

type FormValues = {
  readonly id: string;
  readonly name: string;
};

const agentsQueryKey = ['facility', 'hardware-agents'] as const;
const emptyFormValues: FormValues = { id: '', name: '' };

export function HardwareAgentsPanel() {
  const queryClient = useQueryClient();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [values, setValues] = useState<FormValues>(emptyFormValues);
  const [latestKey, setLatestKey] = useState<HardwareAgentKeyResponse | null>(null);

  const agentsQuery = useQuery({
    queryKey: agentsQueryKey,
    refetchInterval: 10_000,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/hardware/agents', {
        params: { query: { Page: 0, PageSize: 100 } },
      });

      if (error) {
        throw new Error('Could not load hardware agents.');
      }

      return data;
    },
  });

  const createAgent = useMutation({
    mutationFn: async (request: CreateHardwareAgentRequest) => {
      const { data, error } = await api.POST('/api/hardware/agents', { body: request });

      if (error || !data) {
        throw new Error('Could not add hardware agent.');
      }

      return data;
    },
    onSuccess: async (response) => {
      await queryClient.invalidateQueries({ queryKey: agentsQueryKey });
      setValues(emptyFormValues);
      setIsFormOpen(false);
      setLatestKey(response);
      toast.success('Hardware agent added.');
    },
    onError: () => toast.error('Could not add hardware agent.'),
  });

  const rotateKey = useMutation({
    mutationFn: async (agentId: string) => {
      const { data, error } = await api.POST('/api/hardware/agents/{agentId}/rotate-key', {
        params: { path: { agentId } },
      });

      if (error || !data) {
        throw new Error('Could not rotate hardware agent key.');
      }

      return data;
    },
    onSuccess: (response) => {
      setLatestKey(response);
      toast.success('Hardware agent key rotated.');
    },
    onError: () => toast.error('Could not rotate hardware agent key.'),
  });

  const deleteAgent = useMutation({
    mutationFn: async (agent: HardwareAgent) => {
      const { error } = await api.DELETE('/api/hardware/agents/{agentId}', {
        params: { path: { agentId: agent.id } },
      });

      if (error) {
        throw new Error('Could not delete hardware agent.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: agentsQueryKey });
      toast.success('Hardware agent deleted.');
    },
    onError: () => toast.error('Could not delete hardware agent.'),
  });

  const agents = agentsQuery.data?.items ?? [];
  const totalItems = Number(agentsQuery.data?.totalItems ?? agents.length);
  const hasAgentData = agentsQuery.data !== undefined;
  const isAgentDataStale = hasAgentData && agentsQuery.isError;
  const isAgentDataLive = hasAgentData && !isAgentDataStale;

  function updateValue<TKey extends keyof FormValues>(key: TKey, value: FormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    createAgent.mutate({
      id: values.id,
      name: values.name,
    });
  }

  return (
    <section className="rounded-structural border border-border bg-content">
      <div className="border-b border-border p-4 sm:p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-[20px] font-semibold tracking-tight">Hardware Agents</h2>
              {isAgentDataLive ? <PollingStatusBadge state="live" /> : null}
              {isAgentDataStale ? <PollingStatusBadge state="stale" /> : null}
            </div>
            <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Manage local hardware agents used by kiosks, reception desks, scanners, and printers.</p>
            {isAgentDataStale ? <p className="mt-2 text-[14px] text-warning">Showing last successful update. Live polling is temporarily unavailable.</p> : null}
          </div>
          <Button type="button" className="w-full sm:w-fit" onClick={() => setIsFormOpen((current) => !current)}>
            <Plus className="size-4" aria-hidden="true" />
            Add agent
          </Button>
        </div>
      </div>

      <div className="grid gap-6 p-4 sm:p-6">
        {latestKey ? <AgentKeyPanel response={latestKey} onClose={() => setLatestKey(null)} /> : null}

        {isFormOpen ? (
          <form className="grid gap-5 rounded-structural border border-border p-4" onSubmit={handleSubmit}>
            {createAgent.isError ? (
              <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
                Could not add hardware agent.
              </p>
            ) : null}

            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium">
                Agent id
                <Input value={values.id} onChange={(event) => updateValue('id', event.target.value)} placeholder="reception-01" required />
              </label>

              <label className="grid gap-2 text-[14px] font-medium">
                Name
                <Input value={values.name} onChange={(event) => updateValue('name', event.target.value)} placeholder="Reception station 01" required />
              </label>
            </div>

            <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <Button type="button" variant="outline" onClick={() => setIsFormOpen(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={createAgent.isPending}>
                {createAgent.isPending ? 'Adding...' : 'Add hardware agent'}
              </Button>
            </div>
          </form>
        ) : null}

        {!agentsQuery.isLoading && !agentsQuery.isError && totalItems === 0 ? (
          <Empty>
            <EmptyHeader>
              <EmptyTitle>No hardware agents yet</EmptyTitle>
              <EmptyDescription>Add an agent before connecting local scanners, passport readers, RFID encoders, or printers.</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <div className="grid gap-3">
            <div className="grid gap-3 lg:hidden">
              {agentsQuery.isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading hardware agents...</p> : null}
              {agentsQuery.isError && !hasAgentData ? <p className="rounded-structural border border-border p-4 text-[14px] text-error">Could not load hardware agents.</p> : null}
              {agents.map((agent) => (
                <AgentCard key={agent.id} agent={agent} onRotate={() => rotateKey.mutate(agent.id)} onDelete={() => deleteAgent.mutate(agent)} busy={rotateKey.isPending || deleteAgent.isPending} />
              ))}
            </div>

            <div className="hidden overflow-x-auto rounded-structural border border-border lg:block">
              <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
                <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-semibold">Agent</th>
                    <th className="px-4 py-3 font-semibold">Last seen</th>
                    <th className="px-4 py-3 font-semibold">Inventory</th>
                    <th className="px-4 py-3 font-semibold">Status</th>
                    <th className="px-4 py-3 text-right font-semibold">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {agentsQuery.isLoading ? (
                    <tr>
                      <td className="px-4 py-5 text-muted-foreground" colSpan={5}>
                        Loading hardware agents...
                      </td>
                    </tr>
                  ) : null}

                  {agentsQuery.isError && !hasAgentData ? (
                    <tr>
                      <td className="px-4 py-5 text-error" colSpan={5}>
                        Could not load hardware agents.
                      </td>
                    </tr>
                  ) : null}

                  {agents.map((agent) => (
                    <AgentRow key={agent.id} agent={agent} onRotate={() => rotateKey.mutate(agent.id)} onDelete={() => deleteAgent.mutate(agent)} busy={rotateKey.isPending || deleteAgent.isPending} />
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

function AgentKeyPanel({ response, onClose }: { response: HardwareAgentKeyResponse; onClose: () => void }) {
  async function copyKey() {
    await navigator.clipboard.writeText(response.apiKey);
    toast.success('Agent key copied.');
  }

  return (
    <div className="rounded-structural border border-success bg-success-background p-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <div className="flex items-center gap-2 text-[14px] font-semibold text-foreground">
            <KeyRound className="size-4" aria-hidden="true" />
            New key for {response.agent.name}
          </div>
          <p className="mt-2 text-[14px] text-muted-foreground">Copy this key now. It is only shown once and should be stored in the local agent config.</p>
          <code className="mt-3 block overflow-x-auto rounded-interactive border border-border bg-content px-3 py-2 text-[13px] text-foreground">{response.apiKey}</code>
        </div>
        <div className="flex gap-2">
          <Button type="button" variant="outline" onClick={copyKey}>
            <Copy className="size-4" aria-hidden="true" />
            Copy
          </Button>
          <Button type="button" variant="ghost" onClick={onClose}>
            Dismiss
          </Button>
        </div>
      </div>
    </div>
  );
}

function AgentCard({ agent, onRotate, onDelete, busy }: AgentActionsProps) {
  return (
    <article className="rounded-structural border border-border p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="truncate text-[15px] font-semibold text-foreground">{agent.name}</h3>
            <StatusBadge enabled={agent.enabled} />
          </div>
          <p className="mt-1 text-[13px] text-muted-foreground">{agent.id}</p>
          <dl className="mt-4 grid gap-2 text-[13px]">
            <InfoRow label="Connection" value={formatConnectionStatus(agent.connectionStatus)} />
            <InfoRow label="Last seen" value={formatDate(agent.lastSeenAt)} />
            <InfoRow label="Inventory" value={formatDate(agent.lastInventoryAt)} />
          </dl>
        </div>
      </div>
      <AgentActions agent={agent} onRotate={onRotate} onDelete={onDelete} busy={busy} className="mt-4" />
    </article>
  );
}

function AgentRow({ agent, onRotate, onDelete, busy }: AgentActionsProps) {
  return (
    <tr>
      <td className="px-4 py-4">
        <div className="font-medium text-foreground">{agent.name}</div>
        <div className="mt-1 text-[13px] text-muted-foreground">{agent.id}</div>
      </td>
      <td className="px-4 py-4 text-muted-foreground">{formatDate(agent.lastSeenAt)}</td>
      <td className="px-4 py-4 text-muted-foreground">{formatDate(agent.lastInventoryAt)}</td>
      <td className="px-4 py-4"><div className="flex flex-wrap gap-2"><StatusBadge enabled={agent.enabled} /><ConnectionStatusBadge status={agent.connectionStatus} /></div></td>
      <td className="px-4 py-4">
        <AgentActions agent={agent} onRotate={onRotate} onDelete={onDelete} busy={busy} className="justify-end" />
      </td>
    </tr>
  );
}

type AgentActionsProps = {
  readonly agent: HardwareAgent;
  readonly onRotate: () => void;
  readonly onDelete: () => void;
  readonly busy: boolean;
  readonly className?: string;
};

function AgentActions({ agent, onRotate, onDelete, busy, className }: AgentActionsProps) {
  function confirmDelete() {
    if (window.confirm(`Delete hardware agent "${agent.name}"? This removes its reported devices and event inbox entries.`)) {
      onDelete();
    }
  }

  return (
    <div className={`flex flex-wrap gap-2 ${className ?? ''}`}>
      <Button type="button" variant="outline" size="sm" onClick={onRotate} disabled={busy}>
        <RotateCcw className="size-4" aria-hidden="true" />
        Rotate key
      </Button>
      <Link className={buttonVariants({ variant: 'outline', size: 'sm' })} to="/administration/clients/hardware-agents/$agentId" params={{ agentId: agent.id }}>
        <Eye className="size-4" aria-hidden="true" />
        Details
      </Link>
      <Button type="button" variant="destructive" size="sm" onClick={confirmDelete} disabled={busy}>
        <Trash2 className="size-4" aria-hidden="true" />
        Delete
      </Button>
    </div>
  );
}

function InfoRow({ label, value }: { readonly label: string; readonly value: string }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="text-right text-foreground">{value}</dd>
    </div>
  );
}

function StatusBadge({ enabled }: { readonly enabled: boolean }) {
  return <Badge variant={enabled ? 'success' : 'secondary'}>{enabled ? 'Enabled' : 'Disabled'}</Badge>;
}

function ConnectionStatusBadge({ status }: { readonly status: HardwareConnectionStatus }) {
  const variant = status === 'Online' ? 'success' : status === 'Stale' ? 'warning' : 'secondary';
  return <Badge variant={variant}>{formatConnectionStatus(status)}</Badge>;
}

function PollingStatusBadge({ state }: { readonly state: 'live' | 'stale' }) {
  return <Badge variant={state === 'live' ? 'success' : 'warning'}>{state === 'live' ? 'Live' : 'Stale'}</Badge>;
}

function formatConnectionStatus(status: HardwareConnectionStatus) {
  return status === 'Online' ? 'Online' : status === 'Stale' ? 'Stale' : 'Offline';
}

function formatDate(value: string | null) {
  if (!value) {
    return 'Never';
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}
