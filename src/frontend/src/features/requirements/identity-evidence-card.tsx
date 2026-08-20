import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Pencil, Plus, Trash2, X } from 'lucide-react';
import { useMemo, useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Textarea } from '@/shared/components/ui/textarea';

type RequirementDefinitionResponse = components['schemas']['RequirementDefinitionResponse'];
type RequirementEvidenceKind = Exclude<components['schemas']['RequirementEvidenceKind'], null>;
type RequirementEvidenceResponse = components['schemas']['RequirementEvidenceResponse'];
type RequirementEvidenceStatus = Exclude<components['schemas']['RequirementEvidenceStatus'], null>;

type EvidenceFormValues = {
  readonly requirementDefinitionId: string;
  readonly evidenceKind: RequirementEvidenceKind;
  readonly status: RequirementEvidenceStatus;
  readonly validFrom: string;
  readonly validUntil: string;
  readonly sourceReference: string;
  readonly summary: string;
  readonly isSensitive: boolean;
  readonly verifiedAt: string;
};

const evidenceKinds: RequirementEvidenceKind[] = ['UploadedDocument', 'LearningCourseCompletion'];
const evidenceStatuses: RequirementEvidenceStatus[] = ['Valid', 'Invalid'];
const evidenceQueryKey = ['requirements', 'evidence'] as const;
const definitionsQueryKey = ['requirements', 'definitions'] as const;

const emptyForm: EvidenceFormValues = {
  requirementDefinitionId: '',
  evidenceKind: 'UploadedDocument',
  status: 'Valid',
  validFrom: '',
  validUntil: '',
  sourceReference: '',
  summary: '',
  isSensitive: false,
  verifiedAt: getNowLocalValue(),
};

export function IdentityEvidenceCard({ identityId, title = 'Evidence', description = 'Evidence attached to this identity. By default expired items are hidden and evidence is grouped by requirement.', includeExpired = false, groupByRequirement = true }: { readonly identityId: string; readonly title?: string; readonly description?: string; readonly includeExpired?: boolean; readonly groupByRequirement?: boolean; }) {
  const queryClient = useQueryClient();
  const [isAdding, setIsAdding] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formValues, setFormValues] = useState<EvidenceFormValues>(emptyForm);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const evidenceQuery = useQuery({
    queryKey: [...evidenceQueryKey, identityId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/requirements/evidence', {
        params: { query: { Query: undefined, IsActive: undefined, LocationId: undefined, identityId: identityId, Page: 0, PageSize: 500 } as never },
      });
      if (error) {
        throw new Error('Could not load evidence.');
      }
      return data?.items ?? [];
    },
  });

  const definitionsQuery = useQuery({
    queryKey: [...definitionsQueryKey, 'evidence-options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/requirements/definitions', {
        params: { query: { Query: undefined, IsActive: true, LocationId: undefined, Page: 0, PageSize: 500 } as never },
      });
      if (error) {
        throw new Error('Could not load requirement definitions.');
      }
      return data?.items ?? [];
    },
  });

  const createEvidence = useMutation({
    mutationFn: async (request: FormData) => {
      const { data, error } = await api.POST('/api/requirements/evidence', { body: request as never });
      if (error || !data) {
        throw new Error('Could not create evidence.');
      }
      return data;
    },
    onSuccess: async () => {
      resetForm();
      await queryClient.invalidateQueries({ queryKey: [...evidenceQueryKey, identityId] });
      toast.success('Evidence created.');
    },
    onError: () => toast.error('Could not create evidence.'),
  });

  const updateEvidence = useMutation({
    mutationFn: async ({ evidenceId, request }: { evidenceId: string; request: FormData }) => {
      const { data, error } = await api.PUT('/api/requirements/evidence/{id}', { params: { path: { id: evidenceId } }, body: request as never });
      if (error || !data) {
        throw new Error('Could not update evidence.');
      }
      return data;
    },
    onSuccess: async () => {
      resetForm();
      await queryClient.invalidateQueries({ queryKey: [...evidenceQueryKey, identityId] });
      toast.success('Evidence saved.');
    },
    onError: () => toast.error('Could not save evidence.'),
  });

  const deleteEvidence = useMutation({
    mutationFn: async (evidenceId: string) => {
      const { error } = await api.DELETE('/api/requirements/evidence/{id}', { params: { path: { id: evidenceId } } });
      if (error) {
        throw new Error('Could not delete evidence.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...evidenceQueryKey, identityId] });
      toast.success('Evidence deleted.');
    },
    onError: () => toast.error('Could not delete evidence.'),
  });

  const definitionsById = new Map((definitionsQuery.data ?? []).map((item: RequirementDefinitionResponse) => [item.id, item]));
  const filteredEvidence = useMemo(() => {
    const now = new Date();
    return (evidenceQuery.data ?? [])
      .filter((item: RequirementEvidenceResponse) => includeExpired || !item.validUntil || new Date(item.validUntil) > now)
      .sort((left: RequirementEvidenceResponse, right: RequirementEvidenceResponse) => new Date(right.verifiedAt).getTime() - new Date(left.verifiedAt).getTime());
  }, [evidenceQuery.data, includeExpired]);

  const groupedEvidence = useMemo(() => {
    if (!groupByRequirement) {
      return [['all', filteredEvidence] as const];
    }

    const groups = new Map<string, RequirementEvidenceResponse[]>();
    filteredEvidence.forEach((item) => {
      const current = groups.get(item.requirementDefinitionId) ?? [];
      current.push(item);
      groups.set(item.requirementDefinitionId, current);
    });
    return Array.from(groups.entries());
  }, [filteredEvidence, groupByRequirement]);

  function resetForm() {
    setIsAdding(false);
    setEditingId(null);
    setFormValues({ ...emptyForm, verifiedAt: getNowLocalValue() });
    setSelectedFile(null);
  }

  function startAdd() {
    setIsAdding(true);
    setEditingId(null);
    setFormValues({ ...emptyForm, verifiedAt: getNowLocalValue() });
    setSelectedFile(null);
  }

  function startEdit(evidence: RequirementEvidenceResponse) {
    setIsAdding(false);
    setEditingId(evidence.id);
    setFormValues({
      requirementDefinitionId: evidence.requirementDefinitionId,
      evidenceKind: (evidence.evidenceKind ?? 'UploadedDocument') as RequirementEvidenceKind,
      status: evidence.status as RequirementEvidenceStatus,
      validFrom: toLocalDateTimeValue(evidence.validFrom),
      validUntil: toLocalDateTimeValue(evidence.validUntil),
      sourceReference: evidence.sourceReference ?? '',
      summary: evidence.summary,
      isSensitive: evidence.isSensitive,
      verifiedAt: toLocalDateTimeValue(evidence.verifiedAt),
    });
    setSelectedFile(null);
  }

  function buildFormData(values: EvidenceFormValues) {
    const formData = new FormData();
    if (!editingId) {
      formData.set('IdentityId', identityId);
      formData.set('RequirementDefinitionId', values.requirementDefinitionId);
      formData.set('EvidenceKind', values.evidenceKind);
    }
    formData.set('Status', values.status);
    if (values.validFrom) formData.set('ValidFrom', new Date(values.validFrom).toISOString());
    if (values.validUntil) formData.set('ValidUntil', new Date(values.validUntil).toISOString());
    if (values.sourceReference.trim()) formData.set('SourceReference', values.sourceReference.trim());
    formData.set('Summary', values.summary);
    formData.set('IsSensitive', String(values.isSensitive));
    formData.set('VerifiedAt', new Date(values.verifiedAt).toISOString());
    if (selectedFile) formData.set('File', selectedFile);
    return formData;
  }

  function handleSubmit() {
    if (!formValues.requirementDefinitionId && !editingId) {
      toast.error('Select a requirement.');
      return;
    }
    if (!formValues.summary.trim()) {
      toast.error('Summary is required.');
      return;
    }

    const formData = buildFormData(formValues);
    if (editingId) {
      updateEvidence.mutate({ evidenceId: editingId, request: formData });
      return;
    }

    createEvidence.mutate(formData);
  }

  return (
    <Card className="p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-[18px] font-semibold tracking-tight">{title}</h3>
          <p className="mt-2 text-[14px] text-muted-foreground">{description}</p>
        </div>
        <Button type="button" variant="outline" onClick={() => (isAdding || editingId ? resetForm() : startAdd())}>
          {isAdding || editingId ? <X className="size-4" aria-hidden="true" /> : <Plus className="size-4" aria-hidden="true" />}
          {isAdding || editingId ? 'Cancel' : 'Add evidence'}
        </Button>
      </div>

      {evidenceQuery.isError || definitionsQuery.isError || createEvidence.isError || updateEvidence.isError || deleteEvidence.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{evidenceQuery.isError ? 'Could not load evidence.' : definitionsQuery.isError ? 'Could not load requirement definitions.' : createEvidence.isError ? 'Could not create evidence.' : updateEvidence.isError ? 'Could not save evidence.' : 'Could not delete evidence.'}</p> : null}

      {isAdding || editingId ? (
        <div className="grid gap-4 rounded-structural border border-border p-4">
          {!editingId ? (
            <label className="grid gap-2 text-[14px] font-medium">
              <span>Requirement</span>
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={formValues.requirementDefinitionId} onChange={(event) => setFormValues((current) => ({ ...current, requirementDefinitionId: event.target.value }))}>
                <option value="">Select requirement</option>
                {(definitionsQuery.data ?? []).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
              </select>
            </label>
          ) : null}

          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              <span>Evidence kind</span>
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={formValues.evidenceKind} onChange={(event) => setFormValues((current) => ({ ...current, evidenceKind: event.target.value as RequirementEvidenceKind }))} disabled={Boolean(editingId)}>
                {evidenceKinds.map((option) => <option key={option} value={option}>{option}</option>)}
              </select>
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              <span>Status</span>
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={formValues.status} onChange={(event) => setFormValues((current) => ({ ...current, status: event.target.value as RequirementEvidenceStatus }))}>
                {evidenceStatuses.map((option) => <option key={option} value={option}>{option}</option>)}
              </select>
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              <span>Valid from</span>
              <Input type="datetime-local" value={formValues.validFrom} onChange={(event) => setFormValues((current) => ({ ...current, validFrom: event.target.value }))} />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              <span>Valid until</span>
              <Input type="datetime-local" value={formValues.validUntil} onChange={(event) => setFormValues((current) => ({ ...current, validUntil: event.target.value }))} />
            </label>
          </div>

          <label className="grid gap-2 text-[14px] font-medium">
            <span>Summary</span>
            <Textarea value={formValues.summary} onChange={(event) => setFormValues((current) => ({ ...current, summary: event.target.value }))} rows={3} />
          </label>

          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              <span>Source reference</span>
              <Input value={formValues.sourceReference} onChange={(event) => setFormValues((current) => ({ ...current, sourceReference: event.target.value }))} />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              <span>Verified at</span>
              <Input type="datetime-local" value={formValues.verifiedAt} onChange={(event) => setFormValues((current) => ({ ...current, verifiedAt: event.target.value }))} />
            </label>
          </div>

          <label className="grid gap-2 text-[14px] font-medium">
            <span>Document</span>
            <Input type="file" onChange={(event) => setSelectedFile(event.target.files?.[0] ?? null)} />
          </label>

          <label className="inline-flex items-center gap-3 rounded-structural border border-border p-4 text-[14px] font-medium">
            <input type="checkbox" checked={formValues.isSensitive} onChange={(event) => setFormValues((current) => ({ ...current, isSensitive: event.target.checked }))} />
            Sensitive evidence
          </label>

          <div className="flex justify-end">
            <Button type="button" disabled={createEvidence.isPending || updateEvidence.isPending} onClick={handleSubmit}>{createEvidence.isPending || updateEvidence.isPending ? 'Saving...' : editingId ? 'Save evidence' : 'Create evidence'}</Button>
          </div>
        </div>
      ) : null}

      {evidenceQuery.isLoading || definitionsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading evidence...</p> : null}

      {!evidenceQuery.isLoading && filteredEvidence.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No evidence to show.</p> : null}

      {filteredEvidence.length > 0 ? (
        <div className="grid gap-4">
          {groupedEvidence.map(([key, items]) => {
            const definition = key === 'all' ? null : definitionsById.get(key);
            return (
              <div key={key} className="rounded-structural border border-border p-4">
                {groupByRequirement ? (
                  <div className="mb-4 flex flex-wrap items-center gap-2">
                    <p className="font-medium text-foreground">{definition?.name ?? key}</p>
                    {definition ? <Badge variant="secondary">{definition.code}</Badge> : null}
                    {definition?.isSensitive ? <Badge variant="error">Sensitive</Badge> : null}
                    {definition ? <Link to="/administration/access-model/compliancy/$requirementId/edit" params={{ requirementId: definition.id }} className="text-[13px] text-primary hover:underline">Open definition</Link> : null}
                  </div>
                ) : null}
                <div className="grid gap-3">
                  {items.map((item) => (
                    <div key={item.id} className="flex items-center justify-between gap-4 rounded-structural border border-border bg-background p-4">
                      <div className="min-w-0">
                        <div className="flex flex-wrap items-center gap-2">
                          <Badge variant={item.status === 'Valid' ? 'success' : 'error'}>{item.status}</Badge>
                          <Badge variant="secondary">{item.evidenceKind}</Badge>
                          {item.fileName ? <Badge variant="secondary">{item.fileName}</Badge> : null}
                        </div>
                        <p className="mt-2 font-medium text-foreground">{item.summary}</p>
                        <p className="mt-1 text-[13px] text-muted-foreground">Verified {formatDateTime(item.verifiedAt)}</p>
                        {item.validUntil ? <p className="mt-1 text-[13px] text-muted-foreground">Valid until {formatDateTime(item.validUntil)}</p> : null}
                        {item.sourceReference ? <p className="mt-1 text-[13px] text-muted-foreground">Ref: {item.sourceReference}</p> : null}
                      </div>
                      <div className="flex items-center gap-2">
                        <button type="button" className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label="Edit evidence" onClick={() => startEdit(item)}>
                          <Pencil className="size-4" aria-hidden="true" />
                        </button>
                        <Button type="button" variant="outline" size="sm" disabled={deleteEvidence.isPending} onClick={() => deleteEvidence.mutate(item.id)}>
                          <Trash2 className="size-4" aria-hidden="true" />
                          Delete
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      ) : null}
    </Card>
  );
}

function toLocalDateTimeValue(value: string | null) {
  if (!value) return '';
  const date = new Date(value);
  const pad = (part: number) => String(part).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function getNowLocalValue() {
  return toLocalDateTimeValue(new Date().toISOString());
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}
