import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { VisitForm, type VisitFormValues, getDefaultVisitFormValues } from './visit-form';

const visitsQueryKey = ['visitors-management', 'visits'] as const;

export default function VisitCreatePage() {
  const queryClient = useQueryClient();

  const createVisit = useMutation({
    mutationFn: async (values: VisitFormValues) => {
      const { error } = await api.POST('/api/visitors/visits', {
        body: {
          hostEmployeeId: values.hostEmployeeId,
          summary: values.summary,
          start: new Date(values.start).toISOString(),
          stop: new Date(values.stop).toISOString(),
          locationId: values.locationId,
        },
      });

      if (error) {
        throw new Error('Could not create visit.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: visitsQueryKey });
      toast.success('Visit created.');
      window.history.back();
    },
  });

  function handleSubmit(values: VisitFormValues) {
    createVisit.mutate(values);
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button
          variant="outline"
          size="icon"
          aria-label="Go back"
          onClick={() => window.history.back()}
        >
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>

        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add visit</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Schedule a new visit with a host, time range, and summary.</p>
        </div>
      </header>

      <Card className="p-6">
        {createVisit.isError ? (
          <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            Could not create visit.
          </p>
        ) : null}

        <VisitForm initialValues={getDefaultVisitFormValues()} isSubmitting={createVisit.isPending} submitLabel="Create visit" onSubmit={handleSubmit} />
      </Card>
    </div>
  );
}
