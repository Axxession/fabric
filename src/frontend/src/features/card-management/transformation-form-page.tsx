import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

import { chipDesignsQueryKey, systemProvidersQueryKey, transformationsQueryKey, variableFormatKinds, type DesfireVariableFormatKind, type SystemProvider, type TransformationPlan, type TransformationRequest, type TransformationVariableConfig } from './card-management-types';

type SourceMode = 'blank' | 'design';
type FormValues = { readonly name: string; readonly sourceMode: SourceMode; readonly fromChipDesignName: string; readonly toChipDesignName: string; readonly variables: TransformationVariableConfig[] };

const emptyValues: FormValues = {
  name: '',
  sourceMode: 'blank',
  fromChipDesignName: '',
  toChipDesignName: '',
  variables: [],
};

export function TransformationCreatePage() {
  return <TransformationFormPage mode="create" />;
}

export default function TransformationEditPage() {
  const { transformationId } = useParams({ from: '/desfire-studio/transformations/$transformationId/edit' });
  return <TransformationEditPageContent transformationId={transformationId} />;
}

export function TransformationEditPageContent({ transformationId }: { readonly transformationId: string }) {
  return <TransformationFormPage mode="edit" transformationId={transformationId} />;
}

function TransformationFormPage({ mode, transformationId }: { readonly mode: 'create' | 'edit'; readonly transformationId?: string }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [values, setValues] = useState<FormValues>(emptyValues);

  const chipDesignsQuery = useQuery({
    queryKey: chipDesignsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/chip-designs', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error) {
        throw new Error(t('cardManagement.transformationForm.couldNotLoadChipDesigns'));
      }
      return data;
    },
  });

  const systemProvidersQuery = useQuery({
    queryKey: systemProvidersQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/system-providers', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error) {
        throw new Error(t('cardManagement.transformationForm.couldNotLoadSystemProviders'));
      }
      return data;
    },
  });

  const transformationQuery = useQuery({
    queryKey: [...transformationsQueryKey, transformationId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/transformations/{id}', { params: { path: { id: transformationId ?? '' } } });
      if (error || !data) {
        throw new Error(t('cardManagement.transformationForm.couldNotLoadTransformation'));
      }
      return data;
    },
    enabled: mode === 'edit' && !!transformationId,
  });

  const planQuery = useQuery({
    queryKey: [...transformationsQueryKey, transformationId, 'plan'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/transformations/{id}/plan', { params: { path: { id: transformationId ?? '' } } });
      if (error || !data) {
        throw new Error(t('cardManagement.transformationForm.couldNotLoadPlan'));
      }
      return data;
    },
    enabled: mode === 'edit' && !!transformationId,
  });

  useEffect(() => {
    if (!transformationQuery.data) {
      return;
    }

    setValues({
      name: transformationQuery.data.name,
      sourceMode: transformationQuery.data.fromBlank ? 'blank' : 'design',
      fromChipDesignName: transformationQuery.data.fromChipDesignName ?? '',
      toChipDesignName: transformationQuery.data.toChipDesignName,
      variables: transformationQuery.data.variables,
    });
  }, [transformationQuery.data]);

  useEffect(() => {
    if (!planQuery.data) {
      return;
    }

    setValues((current) => ({ ...current, variables: reconcileVariables(planQuery.data.requiredVariables, current.variables) }));
  }, [planQuery.data]);

  const saveTransformation = useMutation({
    mutationFn: async (request: TransformationRequest) => {
      if (mode === 'create') {
        const { error } = await api.POST('/api/desfire/transformations', { body: request });
        if (error) {
          throw new Error(t('cardManagement.transformationForm.createFailed'));
        }
        return;
      }

      const { error } = await api.PUT('/api/desfire/transformations/{id}', { params: { path: { id: transformationId ?? '' } }, body: request });
      if (error) {
        throw new Error(t('cardManagement.transformationForm.updateFailed'));
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: transformationsQueryKey });
      if (transformationId) {
        await queryClient.invalidateQueries({ queryKey: [...transformationsQueryKey, transformationId] });
        await queryClient.invalidateQueries({ queryKey: [...transformationsQueryKey, transformationId, 'plan'] });
      }
      toast.success(mode === 'create' ? t('cardManagement.transformationForm.created') : t('cardManagement.transformationForm.updated'));
      window.history.back();
    },
    onError: () => toast.error(mode === 'create' ? t('cardManagement.transformationForm.createFailed') : t('cardManagement.transformationForm.updateFailed')),
  });

  const designNames = Array.from(new Set((chipDesignsQuery.data?.items ?? []).map((design) => design.name))).sort((left, right) => left.localeCompare(right));
  const systemProviders = systemProvidersQuery.data?.items ?? [];

  function updateValue<TKey extends keyof FormValues>(key: TKey, value: FormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    saveTransformation.mutate(toRequest(values));
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label={t('cardManagement.transformationForm.back')} onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">{mode === 'create' ? t('cardManagement.transformationForm.addTitle') : values.name || t('cardManagement.transformationForm.editTitle')}</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{t('cardManagement.transformationForm.description')}</p>
        </div>
      </header>

      {transformationQuery.isError ? <PanelError>{t('cardManagement.transformationForm.couldNotLoadTransformation')}</PanelError> : null}
      {chipDesignsQuery.isError ? <PanelError>{t('cardManagement.transformationForm.couldNotLoadChipDesigns')}</PanelError> : null}
      {systemProvidersQuery.isError ? <PanelError>{t('cardManagement.transformationForm.couldNotLoadSystemProviders')}</PanelError> : null}

      <Card className="p-4 sm:p-6">
        {transformationQuery.isLoading ? <p className="text-[14px] text-muted-foreground">{t('cardManagement.transformationForm.loading')}</p> : null}
        {mode === 'create' || transformationQuery.data ? (
          <form className="grid gap-5" onSubmit={handleSubmit}>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium">
                 {t('cardManagement.transformationForm.name')}
                <Input value={values.name} onChange={(event) => updateValue('name', event.target.value)} required />
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                 {t('cardManagement.transformationForm.sourceType')}
                <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={values.sourceMode} onChange={(event) => updateValue('sourceMode', event.target.value as SourceMode)}>
                   <option value="blank">{t('cardManagement.transformationForm.blankChip')}</option>
                   <option value="design">{t('cardManagement.transformationForm.existingChipDesign')}</option>
                </select>
              </label>
               {values.sourceMode === 'blank' ? <p className="rounded-interactive border border-border bg-hover-gray px-3 py-2 text-[14px] text-muted-foreground">{t('cardManagement.transformationForm.blankChipHint')}</p> : (
                 <label className="grid gap-2 text-[14px] font-medium">
                   {t('cardManagement.transformationForm.sourceChipDesign')}
                   <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={values.fromChipDesignName} onChange={(event) => updateValue('fromChipDesignName', event.target.value)} required>
                     <option value="">{t('cardManagement.transformationForm.selectChipDesign')}</option>
                    {designNames.map((name) => <option key={name} value={name}>{name}</option>)}
                  </select>
                </label>
              )}
              <label className="grid gap-2 text-[14px] font-medium">
                 {t('cardManagement.transformationForm.targetChipDesign')}
                 <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={values.toChipDesignName} onChange={(event) => updateValue('toChipDesignName', event.target.value)} required>
                   <option value="">{t('cardManagement.transformationForm.selectChipDesign')}</option>
                  {designNames.map((name) => <option key={name} value={name}>{name}</option>)}
                </select>
              </label>
            </div>

            {mode === 'edit' ? <PlanSummary plan={planQuery.data} isLoading={planQuery.isLoading} isError={planQuery.isError} /> : null}
            {mode === 'edit' ? <VariableConfigEditor values={values.variables} systemProviders={systemProviders} onChange={(variables) => updateValue('variables', variables)} /> : null}

            <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
               <Button type="button" variant="outline" onClick={() => window.history.back()}>{t('cardManagement.transformationForm.cancel')}</Button>
               <Button type="submit" disabled={saveTransformation.isPending}>{saveTransformation.isPending ? t('cardManagement.transformationForm.saving') : t('cardManagement.transformationForm.save')}</Button>
             </div>
           </form>
        ) : null}
      </Card>
    </div>
  );
}

function PlanSummary({ plan, isLoading, isError }: { readonly plan?: TransformationPlan; readonly isLoading: boolean; readonly isError: boolean }) {
  const { t } = useTranslation();
  if (isLoading) {
    return <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">{t('cardManagement.transformationForm.loadingPlan')}</p>;
  }

  if (isError) {
    return <PanelError>{t('cardManagement.transformationForm.couldNotLoadPlan')}</PanelError>;
  }

  if (!plan) {
    return null;
  }

  return (
    <section className="grid gap-4 rounded-structural border border-border p-4">
      <div className="flex flex-wrap items-center gap-2">
        <h3 className="text-[16px] font-semibold tracking-tight">{t('cardManagement.transformationForm.planSummary')}</h3>
        <Badge variant="outline">{t('cardManagement.transformationForm.operations', { count: plan.operationCount })}</Badge>
      </div>
      <MetadataList label={t('cardManagement.transformationForm.requiredVariables')} values={plan.requiredVariables} />
      <MetadataList label={t('cardManagement.transformationForm.requiredKeyGroups')} values={plan.requiredKeyGroups} />
      <MetadataList label={t('cardManagement.transformationForm.planErrors')} values={plan.errors} tone="error" />
      <PlannedOperations operations={plan.operations} />
    </section>
  );
}

function PlannedOperations({ operations }: { readonly operations: TransformationPlan['operations'] }) {
  const { t } = useTranslation();
  if (operations.length === 0) {
    return <p className="rounded-structural border border-border p-3 text-[14px] text-muted-foreground">{t('cardManagement.transformationForm.noPlannedOperations')}</p>;
  }

  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[44rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr><th className="px-4 py-3 font-semibold">{t('cardManagement.transformationForm.order')}</th><th className="px-4 py-3 font-semibold">{t('cardManagement.transformationForm.type')}</th><th className="px-4 py-3 font-semibold">{t('cardManagement.transformationForm.descriptionLabel')}</th></tr>
        </thead>
        <tbody className="divide-y divide-border">
          {operations.map((operation) => <tr key={operation.order}><td className="px-4 py-3 text-muted-foreground">{operation.order}</td><td className="px-4 py-3"><Badge variant="outline">{operation.type}</Badge></td><td className="px-4 py-3 text-muted-foreground">{operation.description}</td></tr>)}
        </tbody>
      </table>
    </div>
  );
}

function VariableConfigEditor({ values, systemProviders, onChange }: { readonly values: TransformationVariableConfig[]; readonly systemProviders: SystemProvider[]; readonly onChange: (values: TransformationVariableConfig[]) => void }) {
  const { t } = useTranslation();
  function updateVariable(index: number, value: TransformationVariableConfig) {
    onChange(values.map((current, currentIndex) => currentIndex === index ? value : current));
  }

  return (
    <section className="grid gap-4 rounded-structural border border-border p-4">
      <div>
        <h3 className="text-[16px] font-semibold tracking-tight">{t('cardManagement.transformationForm.variables')}</h3>
        <p className="mt-1 text-[14px] text-muted-foreground">{t('cardManagement.transformationForm.variablesDescription')}</p>
      </div>
      {values.length === 0 ? <p className="rounded-structural border border-border p-3 text-[14px] text-muted-foreground">{t('cardManagement.transformationForm.noRequiredVariables')}</p> : null}
      {values.map((variable, index) => <VariableConfigRow key={variable.name} value={variable} systemProviders={systemProviders} onChange={(value) => updateVariable(index, value)} />)}
    </section>
  );
}

function VariableConfigRow({ value, systemProviders, onChange }: { readonly value: TransformationVariableConfig; readonly systemProviders: SystemProvider[]; readonly onChange: (value: TransformationVariableConfig) => void }) {
  const { t } = useTranslation();
  return (
    <div className="grid gap-4 rounded-interactive border border-border p-4">
      <div className="flex flex-wrap items-center gap-2">
        <h4 className="text-[14px] font-semibold tracking-tight">{value.name}</h4>
        <Badge variant={value.kind === 'SystemProvided' ? 'warning' : 'secondary'}>{value.kind === 'SystemProvided' ? t('cardManagement.transformationForm.systemProvided') : t('cardManagement.transformationForm.userProvided')}</Badge>
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        <label className="grid gap-2 text-[14px] font-medium">
          {t('cardManagement.transformationForm.source')}
          <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={value.kind} onChange={(event) => onChange(normalizeVariableKind(value, event.target.value as TransformationVariableConfig['kind']))}>
            <option value="UserProvided">{t('cardManagement.transformationForm.userProvided')}</option>
            <option value="SystemProvided">{t('cardManagement.transformationForm.systemProvided')}</option>
          </select>
        </label>
        {value.kind === 'SystemProvided' ? (
          <label className="grid gap-2 text-[14px] font-medium">
            {t('cardManagement.transformationForm.systemProvider')}
            <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={value.systemProviderId ?? ''} onChange={(event) => onChange({ ...value, systemProviderId: event.target.value || null })} required>
              <option value="">{t('cardManagement.transformationForm.selectProvider')}</option>
              {systemProviders.map((provider) => <option key={provider.id} value={provider.id}>{provider.name} ({provider.providerType})</option>)}
            </select>
          </label>
        ) : (
          <label className="grid gap-2 text-[14px] font-medium">
            {t('cardManagement.transformationForm.inputField')}
            <Input value={value.field ?? value.name} onChange={(event) => onChange({ ...value, field: event.target.value })} required />
          </label>
        )}
        <FormatEditor value={value} onChange={onChange} />
      </div>
      {value.kind === 'SystemProvided' && systemProviders.length === 0 ? <p className="rounded-interactive border border-border bg-hover-gray px-3 py-2 text-[14px] text-muted-foreground">{t('cardManagement.transformationForm.createSystemProviderFirst')}</p> : null}
    </div>
  );
}

function FormatEditor({ value, onChange }: { readonly value: TransformationVariableConfig; readonly onChange: (value: TransformationVariableConfig) => void }) {
  const { t } = useTranslation();
  const formatType = value.format.type;
  const needsLength = formatType === 'UInt' || formatType === 'PaddedDecimal' || formatType === 'PaddedHex';
  return (
    <div className="grid gap-4 md:grid-cols-2">
      <label className="grid gap-2 text-[14px] font-medium">
        {t('cardManagement.transformationForm.format')}
        <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={formatType} onChange={(event) => onChange({ ...value, format: { type: event.target.value as DesfireVariableFormatKind, length: value.format.length ?? null, encoding: null, wiegand: null } })}>
          {variableFormatKinds.map((format) => <option key={format} value={format}>{format}</option>)}
        </select>
      </label>
      {needsLength ? <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.transformationForm.length')}</span><Input value={String(value.format.length ?? '')} type="number" min={1} onChange={(event) => onChange({ ...value, format: { ...value.format, length: event.target.value === '' ? null : Number(event.target.value) } })} /></label> : null}
    </div>
  );
}

function MetadataList({ label, values, tone }: { readonly label: string; readonly values: string[]; readonly tone?: 'error' }) {
  const { t } = useTranslation();
  return (
    <div className="grid gap-2">
      <span className="text-[14px] font-medium">{label}</span>
      {values.length === 0 ? <span className="text-[14px] text-muted-foreground">{t('cardManagement.transformationForm.none')}</span> : <div className="flex flex-wrap gap-2">{values.map((value) => <Badge key={value} variant={tone === 'error' ? 'error' : 'secondary'}>{value}</Badge>)}</div>}
    </div>
  );
}

function reconcileVariables(requiredVariables: string[], current: TransformationVariableConfig[]) {
  const currentByName = new Map(current.map((variable) => [variable.name.toLowerCase(), variable]));
  return requiredVariables.map((name) => currentByName.get(name.toLowerCase()) ?? createDefaultVariable(name));
}

function createDefaultVariable(name: string): TransformationVariableConfig {
  return { name, kind: 'UserProvided', field: name, format: { type: 'Hex', length: null, encoding: null, wiegand: null } };
}

function normalizeVariableKind(value: TransformationVariableConfig, kind: TransformationVariableConfig['kind']): TransformationVariableConfig {
  if (kind === 'UserProvided') {
    return { name: value.name, kind, field: value.field ?? value.name, format: value.format };
  }

  return { name: value.name, kind, systemProviderId: value.systemProviderId ?? null, format: value.format };
}

function toRequest(values: FormValues): TransformationRequest {
  return {
    name: values.name,
    fromChipDesignName: values.sourceMode === 'design' ? values.fromChipDesignName : null,
    fromBlank: values.sourceMode === 'blank',
    toChipDesignName: values.toChipDesignName,
    variables: values.variables,
  };
}

function PanelError({ children }: { readonly children: ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
