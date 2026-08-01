import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { CatalogueForm, type CatalogueFormValues } from './catalogue-form';

type CatalogPackageResponse = components['schemas']['CatalogPackageResponse'];
type CatalogResponse = components['schemas']['CatalogResponse'];
type LinkCatalogPackageRequest = components['schemas']['LinkCatalogPackageRequest'];
type PackageResponse = components['schemas']['PackageResponse'];
type UpdateCatalogRequest = components['schemas']['UpdateCatalogRequest'];

const cataloguesQueryKey = ['administration', 'access-model', 'catalogues'] as const;
const packagesQueryKey = ['administration', 'access-model', 'packages'] as const;

export default function CatalogueEditPage() {
  const { catalogueId } = useParams({ from: '/main/administration/access-model/catalogues/$catalogueId/edit' });
  const queryClient = useQueryClient();
  const [selectedPackageId, setSelectedPackageId] = useState('');
  const [isAddOpen, setIsAddOpen] = useState(false);

  const catalogueQuery = useQuery({
    queryKey: [...cataloguesQueryKey, catalogueId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/catalogs/{catalogId}', { params: { path: { catalogId: catalogueId } } });
      if (error || !data) {
        throw new Error('Could not load catalogue.');
      }
      return data;
    },
  });

  const packagesQuery = useQuery({
    queryKey: [...packagesQueryKey, 'options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/packages', { params: { query: { Name: undefined, Page: 0, PageSize: 200 } as never } });
      if (error) {
        throw new Error('Could not load packages.');
      }
      return data?.items ?? [];
    },
  });

  const cataloguePackagesQuery = useQuery({
    queryKey: [...cataloguesQueryKey, catalogueId, 'packages'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/catalogs/{catalogId}/packages', { params: { path: { catalogId: catalogueId }, query: { Page: 0, PageSize: 200 } } });
      if (error) {
        throw new Error('Could not load catalogue packages.');
      }
      return data;
    },
  });

  const updateCatalogue = useMutation({
    mutationFn: async (request: UpdateCatalogRequest) => {
      const { error } = await api.PUT('/api/access-catalog/catalogs/{catalogId}', { params: { path: { catalogId: catalogueId } }, body: request });
      if (error) {
        throw new Error('Could not save catalogue.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: cataloguesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...cataloguesQueryKey, catalogueId] }),
      ]);
      toast.success('Catalogue saved.');
    },
    onError: () => {
      toast.error('Could not save catalogue.');
    },
  });

  const addPackage = useMutation({
    mutationFn: async (request: LinkCatalogPackageRequest) => {
      const { error } = await api.POST('/api/access-catalog/catalogs/{catalogId}/packages', { params: { path: { catalogId: catalogueId } }, body: request });
      if (error) {
        throw new Error('Could not add package to catalogue.');
      }
    },
    onSuccess: async () => {
      setSelectedPackageId('');
      setIsAddOpen(false);
      await queryClient.invalidateQueries({ queryKey: [...cataloguesQueryKey, catalogueId, 'packages'] });
      toast.success('Package linked to catalogue.');
    },
    onError: () => {
      toast.error('Could not add package to catalogue.');
    },
  });

  const removePackage = useMutation({
    mutationFn: async (packageId: string) => {
      const { error } = await api.DELETE('/api/access-catalog/catalogs/{catalogId}/packages/{packageId}', { params: { path: { catalogId: catalogueId, packageId } } });
      if (error) {
        throw new Error('Could not remove package from catalogue.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...cataloguesQueryKey, catalogueId, 'packages'] });
      toast.success('Package removed from catalogue.');
    },
    onError: () => {
      toast.error('Could not remove package from catalogue.');
    },
  });

  const currentCatalogue = catalogueQuery.data;
  const linkedPackages = cataloguePackagesQuery.data?.items ?? [];
  const packagesById = new Map((packagesQuery.data ?? []).map((item) => [item.id, item]));
  const linkedPackageIds = new Set(linkedPackages.map((item) => item.packageId));
  const availablePackages = (packagesQuery.data ?? []).filter((item) => !linkedPackageIds.has(item.id));

  function handleSubmit(values: CatalogueFormValues) {
    updateCatalogue.mutate({ name: values.name, description: values.description.trim() === '' ? null : values.description, status: values.status });
  }

  function handleAddPackage() {
    if (!selectedPackageId) {
      return;
    }
    addPackage.mutate({ packageId: selectedPackageId, isRequestable: true });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Edit catalogue</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update catalogue details and linked packages.</p>
        </div>
      </header>

      <Card className="p-6">
        {catalogueQuery.isError || updateCatalogue.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{catalogueQuery.isError ? 'Could not load catalogue.' : 'Could not save catalogue.'}</p> : null}
        {catalogueQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading catalogue...</p> : null}
        {!catalogueQuery.isLoading && currentCatalogue && !catalogueQuery.isError ? <CatalogueForm initialValues={toFormValues(currentCatalogue)} isSubmitting={updateCatalogue.isPending} submitLabel="Save" includeStatus onSubmit={handleSubmit} /> : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Packages</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Link packages to this catalogue and remove them when no longer needed.</p>
          </div>
          <Button type="button" variant="outline" disabled={addPackage.isPending || availablePackages.length === 0} onClick={() => setIsAddOpen((current) => !current)}>
            <Plus className="size-4" aria-hidden="true" />
            {isAddOpen ? 'Cancel' : 'Add'}
          </Button>
        </div>

        {isAddOpen ? (
          <div className="grid gap-2 rounded-structural border border-border p-4 sm:max-w-80">
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedPackageId} onChange={(event) => setSelectedPackageId(event.target.value)} disabled={addPackage.isPending || availablePackages.length === 0}>
              <option value="">Select package</option>
              {availablePackages.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            <Button type="button" disabled={!selectedPackageId || addPackage.isPending} onClick={handleAddPackage}>
              <Plus className="size-4" aria-hidden="true" />
              Link package
            </Button>
          </div>
        ) : null}

        {packagesQuery.isError || cataloguePackagesQuery.isError || addPackage.isError || removePackage.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{packagesQuery.isError ? 'Could not load packages.' : cataloguePackagesQuery.isError ? 'Could not load catalogue packages.' : addPackage.isError ? 'Could not add package to catalogue.' : 'Could not remove package from catalogue.'}</p> : null}
        {packagesQuery.isLoading || cataloguePackagesQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading packages...</p> : null}
        {!cataloguePackagesQuery.isLoading && linkedPackages.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No packages linked yet.</p> : null}

        {linkedPackages.length > 0 ? (
          <div className="grid gap-3">
            {linkedPackages.map((link: CatalogPackageResponse) => {
              const linkedPackage = packagesById.get(link.packageId);
              return (
                <div key={link.packageId} className="flex items-center justify-between gap-4 rounded-structural border border-border p-4">
                  <div className="min-w-0">
                    <p className="font-medium text-foreground">{linkedPackage?.name ?? link.packageId}</p>
                    <p className="mt-1 text-[14px] text-muted-foreground">{linkedPackage?.description ?? 'Linked package'}</p>
                  </div>
                  <Button type="button" variant="outline" size="sm" disabled={removePackage.isPending} onClick={() => removePackage.mutate(link.packageId)}>
                    <Trash2 className="size-4" aria-hidden="true" />
                    Remove
                  </Button>
                </div>
              );
            })}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function toFormValues(catalogue: CatalogResponse): CatalogueFormValues {
  return {
    name: catalogue.name,
    description: catalogue.description ?? '',
    status: catalogue.status,
  };
}
