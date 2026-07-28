import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useLocation, useNavigate } from '@tanstack/react-router';
import { MoreHorizontal, Plus } from 'lucide-react';

import { Skeleton } from '@/shared/components/ui/skeleton';
import { useState, type FormEvent } from 'react';
import { useAuth } from 'react-oidc-context';
import { toast } from 'sonner';

import { apiBaseUrl } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge, type BadgeVariant } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Input } from '@/shared/components/ui/input';
import { Popover, PopoverContent, PopoverTrigger } from '@/shared/components/ui/popover';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { cn } from '@/shared/utils/cn';
import { AutomationSectionNav } from './automation-section-nav';
import { allWorkflowDefinitionsQueryKey, workflowDefinitionsQueryKey, workflowHistoryQueryKey } from './workflow-query-keys';

type WorkflowTab = 'definitions' | 'history';
type WorkflowDefinition = components['schemas']['LinkedWorkflowDefinitionSummary'];
type WorkflowDefinitionModel = components['schemas']['LinkedWorkflowDefinitionModel'];
type WorkflowInstance = components['schemas']['WorkflowInstanceSummary'];
type WorkflowDefinitionsResponse = components['schemas']['PagedListResponseOfLinkedWorkflowDefinitionSummary'];
type WorkflowInstancesResponse = components['schemas']['Response'];

export default function WorkflowPage() {
  const auth = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [newDefinitionName, setNewDefinitionName] = useState('');
  const activeTab = getActiveTab(location.pathname, location.searchStr);

  const definitionsQuery = useQuery({
    queryKey: workflowDefinitionsQueryKey,
    queryFn: () => fetchElsa<WorkflowDefinitionsResponse>('/elsa/api/workflow-definitions?versionOptions=Published', auth.user?.access_token),
  });

  const allDefinitionsQuery = useQuery({
    queryKey: allWorkflowDefinitionsQueryKey,
    queryFn: () => fetchElsa<WorkflowDefinitionsResponse>('/elsa/api/workflow-definitions', auth.user?.access_token),
  });

  const historyQuery = useQuery({
    queryKey: workflowHistoryQueryKey,
    queryFn: () => fetchElsa<WorkflowInstancesResponse>('/elsa/api/workflow-instances', auth.user?.access_token, { method: 'POST', body: '{}' }),
  });

  const createDefinition = useMutation({
    mutationFn: () => createWorkflowDefinition(newDefinitionName, auth.user?.access_token),
    onSuccess: async (definition) => {
      await queryClient.invalidateQueries({ queryKey: workflowDefinitionsQueryKey });
      await queryClient.invalidateQueries({ queryKey: allWorkflowDefinitionsQueryKey });
      await queryClient.invalidateQueries({ queryKey: workflowHistoryQueryKey });
      setIsCreateOpen(false);
      setNewDefinitionName('');
      void navigate({ to: '/administration/automation/workflow-definitions/$definitionId/edit', params: { definitionId: definition.definitionId ?? definition.id ?? '' } });
    },
    onError: () => toast.error('Could not create workflow definition.'),
  });

  function updateTab(tab: string) {
    const nextTab = tab === 'history' ? 'history' : 'definitions';
    void queryClient.invalidateQueries({ queryKey: workflowDefinitionsQueryKey });
    void queryClient.invalidateQueries({ queryKey: workflowHistoryQueryKey });
    void navigate({ to: '/administration/automation/workflow', search: { tab: nextTab } as never });
  }

  function submitCreateDefinition(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!newDefinitionName.trim() || createDefinition.isPending)
      return;

    createDefinition.mutate();
  }

  return (
    <section className="grid gap-4">
      <AutomationSectionNav />

      <div className="rounded-structural border border-border bg-content p-4 sm:p-6">
        <Tabs value={activeTab} onValueChange={updateTab}>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <TabsList aria-label="Workflow sections">
              <TabsTrigger value="definitions">Definitions</TabsTrigger>
              <TabsTrigger value="history">History</TabsTrigger>
            </TabsList>
            <Popover open={isCreateOpen} onOpenChange={setIsCreateOpen}>
              <PopoverTrigger render={<Button type="button" className="sm:w-fit" />}>
                <Plus className="size-4" />
                New workflow definition
              </PopoverTrigger>
              <PopoverContent align="end" className="grid min-w-[22rem] gap-3 p-4">
                <form className="grid gap-3" onSubmit={submitCreateDefinition}>
                  <div>
                    <h3 className="text-[14px] font-semibold">Create workflow definition</h3>
                    <p className="mt-1 text-[13px] text-muted-foreground">Give the definition a name before opening the editor.</p>
                  </div>
                  <Input value={newDefinitionName} onChange={(event) => setNewDefinitionName(event.target.value)} placeholder="Visitor onboarding" autoFocus required />
                  <div className="flex justify-end gap-2">
                    <Button type="button" variant="ghost" onClick={() => setIsCreateOpen(false)} disabled={createDefinition.isPending}>Cancel</Button>
                    <Button type="submit" disabled={!newDefinitionName.trim() || createDefinition.isPending}>Create</Button>
                  </div>
                </form>
              </PopoverContent>
            </Popover>
          </div>

          <TabsContent value="definitions">
            <WorkflowDefinitionsPanel query={definitionsQuery} />
          </TabsContent>

          <TabsContent value="history">
            <WorkflowHistoryPanel query={historyQuery} definitions={allDefinitionsQuery.data?.items ?? []} />
          </TabsContent>
        </Tabs>
      </div>
    </section>
  );
}

function WorkflowDefinitionsPanel({ query }: { readonly query: ReturnType<typeof useQuery<WorkflowDefinitionsResponse>> }) {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const definitions = query.data?.items ?? [];
  const totalCount = Number(query.data?.totalCount ?? definitions.length);

  const publishDefinition = useMutation({
    mutationFn: (definition: WorkflowDefinition) => updateWorkflowDefinition(definition, 'publish', auth.user?.access_token),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: workflowDefinitionsQueryKey });
      toast.success('Workflow definition published.');
    },
    onError: () => toast.error('Could not publish workflow definition.'),
  });

  const retractDefinition = useMutation({
    mutationFn: (definition: WorkflowDefinition) => updateWorkflowDefinition(definition, 'retract', auth.user?.access_token),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: workflowDefinitionsQueryKey });
      toast.success('Workflow definition unpublished.');
    },
    onError: () => toast.error('Could not unpublish workflow definition.'),
  });

  const deleteDefinition = useMutation({
    mutationFn: (definition: WorkflowDefinition) => deleteWorkflowDefinition(definition, auth.user?.access_token),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: workflowDefinitionsQueryKey });
      toast.success('Workflow definition deleted.');
    },
    onError: () => toast.error('Could not delete workflow definition.'),
  });

  function confirmDelete(definition: WorkflowDefinition) {
    const name = definition.name || definition.definitionId || 'this workflow definition';

    if (window.confirm(`Delete "${name}"? This cannot be undone.`)) {
      deleteDefinition.mutate(definition);
    }
  }

  return (
    <div className="grid gap-4">
      <PanelHeader title="Definitions" description="Browse workflow definitions and open the Elsa Studio designer for edits." count={totalCount} />

      {query.isLoading ? <SkeletonTable rows={5} columns={[1.6, '7rem', '7rem', '9rem']} /> : null}
      {query.isError ? <StatusMessage tone="error">Could not load workflow definitions.</StatusMessage> : null}
      {!query.isLoading && !query.isError && definitions.length === 0 ? (
        <Empty>
          <EmptyHeader>
            <EmptyTitle>No workflow definitions</EmptyTitle>
            <EmptyDescription>Create definitions in Elsa before they appear here.</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : null}
      {definitions.length > 0 ? (
        <WorkflowDefinitionsTable
          definitions={definitions}
          onPublish={(definition) => publishDefinition.mutate(definition)}
          onRetract={(definition) => retractDefinition.mutate(definition)}
          onDelete={confirmDelete}
          busy={publishDefinition.isPending || retractDefinition.isPending || deleteDefinition.isPending}
        />
      ) : null}
    </div>
  );
}

function WorkflowHistoryPanel({ query, definitions }: { readonly query: ReturnType<typeof useQuery<WorkflowInstancesResponse>>; readonly definitions: readonly WorkflowDefinition[] }) {
  const instances = query.data?.items ?? [];
  const totalCount = Number(query.data?.totalCount ?? instances.length);

  return (
    <div className="grid gap-4">
      <PanelHeader title="History" description="Inspect workflow runs, statuses, execution history, and active automation work." count={totalCount} />

      {query.isLoading ? <SkeletonTable rows={5} columns={[1.4, '7rem', '7rem', '9rem', '9rem']} /> : null}
      {query.isError ? <StatusMessage tone="error">Could not load workflow history.</StatusMessage> : null}
      {!query.isLoading && !query.isError && instances.length === 0 ? (
        <Empty>
          <EmptyHeader>
            <EmptyTitle>No workflow history</EmptyTitle>
            <EmptyDescription>Workflow runs appear here after execution starts.</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : null}
      {instances.length > 0 ? <WorkflowHistoryTable instances={instances} definitions={definitions} /> : null}
    </div>
  );
}

function PanelHeader({ title, description, count }: { readonly title: string; readonly description: string; readonly count: number }) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
      <div>
        <h2 className="text-[20px] font-semibold tracking-tight">{title}</h2>
        <p className="mt-1 max-w-2xl text-[14px] text-muted-foreground">{description}</p>
      </div>
      <Badge variant="outline">{count} total</Badge>
    </div>
  );
}

function WorkflowDefinitionsTable({ definitions, onPublish, onRetract, onDelete, busy }: { readonly definitions: readonly WorkflowDefinition[]; readonly onPublish: (definition: WorkflowDefinition) => void; readonly onRetract: (definition: WorkflowDefinition) => void; readonly onDelete: (definition: WorkflowDefinition) => void; readonly busy: boolean }) {
  const navigate = useNavigate();

  function openDefinition(definition: WorkflowDefinition) {
    void navigate({ to: '/administration/automation/workflow-definitions/$definitionId/edit', params: { definitionId: getDefinitionRouteId(definition) } });
  }

  return (
    <div className="overflow-hidden rounded-structural border border-border">
      <div className="hidden grid-cols-[minmax(0,1.6fr)_7rem_7rem_9rem_auto] gap-4 border-b border-border bg-hover-gray px-4 py-3 text-[12px] font-semibold uppercase text-muted-foreground lg:grid">
        <span>Name</span>
        <span>Version</span>
        <span>Status</span>
        <span>Created</span>
        <span className="text-right">Actions</span>
      </div>

      <div className="divide-y divide-border">
        {definitions.map((definition) => (
          <div
            key={definition.definitionId ?? definition.id ?? definition.name}
            role="link"
            tabIndex={0}
            className="grid cursor-pointer gap-4 px-4 py-4 transition hover:bg-hover-gray focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-primary/20 lg:grid-cols-[minmax(0,1.6fr)_7rem_7rem_9rem_auto] lg:items-center"
            onClick={() => openDefinition(definition)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                openDefinition(definition);
              }
            }}
          >
            <div className="min-w-0">
              <p className="truncate text-[14px] font-semibold">{definition.name || 'Untitled workflow'}</p>
              <p className="mt-1 line-clamp-2 text-[13px] text-muted-foreground">{definition.description || definition.definitionId || 'No description'}</p>
            </div>
            <p className="text-[14px] text-muted-foreground">v{definition.version ?? '-'}</p>
            <div><PublicationBadge definition={definition} /></div>
            <p className="text-[14px] text-muted-foreground">{formatDate(definition.createdAt)}</p>
            <WorkflowDefinitionActions definition={definition} busy={busy} onPublish={onPublish} onRetract={onRetract} onDelete={onDelete} />
          </div>
        ))}
      </div>
    </div>
  );
}

function WorkflowDefinitionActions({ definition, busy, onPublish, onRetract, onDelete }: { readonly definition: WorkflowDefinition; readonly busy: boolean; readonly onPublish: (definition: WorkflowDefinition) => void; readonly onRetract: (definition: WorkflowDefinition) => void; readonly onDelete: (definition: WorkflowDefinition) => void }) {
  return (
    <div className="justify-self-start lg:justify-self-end" onClick={(event) => event.stopPropagation()} onKeyDown={(event) => event.stopPropagation()}>
      <Popover>
        <PopoverTrigger render={<Button type="button" variant="ghost" size="icon-sm" aria-label={`Actions for ${definition.name || 'workflow definition'}`} disabled={busy} />}>
          <MoreHorizontal className="size-4" aria-hidden="true" />
        </PopoverTrigger>
        <PopoverContent align="end" className="grid min-w-44 gap-1 p-1">
          {definition.isPublished ? (
            <WorkflowActionButton onClick={() => onRetract(definition)} disabled={busy}>Unpublish</WorkflowActionButton>
          ) : (
            <WorkflowActionButton onClick={() => onPublish(definition)} disabled={busy}>Publish</WorkflowActionButton>
          )}
          <WorkflowActionButton onClick={() => onDelete(definition)} disabled={busy} destructive>Delete</WorkflowActionButton>
        </PopoverContent>
      </Popover>
    </div>
  );
}

function WorkflowActionButton({ destructive, className, ...props }: React.ComponentProps<'button'> & { readonly destructive?: boolean }) {
  return (
    <button
      type="button"
      className={cn(
        'w-full rounded-interactive px-3 py-2 text-left text-[14px] font-medium transition hover:bg-hover-blue disabled:pointer-events-none disabled:opacity-50',
        destructive && 'text-error hover:bg-error-background',
        className,
      )}
      {...props}
    />
  );
}

function WorkflowHistoryTable({ instances, definitions }: { readonly instances: readonly WorkflowInstance[]; readonly definitions: readonly WorkflowDefinition[] }) {
  const navigate = useNavigate();
  const definitionsById = new Map(definitions.filter((definition): definition is WorkflowDefinition & { definitionId: string } => !!definition.definitionId).map((definition) => [definition.definitionId, definition]));

  function openInstance(instance: WorkflowInstance) {
    void navigate({ to: '/administration/automation/workflow-instances/$instanceId', params: { instanceId: instance.id ?? '' } });
  }

  return (
    <div className="overflow-hidden rounded-structural border border-border">
      <div className="hidden grid-cols-[minmax(0,1.4fr)_7rem_7rem_9rem_9rem] gap-4 border-b border-border bg-hover-gray px-4 py-3 text-[12px] font-semibold uppercase text-muted-foreground lg:grid">
        <span>Name</span>
        <span>Status</span>
        <span>Incidents</span>
        <span>Started</span>
        <span>Finished</span>
      </div>

      <div className="divide-y divide-border">
        {instances.map((instance) => (
          <div
            key={instance.id ?? instance.correlationId ?? instance.createdAt}
            role="link"
            tabIndex={0}
            className="grid cursor-pointer gap-4 px-4 py-4 transition hover:bg-hover-gray focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-primary/20 lg:grid-cols-[minmax(0,1.4fr)_7rem_7rem_9rem_9rem] lg:items-center"
            onClick={() => openInstance(instance)}
            onKeyDown={(event) => {
              if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                openInstance(instance);
              }
            }}
          >
            <div className="min-w-0">
              <p className="truncate text-[14px] font-semibold">{getWorkflowInstanceDisplayName(instance, definitionsById)}</p>
              <p className="mt-1 truncate text-[13px] text-muted-foreground">{instance.id || instance.correlationId || instance.definitionId}</p>
            </div>
            <div><InstanceStatusBadge instance={instance} /></div>
            <p className="text-[14px] text-muted-foreground">{instance.incidentCount ?? 0}</p>
            <p className="text-[14px] text-muted-foreground">{formatDate(instance.createdAt)}</p>
            <p className="text-[14px] text-muted-foreground">{formatDate(instance.finishedAt)}</p>
          </div>
        ))}
      </div>
    </div>
  );
}

function getWorkflowInstanceDisplayName(instance: WorkflowInstance, definitionsById: ReadonlyMap<string, WorkflowDefinition>) {
  const definition = instance.definitionId ? definitionsById.get(instance.definitionId) : undefined;
  const resolvedName = definition?.name || instance.name || 'Workflow instance';
  const resolvedVersion = instance.version ?? definition?.version;

  return resolvedVersion ? `${resolvedName} v${resolvedVersion}` : resolvedName;
}

function PublicationBadge({ definition }: { readonly definition: WorkflowDefinition }) {
  if (definition.isPublished) {
    return <Badge variant="success">Published</Badge>;
  }

  if (definition.isLatest) {
    return <Badge variant="secondary">Draft</Badge>;
  }

  return <Badge variant="outline">Archived</Badge>;
}

function InstanceStatusBadge({ instance }: { readonly instance: WorkflowInstance }) {
  const status = formatStatus(instance.status, 'Status');
  const variant: BadgeVariant = Number(instance.incidentCount ?? 0) > 0 ? 'error' : status === 'Finished' ? 'success' : 'secondary';

  return <Badge variant={variant}>{status}</Badge>;
}

function SkeletonTable({ rows, columns }: { readonly rows: number; readonly columns: readonly (number | string)[] }) {
  return (
    <div className="overflow-hidden rounded-structural border border-border">
      <div className="divide-y divide-border">
        {Array.from({ length: rows }, (_, index) => (
          <div key={index} className="flex items-center gap-4 px-4 py-4">
            {columns.map((col, colIndex) => (
              <Skeleton
                key={colIndex}
                className="h-4"
                style={{
                  flex: typeof col === 'number' ? `${col} ${col} 0` : undefined,
                  width: typeof col === 'string' ? col : undefined,
                }}
              />
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

function StatusMessage({ children, tone = 'muted' }: { readonly children: string; readonly tone?: 'muted' | 'error' }) {
  return <p className={cn('rounded-structural border border-border p-4 text-[14px]', tone === 'error' ? 'text-error' : 'text-muted-foreground')}>{children}</p>;
}

async function fetchElsa<TResponse>(path: string, accessToken: string | undefined, init?: RequestInit): Promise<TResponse> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`Elsa API request failed with status ${response.status}.`);
  }

  if (response.status === 204 || response.headers.get('content-length') === '0') {
    return undefined as TResponse;
  }

  return response.json() as Promise<TResponse>;
}

function updateWorkflowDefinition(definition: WorkflowDefinition, action: 'publish' | 'retract', accessToken: string | undefined) {
  return fetchElsa<void>(`/elsa/api/workflow-definitions/${encodeURIComponent(getDefinitionRouteId(definition))}/${action}`, accessToken, { method: 'POST' });
}

function deleteWorkflowDefinition(definition: WorkflowDefinition, accessToken: string | undefined) {
  return fetchElsa<void>(`/elsa/api/workflow-definitions/${encodeURIComponent(getDefinitionRouteId(definition))}`, accessToken, { method: 'DELETE' });
}

function createWorkflowDefinition(name: string, accessToken: string | undefined) {
  const trimmedName = name.trim();

  return fetchElsa<WorkflowDefinitionModel>('/elsa/api/workflow-definitions', accessToken, {
    method: 'POST',
    body: JSON.stringify({
      model: {
        definitionId: null,
        name: trimmedName,
        description: null,
        toolVersion: '3.7.1',
        variables: null,
        inputs: null,
        outputs: null,
        outcomes: null,
        customProperties: null,
        isReadonly: false,
        options: null,
        root: {
          id: createFlowchartRootId(),
          type: 'Elsa.Flowchart',
          version: 1,
          name: 'Flowchart1',
        },
        links: null,
        createdAt: '0001-01-01T00:00:00+00:00',
        version: 1,
        isLatest: true,
        isPublished: false,
        id: null,
      },
      publish: null,
    }),
  });
}

function createFlowchartRootId() {
  return crypto.getRandomValues(new Uint8Array(8)).reduce((value, byte) => value + byte.toString(16).padStart(2, '0'), '');
}

function getDefinitionRouteId(definition: WorkflowDefinition) {
  return definition.definitionId ?? definition.id ?? '';
}

function getActiveTab(pathname: string, searchStr: string): WorkflowTab {
  const tab = new URLSearchParams(searchStr).get('tab');

  if (tab === 'history' || pathname.endsWith('/workflow-instances')) {
    return 'history';
  }

  return 'definitions';
}

function formatDate(value: string | null | undefined) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function formatStatus(value: number | string | undefined, fallback: string) {
  if (value === undefined || value === null) {
    return fallback;
  }

  const knownStatuses: Record<number, string> = {
    0: 'Idle',
    1: 'Running',
    2: 'Finished',
    3: 'Canceled',
    4: 'Faulted',
  };

  return knownStatuses[Number(value)] ?? String(value);
}
