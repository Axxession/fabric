import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Copy, KeyRound, Pencil, Plus, RotateCcw, Trash2, X } from 'lucide-react';
import { useState, type FormEvent } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getLocationLabel, LocationSelector, type LocationResponse } from '@/shared/components/location-selector';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Input } from '@/shared/components/ui/input';

type ReceptionDeskWorkstation = components['schemas']['ReceptionDeskWorkstationResponse'];
type ReceptionDeskWorkstationKeyResponse = components['schemas']['ReceptionDeskWorkstationKeyResponse'];
type CreateReceptionDeskWorkstationRequest = components['schemas']['CreateReceptionDeskWorkstationRequest'];
type UpdateReceptionDeskWorkstationRequest = components['schemas']['UpdateReceptionDeskWorkstationRequest'];

type FormValues = {
  readonly name: string;
  readonly locationId: string | null;
  readonly enabled: boolean;
};

const workstationsQueryKey = ['administration', 'clients', 'reception-desk-workstations'] as const;
const emptyFormValues: FormValues = {
  name: '',
  locationId: null,
  enabled: true,
};

export function ReceptionDeskWorkstationAdminPanel() {
  const queryClient = useQueryClient();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingWorkstationId, setEditingWorkstationId] = useState<string | null>(null);
  const [form, setForm] = useState<FormValues>(emptyFormValues);
  const [latestKey, setLatestKey] = useState<ReceptionDeskWorkstationKeyResponse | null>(null);

  const workstationsQuery = useQuery({
    queryKey: workstationsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/workstations', {
        params: { query: { Page: 0, PageSize: 100 } },
      });

      if (error || !data) {
        throw new Error('Could not load reception desk workstations.');
      }

      return data.items ?? [];
    },
  });

  const locationDetailsQuery = useQuery({
    queryKey: [...workstationsQueryKey, 'locations', (workstationsQuery.data ?? []).map((item) => item.locationId).sort().join(',')],
    queryFn: async () => {
      const locationIds = Array.from(new Set((workstationsQuery.data ?? []).map((item) => item.locationId)));
      const locations = await Promise.all(
        locationIds.map(async (locationId) => {
          const { data, error } = await api.GET('/api/locations/locations/{id}', {
            params: { path: { id: locationId } },
          });

          if (error || !data) {
            return null;
          }

          return data;
        }),
      );

      return new Map(locations.filter((item): item is LocationResponse => item !== null).map((item) => [item.id, item]));
    },
    enabled: (workstationsQuery.data?.length ?? 0) > 0,
  });

  const createWorkstation = useMutation({
    mutationFn: async (request: CreateReceptionDeskWorkstationRequest) => {
      const { data, error } = await api.POST('/api/reception/workstations', { body: request });

      if (error || !data) {
        throw new Error('Could not create reception desk workstation.');
      }

      return data;
    },
    onSuccess: async (response) => {
      await queryClient.invalidateQueries({ queryKey: workstationsQueryKey });
      setLatestKey(response);
      closeForm();
      toast.success('Reception desk workstation created.');
    },
    onError: () => toast.error('Could not create reception desk workstation.'),
  });

  const updateWorkstation = useMutation({
    mutationFn: async ({ workstationId, request }: { workstationId: string; request: UpdateReceptionDeskWorkstationRequest }) => {
      const { data, error } = await api.PUT('/api/reception/workstations/{id}', {
        params: { path: { id: workstationId } },
        body: request,
      });

      if (error || !data) {
        throw new Error('Could not update reception desk workstation.');
      }

      return data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: workstationsQueryKey });
      closeForm();
      toast.success('Reception desk workstation updated.');
    },
    onError: () => toast.error('Could not update reception desk workstation.'),
  });

  const rotateKey = useMutation({
    mutationFn: async (workstationId: string) => {
      const { data, error } = await api.POST('/api/reception/workstations/{id}/rotate-key', {
        params: { path: { id: workstationId } },
      });

      if (error || !data) {
        throw new Error('Could not rotate reception desk workstation key.');
      }

      return data;
    },
    onSuccess: (response) => {
      setLatestKey(response);
      toast.success('Reception desk workstation key rotated.');
    },
    onError: () => toast.error('Could not rotate reception desk workstation key.'),
  });

  const deleteWorkstation = useMutation({
    mutationFn: async (workstation: ReceptionDeskWorkstation) => {
      const { error } = await api.DELETE('/api/reception/workstations/{id}', {
        params: { path: { id: workstation.id } },
      });

      if (error) {
        throw new Error('Could not disable reception desk workstation.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: workstationsQueryKey });
      toast.success('Reception desk workstation disabled.');
    },
    onError: () => toast.error('Could not disable reception desk workstation.'),
  });

  const workstations = workstationsQuery.data ?? [];
  const locationsById = locationDetailsQuery.data ?? new Map<string, LocationResponse>();
  const isBusy = createWorkstation.isPending || updateWorkstation.isPending || rotateKey.isPending || deleteWorkstation.isPending;

  function closeForm() {
    setIsFormOpen(false);
    setEditingWorkstationId(null);
    setForm(emptyFormValues);
  }

  function openCreateForm() {
    if (isFormOpen && !editingWorkstationId) {
      closeForm();
      return;
    }

    setEditingWorkstationId(null);
    setForm(emptyFormValues);
    setIsFormOpen(true);
  }

  function openEditForm(workstation: ReceptionDeskWorkstation) {
    setEditingWorkstationId(workstation.id);
    setForm({
      name: workstation.name,
      locationId: workstation.locationId,
      enabled: workstation.enabled,
    });
    setIsFormOpen(true);
  }

  function updateForm<TKey extends keyof FormValues>(key: TKey, value: FormValues[TKey]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!form.locationId) {
      toast.error('Select a location first.');
      return;
    }

    if (editingWorkstationId) {
      updateWorkstation.mutate({
        workstationId: editingWorkstationId,
        request: {
          name: form.name,
          locationId: form.locationId,
          enabled: form.enabled,
        },
      });

      return;
    }

    createWorkstation.mutate({
      name: form.name,
      locationId: form.locationId,
    });
  }

  function confirmDelete(workstation: ReceptionDeskWorkstation) {
    if (window.confirm(`Disable reception desk workstation "${workstation.name}"?`)) {
      deleteWorkstation.mutate(workstation);
    }
  }

  return (
    <section className="rounded-structural border border-border bg-content">
      <div className="border-b border-border p-4 sm:p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h2 className="text-[20px] font-semibold tracking-tight">Reception Desk Workstations</h2>
            <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Manage staffed reception desk workstations, location bindings, and device credentials.</p>
          </div>
          <Button type="button" className="w-full sm:w-fit" onClick={openCreateForm}>
            <Plus className="size-4" aria-hidden="true" />
            {isFormOpen && !editingWorkstationId ? 'Close form' : 'Add workstation'}
          </Button>
        </div>
      </div>

      <div className="grid gap-6 p-4 sm:p-6">
        {latestKey ? <ReceptionDeskWorkstationKeyPanel response={latestKey} onClose={() => setLatestKey(null)} /> : null}

        {isFormOpen ? (
          <form className="grid gap-5 rounded-structural border border-border p-4" onSubmit={handleSubmit}>
            <div className="flex items-start justify-between gap-3">
              <div>
                <h3 className="text-[16px] font-semibold tracking-tight">{editingWorkstationId ? 'Edit workstation' : 'Create workstation'}</h3>
                <p className="mt-1 text-[14px] text-muted-foreground">Bind each workstation to a location and control whether it can be used.</p>
              </div>
              <Button type="button" variant="ghost" size="sm" onClick={closeForm}>
                <X className="size-4" aria-hidden="true" />
                Close
              </Button>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium">
                Name
                <Input value={form.name} onChange={(event) => updateForm('name', event.target.value)} placeholder="Main reception desk" required />
              </label>

              <div className="grid gap-3 rounded-structural border border-border p-4">
                {editingWorkstationId ? <CheckboxField label="Enabled" checked={form.enabled} onChange={(checked) => updateForm('enabled', checked)} /> : <p className="text-[14px] text-muted-foreground">New workstations start enabled.</p>}
              </div>
            </div>

            <div className="grid gap-2 text-[14px] font-medium">
              <span>Location</span>
              <LocationSelector value={form.locationId} onChange={(value) => updateForm('locationId', value)} level="Room" disabled={isBusy} />
            </div>

            <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <Button type="button" variant="outline" onClick={closeForm}>Cancel</Button>
              <Button type="submit" disabled={isBusy}>{editingWorkstationId ? 'Save workstation' : 'Create workstation'}</Button>
            </div>
          </form>
        ) : null}

        {!workstationsQuery.isLoading && !workstationsQuery.isError && workstations.length === 0 ? (
          <Empty>
            <EmptyHeader>
              <EmptyTitle>No reception desk workstations yet</EmptyTitle>
              <EmptyDescription>Create a workstation before provisioning a staffed reception desk.</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <div className="grid gap-3">
            <div className="grid gap-3 lg:hidden">
              {workstationsQuery.isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading reception desk workstations...</p> : null}
              {workstationsQuery.isError ? <p className="rounded-structural border border-border p-4 text-[14px] text-error">Could not load reception desk workstations.</p> : null}
              {workstations.map((workstation) => (
                <article key={workstation.id} className="rounded-structural border border-border p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h3 className="text-[15px] font-semibold text-foreground">{workstation.name}</h3>
                      <p className="mt-1 text-[13px] text-muted-foreground">{getLocationLabel(locationsById.get(workstation.locationId))}</p>
                    </div>
                    <EnabledBadge enabled={workstation.enabled} />
                  </div>
                  <div className="mt-4 flex flex-wrap gap-2">
                    <Button type="button" variant="outline" size="sm" onClick={() => openEditForm(workstation)}><Pencil className="size-4" aria-hidden="true" />Edit</Button>
                    <Button type="button" variant="outline" size="sm" onClick={() => rotateKey.mutate(workstation.id)} disabled={isBusy}><RotateCcw className="size-4" aria-hidden="true" />Rotate key</Button>
                    <Button type="button" variant="destructive" size="sm" onClick={() => confirmDelete(workstation)} disabled={isBusy}><Trash2 className="size-4" aria-hidden="true" />Disable</Button>
                  </div>
                </article>
              ))}
            </div>

            <div className="hidden rounded-structural border border-border lg:block">
              <table className="w-full border-collapse text-left text-[14px]">
                <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-semibold">Name</th>
                    <th className="px-4 py-3 font-semibold">Location</th>
                    <th className="px-4 py-3 font-semibold">State</th>
                    <th className="px-4 py-3 text-right font-semibold">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {workstationsQuery.isLoading ? <LoadingRow colSpan={4} label="Loading reception desk workstations..." /> : null}
                  {workstationsQuery.isError ? <ErrorRow colSpan={4} label="Could not load reception desk workstations." /> : null}
                  {workstations.map((workstation) => (
                    <tr key={workstation.id}>
                      <td className="px-4 py-4 font-medium text-foreground">{workstation.name}</td>
                      <td className="px-4 py-4 text-muted-foreground">{getLocationLabel(locationsById.get(workstation.locationId))}</td>
                      <td className="px-4 py-4"><EnabledBadge enabled={workstation.enabled} /></td>
                      <td className="px-4 py-4">
                        <div className="flex justify-end gap-2">
                          <Button type="button" variant="outline" size="sm" onClick={() => openEditForm(workstation)}><Pencil className="size-4" aria-hidden="true" />Edit</Button>
                          <Button type="button" variant="outline" size="sm" onClick={() => rotateKey.mutate(workstation.id)} disabled={isBusy}><RotateCcw className="size-4" aria-hidden="true" />Rotate key</Button>
                          <Button type="button" variant="destructive" size="sm" onClick={() => confirmDelete(workstation)} disabled={isBusy}><Trash2 className="size-4" aria-hidden="true" />Disable</Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

function ReceptionDeskWorkstationKeyPanel({ response, onClose }: { readonly response: ReceptionDeskWorkstationKeyResponse; readonly onClose: () => void }) {
  async function copyKey() {
    await navigator.clipboard.writeText(response.apiKey);
    toast.success('Reception desk workstation key copied.');
  }

  return (
    <div className="rounded-structural border border-success bg-success-background p-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <div className="flex items-center gap-2 text-[14px] font-semibold text-foreground">
            <KeyRound className="size-4" aria-hidden="true" />
            New key for {response.workstation.name}
          </div>
          <p className="mt-2 text-[14px] text-muted-foreground">Copy this key now. It is only shown once and should be entered on the workstation device.</p>
          <code className="mt-3 block overflow-x-auto rounded-interactive border border-border bg-content px-3 py-2 text-[13px] text-foreground">{response.apiKey}</code>
        </div>
        <div className="flex gap-2">
          <Button type="button" variant="outline" onClick={copyKey}><Copy className="size-4" aria-hidden="true" />Copy</Button>
          <Button type="button" variant="ghost" onClick={onClose}>Dismiss</Button>
        </div>
      </div>
    </div>
  );
}

function CheckboxField({ label, checked, onChange }: { readonly label: string; readonly checked: boolean; readonly onChange: (checked: boolean) => void }) {
  return <label className="flex items-center gap-3 text-[14px] font-medium"><input type="checkbox" className="size-4 rounded border border-border" checked={checked} onChange={(event) => onChange(event.target.checked)} />{label}</label>;
}

function EnabledBadge({ enabled }: { readonly enabled: boolean }) {
  return <Badge variant={enabled ? 'success' : 'secondary'}>{enabled ? 'Enabled' : 'Disabled'}</Badge>;
}

function LoadingRow({ colSpan, label }: { readonly colSpan: number; readonly label: string }) {
  return <tr><td className="px-4 py-5 text-muted-foreground" colSpan={colSpan}>{label}</td></tr>;
}

function ErrorRow({ colSpan, label }: { readonly colSpan: number; readonly label: string }) {
  return <tr><td className="px-4 py-5 text-error" colSpan={colSpan}>{label}</td></tr>;
}
