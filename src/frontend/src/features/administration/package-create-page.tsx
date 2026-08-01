import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { PackageForm, type PackageFormValues } from './package-form';

type CreatePackageRequest = components['schemas']['CreatePackageRequest'];

const packagesQueryKey = ['administration', 'access-model', 'packages'] as const;
const emptyPackage: PackageFormValues = { name: '', description: '', status: 'Active' };

export default function PackageCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const createPackage = useMutation({
    mutationFn: async (request: CreatePackageRequest) => {
      const { data, error } = await api.POST('/api/access-catalog/packages', { body: request });
      if (error || !data) {
        throw new Error('Could not create package.');
      }
      return data;
    },
    onSuccess: async (createdPackage) => {
      await queryClient.invalidateQueries({ queryKey: packagesQueryKey });
      toast.success('Package created.');
      await navigate({ to: '/administration/access-model/packages/$packageId/edit', params: { packageId: createdPackage.id }, replace: true });
    },
    onError: () => {
      toast.error('Could not create package.');
    },
  });

  function handleSubmit(values: PackageFormValues) {
    createPackage.mutate({ name: values.name, description: values.description.trim() === '' ? null : values.description });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add package</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new access package in the access model.</p>
        </div>
      </header>

      <Card className="p-6">
        {createPackage.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not create package.</p> : null}
        <PackageForm initialValues={emptyPackage} isSubmitting={createPackage.isPending} submitLabel="Create package" includeStatus={false} onSubmit={handleSubmit} />
      </Card>
    </div>
  );
}
