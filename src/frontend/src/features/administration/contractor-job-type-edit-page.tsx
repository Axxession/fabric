import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, Pencil, Plus, ToggleLeft, ToggleRight, Trash2, X } from 'lucide-react';
import { useMemo, useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getLocationLabel, LocationSelector, type LocationResponse } from '@/shared/components/location-selector';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { ContractorJobTypeForm, type ContractorJobTypeFormValues } from './contractor-job-type-form';

type JobTypeResponse = components['schemas']['JobTypeResponse'];
type CreateLocationJobRequirementPolicyRequest = components['schemas']['CreateLocationJobRequirementPolicyRequest'];
type LocationJobAttachedRequirementResponse = components['schemas']['LocationJobAttachedRequirementResponse'];
type RequirementDefinitionResponse = components['schemas']['RequirementDefinitionResponse'];
type UpdateJobTypeRequest = components['schemas']['UpdateJobTypeRequest'];
type UpdateLocationJobRequirementPolicyRequest = components['schemas']['UpdateLocationJobRequirementPolicyRequest'];

const contractorJobTypesQueryKey = ['administration', 'my-organization', 'contractor-job-types'] as const;
const attachedRequirementPoliciesQueryKey = ['administration', 'my-organization', 'contractor-job-types', 'attached-requirements'] as const;
const requirementDefinitionsQueryKey = ['administration', 'access-model', 'compliancy', 'requirements'] as const;

export default function ContractorJobTypeEditPage() {
  const { jobTypeId } = useParams({ from: '/main/administration/my-organization/contractor-job-types/$jobTypeId/edit' });
  const queryClient = useQueryClient();
  const [isAddingRequirement, setIsAddingRequirement] = useState(false);
  const [selectedRequirementId, setSelectedRequirementId] = useState('');
  const [selectedLocationId, setSelectedLocationId] = useState<string | null>(null);
  const [isBlockingRequirement, setIsBlockingRequirement] = useState(true);

  const jobTypeQuery = useQuery({
    queryKey: [...contractorJobTypesQueryKey, jobTypeId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/contractors/job-types/{id}', { params: { path: { id: jobTypeId } } });
      if (error || !data) throw new Error('Could not load contractor job type.');
      return data;
    },
  });

  const attachedRequirementsQuery = useQuery({
    queryKey: [...attachedRequirementPoliciesQueryKey, jobTypeId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/requirements/policies/location-job', {
        params: { query: { LocationId: undefined, JobTypeId: jobTypeId, RequirementDefinitionId: undefined, IsEnabled: undefined } },
      });
      if (error || !data) {
        throw new Error('Could not load attached requirements.');
      }
      return data;
    },
  });

  const requirementDefinitionsQuery = useQuery({
    queryKey: [...requirementDefinitionsQueryKey, 'all'],
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

  const locationIds = Array.from(new Set((attachedRequirementsQuery.data ?? []).map((item) => item.locationId)));
  const locationsQuery = useQuery({
    queryKey: [...attachedRequirementPoliciesQueryKey, jobTypeId, 'locations', locationIds.join(',')],
    enabled: locationIds.length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/locations', { params: { query: { ids: locationIds } } });
      if (error) {
        throw new Error('Could not load attached requirement locations.');
      }
      return new Map((data ?? []).map((item: LocationResponse) => [item.id, item]));
    },
  });

  const updateJobType = useMutation({
    mutationFn: async (values: ContractorJobTypeFormValues) => {
      const request: UpdateJobTypeRequest = {
        code: values.code,
        name: values.name,
        description: values.description || null,
      };

      const { error } = await api.PUT('/api/contractors/job-types/{id}', { params: { path: { id: jobTypeId } }, body: request });
      if (error) throw new Error('Could not save contractor job type.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: contractorJobTypesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...contractorJobTypesQueryKey, jobTypeId] }),
      ]);
      toast.success('Contractor job type saved.');
    },
    onError: () => toast.error('Could not save contractor job type.'),
  });

  const activateJobType = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/contractors/job-types/{id}/activate', { params: { path: { id: jobTypeId } } });
      if (error) throw new Error('Could not activate contractor job type.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: contractorJobTypesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...contractorJobTypesQueryKey, jobTypeId] }),
      ]);
      toast.success('Contractor job type activated.');
    },
    onError: () => toast.error('Could not activate contractor job type.'),
  });

  const deactivateJobType = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/contractors/job-types/{id}/deactivate', { params: { path: { id: jobTypeId } } });
      if (error) throw new Error('Could not deactivate contractor job type.');
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: contractorJobTypesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...contractorJobTypesQueryKey, jobTypeId] }),
      ]);
      toast.success('Contractor job type deactivated.');
    },
    onError: () => toast.error('Could not deactivate contractor job type.'),
  });

  const attachRequirement = useMutation({
    mutationFn: async (request: CreateLocationJobRequirementPolicyRequest) => {
      const { error } = await api.POST('/api/requirements/policies/location-job', { body: request });
      if (error) {
        throw new Error('Could not attach requirement.');
      }
    },
    onSuccess: async () => {
      setSelectedRequirementId('');
      setSelectedLocationId(null);
      setIsBlockingRequirement(true);
      setIsAddingRequirement(false);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [...attachedRequirementPoliciesQueryKey, jobTypeId] }),
        queryClient.invalidateQueries({ queryKey: [...attachedRequirementPoliciesQueryKey, jobTypeId, 'locations'] }),
      ]);
      toast.success('Requirement attached.');
    },
    onError: () => toast.error('Could not attach requirement.'),
  });

  const updateAttachedRequirement = useMutation({
    mutationFn: async ({ policyId, request }: { policyId: string; request: UpdateLocationJobRequirementPolicyRequest }) => {
      const { error } = await api.PUT('/api/requirements/policies/location-job/{policyId}', { params: { path: { policyId } }, body: request });
      if (error) {
        throw new Error('Could not update attached requirement.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...attachedRequirementPoliciesQueryKey, jobTypeId] });
      toast.success('Requirement updated.');
    },
    onError: () => toast.error('Could not update attached requirement.'),
  });

  const setAttachedRequirementEnabled = useMutation({
    mutationFn: async ({ policyId, isEnabled }: { policyId: string; isEnabled: boolean }) => {
      const request = isEnabled
        ? api.POST('/api/requirements/policies/location-job/{policyId}/enable', { params: { path: { policyId } } })
        : api.POST('/api/requirements/policies/location-job/{policyId}/disable', { params: { path: { policyId } } });
      const { error } = await request;
      if (error) {
        throw new Error(isEnabled ? 'Could not enable attached requirement.' : 'Could not disable attached requirement.');
      }
    },
    onSuccess: async (_, variables) => {
      await queryClient.invalidateQueries({ queryKey: [...attachedRequirementPoliciesQueryKey, jobTypeId] });
      toast.success(variables.isEnabled ? 'Requirement enabled.' : 'Requirement disabled.');
    },
    onError: (_, variables) => toast.error(variables.isEnabled ? 'Could not enable attached requirement.' : 'Could not disable attached requirement.'),
  });

  const detachRequirement = useMutation({
    mutationFn: async (policyId: string) => {
      const { error } = await api.DELETE('/api/requirements/policies/location-job/{policyId}', { params: { path: { policyId } } });
      if (error) {
        throw new Error('Could not detach requirement.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [...attachedRequirementPoliciesQueryKey, jobTypeId] }),
        queryClient.invalidateQueries({ queryKey: [...attachedRequirementPoliciesQueryKey, jobTypeId, 'locations'] }),
      ]);
      toast.success('Requirement detached.');
    },
    onError: () => toast.error('Could not detach requirement.'),
  });

  const jobType = jobTypeQuery.data;
  const attachedRequirements = attachedRequirementsQuery.data ?? [];
  const attachedRequirementKeys = new Set(attachedRequirements.map((item) => `${item.locationId}:${item.requirementDefinitionId}`));
  const availableRequirements = useMemo(
    () => (requirementDefinitionsQuery.data ?? []).filter((item) => !selectedLocationId || !attachedRequirementKeys.has(`${selectedLocationId}:${item.id}`)),
    [requirementDefinitionsQuery.data, attachedRequirementKeys, selectedLocationId],
  );

  function handleAttachRequirement() {
    if (!selectedLocationId || !selectedRequirementId) {
      return;
    }

    attachRequirement.mutate({
      locationId: selectedLocationId,
      jobTypeId,
      requirementDefinitionId: selectedRequirementId,
      isBlocking: isBlockingRequirement,
    });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div className="flex-1">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h2 className="text-[20px] font-semibold tracking-tight">Edit contractor job type</h2>
              <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update contractor job type details in My Organization.</p>
            </div>
            {jobType ? (
              <Button type="button" variant="outline" disabled={activateJobType.isPending || deactivateJobType.isPending} onClick={() => {
                if (jobType.isActive) {
                  deactivateJobType.mutate();
                } else {
                  activateJobType.mutate();
                }
              }}>
                {jobType.isActive ? <ToggleLeft className="size-4" aria-hidden="true" /> : <ToggleRight className="size-4" aria-hidden="true" />}
                {jobType.isActive ? 'Deactivate contractor job type' : 'Activate contractor job type'}
              </Button>
            ) : null}
          </div>
        </div>
      </header>

      <Card className="p-6">
        {jobTypeQuery.isError || updateJobType.isError || activateJobType.isError || deactivateJobType.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{jobTypeQuery.isError ? 'Could not load contractor job type.' : updateJobType.isError ? 'Could not save contractor job type.' : activateJobType.isError ? 'Could not activate contractor job type.' : 'Could not deactivate contractor job type.'}</p> : null}
        {jobTypeQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading contractor job type...</p> : null}
        {!jobTypeQuery.isLoading && jobType && !jobTypeQuery.isError ? <ContractorJobTypeForm initialValues={toFormValues(jobType)} isSubmitting={updateJobType.isPending} submitLabel="Save" onSubmit={(values) => updateJobType.mutate(values)} /> : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Attached requirements</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Attach requirement definitions to this contractor job type at a location scope.</p>
          </div>
          <Button type="button" variant="outline" onClick={() => setIsAddingRequirement((current) => !current)}>
            {isAddingRequirement ? <X className="size-4" aria-hidden="true" /> : <Plus className="size-4" aria-hidden="true" />}
            {isAddingRequirement ? 'Cancel' : 'Add requirement'}
          </Button>
        </div>

        {attachedRequirementsQuery.isError || requirementDefinitionsQuery.isError || locationsQuery.isError || attachRequirement.isError || updateAttachedRequirement.isError || setAttachedRequirementEnabled.isError || detachRequirement.isError ? (
          <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {attachedRequirementsQuery.isError ? 'Could not load attached requirements.' : requirementDefinitionsQuery.isError ? 'Could not load requirement definitions.' : locationsQuery.isError ? 'Could not load locations.' : attachRequirement.isError ? 'Could not attach requirement.' : updateAttachedRequirement.isError ? 'Could not update attached requirement.' : setAttachedRequirementEnabled.isError ? 'Could not change requirement state.' : 'Could not detach requirement.'}
          </p>
        ) : null}

        {isAddingRequirement ? (
          <div className="grid gap-4 rounded-structural border border-border p-4 md:grid-cols-2">
            <div className="md:col-span-2">
              <LocationSelector value={selectedLocationId} onChange={setSelectedLocationId} level="Room" disabled={attachRequirement.isPending} />
            </div>

            <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
              <span>Requirement</span>
              <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedRequirementId} onChange={(event) => setSelectedRequirementId(event.target.value)} disabled={attachRequirement.isPending || !selectedLocationId}>
                <option value="">Select requirement</option>
                {availableRequirements.map((item: RequirementDefinitionResponse) => <option key={item.id} value={item.id}>{item.name}</option>)}
              </select>
            </label>

            <label className="inline-flex items-center gap-3 rounded-structural border border-border p-4 text-[14px] font-medium md:col-span-2">
              <input type="checkbox" checked={isBlockingRequirement} onChange={(event) => setIsBlockingRequirement(event.target.checked)} disabled={attachRequirement.isPending} />
              Blocking requirement
            </label>

            <div className="md:col-span-2 flex justify-end">
              <Button type="button" disabled={!selectedLocationId || !selectedRequirementId || attachRequirement.isPending} onClick={handleAttachRequirement}>
                <Plus className="size-4" aria-hidden="true" />
                {attachRequirement.isPending ? 'Attaching...' : 'Attach requirement'}
              </Button>
            </div>
          </div>
        ) : null}

        {attachedRequirementsQuery.isLoading || requirementDefinitionsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading attached requirements...</p> : null}

        {!attachedRequirementsQuery.isLoading && attachedRequirements.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No requirements attached to this contractor job type yet.</p> : null}

        {attachedRequirements.length > 0 ? (
          <div className="grid gap-3">
            {attachedRequirements.map((item: LocationJobAttachedRequirementResponse) => {
              const location = locationsQuery.data?.get(item.locationId);

              return (
                <div key={item.policyId} className="flex items-center justify-between gap-4 rounded-structural border border-border p-4">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="font-medium text-foreground">{item.requirementName}</p>
                      <Badge variant="secondary">{item.requirementCode}</Badge>
                      {item.isBlocking ? <Badge variant="error">Blocking</Badge> : <Badge variant="secondary">Non-blocking</Badge>}
                      {item.isEnabled ? <Badge variant="success">Enabled</Badge> : <Badge variant="secondary">Disabled</Badge>}
                    </div>
                    <p className="mt-1 text-[14px] text-muted-foreground">Location: {getLocationLabel(location)} • Evaluator: {item.evaluatorKind}{item.isSensitive ? ' • Sensitive' : ''}</p>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    <Link to="/administration/access-model/compliancy/$requirementId/edit" params={{ requirementId: item.requirementDefinitionId }} className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${item.requirementName}`}>
                      <Pencil className="size-4" aria-hidden="true" />
                    </Link>
                    <Button type="button" variant="outline" size="sm" disabled={updateAttachedRequirement.isPending} onClick={() => updateAttachedRequirement.mutate({ policyId: item.policyId, request: { isBlocking: !item.isBlocking } })}>
                      {item.isBlocking ? 'Make non-blocking' : 'Make blocking'}
                    </Button>
                    <Button type="button" variant="outline" size="sm" disabled={setAttachedRequirementEnabled.isPending} onClick={() => setAttachedRequirementEnabled.mutate({ policyId: item.policyId, isEnabled: !item.isEnabled })}>
                      {item.isEnabled ? 'Disable' : 'Enable'}
                    </Button>
                    <Button type="button" variant="outline" size="sm" disabled={detachRequirement.isPending} onClick={() => detachRequirement.mutate(item.policyId)}>
                      <Trash2 className="size-4" aria-hidden="true" />
                      Detach
                    </Button>
                  </div>
                </div>
              );
            })}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function toFormValues(jobType: JobTypeResponse): ContractorJobTypeFormValues {
  return {
    code: jobType.code,
    name: jobType.name,
    description: jobType.description ?? '',
  };
}
