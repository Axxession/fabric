import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { ApprovalGroupForm, type ApprovalGroupFormValues } from './approval-group-form';

type CreateApprovalGroupRequest = components['schemas']['CreateApprovalGroupRequest'];

const approvalGroupsQueryKey = ['administration', 'access-model', 'approval-groups'] as const;
const emptyApprovalGroup: ApprovalGroupFormValues = { name: '', status: 'Active' };

export default function ApprovalGroupCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const createApprovalGroup = useMutation({
    mutationFn: async (request: CreateApprovalGroupRequest) => {
      const { data, error } = await api.POST('/api/access-catalog/approval-groups', { body: request });
      if (error || !data) throw new Error('Could not create approval group.');
      return data;
    },
    onSuccess: async (approvalGroup) => {
      await queryClient.invalidateQueries({ queryKey: approvalGroupsQueryKey });
      toast.success('Approval group created.');
      await navigate({ to: '/administration/access-model/approval-groups/$approvalGroupId/edit', params: { approvalGroupId: approvalGroup.id }, replace: true });
    },
    onError: () => toast.error('Could not create approval group.'),
  });

  function handleSubmit(values: ApprovalGroupFormValues) {
    createApprovalGroup.mutate({ name: values.name });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add approval group</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new approval group in the access model.</p>
        </div>
      </header>

      <Card className="p-6">
        {createApprovalGroup.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not create approval group.</p> : null}
        <ApprovalGroupForm initialValues={emptyApprovalGroup} isSubmitting={createApprovalGroup.isPending} submitLabel="Create approval group" includeStatus={false} onSubmit={handleSubmit} />
      </Card>
    </div>
  );
}
