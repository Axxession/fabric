import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { ArrowLeft, Save, Upload } from 'lucide-react';
import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Textarea } from '@/shared/components/ui/textarea';

import { printingBatchesQueryKey, type CreateEncodingBatchRequest, type Encoder } from './card-management-types';

const emptyCsv = 'badgeNumber,facilityCode\n10001,10\n10002,10';

const printBatchCreateEncodersQueryKey = ['card-management', 'printing', 'print-batch-create-page', 'encoders'] as const;
const printBatchCreateTransformationsQueryKey = ['card-management', 'print-batch-create-page', 'transformations'] as const;

type InputMode = 'count' | 'csv';

export default function PrintBatchCreatePage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const [encoderId, setEncoderId] = useState('');
  const [transformationId, setTransformationId] = useState('');
  const [csvText, setCsvText] = useState(emptyCsv);
  const [badgeCount, setBadgeCount] = useState('1');
  const [inputMode, setInputMode] = useState<InputMode>('csv');
  const [priority, setPriority] = useState('0');

  const transformationsQuery = useQuery({
    queryKey: printBatchCreateTransformationsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/transformations', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error(t('cardManagement.printBatchCreate.couldNotLoadTransformations'));
      }
      return data;
    },
  });

  const encodersQuery = useQuery({
    queryKey: printBatchCreateEncodersQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/encoders', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error(t('cardManagement.printBatchCreate.couldNotLoadEncoders'));
      }
      return data;
    },
  });

  const transformations = transformationsQuery.data?.items ?? [];
  const encoders = (encodersQuery.data?.items ?? []).filter((encoder) => encoder.enabled && encoder.supportsEncoding);
  const selectedTransformation = transformations.find((transformation) => transformation.id === transformationId);
  const userVariables = selectedTransformation?.variables.filter((variable) => variable.kind === 'UserProvided') ?? [];
  const userVariableFields = [...new Set(userVariables.map((variable) => (variable.field ?? variable.name).trim()).filter(Boolean))];
  const hasUserVariables = userVariableFields.length > 0;
  const parseResult = parseCsv(csvText);
  const missingHeaders = userVariableFields.filter((variable) => !parseResult.headers.includes(variable));

  useEffect(() => {
    setInputMode(hasUserVariables ? 'csv' : 'count');
  }, [hasUserVariables, transformationId]);

  const createBatch = useMutation({
    mutationFn: async (request: CreateEncodingBatchRequest) => {
      const { data, error } = await api.POST('/api/desfire/encoding-batches', { body: request });
      if (error || !data) {
        throw new Error(t('cardManagement.printBatchCreate.couldNotSchedule'));
      }
      return data;
    },
    onSuccess: async (batch) => {
      await queryClient.invalidateQueries({ queryKey: printingBatchesQueryKey });
      toast.success(t('cardManagement.printBatchCreate.scheduled'));
      window.location.assign(`/desfire-studio/printing/${batch.id}`);
    },
    onError: () => toast.error(t('cardManagement.printBatchCreate.couldNotSchedule')),
  });

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedTransformation) {
      toast.error(t('cardManagement.printBatchCreate.selectTransformation'));
      return;
    }
    if (!encoderId) {
      toast.error(t('cardManagement.printBatchCreate.selectEncoder'));
      return;
    }

    if (inputMode === 'count') {
      const parsedBadgeCount = Number(badgeCount);
      if (hasUserVariables) {
        toast.error(t('cardManagement.printBatchCreate.csvRequired'));
        return;
      }
      if (!Number.isInteger(parsedBadgeCount) || parsedBadgeCount < 1) {
        toast.error(t('cardManagement.printBatchCreate.numberOfBadgesMin'));
        return;
      }

      createBatch.mutate({
        name: name.trim(),
        encoderId,
        transformationId: selectedTransformation.id,
        originalInput: { format: 'count', count: parsedBadgeCount },
        normalizedRows: Array.from({ length: parsedBadgeCount }, () => ({})),
        requestedAgentId: null,
        requestedDeviceId: null,
        priority: Number(priority || 0),
      });
      return;
    }

    if (parseResult.error) {
      toast.error(parseResult.error);
      return;
    }
    if (missingHeaders.length > 0) {
      toast.error(t('cardManagement.printBatchCreate.csvMissingHeaders', { headers: missingHeaders.join(', ') }));
      return;
    }
    if (parseResult.rows.length === 0) {
      toast.error(t('cardManagement.printBatchCreate.csvNeedsData'));
      return;
    }

    createBatch.mutate({
      name: name.trim(),
      encoderId,
      transformationId: selectedTransformation.id,
      originalInput: { format: 'csv', text: csvText },
      normalizedRows: parseResult.rows,
      requestedAgentId: null,
      requestedDeviceId: null,
      priority: Number(priority || 0),
    });
  };

  return (
    <section className="grid gap-6">
      <Link to="/desfire-studio/printing" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground"><ArrowLeft className="size-4" />{t('cardManagement.printBatchCreate.backToPrinting')}</Link>
      <Card>
        <CardHeader>
          <CardTitle>{t('cardManagement.printBatchCreate.title')}</CardTitle>
          <CardDescription>{t('cardManagement.printBatchCreate.description')}</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-5" onSubmit={submit}>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.printBatchCreate.name')}</span><Input value={name} onChange={(event) => setName(event.target.value)} placeholder={t('cardManagement.printBatchCreate.namePlaceholder')} required /></label>
              <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.printBatchCreate.transformation')}</span><select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={transformationId} onChange={(event) => setTransformationId(event.target.value)} required><option value="">{t('cardManagement.printBatchCreate.selectTransformationOption')}</option>{transformations.map((transformation) => <option key={transformation.id} value={transformation.id}>{transformation.name}</option>)}</select></label>
              <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.printBatchCreate.encoder')}</span><EncoderSelect value={encoderId} encoders={encoders} onChange={setEncoderId} /></label>
              <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.printBatchCreate.priority')}</span><Input value={priority} type="number" onChange={(event) => setPriority(event.target.value)} /></label>
            </div>

            {selectedTransformation ? <VariableHint userVariableFields={userVariableFields} missingHeaders={missingHeaders} /> : null}

            {!hasUserVariables ? (
              <div className="flex flex-wrap gap-2">
                <Button type="button" variant={inputMode === 'count' ? 'default' : 'outline'} onClick={() => setInputMode('count')}>{t('cardManagement.printBatchCreate.badgeCount')}</Button>
                <Button type="button" variant={inputMode === 'csv' ? 'default' : 'outline'} onClick={() => setInputMode('csv')}>{t('cardManagement.printBatchCreate.csvInput')}</Button>
              </div>
            ) : null}

            {inputMode === 'count' ? (
              <label className="grid gap-2 text-[14px] font-medium">
                <span>{t('cardManagement.printBatchCreate.numberOfBadges')}</span>
                <Input value={badgeCount} type="number" min={1} onChange={(event) => setBadgeCount(event.target.value)} />
              </label>
            ) : (
              <>
                <label className="grid gap-2 text-[14px] font-medium">
                  <span>{t('cardManagement.printBatchCreate.csvRows')}</span>
                  <Textarea value={csvText} onChange={(event) => setCsvText(event.target.value)} rows={10} />
                </label>
                <label className="inline-flex w-fit cursor-pointer items-center gap-2 rounded-interactive border border-border px-3 py-2 text-[14px] font-medium transition hover:bg-hover-gray">
                  <Upload className="size-4" aria-hidden="true" />
                  {t('cardManagement.printBatchCreate.uploadCsv')}
                  <input className="sr-only" type="file" accept=".csv,text/csv" onChange={(event) => void loadCsvFile(event, setCsvText)} />
                </label>

                {parseResult.error ? <PanelError>{parseResult.error}</PanelError> : null}
                {parseResult.rows.length > 0 ? <CsvPreview headers={parseResult.headers} rows={parseResult.rows.slice(0, 5)} totalRows={parseResult.rows.length} /> : null}
              </>
            )}

            <div className="flex justify-end gap-2">
              <Button type="submit" disabled={createBatch.isPending}><Save className="size-4" aria-hidden="true" />{t('cardManagement.printBatchCreate.schedule')}</Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </section>
  );
}

function EncoderSelect({ value, encoders, onChange }: { readonly value: string; readonly encoders: Encoder[]; readonly onChange: (value: string) => void }) {
  const { t } = useTranslation();

  return <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={value} onChange={(event) => onChange(event.target.value)} required><option value="">{t('cardManagement.printBatchCreate.selectEncoderOption')}</option>{encoders.map((encoder) => <option key={encoder.id} value={encoder.id}>{encoder.name} ({encoder.agentId} / {encoder.deviceId})</option>)}</select>;
}

function VariableHint({ userVariableFields, missingHeaders }: { readonly userVariableFields: string[]; readonly missingHeaders: string[] }) {
  const { t } = useTranslation();
  if (userVariableFields.length === 0) {
    return <div className="rounded-structural border border-border bg-hover-gray p-4 text-[14px] text-muted-foreground">{t('cardManagement.printBatchCreate.noUserVariables')}</div>;
  }

  return <div className="rounded-structural border border-border bg-hover-gray p-4 text-[14px]"><div className="font-medium text-foreground">{t('cardManagement.printBatchCreate.requiredCsvHeaders')}</div><div className="mt-2 flex flex-wrap gap-2">{userVariableFields.map((variable) => <span key={variable} className={missingHeaders.includes(variable) ? 'rounded-full bg-error-background px-3 py-1 text-error' : 'rounded-full bg-content px-3 py-1 text-muted-foreground'}>{variable}</span>)}</div></div>;
}

function CsvPreview({ headers, rows, totalRows }: { readonly headers: string[]; readonly rows: Record<string, string>[]; readonly totalRows: number }) {
  const { t } = useTranslation();

  return <div className="overflow-x-auto rounded-structural border border-border"><table className="w-full min-w-[36rem] border-collapse text-left text-[13px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr>{headers.map((header) => <th key={header} className="px-3 py-2 font-semibold">{header}</th>)}</tr></thead><tbody className="divide-y divide-border">{rows.map((row, index) => <tr key={index}>{headers.map((header) => <td key={header} className="px-3 py-2 text-muted-foreground">{row[header]}</td>)}</tr>)}</tbody></table><div className="border-t border-border px-3 py-2 text-[12px] text-muted-foreground">{t('cardManagement.printBatchCreate.showingRows', { shown: rows.length, total: totalRows })}</div></div>;
}

function parseCsv(text: string): { headers: string[]; rows: Record<string, string>[]; error: string | null } {
  const lines = text.replace(/\r\n/g, '\n').replace(/\r/g, '\n').split('\n').filter((line) => line.trim().length > 0);
  if (lines.length === 0) {
    return { headers: [], rows: [], error: null };
  }
  const parsed = lines.map(parseCsvLine);
  if (parsed.some((row) => row.error)) {
    return { headers: [], rows: [], error: 'CSV contains an unterminated quoted value.' };
  }
  const headers = parsed[0].values.map((header) => header.trim()).filter(Boolean);
  if (headers.length === 0) {
    return { headers: [], rows: [], error: 'CSV must include a header row.' };
  }
  const rows = parsed.slice(1).map((row) => Object.fromEntries(headers.map((header, index) => [header, row.values[index]?.trim() ?? ''])));
  return { headers, rows, error: null };
}

function parseCsvLine(line: string): { values: string[]; error: boolean } {
  const values: string[] = [];
  let current = '';
  let quoted = false;
  for (let index = 0; index < line.length; index++) {
    const char = line[index];
    if (char === '"' && quoted && line[index + 1] === '"') {
      current += '"';
      index++;
    } else if (char === '"') {
      quoted = !quoted;
    } else if (char === ',' && !quoted) {
      values.push(current);
      current = '';
    } else {
      current += char;
    }
  }
  values.push(current);
  return { values, error: quoted };
}

async function loadCsvFile(event: ChangeEvent<HTMLInputElement>, setCsvText: (value: string) => void) {
  const file = event.target.files?.[0];
  if (!file) {
    return;
  }
  setCsvText(await file.text());
}

function PanelError({ children }: { readonly children: React.ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
