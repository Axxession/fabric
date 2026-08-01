import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { SiteForm, type SiteFormValues } from './site-form';

const locationsQueryKey = ['facility', 'locations'] as const;
const emptySite: SiteFormValues = { name: '', address: '' };

export default function SiteCreatePage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const createSite = useMutation({
    mutationFn: async (values: SiteFormValues) => {
      const { data, error } = await api.POST('/api/locations/sites', {
        body: {
          id: null,
          name: values.name,
          address: values.address || null,
        },
      });

      if (error || !data) {
        throw new Error('Could not create site.');
      }

      return data;
    },
    onSuccess: async (location) => {
      await queryClient.invalidateQueries({ queryKey: locationsQueryKey });
      toast.success('Site created.');
      await navigate({ to: '/administration/sites/$siteId/edit', params: { siteId: location.site.id }, replace: true });
    },
  });

  function handleSubmit(values: SiteFormValues) {
    createSite.mutate(values);
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>

        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add site</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a site that can contain buildings and rooms.</p>
        </div>
      </header>

      <Card className="p-6">
        {createSite.isError ? (
          <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            Could not create site.
          </p>
        ) : null}

        <SiteForm initialValues={emptySite} isSubmitting={createSite.isPending} submitLabel="Create site" onSubmit={handleSubmit} />
      </Card>
    </div>
  );
}
