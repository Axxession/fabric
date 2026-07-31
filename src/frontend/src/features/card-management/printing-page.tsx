import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { Eye, Pencil, Plus, Printer, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

import { formatDateTime, printingBatchesQueryKey, printingRunsQueryKey, type Encoder, type EncodingBatch, type EncodingRun, type Transformation } from './card-management-types';

const printingPageEncodersQueryKey = ['card-management', 'printing', 'printing-page', 'encoders'] as const;
const printingPageTransformationsQueryKey = ['card-management', 'printing-page', 'transformations'] as const;

export default function PrintingPage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const batchesQuery = useQuery({
    queryKey: printingBatchesQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/encoding-batches', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error('Could not load print batches.');
      }
      return data;
    },
  });

  const transformationsQuery = useQuery({
    queryKey: printingPageTransformationsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/transformations', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error('Could not load transformations.');
      }
      return data;
    },
  });

  const runsQuery = useQuery({
    queryKey: [...printingRunsQueryKey, 'all'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/encoding-runs', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error('Could not load encoding runs.');
      }
      return data.items ?? [];
    },
  });

  const encodersQuery = useQuery({
    queryKey: printingPageEncodersQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/encoders', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error('Could not load encoders.');
      }
      return data;
    },
  });

  const deleteEncoder = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/desfire/encoders/{id}', { params: { path: { id } } });
      if (error) {
        throw new Error('Could not delete encoder.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: printingPageEncodersQueryKey });
      toast.success('Encoder deleted.');
    },
    onError: () => toast.error('Could not delete encoder. Print history may reference it.'),
  });

  const batches = batchesQuery.data?.items ?? [];
  const encoders = encodersQuery.data?.items ?? [];
  const runs = runsQuery.data ?? [];
  const transformationById = new Map((transformationsQuery.data?.items ?? []).map((transformation) => [transformation.id, transformation]));

  return (
    <section className="rounded-structural border border-border bg-content">
      <div className="flex flex-col gap-3 border-b border-border p-4 sm:flex-row sm:items-start sm:justify-between sm:p-6">
        <div>
          <h1 className="text-[20px] font-semibold tracking-tight">Printing</h1>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Schedule DESFire card print batches and inspect every card encoding run.</p>
        </div>
        <Link to="/desfire-studio/printing/new" className={buttonVariants({ className: 'w-full sm:w-fit' })}>
          <Plus className="size-4" aria-hidden="true" />
          Schedule print batch
        </Link>
      </div>

      <div className="p-4 sm:p-6">
        <Tabs defaultValue="batches">
          <TabsList>
            <TabsTrigger value="batches">Print batches</TabsTrigger>
            <TabsTrigger value="runs">Runs</TabsTrigger>
            <TabsTrigger value="encoders">Encoders</TabsTrigger>
          </TabsList>
          <TabsContent value="batches">
            {batchesQuery.isError ? <PanelError>Could not load print batches.</PanelError> : null}
            {batchesQuery.isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading print batches...</p> : null}
            {!batchesQuery.isLoading && !batchesQuery.isError && batches.length === 0 ? <Empty><EmptyHeader><EmptyTitle>No print batches yet</EmptyTitle><EmptyDescription>Schedule a print batch from a transformation and CSV rows.</EmptyDescription></EmptyHeader></Empty> : null}
            {batches.length > 0 ? <PrintBatchesTable batches={batches} transformationById={transformationById} /> : null}
          </TabsContent>
          <TabsContent value="runs">
            {runsQuery.isError ? <PanelError>Could not load encoding runs.</PanelError> : null}
            {runsQuery.isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading encoding runs...</p> : null}
            {!runsQuery.isLoading && !runsQuery.isError && runs.length === 0 ? <Empty><EmptyHeader><EmptyTitle>No runs yet</EmptyTitle><EmptyDescription>Single kiosk runs and batch item runs will appear here.</EmptyDescription></EmptyHeader></Empty> : null}
            {runs.length > 0 ? <RunsTable runs={runs} transformationById={transformationById} encoders={encoders} onOpenRun={(runId) => navigate({ to: '/desfire-studio/printing/runs/$runId', params: { runId } })} /> : null}
          </TabsContent>
          <TabsContent value="encoders">
            <EncodersPanel encoders={encoders} isLoading={encodersQuery.isLoading} isError={encodersQuery.isError} isDeleting={deleteEncoder.isPending} onDelete={(encoder) => {
              if (window.confirm(`Delete encoder "${encoder.name}"?`)) {
                deleteEncoder.mutate(encoder.id);
              }
            }} />
          </TabsContent>
        </Tabs>
      </div>
    </section>
  );
}

function EncodersPanel({ encoders, isLoading, isError, isDeleting, onDelete }: { readonly encoders: Encoder[]; readonly isLoading: boolean; readonly isError: boolean; readonly isDeleting: boolean; readonly onDelete: (encoder: Encoder) => void }) {
  return (
    <div className="grid gap-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-[16px] font-semibold tracking-tight">Encoders</h2>
          <p className="mt-1 text-[14px] text-muted-foreground">Named DESFire encoder bindings used when scheduling print batches.</p>
        </div>
        <Link to="/desfire-studio/printing/encoders/new" className={buttonVariants({ className: 'w-full sm:w-fit' })}><Plus className="size-4" aria-hidden="true" />Add encoder</Link>
      </div>
      {isError ? <PanelError>Could not load encoders.</PanelError> : null}
      {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading encoders...</p> : null}
      {!isLoading && !isError && encoders.length === 0 ? <Empty><EmptyHeader><EmptyTitle>No encoders yet</EmptyTitle><EmptyDescription>Add an encoder before scheduling print batches.</EmptyDescription></EmptyHeader></Empty> : null}
      {encoders.length > 0 ? <EncodersTable encoders={encoders} isDeleting={isDeleting} onDelete={onDelete} /> : null}
    </div>
  );
}

function EncodersTable({ encoders, isDeleting, onDelete }: { readonly encoders: Encoder[]; readonly isDeleting: boolean; readonly onDelete: (encoder: Encoder) => void }) {
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[68rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Hardware</th><th className="px-4 py-3 font-semibold">State</th><th className="px-4 py-3 font-semibold">Capabilities</th><th className="px-4 py-3 font-semibold">Updated</th><th className="px-4 py-3 text-right font-semibold">Actions</th></tr></thead>
        <tbody className="divide-y divide-border">
          {encoders.map((encoder) => <tr key={encoder.id}><td className="px-4 py-4 font-medium text-foreground">{encoder.name}</td><td className="px-4 py-4 text-muted-foreground">{encoder.agentId} / {encoder.deviceId}</td><td className="px-4 py-4"><Badge variant={encoder.enabled ? 'success' : 'secondary'}>{encoder.enabled ? 'Enabled' : 'Disabled'}</Badge></td><td className="px-4 py-4"><div className="flex flex-wrap gap-2"><Badge variant={encoder.supportsEncoding ? 'success' : 'secondary'}>Encoding</Badge><Badge variant={encoder.supportsPrinting ? 'success' : 'secondary'}>{encoder.supportsPrinting ? 'Printing' : 'No printing'}</Badge></div></td><td className="px-4 py-4 text-muted-foreground">{formatDateTime(encoder.updatedAt)}</td><td className="px-4 py-4"><div className="flex justify-end gap-2"><Link to="/desfire-studio/printing/encoders/$encoderId/edit" params={{ encoderId: encoder.id }} className={buttonVariants({ variant: 'outline', size: 'sm' })}><Pencil className="size-4" aria-hidden="true" />Edit</Link><Button type="button" variant="outline" size="sm" disabled={isDeleting} onClick={() => onDelete(encoder)}><Trash2 className="size-4" aria-hidden="true" />Delete</Button></div></td></tr>)}
        </tbody>
      </table>
    </div>
  );
}

function PrintBatchesTable({ batches, transformationById }: { readonly batches: EncodingBatch[]; readonly transformationById: Map<string, Transformation> }) {
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[70rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Transformation</th><th className="px-4 py-3 font-semibold">Status</th><th className="px-4 py-3 font-semibold">Progress</th><th className="px-4 py-3 font-semibold">Failures</th><th className="px-4 py-3 font-semibold">Created</th><th className="px-4 py-3 text-right font-semibold">Actions</th></tr>
        </thead>
        <tbody className="divide-y divide-border">
          {batches.map((batch) => (
            <tr key={batch.id}>
              <td className="px-4 py-4 font-medium text-foreground"><span className="inline-flex items-center gap-2"><Printer className="size-4 text-primary" aria-hidden="true" />{batch.name}</span></td>
              <td className="px-4 py-4 text-muted-foreground">{transformationById.get(batch.transformationId)?.name ?? batch.transformationId}</td>
              <td className="px-4 py-4"><StatusBadge status={batch.status} /></td>
              <td className="px-4 py-4 text-muted-foreground">{Number(batch.succeededRuns)} / {Number(batch.totalRuns)} printed</td>
              <td className="px-4 py-4 text-muted-foreground">{Number(batch.failedRuns) + Number(batch.cancelledRuns)}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatDateTime(batch.createdAt)}</td>
              <td className="px-4 py-4">
                <div className="flex justify-end gap-2">
                  <Link to="/desfire-studio/printing/$batchId" params={{ batchId: batch.id }} className={buttonVariants({ variant: 'outline', size: 'sm' })}><Eye className="size-4" aria-hidden="true" />View</Link>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function RunsTable({ runs, transformationById, encoders, onOpenRun }: { readonly runs: EncodingRun[]; readonly transformationById: Map<string, Transformation>; readonly encoders: Encoder[]; readonly onOpenRun: (runId: string) => void }) {
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[84rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr><th className="px-4 py-3 font-semibold">Transformation</th><th className="px-4 py-3 font-semibold">Kind</th><th className="px-4 py-3 font-semibold">Source</th><th className="px-4 py-3 font-semibold">Status</th><th className="px-4 py-3 font-semibold">Card UID</th><th className="px-4 py-3 font-semibold">Device</th><th className="px-4 py-3 font-semibold">Requested</th><th className="px-4 py-3 text-right font-semibold">Actions</th></tr>
        </thead>
        <tbody className="divide-y divide-border">
          {runs.map((run) => (
            <tr key={run.id} className="cursor-pointer transition hover:bg-hover-gray" onClick={() => onOpenRun(run.id)}>
              <td className="px-4 py-4 text-muted-foreground">{transformationById.get(run.transformationId)?.name ?? run.transformationId}</td>
              <td className="px-4 py-4 text-muted-foreground">{run.kind}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatRunSource(run)}</td>
              <td className="px-4 py-4"><StatusBadge status={run.status} /></td>
              <td className="px-4 py-4 text-muted-foreground">{run.cardUid ?? 'Not read'}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatRunDevice(run, encoders)}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatDateTime(run.requestedAt)}</td>
              <td className="px-4 py-4" onClick={(event) => event.stopPropagation()}><div className="flex justify-end"><Link to="/desfire-studio/printing/runs/$runId" params={{ runId: run.id }} className={buttonVariants({ variant: 'outline', size: 'sm' })}><Eye className="size-4" aria-hidden="true" />View</Link></div></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function formatRunDevice(run: EncodingRun, encoders: Encoder[]) {
  const encoder = encoders.find((item) => item.id === run.encoderId);
  const hardware = run.hardwareAgentId && run.deviceId ? `${run.hardwareAgentId} / ${run.deviceId}` : 'Unassigned';
  return encoder ? `${encoder.name} (${hardware})` : hardware;
}

function formatRunSource(run: EncodingRun) {
  return run.source ?? '-';
}

export function StatusBadge({ status }: { readonly status: string }) {
  const variant = status === 'Completed' || status === 'Succeeded' ? 'success' : status === 'Failed' || status === 'Timeout' || status === 'DeviceUnavailable' ? 'error' : status === 'Running' || status === 'Claimed' ? 'warning' : 'secondary';
  return <Badge variant={variant}>{status}</Badge>;
}

export function JsonDetails({ title, value }: { readonly title: string; readonly value: unknown }) {
  return (
    <details className="rounded-structural border border-border bg-content p-4">
      <summary className="cursor-pointer text-[14px] font-semibold text-foreground">{title}</summary>
      <pre className="mt-3 max-h-[28rem] overflow-auto rounded-interactive bg-hover-gray p-3 text-[12px] text-muted-foreground">{JSON.stringify(value, null, 2)}</pre>
    </details>
  );
}

function PanelError({ children }: { readonly children: React.ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
