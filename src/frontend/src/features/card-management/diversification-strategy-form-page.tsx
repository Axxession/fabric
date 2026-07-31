import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft, Plus, Trash2 } from 'lucide-react';
import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

import { diversificationAlgorithms, diversificationInputOptions, strategiesQueryKey, type DiversificationInput, type DiversificationInputOption, type KeyDiversificationAlgorithm, type KeyDiversificationStrategyRequest } from './card-management-types';

type InputRow = { readonly option: DiversificationInputOption; readonly data: string };
type FormValues = { readonly name: string; readonly algorithm: KeyDiversificationAlgorithm; readonly inputs: InputRow[] };

const emptyValues: FormValues = {
  name: '',
  algorithm: 'NxpAn10922',
  inputs: [{ option: 'Uid', data: '' }],
};

export function DiversificationStrategyCreatePage() {
  return <DiversificationStrategyFormPage mode="create" />;
}

export default function DiversificationStrategyEditPage() {
  const { strategyId } = useParams({ from: '/desfire-studio/diversification-strategies/$strategyId/edit' });
  return <DiversificationStrategyEditPageContent strategyId={strategyId} />;
}

export function DiversificationStrategyEditPageContent({ strategyId }: { readonly strategyId: string }) {
  return <DiversificationStrategyFormPage mode="edit" strategyId={strategyId} />;
}

function DiversificationStrategyFormPage({ mode, strategyId }: { readonly mode: 'create' | 'edit'; readonly strategyId?: string }) {
  const queryClient = useQueryClient();
  const [values, setValues] = useState<FormValues>(emptyValues);

  const strategyQuery = useQuery({
    queryKey: [...strategiesQueryKey, strategyId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/key-diversification-strategies/{id}', { params: { path: { id: strategyId ?? '' } } });
      if (error || !data) {
        throw new Error('Could not load diversification strategy.');
      }
      return data;
    },
    enabled: mode === 'edit' && !!strategyId,
  });

  useEffect(() => {
    if (!strategyQuery.data) {
      return;
    }

    setValues({
      name: strategyQuery.data.name,
      algorithm: strategyQuery.data.algorithm,
      inputs: strategyQuery.data.inputs.map((input) => ({ option: input.option ?? 'Uid', data: input.data ?? '' })),
    });
  }, [strategyQuery.data]);

  const saveStrategy = useMutation({
    mutationFn: async (request: KeyDiversificationStrategyRequest) => {
      if (mode === 'create') {
        const { error } = await api.POST('/api/desfire/key-diversification-strategies', { body: request });
        if (error) {
          throw new Error('Could not add diversification strategy.');
        }
        return;
      }

      const { error } = await api.PUT('/api/desfire/key-diversification-strategies/{id}', { params: { path: { id: strategyId ?? '' } }, body: request });
      if (error) {
        throw new Error('Could not update diversification strategy.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: strategiesQueryKey });
      if (strategyId) {
        await queryClient.invalidateQueries({ queryKey: [...strategiesQueryKey, strategyId] });
      }
      toast.success(mode === 'create' ? 'Diversification strategy added.' : 'Diversification strategy updated.');
      window.history.back();
    },
    onError: () => toast.error(mode === 'create' ? 'Could not add diversification strategy.' : 'Could not update diversification strategy.'),
  });

  function updateValue<TKey extends keyof FormValues>(key: TKey, value: FormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  function updateInput(index: number, input: InputRow) {
    updateValue('inputs', values.inputs.map((current, currentIndex) => currentIndex === index ? input : current));
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    saveStrategy.mutate(toRequest(values));
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">{mode === 'create' ? 'Add diversification strategy' : values.name || 'Edit diversification strategy'}</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Define ordered diversification inputs used when deriving card-specific keys.</p>
        </div>
      </header>

      {strategyQuery.isError ? <PanelError>Could not load diversification strategy.</PanelError> : null}

      <Card className="p-4 sm:p-6">
        {strategyQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading diversification strategy...</p> : null}
        {mode === 'create' || strategyQuery.data ? (
          <form className="grid gap-5" onSubmit={handleSubmit}>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium">
                Name
                <Input value={values.name} onChange={(event) => updateValue('name', event.target.value)} required />
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                Algorithm
                <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={values.algorithm} onChange={(event) => updateValue('algorithm', event.target.value as KeyDiversificationAlgorithm)}>
                  {diversificationAlgorithms.map((algorithm) => <option key={algorithm} value={algorithm}>{algorithm}</option>)}
                </select>
              </label>
            </div>

            <section className="grid gap-4">
              <div className="flex items-center justify-between gap-3">
                <h3 className="text-[16px] font-semibold tracking-tight">Inputs</h3>
                <Button type="button" variant="outline" size="sm" onClick={() => updateValue('inputs', [...values.inputs, { option: 'Uid', data: '' }])}>
                  <Plus className="size-4" aria-hidden="true" />Add input
                </Button>
              </div>

              <div className="grid gap-3">
                {values.inputs.map((input, index) => (
                  <div key={index} className="grid gap-3 rounded-structural border border-border p-4 md:grid-cols-[1fr_1fr_auto] md:items-end">
                    <label className="grid gap-2 text-[14px] font-medium">
                      Input option
                      <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={input.option} onChange={(event) => updateInput(index, { ...input, option: event.target.value as DiversificationInputOption, data: '' })}>
                        {diversificationInputOptions.map((option) => <option key={option} value={option}>{option}</option>)}
                      </select>
                    </label>
                    <label className="grid gap-2 text-[14px] font-medium">
                      Fixed hex data
                      <Input value={input.data} disabled={input.option !== 'FixedHexValue'} onChange={(event) => updateInput(index, { ...input, data: event.target.value })} required={input.option === 'FixedHexValue'} placeholder={input.option === 'FixedHexValue' ? 'A1B2C3' : 'Only for fixed hex'} />
                    </label>
                    <Button type="button" variant="outline" size="sm" disabled={values.inputs.length === 1} onClick={() => updateValue('inputs', values.inputs.filter((_, currentIndex) => currentIndex !== index))}>
                      <Trash2 className="size-4" aria-hidden="true" />Remove
                    </Button>
                  </div>
                ))}
              </div>
            </section>

            <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <Button type="button" variant="outline" onClick={() => window.history.back()}>Cancel</Button>
              <Button type="submit" disabled={saveStrategy.isPending}>{saveStrategy.isPending ? 'Saving...' : 'Save strategy'}</Button>
            </div>
          </form>
        ) : null}
      </Card>
    </div>
  );
}

function toRequest(values: FormValues): KeyDiversificationStrategyRequest {
  return {
    name: values.name,
    algorithm: values.algorithm,
    inputs: values.inputs.map<DiversificationInput>((input) => ({ option: input.option, data: input.option === 'FixedHexValue' ? input.data : null })),
  };
}

function PanelError({ children }: { readonly children: ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
