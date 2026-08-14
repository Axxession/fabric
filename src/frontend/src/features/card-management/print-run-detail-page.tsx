import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { api } from '@/shared/api/client';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';

import { formatDateTime, printingRunsQueryKey } from './card-management-types';
import { JsonDetails, StatusBadge } from './printing-page';

const printRunDetailEncodersQueryKey = ['card-management', 'printing', 'print-run-detail-page', 'encoders'] as const;
const printRunDetailTransformationsQueryKey = ['card-management', 'print-run-detail-page', 'transformations'] as const;
const printRunDetailPrintDesignsQueryKey = ['card-management', 'print-run-detail-page', 'print-designs'] as const;

export default function PrintRunDetailPage() {
  const { runId } = useParams({ from: '/desfire-studio/printing/runs/$runId' });

  return <PrintRunDetailPageContent runId={runId} />;
}

export function PrintRunDetailPageContent({ runId }: { readonly runId: string }) {
  const { t } = useTranslation();

  const runQuery = useQuery({
    queryKey: [...printingRunsQueryKey, runId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/badge-jobs/{id}', { params: { path: { id: runId } } });
      if (error || !data) {
        throw new Error(t('cardManagement.printing.couldNotLoadPrintRun'));
      }
      return data;
    },
  });

  const transformationsQuery = useQuery({
    queryKey: printRunDetailTransformationsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/transformations', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error('Could not load transformations.');
      }
      return data;
    },
  });

  const printDesignsQuery = useQuery({
    queryKey: printRunDetailPrintDesignsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/printing/designs', { params: { query: { SurfaceKind: 'Card', ids: [] } } });
      if (error || !data) {
        throw new Error(t('cardManagement.printing.couldNotLoadPrintDesigns'));
      }
      return data;
    },
  });

  const encodersQuery = useQuery({
    queryKey: printRunDetailEncodersQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/encoders', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error(t('cardManagement.printing.couldNotLoadEncoders'));
      }
      return data;
    },
  });

  const run = runQuery.data;
  const transformation = (transformationsQuery.data?.items ?? []).find((item) => item.id === run?.transformationId);
  const printDesign = (printDesignsQuery.data?.items ?? []).find((item) => item.id === run?.printDesignId);
  const encoder = (encodersQuery.data?.items ?? []).find((item) => item.id === run?.encoderId);

  if (runQuery.isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('cardManagement.printing.printRunLoading')}</p>;
  }

  if (!run) {
    return <PanelError>{t('cardManagement.printing.couldNotLoadPrintRun')}</PanelError>;
  }

  return (
    <section className="grid gap-6">
      <Link to={run.batchId ? '/desfire-studio/printing/$batchId' : '/desfire-studio/printing'} params={run.batchId ? { batchId: run.batchId } : undefined} className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground"><ArrowLeft className="size-4" />{t('cardManagement.printing.backToBatch')}</Link>
      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>{t('cardManagement.printing.printRunTitle')}</CardTitle>
              <CardDescription>{formatRunSubtitle(transformation?.name, printDesign?.name, t)}</CardDescription>
            </div>
            <StatusBadge status={run.status} />
          </div>
        </CardHeader>
        <CardContent className="grid gap-4">
          {run.errorMessage ? <PanelError>{run.errorMessage}</PanelError> : null}
          <div className="grid gap-3 md:grid-cols-3">
            <Info label={t('cardManagement.printing.cardUid')} value={run.cardUid ?? t('cardManagement.printing.notRead')} />
            <Info label={t('cardManagement.printing.encoder')} value={encoder?.name ?? run.encoderId ?? t('cardManagement.printing.unknown')} />
            <Info label={t('cardManagement.printing.device')} value={run.hardwareAgentId && run.deviceId ? `${run.hardwareAgentId} / ${run.deviceId}` : t('cardManagement.printing.unassigned')} />
            <Info label={t('cardManagement.printing.requested')} value={formatDateTime(run.requestedAt)} />
            <Info label={t('cardManagement.printing.started')} value={run.startedAt ? formatDateTime(run.startedAt) : t('cardManagement.printing.notStarted')} />
            <Info label={t('cardManagement.printing.completed')} value={run.completedAt ? formatDateTime(run.completedAt) : t('cardManagement.printing.notCompleted')} />
            <Info label={t('cardManagement.printing.kind')} value={run.kind} />
            <Info label={t('cardManagement.printing.source')} value={run.source ?? t('cardManagement.printing.unknown')} />
            <Info label={t('cardManagement.printing.jobType')} value={formatJobType(run.transformationId, run.printDesignId, t)} />
            <Info label={t('cardManagement.printing.transformation')} value={transformation?.name ?? (run.transformationId ?? '-')} />
            <Info label={t('cardManagement.printing.printDesign')} value={printDesign?.name ?? (run.printDesignId ?? '-')} />
          </div>
        </CardContent>
      </Card>

      <JsonDetails title={t('cardManagement.printing.jsonInput')} value={run.input} />
      <JsonDetails title={t('cardManagement.printing.jsonResolvedVariables')} value={run.resolvedVariables} />
      <JsonDetails title={t('cardManagement.printing.jsonPlanSummary')} value={run.planSummary} />
      <JsonDetails title={t('cardManagement.printing.jsonCommandAudit')} value={run.commandAudit} />
    </section>
  );
}

function formatRunSubtitle(transformationName: string | undefined, printDesignName: string | undefined, t: ReturnType<typeof useTranslation>['t']) {
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
  return <div className="rounded-interactive border border-border p-3"><div className="text-[12px] uppercase text-muted-foreground">{label}</div><div className="mt-1 break-all text-[14px] font-medium text-foreground">{value}</div></div>;
}

function PanelError({ children }: { readonly children: React.ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
