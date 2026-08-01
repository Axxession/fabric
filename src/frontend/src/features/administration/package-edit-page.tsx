import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, Pencil, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { PackageForm, type PackageFormValues } from './package-form';

type AccessItemResponse = components['schemas']['AccessItemResponse'];
type AddPackageAccessItemRequest = components['schemas']['AddPackageAccessItemRequest'];
type PackageAccessItemResponse = components['schemas']['PackageAccessItemResponse'];
type PackageResponse = components['schemas']['PackageResponse'];
type UpdatePackageRequest = components['schemas']['UpdatePackageRequest'];

const packagesQueryKey = ['administration', 'access-model', 'packages'] as const;
const accessItemsQueryKey = ['administration', 'access-control', 'items'] as const;

export default function PackageEditPage() {
  const { packageId } = useParams({ from: '/main/administration/access-model/packages/$packageId/edit' });
  const queryClient = useQueryClient();
  const [selectedAccessItemId, setSelectedAccessItemId] = useState('');
  const [isAssignOpen, setIsAssignOpen] = useState(false);

  const packageQuery = useQuery({
    queryKey: [...packagesQueryKey, packageId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/packages/{packageId}', { params: { path: { packageId } } });
      if (error || !data) {
        throw new Error('Could not load package.');
      }
      return data;
    },
  });

  const accessItemsQuery = useQuery({
    queryKey: [...accessItemsQueryKey, 'options'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/items', { params: { query: { Name: undefined, Page: 0, PageSize: 200 } as never } });
      if (error) {
        throw new Error('Could not load access items.');
      }
      return data?.items ?? [];
    },
  });

  const packageAccessItemsQuery = useQuery({
    queryKey: [...packagesQueryKey, packageId, 'access-items'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-catalog/packages/{packageId}/access-items', { params: { path: { packageId }, query: { Page: 0, PageSize: 200 } } });
      if (error) {
        throw new Error('Could not load package access items.');
      }
      return data;
    },
  });

  const updatePackage = useMutation({
    mutationFn: async (request: UpdatePackageRequest) => {
      const { error } = await api.PUT('/api/access-catalog/packages/{packageId}', { params: { path: { packageId } }, body: request });
      if (error) {
        throw new Error('Could not save package.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: packagesQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...packagesQueryKey, packageId] }),
      ]);
      toast.success('Package saved.');
    },
    onError: () => {
      toast.error('Could not save package.');
    },
  });

  const addAccessItem = useMutation({
    mutationFn: async (request: AddPackageAccessItemRequest) => {
      const { error } = await api.POST('/api/access-catalog/packages/{packageId}/access-items', { params: { path: { packageId } }, body: request });
      if (error) {
        throw new Error('Could not add access item to package.');
      }
    },
    onSuccess: async () => {
      setSelectedAccessItemId('');
      setIsAssignOpen(false);
      await queryClient.invalidateQueries({ queryKey: [...packagesQueryKey, packageId, 'access-items'] });
      toast.success('Access item linked to package.');
    },
    onError: () => {
      toast.error('Could not add access item to package.');
    },
  });

  const removeAccessItem = useMutation({
    mutationFn: async (accessItemId: string) => {
      const { error } = await api.DELETE('/api/access-catalog/packages/{packageId}/access-items/{accessItemId}', { params: { path: { packageId, accessItemId } } });
      if (error) {
        throw new Error('Could not remove access item from package.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...packagesQueryKey, packageId, 'access-items'] });
      toast.success('Access item removed from package.');
    },
    onError: () => {
      toast.error('Could not remove access item from package.');
    },
  });

  const currentPackage = packageQuery.data;
  const linkedItems = packageAccessItemsQuery.data?.items ?? [];
  const accessItemsById = new Map((accessItemsQuery.data ?? []).map((item) => [item.id, item]));
  const linkedItemIds = new Set(linkedItems.map((item) => item.accessItemId));
  const availableItems = (accessItemsQuery.data ?? []).filter((item) => !linkedItemIds.has(item.id));

  function handleSubmit(values: PackageFormValues) {
    updatePackage.mutate({
      name: values.name,
      description: values.description.trim() === '' ? null : values.description,
      status: values.status,
    });
  }

  function handleAddAccessItem() {
    if (!selectedAccessItemId) {
      return;
    }

    addAccessItem.mutate({ accessItemId: selectedAccessItemId });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Edit package</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update package details and access item links.</p>
        </div>
      </header>

      <Card className="p-6">
        {packageQuery.isError || updatePackage.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{packageQuery.isError ? 'Could not load package.' : 'Could not save package.'}</p> : null}
        {packageQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading package...</p> : null}
        {!packageQuery.isLoading && currentPackage && !packageQuery.isError ? <PackageForm initialValues={toFormValues(currentPackage)} isSubmitting={updatePackage.isPending} submitLabel="Save" includeStatus onSubmit={handleSubmit} /> : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Access Items</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Link access items to this package and remove them when no longer needed.</p>
          </div>
          <Button type="button" variant="outline" disabled={addAccessItem.isPending || availableItems.length === 0} onClick={() => setIsAssignOpen((current) => !current)}>
            <Plus className="size-4" aria-hidden="true" />
            {isAssignOpen ? 'Cancel' : 'Add'}
          </Button>
        </div>

        {isAssignOpen ? (
          <div className="grid gap-2 rounded-structural border border-border p-4 sm:max-w-80">
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={selectedAccessItemId} onChange={(event) => setSelectedAccessItemId(event.target.value)} disabled={addAccessItem.isPending || availableItems.length === 0}>
              <option value="">Select access item</option>
              {availableItems.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            <Button type="button" disabled={!selectedAccessItemId || addAccessItem.isPending} onClick={handleAddAccessItem}>
              <Plus className="size-4" aria-hidden="true" />
              Link access item
            </Button>
          </div>
        ) : null}

        {accessItemsQuery.isError || packageAccessItemsQuery.isError || addAccessItem.isError || removeAccessItem.isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{accessItemsQuery.isError ? 'Could not load access items.' : packageAccessItemsQuery.isError ? 'Could not load package access items.' : addAccessItem.isError ? 'Could not add access item to package.' : 'Could not remove access item from package.'}</p> : null}
        {accessItemsQuery.isLoading || packageAccessItemsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading access items...</p> : null}
        {!packageAccessItemsQuery.isLoading && linkedItems.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No access items linked yet.</p> : null}

        {linkedItems.length > 0 ? (
          <div className="grid gap-3">
            {linkedItems.map((link: PackageAccessItemResponse) => {
              const accessItem = accessItemsById.get(link.accessItemId);
              return (
                <div key={link.accessItemId} className="flex items-center justify-between gap-4 rounded-structural border border-border p-4">
                  <div className="min-w-0">
                    <p className="font-medium text-foreground">{accessItem?.name ?? link.accessItemId}</p>
                    <p className="mt-1 text-[14px] text-muted-foreground">{accessItem?.description ?? 'Linked access item'}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Link to="/administration/access-control/items/$itemId/edit" params={{ itemId: link.accessItemId }} className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground" aria-label={`Edit ${accessItem?.name ?? 'access item'}`}>
                      <Pencil className="size-4" aria-hidden="true" />
                    </Link>
                    <Button type="button" variant="outline" size="sm" disabled={removeAccessItem.isPending} onClick={() => removeAccessItem.mutate(link.accessItemId)}>
                      <Trash2 className="size-4" aria-hidden="true" />
                      Remove
                    </Button>
                  </div>
                </div>
              );
            })}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function toFormValues(currentPackage: PackageResponse): PackageFormValues {
  return {
    name: currentPackage.name,
    description: currentPackage.description ?? '',
    status: currentPackage.status,
  };
}
