import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { ContractorJobTypeForm, type ContractorJobTypeFormValues } from './contractor-job-type-form';

type CreateJobTypeRequest = components['schemas']['CreateJobTypeRequest'];

const contractorJobTypesQueryKey = ['administration', 'my-organization', 'contractor-job-types'] as const;
const emptyJobType: ContractorJobTypeFormValues = { code: '', name: '', description: '' };

export default function ContractorJobTypeCreatePage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const createJobType = useMutation({
    mutationFn: async (values: ContractorJobTypeFormValues) => {
      const request: CreateJobTypeRequest = {
        code: values.code,
        name: values.name,
        description: values.description || null,
      };

      const { data, error } = await api.POST('/api/contractors/job-types', { body: request });
      if (error || !data) throw new Error('Could not create contractor job type.');
      return data;
    },
    onSuccess: async (jobType) => {
      await queryClient.invalidateQueries({ queryKey: contractorJobTypesQueryKey });
      toast.success('Contractor job type created.');
      await navigate({ to: '/administration/my-organization/contractor-job-types/$jobTypeId/edit', params: { jobTypeId: jobType.id }, replace: true });
    },
    onError: () => toast.error('Could not create contractor job type.'),
  });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add contractor job type</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new contractor job type in My Organization.</p>
        </div>
      </header>

      <Card className="p-6">
        {createJobType.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not create contractor job type.</p> : null}
        <ContractorJobTypeForm initialValues={emptyJobType} isSubmitting={createJobType.isPending} submitLabel="Create contractor job type" onSubmit={(values) => createJobType.mutate(values)} />
      </Card>
    </div>
  );
}
