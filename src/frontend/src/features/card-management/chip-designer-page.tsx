import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Cpu, Pencil, Plus, Route, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Badge } from '@/shared/components/ui/badge';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

import { chipDesignsQueryKey, formatDateTime, systemProvidersQueryKey, type ChipDesign, type SystemProvider, type Transformation } from './card-management-types';

const chipDesignerTransformationsQueryKey = ['card-management', 'chip-designer-page', 'transformations'] as const;

export default function ChipDesignerPage() {
  const queryClient = useQueryClient();

  const chipDesignsQuery = useQuery({
    queryKey: chipDesignsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/chip-designs', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error) {
        throw new Error('Could not load chip designs.');
      }
      return data;
    },
  });

  const transformationsQuery = useQuery({
    queryKey: chipDesignerTransformationsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/transformations', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error) {
        throw new Error('Could not load transformations.');
      }
      return data;
    },
  });

  const systemProvidersQuery = useQuery({
    queryKey: systemProvidersQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/system-providers', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error) {
        throw new Error('Could not load system providers.');
      }
      return data;
    },
  });

  const deleteChipDesign = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/desfire/chip-designs/{id}', { params: { path: { id } } });
      if (error) {
        throw new Error('Could not delete chip design.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: chipDesignsQueryKey });
      toast.success('Chip design deleted.');
    },
    onError: () => toast.error('Could not delete chip design. Remove referencing transformations first.'),
  });

  const deleteTransformation = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/desfire/transformations/{id}', { params: { path: { id } } });
      if (error) {
        throw new Error('Could not delete transformation.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: chipDesignerTransformationsQueryKey });
      toast.success('Transformation deleted.');
    },
    onError: () => toast.error('Could not delete transformation. Encoding history may reference it.'),
  });

  const deleteSystemProvider = useMutation({
    mutationFn: async (id: string) => {
      const { error } = await api.DELETE('/api/desfire/system-providers/{id}', { params: { path: { id } } });
      if (error) {
        throw new Error('Could not delete system provider.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: systemProvidersQueryKey });
      toast.success('System provider deleted.');
    },
    onError: () => toast.error('Could not delete system provider. A transformation may reference it.'),
  });

  const chipDesigns = chipDesignsQuery.data?.items ?? [];
  const transformations = transformationsQuery.data?.items ?? [];
  const systemProviders = systemProvidersQuery.data?.items ?? [];

  return (
    <section className="rounded-structural border border-border bg-content">
      <div className="border-b border-border p-4 sm:p-6">
        <h1 className="text-[20px] font-semibold tracking-tight">Chip Designer</h1>
        <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Design DESFire chip templates and define transformations between blank or existing card layouts.</p>
      </div>

      <div className="p-4 sm:p-6">
        <Tabs defaultValue="chip-designs">
          <TabsList>
            <TabsTrigger value="chip-designs">Chip designs</TabsTrigger>
            <TabsTrigger value="transformations">Transformations</TabsTrigger>
            <TabsTrigger value="system-providers">System providers</TabsTrigger>
          </TabsList>

          <TabsContent value="chip-designs">
            <ChipDesignsPanel
              designs={chipDesigns}
              isLoading={chipDesignsQuery.isLoading}
              isError={chipDesignsQuery.isError}
              isDeleting={deleteChipDesign.isPending}
              onDelete={(design) => {
                if (window.confirm(`Delete chip design "${design.name}" v${design.version}?`)) {
                  deleteChipDesign.mutate(design.id);
                }
              }}
            />
          </TabsContent>

          <TabsContent value="transformations">
            <TransformationsPanel
              transformations={transformations}
              isLoading={transformationsQuery.isLoading}
              isError={transformationsQuery.isError}
              isDeleting={deleteTransformation.isPending}
              onDelete={(transformation) => {
                if (window.confirm(`Delete transformation "${transformation.name}"?`)) {
                  deleteTransformation.mutate(transformation.id);
                }
              }}
            />
          </TabsContent>

          <TabsContent value="system-providers">
            <SystemProvidersPanel
              providers={systemProviders}
              isLoading={systemProvidersQuery.isLoading}
              isError={systemProvidersQuery.isError}
              isDeleting={deleteSystemProvider.isPending}
              onDelete={(provider) => {
                if (window.confirm(`Delete system provider "${provider.name}"?`)) {
                  deleteSystemProvider.mutate(provider.id);
                }
              }}
            />
          </TabsContent>
        </Tabs>
      </div>
    </section>
  );
}

function SystemProvidersPanel({ providers, isLoading, isError, isDeleting, onDelete }: { readonly providers: SystemProvider[]; readonly isLoading: boolean; readonly isError: boolean; readonly isDeleting: boolean; readonly onDelete: (provider: SystemProvider) => void }) {
  return (
    <div className="grid gap-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-[16px] font-semibold tracking-tight">System providers</h2>
          <p className="mt-1 text-[14px] text-muted-foreground">Named fixed values and sequence counters used by system-provided transformation variables.</p>
        </div>
        <Link to="/desfire-studio/system-providers/new" className={buttonVariants({ className: 'w-full sm:w-fit' })}>
          <Plus className="size-4" aria-hidden="true" />
          Add system provider
        </Link>
      </div>

      {isError ? <PanelError>Could not load system providers.</PanelError> : null}
      {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading system providers...</p> : null}
      {!isLoading && !isError && providers.length === 0 ? <Empty><EmptyHeader><EmptyTitle>No system providers yet</EmptyTitle><EmptyDescription>Add a fixed value or sequence provider before assigning system-provided variables.</EmptyDescription></EmptyHeader></Empty> : null}
      {providers.length > 0 ? <SystemProvidersTable providers={providers} isDeleting={isDeleting} onDelete={onDelete} /> : null}
    </div>
  );
}

function SystemProvidersTable({ providers, isDeleting, onDelete }: { readonly providers: SystemProvider[]; readonly isDeleting: boolean; readonly onDelete: (provider: SystemProvider) => void }) {
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[54rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Type</th><th className="px-4 py-3 font-semibold">Value source</th><th className="px-4 py-3 font-semibold">Created</th><th className="px-4 py-3 text-right font-semibold">Actions</th></tr>
        </thead>
        <tbody className="divide-y divide-border">
          {providers.map((provider) => (
            <tr key={provider.id}>
              <td className="px-4 py-4 font-medium text-foreground">{provider.name}</td>
              <td className="px-4 py-4"><Badge variant={provider.providerType === 'Sequence' ? 'warning' : 'secondary'}>{provider.providerType}</Badge></td>
              <td className="px-4 py-4 text-muted-foreground">{formatProviderValue(provider)}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatDateTime(provider.createdAt)}</td>
              <td className="px-4 py-4">
                <div className="flex justify-end gap-2">
                  <Button type="button" variant="outline" size="sm" disabled={isDeleting} onClick={() => onDelete(provider)}><Trash2 className="size-4" aria-hidden="true" />Delete</Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function ChipDesignsPanel({ designs, isLoading, isError, isDeleting, onDelete }: { readonly designs: ChipDesign[]; readonly isLoading: boolean; readonly isError: boolean; readonly isDeleting: boolean; readonly onDelete: (design: ChipDesign) => void }) {
  return (
    <div className="grid gap-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-[16px] font-semibold tracking-tight">Chip designs</h2>
          <p className="mt-1 text-[14px] text-muted-foreground">Versioned DESFire template specifications used as transformation sources and targets.</p>
        </div>
        <Link to="/desfire-studio/chip-designs/new" className={buttonVariants({ className: 'w-full sm:w-fit' })}>
          <Plus className="size-4" aria-hidden="true" />
          Add chip design
        </Link>
      </div>

      {isError ? <PanelError>Could not load chip designs.</PanelError> : null}
      {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading chip designs...</p> : null}
      {!isLoading && !isError && designs.length === 0 ? <Empty><EmptyHeader><EmptyTitle>No chip designs yet</EmptyTitle><EmptyDescription>Add a chip design before creating transformations.</EmptyDescription></EmptyHeader></Empty> : null}
      {designs.length > 0 ? <ChipDesignsTable designs={designs} isDeleting={isDeleting} onDelete={onDelete} /> : null}
    </div>
  );
}

function ChipDesignsTable({ designs, isDeleting, onDelete }: { readonly designs: ChipDesign[]; readonly isDeleting: boolean; readonly onDelete: (design: ChipDesign) => void }) {
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[58rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Version</th><th className="px-4 py-3 font-semibold">Applications</th><th className="px-4 py-3 font-semibold">Description</th><th className="px-4 py-3 font-semibold">Created</th><th className="px-4 py-3 text-right font-semibold">Actions</th></tr>
        </thead>
        <tbody className="divide-y divide-border">
          {designs.map((design) => (
            <tr key={design.id}>
              <td className="px-4 py-4 font-medium text-foreground"><span className="inline-flex items-center gap-2"><Cpu className="size-4 text-primary" aria-hidden="true" />{design.name}</span></td>
              <td className="px-4 py-4"><Badge variant="outline">v{design.version}</Badge></td>
              <td className="px-4 py-4 text-muted-foreground">{Object.keys(design.specification.applications ?? {}).length}</td>
              <td className="max-w-[18rem] truncate px-4 py-4 text-muted-foreground">{design.description || 'None'}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatDateTime(design.createdAt)}</td>
              <td className="px-4 py-4">
                <div className="flex justify-end gap-2">
                  <Link to="/desfire-studio/chip-designs/$chipDesignId/edit" params={{ chipDesignId: design.id }} className={buttonVariants({ variant: 'outline', size: 'sm' })}><Pencil className="size-4" aria-hidden="true" />Edit</Link>
                  <Button type="button" variant="outline" size="sm" disabled={isDeleting} onClick={() => onDelete(design)}><Trash2 className="size-4" aria-hidden="true" />Delete</Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function TransformationsPanel({ transformations, isLoading, isError, isDeleting, onDelete }: { readonly transformations: Transformation[]; readonly isLoading: boolean; readonly isError: boolean; readonly isDeleting: boolean; readonly onDelete: (transformation: Transformation) => void }) {
  return (
    <div className="grid gap-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-[16px] font-semibold tracking-tight">Transformations</h2>
          <p className="mt-1 text-[14px] text-muted-foreground">Conversion definitions from a blank card or existing chip design to a target chip design.</p>
        </div>
        <Link to="/desfire-studio/transformations/new" className={buttonVariants({ className: 'w-full sm:w-fit' })}>
          <Plus className="size-4" aria-hidden="true" />
          Add transformation
        </Link>
      </div>

      {isError ? <PanelError>Could not load transformations.</PanelError> : null}
      {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading transformations...</p> : null}
      {!isLoading && !isError && transformations.length === 0 ? <Empty><EmptyHeader><EmptyTitle>No transformations yet</EmptyTitle><EmptyDescription>Add a transformation once chip designs are available.</EmptyDescription></EmptyHeader></Empty> : null}
      {transformations.length > 0 ? <TransformationsTable transformations={transformations} isDeleting={isDeleting} onDelete={onDelete} /> : null}
    </div>
  );
}

function TransformationsTable({ transformations, isDeleting, onDelete }: { readonly transformations: Transformation[]; readonly isDeleting: boolean; readonly onDelete: (transformation: Transformation) => void }) {
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[64rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr><th className="px-4 py-3 font-semibold">Name</th><th className="px-4 py-3 font-semibold">Source</th><th className="px-4 py-3 font-semibold">Target</th><th className="px-4 py-3 font-semibold">Variables</th><th className="px-4 py-3 font-semibold">Key groups</th><th className="px-4 py-3 font-semibold">Updated</th><th className="px-4 py-3 text-right font-semibold">Actions</th></tr>
        </thead>
        <tbody className="divide-y divide-border">
          {transformations.map((transformation) => (
            <tr key={transformation.id}>
              <td className="px-4 py-4 font-medium text-foreground"><span className="inline-flex items-center gap-2"><Route className="size-4 text-primary" aria-hidden="true" />{transformation.name}</span></td>
              <td className="px-4 py-4 text-muted-foreground">{transformation.fromBlank ? 'Blank chip' : transformation.fromChipDesignName ?? 'Unknown'}</td>
              <td className="px-4 py-4 text-muted-foreground">{transformation.toChipDesignName}</td>
              <td className="px-4 py-4 text-muted-foreground">{transformation.requiredVariables.length}</td>
              <td className="px-4 py-4 text-muted-foreground">{transformation.requiredKeyGroups.length}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatDateTime(transformation.updatedAt)}</td>
              <td className="px-4 py-4">
                <div className="flex justify-end gap-2">
                  <Link to="/desfire-studio/transformations/$transformationId/edit" params={{ transformationId: transformation.id }} className={buttonVariants({ variant: 'outline', size: 'sm' })}><Pencil className="size-4" aria-hidden="true" />Edit</Link>
                  <Button type="button" variant="outline" size="sm" disabled={isDeleting} onClick={() => onDelete(transformation)}><Trash2 className="size-4" aria-hidden="true" />Delete</Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function formatProviderValue(provider: SystemProvider) {
  if (provider.providerType === 'Fixed') {
    return provider.fixedValue ?? '';
  }

  return `Current ${provider.currentValue ?? provider.initialValue ?? 0}`;
}

function PanelError({ children }: { readonly children: React.ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
