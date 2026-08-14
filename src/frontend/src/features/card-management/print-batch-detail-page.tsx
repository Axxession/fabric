import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, Eye } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { api } from '@/shared/api/client';
import { buttonVariants } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { i18n } from '@/shared/i18n/i18n';

import { formatDateTime, printingBatchesQueryKey, printingRunsQueryKey, type BadgeJob, type Encoder, type PrintDesignSummary, type Transformation } from './card-management-types';
import { JsonDetails, StatusBadge } from './printing-page';

const printBatchDetailEncodersQueryKey = ['card-management', 'printing', 'print-batch-detail-page', 'encoders'] as const;
const printBatchDetailTransformationsQueryKey = ['card-management', 'print-batch-detail-page', 'transformations'] as const;
const printBatchDetailPrintDesignsQueryKey = ['card-management', 'print-batch-detail-page', 'print-designs'] as const;

export default function PrintBatchDetailPage() {
  const { batchId } = useParams({ from: '/desfire-studio/printing/$batchId' });

  return <PrintBatchDetailPageContent batchId={batchId} />;
}

export function PrintBatchDetailPageContent({ batchId }: { readonly batchId: string }) {
  const { t } = useTranslation();

  const batchQuery = useQuery({
    queryKey: [...printingBatchesQueryKey, batchId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/badge-batches/{id}', { params: { path: { id: batchId } } });
      if (error || !data) {
        throw new Error(t('cardManagement.printing.couldNotLoadPrintBatch'));
      }
      return data;
    },
  });

  const runsQuery = useQuery({
    queryKey: [...printingRunsQueryKey, 'batch', batchId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/badge-jobs', { params: { query: { Page: 0, PageSize: 500, batchId } } });
      if (error || !data) {
        throw new Error(t('cardManagement.printing.couldNotLoadPrintRuns'));
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

  const printDesignsQuery = useQuery({
    queryKey: printBatchDetailPrintDesignsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/printing/designs', { params: { query: { SurfaceKind: 'Card', ids: [] } } });
      if (error || !data) {
        throw new Error(t('cardManagement.printing.couldNotLoadPrintDesigns'));
      }
      return data;
    },
  });

  const encodersQuery = useQuery({
    queryKey: printBatchDetailEncodersQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/encoders', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error(t('cardManagement.printing.couldNotLoadEncoders'));
      }
      return data;
    },
  });

  const batch = batchQuery.data;
  const transformation = (transformationsQuery.data?.items ?? []).find((item) => item.id === batch?.transformationId);
  const printDesign = (printDesignsQuery.data?.items ?? []).find((item) => item.id === batch?.printDesignId);
  const encoder = (encodersQuery.data?.items ?? []).find((item) => item.id === batch?.encoderId);
  const runs = runsQuery.data ?? [];

  if (batchQuery.isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('cardManagement.printing.printBatchLoading')}</p>;
  }

  if (!batch) {
    return <PanelError>{t('cardManagement.printing.couldNotLoadPrintBatch')}</PanelError>;
  }

  return (
    <section className="grid gap-6">
      <Link to="/desfire-studio/printing" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground"><ArrowLeft className="size-4" />{t('cardManagement.printing.backToPrinting')}</Link>
      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>{batch.name}</CardTitle>
              <CardDescription>{formatBatchSubtitle(transformation?.name, printDesign?.name, t)}</CardDescription>
            </div>
            <StatusBadge status={batch.status} />
          </div>
        </CardHeader>
        <CardContent className="grid gap-4">
          <div className="grid gap-3 md:grid-cols-4">
            <Info label={t('cardManagement.printing.total')} value={String(batch.totalJobs)} />
            <Info label={t('cardManagement.printing.succeeded')} value={String(batch.succeededJobs)} />
            <Info label={t('cardManagement.printing.failed')} value={String(Number(batch.failedJobs) + Number(batch.cancelledJobs))} />
            <Info label={t('cardManagement.printing.created')} value={formatDateTime(batch.createdAt)} />
            <Info label={t('cardManagement.printing.encoder')} value={encoder?.name ?? batch.encoderId ?? t('cardManagement.printing.unknown')} />
            <Info label={t('cardManagement.printing.transformation')} value={transformation?.name ?? (batch.transformationId ?? '-')} />
            <Info label={t('cardManagement.printing.printDesign')} value={printDesign?.name ?? (batch.printDesignId ?? '-')} />
            <Info label={t('cardManagement.printing.jobType')} value={formatJobType(batch.transformationId, batch.printDesignId, t)} />
          </div>
          <JsonDetails title={t('cardManagement.printing.jsonOriginalInput')} value={batch.originalInput} />
          <NormalizedRowsTable rows={Array.isArray(batch.normalizedRows) ? (batch.normalizedRows as Record<string, string>[]) : []} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('cardManagement.printing.cardRuns')}</CardTitle>
          <CardDescription>{t('cardManagement.printing.cardRunsDescription')}</CardDescription>
        </CardHeader>
        <CardContent>
          {runsQuery.isError ? <PanelError>{t('cardManagement.printing.couldNotLoadPrintRuns')}</PanelError> : null}
          {runsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">{t('cardManagement.printing.loadingPrintRuns')}</p> : null}
          {runs.length > 0 ? <RunsTable runs={runs} transformation={transformation} printDesign={printDesign} encoders={encodersQuery.data?.items ?? []} /> : null}
        </CardContent>
      </Card>
    </section>
  );
}

function RunsTable({ runs, transformation, printDesign, encoders }: { readonly runs: BadgeJob[]; readonly transformation?: Transformation; readonly printDesign?: PrintDesignSummary; readonly encoders: Encoder[] }) {
  const { t } = useTranslation();
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[84rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr><th className="px-4 py-3 font-semibold">{t('cardManagement.printing.input')}</th><th className="px-4 py-3 font-semibold">{t('cardManagement.printing.status')}</th><th className="px-4 py-3 font-semibold">{t('cardManagement.printing.cardUid')}</th><th className="px-4 py-3 font-semibold">{t('cardManagement.printing.device')}</th><th className="px-4 py-3 font-semibold">{t('cardManagement.printing.requested')}</th><th className="px-4 py-3 text-right font-semibold">{t('cardManagement.printing.actions')}</th></tr>
        </thead>
        <tbody className="divide-y divide-border">
          {runs.map((run) => (
            <tr key={run.id}>
              <td className="max-w-[22rem] truncate px-4 py-4 text-muted-foreground">{summarizeInput(run.input, transformation, printDesign)}</td>
              <td className="px-4 py-4"><StatusBadge status={run.status} /></td>
              <td className="px-4 py-4 text-muted-foreground">{run.cardUid ?? t('cardManagement.printing.notRead')}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatRunDevice(run, encoders)}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatDateTime(run.requestedAt)}</td>
              <td className="px-4 py-4"><div className="flex justify-end"><Link to="/desfire-studio/printing/runs/$runId" params={{ runId: run.id }} className={buttonVariants({ variant: 'outline', size: 'sm' })}><Eye className="size-4" aria-hidden="true" />{t('cardManagement.printing.view')}</Link></div></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function formatRunDevice(run: BadgeJob, encoders: Encoder[]) {
  const encoder = encoders.find((item) => item.id === run.encoderId);
  const hardware = run.hardwareAgentId && run.deviceId ? `${run.hardwareAgentId} / ${run.deviceId}` : i18n.t('cardManagement.printing.unassigned');
  return encoder ? `${encoder.name} (${hardware})` : hardware;
}

function summarizeInput(input: unknown, transformation?: Transformation, printDesign?: PrintDesignSummary) {
  if (!input || typeof input !== 'object' || Array.isArray(input)) {
    return JSON.stringify(input);
  }
  const row = input as Record<string, unknown>;
  const fields = transformation?.requiredVariables.length ? transformation.requiredVariables : Object.keys(row);
  const summary = fields.slice(0, 4).map((field) => `${field}: ${String(row[field] ?? '')}`).join(', ');
  return summary || printDesign?.name || '{}';
}

function formatBatchSubtitle(transformationName: string | undefined, printDesignName: string | undefined, t: ReturnType<typeof useTranslation>['t']) {
  if (transformationName && printDesignName) {
    return t('cardManagement.printing.batchSubtitleEncodeAndPrint', { transformation: transformationName, printDesign: printDesignName });
  }
  if (transformationName) {
    return transformationName;
  }
  if (printDesignName) {
    return printDesignName;
  }
  return '-';
}

function formatJobType(transformationId: string | null | undefined, printDesignId: string | null | undefined, t: ReturnType<typeof useTranslation>['t']) {
  if (transformationId && printDesignId) {
    return t('cardManagement.printing.jobTypeEncodeAndPrint');
  }
  if (transformationId) {
    return t('cardManagement.printing.jobTypeEncodeOnly');
  }
  if (printDesignId) {
    return t('cardManagement.printing.jobTypePrintOnly');
  }
  return '-';
}

function Info({ label, value }: { readonly label: string; readonly value: string }) {
  return <div className="rounded-interactive border border-border p-3"><div className="text-[12px] uppercase text-muted-foreground">{label}</div><div className="mt-1 text-[14px] font-medium text-foreground">{value}</div></div>;
}

function NormalizedRowsTable({ rows }: { readonly rows: Record<string, string>[] }) {
  const { t } = useTranslation();
  const headers = rows.length > 0 ? Object.keys(rows[0]) : [];
  return (
    <details className="rounded-structural border border-border bg-content p-4">
      <summary className="cursor-pointer text-[14px] font-semibold text-foreground">{t('cardManagement.printing.normalizedRows')}</summary>
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
