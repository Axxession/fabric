import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { RequirementForm, type RequirementFormValues } from './requirement-form';

type RequirementDefinitionResponse = components['schemas']['RequirementDefinitionResponse'];
type UpdateRequirementDefinitionRequest = components['schemas']['UpdateRequirementDefinitionRequest'];

const requirementsQueryKey = ['administration', 'access-model', 'compliancy', 'requirements'] as const;

export default function RequirementEditPage() {
  const { requirementId } = useParams({ from: '/main/administration/access-model/compliancy/$requirementId/edit' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const requirementQuery = useQuery({
    queryKey: [...requirementsQueryKey, requirementId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/requirements/definitions/{id}', { params: { path: { id: requirementId } } });
      if (error || !data) {
        throw new Error('Could not load requirement.');
      }
      return data;
    },
  });

  const updateRequirement = useMutation({
    mutationFn: async (request: UpdateRequirementDefinitionRequest) => {
      const { error } = await api.PUT('/api/requirements/definitions/{id}', { params: { path: { id: requirementId } }, body: request });
      if (error) {
        throw new Error('Could not save requirement.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: requirementsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...requirementsQueryKey, requirementId] }),
      ]);
      toast.success('Requirement saved.');
    },
    onError: () => {
      toast.error('Could not save requirement.');
    },
  });

  const deleteRequirement = useMutation({
    mutationFn: async () => {
      const { error } = await api.DELETE('/api/requirements/definitions/{id}', { params: { path: { id: requirementId } } });
      if (error) {
        throw new Error('Could not delete requirement.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: requirementsQueryKey });
      toast.success('Requirement deleted.');
      await navigate({ to: '/administration/access-model', search: { tab: 'compliancy' } as never, replace: true });
    },
    onError: () => {
      toast.error('Could not delete requirement. It may still be in use.');
    },
  });

  function handleSubmit(values: RequirementFormValues) {
    updateRequirement.mutate({
      code: values.code,
      name: values.name,
      description: values.description.trim() === '' ? null : values.description,
      evaluatorKind: values.evaluatorKind,
      isSensitive: values.isSensitive,
    });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-4">
          <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
            <ArrowLeft className="size-4" aria-hidden="true" />
          </Button>
          <div>
            <h2 className="text-[20px] font-semibold tracking-tight">Edit requirement</h2>
            <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update the compliancy requirement definition.</p>
          </div>
        </div>
        <Button type="button" variant="outline" disabled={deleteRequirement.isPending || requirementQuery.isLoading} onClick={() => deleteRequirement.mutate()}>
          <Trash2 className="size-4" aria-hidden="true" />
          Delete
        </Button>
      </header>

      <Card className="p-6">
        {requirementQuery.isError || updateRequirement.isError || deleteRequirement.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{requirementQuery.isError ? 'Could not load requirement.' : updateRequirement.isError ? 'Could not save requirement.' : 'Could not delete requirement.'}</p> : null}
        {requirementQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading requirement...</p> : null}
        {!requirementQuery.isLoading && requirementQuery.data ? <RequirementForm initialValues={toFormValues(requirementQuery.data)} isSubmitting={updateRequirement.isPending} submitLabel="Save requirement" onSubmit={handleSubmit} /> : null}
      </Card>
    </div>
  );
}

function toFormValues(requirement: RequirementDefinitionResponse): RequirementFormValues {
  return {
    code: requirement.code,
    name: requirement.name,
    description: requirement.description ?? '',
    evaluatorKind: requirement.evaluatorKind,
    isSensitive: requirement.isSensitive,
  };
}
