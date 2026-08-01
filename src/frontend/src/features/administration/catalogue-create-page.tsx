import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { CatalogueForm, type CatalogueFormValues } from './catalogue-form';

type CreateCatalogRequest = components['schemas']['CreateCatalogRequest'];

const cataloguesQueryKey = ['administration', 'access-model', 'catalogues'] as const;
const emptyCatalogue: CatalogueFormValues = { name: '', description: '', status: 'Active' };

export default function CatalogueCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const createCatalogue = useMutation({
    mutationFn: async (request: CreateCatalogRequest) => {
      const { data, error } = await api.POST('/api/access-catalog/catalogs', { body: request });
      if (error || !data) {
        throw new Error('Could not create catalogue.');
      }
      return data;
    },
    onSuccess: async (catalogue) => {
      await queryClient.invalidateQueries({ queryKey: cataloguesQueryKey });
      toast.success('Catalogue created.');
      await navigate({ to: '/administration/access-model/catalogues/$catalogueId/edit', params: { catalogueId: catalogue.id }, replace: true });
    },
    onError: () => {
      toast.error('Could not create catalogue.');
    },
  });

  function handleSubmit(values: CatalogueFormValues) {
    createCatalogue.mutate({ name: values.name, description: values.description.trim() === '' ? null : values.description });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add catalogue</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new catalogue in the access model.</p>
        </div>
      </header>

      <Card className="p-6">
        {createCatalogue.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not create catalogue.</p> : null}
        <CatalogueForm initialValues={emptyCatalogue} isSubmitting={createCatalogue.isPending} submitLabel="Create catalogue" includeStatus={false} onSubmit={handleSubmit} />
      </Card>
    </div>
  );
}
