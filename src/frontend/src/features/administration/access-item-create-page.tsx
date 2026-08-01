import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { AccessItemForm, type AccessItemFormValues } from './access-item-form';

type CreateAccessItemRequest = components['schemas']['CreateAccessItemRequest'];

const accessItemsQueryKey = ['administration', 'access-control', 'items'] as const;
const emptyFormValues: AccessItemFormValues = {
  name: '',
  description: '',
  status: 'Active',
};

export default function AccessItemCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const createAccessItem = useMutation({
    mutationFn: async (request: CreateAccessItemRequest) => {
      const { data, error } = await api.POST('/api/access-control/items', { body: request });
      if (error || !data) {
        throw new Error('Could not create access item.');
      }
      return data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: accessItemsQueryKey });
      toast.success('Access item created.');
      await navigate({ to: '/administration/access-control', search: { tab: 'access-items' } as never, replace: true });
    },
    onError: () => {
      toast.error('Could not create access item.');
    },
  });

  function handleSubmit(values: AccessItemFormValues) {
    createAccessItem.mutate({
      name: values.name,
      description: values.description.trim() === '' ? null : values.description,
    });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>

        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add access item</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new physical access item for your access control model.</p>
        </div>
      </header>

      <Card className="p-6">
        {createAccessItem.isError ? (
          <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            Could not create access item.
          </p>
        ) : null}

        <AccessItemForm initialValues={emptyFormValues} isSubmitting={createAccessItem.isPending} submitLabel="Create access item" includeStatus={false} onSubmit={handleSubmit} />
      </Card>
    </div>
  );
}
