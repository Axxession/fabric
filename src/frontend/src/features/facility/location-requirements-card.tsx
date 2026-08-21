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

type CreateLocationRequirementPolicyRequest = components['schemas']['CreateLocationRequirementPolicyRequest'];
type LocationAttachedRequirementResponse = components['schemas']['LocationAttachedRequirementResponse'];
type RequirementDefinitionResponse = components['schemas']['RequirementDefinitionResponse'];
type RequirementSubjectKind = components['schemas']['RequirementSubjectKind'];

const requirementsQueryKey = ['requirements', 'definitions'] as const;
const locationRequirementsQueryKey = ['requirements', 'policies', 'location'] as const;

const subjectKindOptions: RequirementSubjectKind[] = ['Any', 'Employee', 'Visitor', 'Contractor'];

export function LocationRequirementsCard({ locationId, title = 'Requirements', description = 'Requirements attached directly to this location. They apply here and to descendant locations. Inherited requirements from parent locations are not shown here.' }: { readonly locationId: string; readonly title?: string; readonly description?: string; }) {
  const queryClient = useQueryClient();
  const [isAdding, setIsAdding] = useState(false);
  const [selectedRequirementId, setSelectedRequirementId] = useState('');
  const [subjectKind, setSubjectKind] = useState<RequirementSubjectKind>('Any');
  const [isBlocking, setIsBlocking] = useState(true);

  const attachedRequirementsQuery = useQuery({
    queryKey: [...locationRequirementsQueryKey, locationId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/requirements/policies/location/{locationId}', { params: { path: { locationId } } });
      if (error || !data) {
        throw new Error('Could not load attached requirements.');
      }
      return data;
    },
  });

  const allRequirementsQuery = useQuery({
    queryKey: [...requirementsQueryKey, 'all'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/requirements/definitions', {
        params: { query: { Query: undefined, IsActive: true, LocationId: undefined, Page: 0, PageSize: 200 } as never },
      });
      if (error) {
        throw new Error('Could not load requirement definitions.');
      }
      return data?.items ?? [];
    },
  });

  const attachRequirement = useMutation({
    mutationFn: async (request: CreateLocationRequirementPolicyRequest) => {
      const { error } = await api.POST('/api/requirements/policies/location', { body: request });
      if (error) {
        throw new Error('Could not attach requirement.');
      }
    },
    onSuccess: async () => {
      setSelectedRequirementId('');
      setSubjectKind('Any');
      setIsBlocking(true);
      setIsAdding(false);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [...locationRequirementsQueryKey, locationId] }),
        queryClient.invalidateQueries({ queryKey: [...requirementsQueryKey, 'all'] }),
      ]);
      toast.success('Requirement attached.');
    },
    onError: () => toast.error('Could not attach requirement.'),
  });

  const detachRequirement = useMutation({
    mutationFn: async (policyId: string) => {
      const { error } = await api.DELETE('/api/requirements/policies/location/{policyId}', { params: { path: { policyId } } });
      if (error) {
        throw new Error('Could not detach requirement.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [...locationRequirementsQueryKey, locationId] }),
        queryClient.invalidateQueries({ queryKey: [...requirementsQueryKey, 'all'] }),
      ]);
      toast.success('Requirement detached.');
    },
    onError: () => toast.error('Could not detach requirement.'),
  });

  const attachedRequirements = attachedRequirementsQuery.data ?? [];
  const attachedRequirementIds = new Set(attachedRequirements.map((item) => item.requirementDefinitionId));
  const availableRequirements = useMemo(
    () => (allRequirementsQuery.data ?? []).filter((item) => !attachedRequirementIds.has(item.id)),
    [allRequirementsQuery.data, attachedRequirementIds],
  );

  function handleAttach() {
    if (!selectedRequirementId) {
      return;
    }

    attachRequirement.mutate({
      locationId,
      requirementDefinitionId: selectedRequirementId,
      subjectKind,
      isBlocking,
    });
  }

  return (
    <Card className="p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="text-[18px] font-semibold tracking-tight">{title}</h3>
          <p className="mt-2 text-[14px] text-muted-foreground">{description}</p>
        </div>
        <Button type="button" variant="outline" onClick={() => setIsAdding((current) => !current)}>
          {isAdding ? <X className="size-4" aria-hidden="true" /> : <Plus className="size-4" aria-hidden="true" />}
          {isAdding ? 'Cancel' : 'Add requirement'}
        </Button>
      </div>

      {attachedRequirementsQuery.isError || allRequirementsQuery.isError || attachRequirement.isError || detachRequirement.isError ? (
        <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
          {attachedRequirementsQuery.isError ? 'Could not load attached requirements.' : allRequirementsQuery.isError ? 'Could not load requirement definitions.' : attachRequirement.isError ? 'Could not attach requirement.' : 'Could not detach requirement.'}
        </p>
      ) : null}

      {isAdding ? (
        <div className="grid gap-4 rounded-structural border border-border p-4 md:grid-cols-2">
          <label className="grid gap-2 text-[14px] font-medium">
            <span>Requirement</span>
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedRequirementId} onChange={(event) => setSelectedRequirementId(event.target.value)} disabled={attachRequirement.isPending}>
              <option value="">Select requirement</option>
              {availableRequirements.map((item: RequirementDefinitionResponse) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
          </label>

          <label className="grid gap-2 text-[14px] font-medium">
            <span>Subject kind</span>
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={subjectKind} onChange={(event) => setSubjectKind(event.target.value as RequirementSubjectKind)} disabled={attachRequirement.isPending}>
              {subjectKindOptions.map((option) => <option key={option} value={option}>{option}</option>)}
            </select>
          </label>

          <label className="inline-flex items-center gap-3 rounded-structural border border-border p-4 text-[14px] font-medium md:col-span-2">
            <input type="checkbox" checked={isBlocking} onChange={(event) => setIsBlocking(event.target.checked)} disabled={attachRequirement.isPending} />
            Blocking requirement
          </label>

          <div className="md:col-span-2 flex justify-end">
            <Button type="button" disabled={!selectedRequirementId || attachRequirement.isPending} onClick={handleAttach}>
              <Plus className="size-4" aria-hidden="true" />
              {attachRequirement.isPending ? 'Attaching...' : 'Attach requirement'}
            </Button>
          </div>
        </div>
      ) : null}

      {attachedRequirementsQuery.isLoading || allRequirementsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading requirements...</p> : null}

      {!attachedRequirementsQuery.isLoading && attachedRequirements.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No requirements attached directly to this location.</p> : null}

      {attachedRequirements.length > 0 ? (
        <div className="grid gap-3">
          {attachedRequirements.map((item: LocationAttachedRequirementResponse) => (
            <div key={item.policyId} className="flex items-center justify-between gap-4 rounded-structural border border-border p-4">
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="font-medium text-foreground">{item.requirementName}</p>
                  <Badge variant="secondary">{item.requirementCode}</Badge>
                  <Badge variant="secondary">{item.subjectKind}</Badge>
                  {item.isBlocking ? <Badge variant="error">Blocking</Badge> : <Badge variant="secondary">Non-blocking</Badge>}
                </div>
                <p className="mt-1 text-[14px] text-muted-foreground">Evidence kinds: {formatAllowedEvidenceKinds(item.allowedEvidenceKinds)}{item.isSensitive ? ' • Sensitive' : ''}</p>
              </div>
              <div className="flex items-center gap-2">
                <Link to="/administration/access-model/compliancy/$requirementId/edit" params={{ requirementId: item.requirementDefinitionId }} className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${item.requirementName}`}>
                  <Pencil className="size-4" aria-hidden="true" />
                </Link>
                <Button type="button" variant="outline" size="sm" disabled={detachRequirement.isPending} onClick={() => detachRequirement.mutate(item.policyId)}>
                  <Trash2 className="size-4" aria-hidden="true" />
                  Detach
                </Button>
              </div>
            </div>
          ))}
        </div>
      ) : null}
    </Card>
  );
}

function formatAllowedEvidenceKinds(allowedEvidenceKinds: Array<string | null> | null | undefined) {
  if (!allowedEvidenceKinds || allowedEvidenceKinds.length === 0) return 'None';
  return allowedEvidenceKinds.filter((item): item is string => item !== null).map((item) => item === 'CourseCompletion' ? 'Course completion' : item === 'RequirementWaiver' ? 'Requirement waiver' : 'Document').join(', ');
}
