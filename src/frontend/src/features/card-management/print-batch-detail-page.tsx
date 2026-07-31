import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, Eye } from 'lucide-react';

import { api } from '@/shared/api/client';
import { buttonVariants } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';

import { formatDateTime, printingBatchesQueryKey, printingRunsQueryKey, type Encoder, type EncodingRun, type Transformation } from './card-management-types';
import { JsonDetails, StatusBadge } from './printing-page';

const printBatchDetailEncodersQueryKey = ['card-management', 'printing', 'print-batch-detail-page', 'encoders'] as const;
const printBatchDetailTransformationsQueryKey = ['card-management', 'print-batch-detail-page', 'transformations'] as const;

export default function PrintBatchDetailPage() {
  const { batchId } = useParams({ from: '/desfire-studio/printing/$batchId' });

  return <PrintBatchDetailPageContent batchId={batchId} />;
}

export function PrintBatchDetailPageContent({ batchId }: { readonly batchId: string }) {

  const batchQuery = useQuery({
    queryKey: [...printingBatchesQueryKey, batchId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/encoding-batches/{id}', { params: { path: { id: batchId } } });
      if (error || !data) {
        throw new Error('Could not load print batch.');
      }
      return data;
    },
  });

  const runsQuery = useQuery({
    queryKey: [...printingRunsQueryKey, 'batch', batchId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/encoding-runs', { params: { query: { Page: 0, PageSize: 500, batchId } } });
      if (error || !data) {
        throw new Error('Could not load print runs.');
      }
      return data.items ?? [];
    },
  });

  const transformationsQuery = useQuery({
    queryKey: printBatchDetailTransformationsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/transformations', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error('Could not load transformations.');
      }
      return data;
    },
  });

  const encodersQuery = useQuery({
    queryKey: printBatchDetailEncodersQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/encoders', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error('Could not load encoders.');
      }
      return data;
    },
  });

  const batch = batchQuery.data;
  const transformation = (transformationsQuery.data?.items ?? []).find((item) => item.id === batch?.transformationId);
  const encoder = (encodersQuery.data?.items ?? []).find((item) => item.id === batch?.encoderId);
  const runs = runsQuery.data ?? [];

  if (batchQuery.isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">Loading print batch...</p>;
  }

  if (!batch) {
    return <PanelError>Could not load print batch.</PanelError>;
  }

  return (
    <section className="grid gap-6">
      <Link to="/desfire-studio/printing" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground"><ArrowLeft className="size-4" />Back to printing</Link>
      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>{batch.name}</CardTitle>
              <CardDescription>{transformation?.name ?? batch.transformationId}</CardDescription>
            </div>
            <StatusBadge status={batch.status} />
          </div>
        </CardHeader>
        <CardContent className="grid gap-4">
          <div className="grid gap-3 md:grid-cols-4">
            <Info label="Total" value={String(batch.totalRuns)} />
            <Info label="Succeeded" value={String(batch.succeededRuns)} />
            <Info label="Failed" value={String(Number(batch.failedRuns) + Number(batch.cancelledRuns))} />
            <Info label="Created" value={formatDateTime(batch.createdAt)} />
            <Info label="Encoder" value={encoder?.name ?? batch.encoderId ?? 'Unknown'} />
          </div>
          <JsonDetails title="Original input" value={batch.originalInput} />
          <NormalizedRowsTable rows={Array.isArray(batch.normalizedRows) ? (batch.normalizedRows as Record<string, string>[]) : []} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Card Runs</CardTitle>
          <CardDescription>Every row scheduled in this print batch.</CardDescription>
        </CardHeader>
        <CardContent>
          {runsQuery.isError ? <PanelError>Could not load print runs.</PanelError> : null}
          {runsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading print runs...</p> : null}
          {runs.length > 0 ? <RunsTable runs={runs} transformation={transformation} encoders={encodersQuery.data?.items ?? []} /> : null}
        </CardContent>
      </Card>
    </section>
  );
}

function RunsTable({ runs, transformation, encoders }: { readonly runs: EncodingRun[]; readonly transformation?: Transformation; readonly encoders: Encoder[] }) {
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[72rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr><th className="px-4 py-3 font-semibold">Input</th><th className="px-4 py-3 font-semibold">Status</th><th className="px-4 py-3 font-semibold">Card UID</th><th className="px-4 py-3 font-semibold">Device</th><th className="px-4 py-3 font-semibold">Requested</th><th className="px-4 py-3 text-right font-semibold">Actions</th></tr>
        </thead>
        <tbody className="divide-y divide-border">
          {runs.map((run) => (
            <tr key={run.id}>
              <td className="max-w-[22rem] truncate px-4 py-4 text-muted-foreground">{summarizeInput(run.input, transformation)}</td>
              <td className="px-4 py-4"><StatusBadge status={run.status} /></td>
              <td className="px-4 py-4 text-muted-foreground">{run.cardUid ?? 'Not read'}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatRunDevice(run, encoders)}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatDateTime(run.requestedAt)}</td>
              <td className="px-4 py-4"><div className="flex justify-end"><Link to="/desfire-studio/printing/runs/$runId" params={{ runId: run.id }} className={buttonVariants({ variant: 'outline', size: 'sm' })}><Eye className="size-4" aria-hidden="true" />View</Link></div></td>
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

function summarizeInput(input: unknown, transformation?: Transformation) {
  if (!input || typeof input !== 'object' || Array.isArray(input)) {
    return JSON.stringify(input);
  }
  const row = input as Record<string, unknown>;
  const fields = transformation?.requiredVariables.length ? transformation.requiredVariables : Object.keys(row);
  return fields.slice(0, 4).map((field) => `${field}: ${String(row[field] ?? '')}`).join(', ');
}

function Info({ label, value }: { readonly label: string; readonly value: string }) {
  return <div className="rounded-interactive border border-border p-3"><div className="text-[12px] uppercase text-muted-foreground">{label}</div><div className="mt-1 text-[14px] font-medium text-foreground">{value}</div></div>;
}

function NormalizedRowsTable({ rows }: { readonly rows: Record<string, string>[] }) {
  const headers = rows.length > 0 ? Object.keys(rows[0]) : [];
  return (
    <details className="rounded-structural border border-border bg-content p-4">
      <summary className="cursor-pointer text-[14px] font-semibold text-foreground">Normalized rows</summary>
      <div className="mt-3 overflow-x-auto rounded-interactive border border-border">
        <table className="w-full min-w-[36rem] border-collapse text-left text-[13px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>{headers.map((header) => <th key={header} className="px-3 py-2 font-semibold">{header}</th>)}</tr>
          </thead>
          <tbody className="divide-y divide-border">
            {rows.map((row, index) => (
              <tr key={index}>{headers.map((header) => <td key={header} className="px-3 py-2 text-muted-foreground">{row[header]}</td>)}</tr>
            ))}
          </tbody>
        </table>
      </div>
    </details>
  );
}

function PanelError({ children }: { readonly children: React.ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
