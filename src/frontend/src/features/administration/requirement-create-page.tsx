import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { RequirementForm, type RequirementFormValues } from './requirement-form';

type CreateRequirementDefinitionRequest = components['schemas']['CreateRequirementDefinitionRequest'];

const requirementsQueryKey = ['administration', 'access-model', 'compliancy', 'requirements'] as const;
const emptyRequirement: RequirementFormValues = { code: '', name: '', description: '', allowedEvidenceKinds: ['Document'], isSensitive: false };

export default function RequirementCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const createRequirement = useMutation({
    mutationFn: async (request: CreateRequirementDefinitionRequest) => {
      const { data, error } = await api.POST('/api/requirements/definitions', { body: request });
      if (error || !data) {
        throw new Error('Could not create requirement.');
      }
      return data;
    },
    onSuccess: async (createdRequirement) => {
      await queryClient.invalidateQueries({ queryKey: requirementsQueryKey });
      toast.success('Requirement created.');
      await navigate({ to: '/administration/access-model/compliancy/$requirementId/edit', params: { requirementId: createdRequirement.id }, replace: true });
    },
    onError: () => {
      toast.error('Could not create requirement.');
    },
  });

  function handleSubmit(values: RequirementFormValues) {
    createRequirement.mutate({
      code: values.code,
      name: values.name,
      description: values.description.trim() === '' ? null : values.description,
      allowedEvidenceKinds: [...values.allowedEvidenceKinds],
      isSensitive: values.isSensitive,
    });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add requirement</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new compliancy requirement definition.</p>
        </div>
      </header>

      <Card className="p-6">
        {createRequirement.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not create requirement.</p> : null}
        <RequirementForm initialValues={emptyRequirement} isSubmitting={createRequirement.isPending} submitLabel="Create requirement" onSubmit={handleSubmit} />
      </Card>
    </div>
  );
}
