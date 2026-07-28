import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Copy, KeyRound, Pencil, Plus, RotateCcw, Trash2, X } from 'lucide-react';
import { useEffect, useState, type FormEvent } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getLocationLabel, LocationSelector, type LocationResponse } from '@/shared/components/location-selector';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Input } from '@/shared/components/ui/input';

type IdentityVerificationMethod = Exclude<components['schemas']['IdentityVerificationMethod'], null>;
type ReceptionKiosk = components['schemas']['ReceptionKioskResponse'];
type ReceptionKioskKeyResponse = components['schemas']['ReceptionKioskKeyResponse'];
type CreateReceptionKioskRequest = components['schemas']['CreateReceptionKioskRequest'];
type UpdateReceptionKioskRequest = components['schemas']['UpdateReceptionKioskRequest'];

type FormValues = {
  readonly name: string;
  readonly locationId: string | null;
  readonly enabled: boolean;
  readonly requireFacePicture: boolean;
  readonly identityVerificationMethod: '' | IdentityVerificationMethod;
  readonly onboardingGracePeriodMinutes: string;
};

const kiosksQueryKey = ['administration', 'clients', 'reception-kiosks'] as const;
const emptyFormValues: FormValues = {
  name: '',
  locationId: null,
  enabled: true,
  requireFacePicture: false,
  identityVerificationMethod: '',
  onboardingGracePeriodMinutes: '15',
};
const identityVerificationOptions: readonly { readonly value: '' | IdentityVerificationMethod; readonly label: string }[] = [
  { value: '', label: 'None' },
  { value: 'Picture', label: 'Picture' },
  { value: 'PassportScanner', label: 'Passport Scanner' },
  { value: 'EidReader', label: 'eID Reader' },
  { value: 'Itsme', label: 'itsme' },
];

export function ReceptionKioskAdminPanel() {
  const queryClient = useQueryClient();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingKioskId, setEditingKioskId] = useState<string | null>(null);
  const [form, setForm] = useState<FormValues>(emptyFormValues);
  const [latestKey, setLatestKey] = useState<ReceptionKioskKeyResponse | null>(null);

  const kiosksQuery = useQuery({
    queryKey: kiosksQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/kiosks', {
        params: { query: { Page: 0, PageSize: 100 } },
      });

      if (error || !data) {
        throw new Error('Could not load reception kiosks.');
      }

      return data.items ?? [];
    },
  });

  const locationDetailsQuery = useQuery({
    queryKey: [...kiosksQueryKey, 'locations', (kiosksQuery.data ?? []).map((item) => item.locationId).sort().join(',')],
    queryFn: async () => {
      const locationIds = Array.from(new Set((kiosksQuery.data ?? []).map((item) => item.locationId)));
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
    enabled: (kiosksQuery.data?.length ?? 0) > 0,
  });

  const createKiosk = useMutation({
    mutationFn: async (request: CreateReceptionKioskRequest) => {
      const { data, error } = await api.POST('/api/reception/kiosks', { body: request });

      if (error || !data) {
        throw new Error('Could not create reception kiosk.');
      }

      return data;
    },
    onSuccess: async (response) => {
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      setLatestKey(response);
      closeForm();
      toast.success('Reception kiosk created.');
    },
    onError: () => toast.error('Could not create reception kiosk.'),
  });

  const updateKiosk = useMutation({
    mutationFn: async ({ kioskId, request }: { kioskId: string; request: UpdateReceptionKioskRequest }) => {
      const { data, error } = await api.PUT('/api/reception/kiosks/{id}', {
        params: { path: { id: kioskId } },
        body: request,
      });

      if (error || !data) {
        throw new Error('Could not update reception kiosk.');
      }

      return data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      closeForm();
      toast.success('Reception kiosk updated.');
    },
    onError: () => toast.error('Could not update reception kiosk.'),
  });

  const rotateKey = useMutation({
    mutationFn: async (kioskId: string) => {
      const { data, error } = await api.POST('/api/reception/kiosks/{id}/rotate-key', {
        params: { path: { id: kioskId } },
      });

      if (error || !data) {
        throw new Error('Could not rotate reception kiosk key.');
      }

      return data;
    },
    onSuccess: (response) => {
      setLatestKey(response);
      toast.success('Reception kiosk key rotated.');
    },
    onError: () => toast.error('Could not rotate reception kiosk key.'),
  });

  const deleteKiosk = useMutation({
    mutationFn: async (kiosk: ReceptionKiosk) => {
      const { error } = await api.DELETE('/api/reception/kiosks/{id}', {
        params: { path: { id: kiosk.id } },
      });

      if (error) {
        throw new Error('Could not disable reception kiosk.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      toast.success('Reception kiosk disabled.');
    },
    onError: () => toast.error('Could not disable reception kiosk.'),
  });

  const kiosks = kiosksQuery.data ?? [];
  const locationsById = locationDetailsQuery.data ?? new Map<string, LocationResponse>();
  const isBusy = createKiosk.isPending || updateKiosk.isPending || rotateKey.isPending || deleteKiosk.isPending;

  useEffect(() => {
    if (form.identityVerificationMethod === 'Picture' && !form.requireFacePicture) {
      setForm((current) => ({ ...current, requireFacePicture: true }));
    }
  }, [form.identityVerificationMethod, form.requireFacePicture]);

  function closeForm() {
    setIsFormOpen(false);
    setEditingKioskId(null);
    setForm(emptyFormValues);
  }

  function openCreateForm() {
    if (isFormOpen && !editingKioskId) {
      closeForm();
      return;
    }

    setEditingKioskId(null);
    setForm(emptyFormValues);
    setIsFormOpen(true);
  }

  function openEditForm(kiosk: ReceptionKiosk) {
    setEditingKioskId(kiosk.id);
    setForm({
      name: kiosk.name,
      locationId: kiosk.locationId,
      enabled: kiosk.enabled,
      requireFacePicture: kiosk.requireFacePicture,
      identityVerificationMethod: kiosk.identityVerificationMethod ?? '',
      onboardingGracePeriodMinutes: String(kiosk.onboardingGracePeriodMinutes),
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

    const onboardingGracePeriodMinutes = Number(form.onboardingGracePeriodMinutes);
    if (!Number.isFinite(onboardingGracePeriodMinutes) || onboardingGracePeriodMinutes < 0) {
      toast.error('Grace period must be a non-negative number.');
      return;
    }

    if (editingKioskId) {
      updateKiosk.mutate({
        kioskId: editingKioskId,
        request: {
          name: form.name,
          locationId: form.locationId,
          enabled: form.enabled,
          requireFacePicture: form.requireFacePicture,
          identityVerificationMethod: form.identityVerificationMethod || null,
          onboardingGracePeriodMinutes,
        },
      });

      return;
    }

    createKiosk.mutate({
      name: form.name,
      locationId: form.locationId,
      requireFacePicture: form.requireFacePicture,
      identityVerificationMethod: form.identityVerificationMethod || null,
      onboardingGracePeriodMinutes,
    });
  }

  function confirmDelete(kiosk: ReceptionKiosk) {
    if (window.confirm(`Disable reception kiosk "${kiosk.name}"?`)) {
      deleteKiosk.mutate(kiosk);
    }
  }

  return (
    <section className="rounded-structural border border-border bg-content">
      <div className="border-b border-border p-4 sm:p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h2 className="text-[20px] font-semibold tracking-tight">Reception Desk Kiosk</h2>
            <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Manage self-service reception kiosks, onboarding requirements, and device credentials.</p>
          </div>
          <Button type="button" className="w-full sm:w-fit" onClick={openCreateForm}>
            <Plus className="size-4" aria-hidden="true" />
            {isFormOpen && !editingKioskId ? 'Close form' : 'Add kiosk'}
          </Button>
        </div>
      </div>

      <div className="grid gap-6 p-4 sm:p-6">
        {latestKey ? <ReceptionKioskKeyPanel response={latestKey} onClose={() => setLatestKey(null)} /> : null}

        {isFormOpen ? (
          <form className="grid gap-5 rounded-structural border border-border p-4" onSubmit={handleSubmit}>
            <div className="flex items-start justify-between gap-3">
              <div>
                <h3 className="text-[16px] font-semibold tracking-tight">{editingKioskId ? 'Edit kiosk' : 'Create kiosk'}</h3>
                <p className="mt-1 text-[14px] text-muted-foreground">Configure location, onboarding checks, and whether the kiosk is enabled.</p>
              </div>
              <Button type="button" variant="ghost" size="sm" onClick={closeForm}>
                <X className="size-4" aria-hidden="true" />
                Close
              </Button>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium">
                Name
                <Input value={form.name} onChange={(event) => updateForm('name', event.target.value)} placeholder="Main reception kiosk" required />
              </label>

              <label className="grid gap-2 text-[14px] font-medium">
                Grace period (minutes)
                <Input type="number" min="0" value={form.onboardingGracePeriodMinutes} onChange={(event) => updateForm('onboardingGracePeriodMinutes', event.target.value)} required />
              </label>
            </div>

            <div className="grid gap-2 text-[14px] font-medium">
              <span>Location</span>
              <LocationSelector value={form.locationId} onChange={(value) => updateForm('locationId', value)} level="Room" disabled={isBusy} />
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium">
                Identity verification
                <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={form.identityVerificationMethod} onChange={(event) => updateForm('identityVerificationMethod', event.target.value as FormValues['identityVerificationMethod'])}>
                  {identityVerificationOptions.map((option) => (
                    <option key={option.value || 'none'} value={option.value}>{option.label}</option>
                  ))}
                </select>
              </label>

              <div className="grid gap-3 rounded-structural border border-border p-4">
                {editingKioskId ? <CheckboxField label="Enabled" checked={form.enabled} onChange={(checked) => updateForm('enabled', checked)} /> : null}
                <CheckboxField label="Require face picture" checked={form.requireFacePicture} onChange={(checked) => updateForm('requireFacePicture', checked)} />
              </div>
            </div>

            <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <Button type="button" variant="outline" onClick={closeForm}>Cancel</Button>
              <Button type="submit" disabled={isBusy}>{editingKioskId ? 'Save kiosk' : 'Create kiosk'}</Button>
            </div>
          </form>
        ) : null}

        {!kiosksQuery.isLoading && !kiosksQuery.isError && kiosks.length === 0 ? (
          <Empty>
            <EmptyHeader>
              <EmptyTitle>No reception kiosks yet</EmptyTitle>
              <EmptyDescription>Create a kiosk before provisioning a device in reception.</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <div className="grid gap-3">
            <div className="grid gap-3 lg:hidden">
              {kiosksQuery.isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading reception kiosks...</p> : null}
              {kiosksQuery.isError ? <p className="rounded-structural border border-border p-4 text-[14px] text-error">Could not load reception kiosks.</p> : null}
              {kiosks.map((kiosk) => (
                <article key={kiosk.id} className="rounded-structural border border-border p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h3 className="text-[15px] font-semibold text-foreground">{kiosk.name}</h3>
                      <p className="mt-1 text-[13px] text-muted-foreground">{getLocationLabel(locationsById.get(kiosk.locationId))}</p>
                    </div>
                    <EnabledBadge enabled={kiosk.enabled} />
                  </div>
                  <dl className="mt-4 grid gap-2 text-[13px]">
                    <InfoRow label="Identity" value={formatIdentityVerificationMethod(kiosk.identityVerificationMethod)} />
                    <InfoRow label="Face picture" value={kiosk.requireFacePicture ? 'Required' : 'Optional'} />
                    <InfoRow label="Grace period" value={`${kiosk.onboardingGracePeriodMinutes} min`} />
                  </dl>
                  <div className="mt-4 flex flex-wrap gap-2">
                    <Button type="button" variant="outline" size="sm" onClick={() => openEditForm(kiosk)}><Pencil className="size-4" aria-hidden="true" />Edit</Button>
                    <Button type="button" variant="outline" size="sm" onClick={() => rotateKey.mutate(kiosk.id)} disabled={isBusy}><RotateCcw className="size-4" aria-hidden="true" />Rotate key</Button>
                    <Button type="button" variant="destructive" size="sm" onClick={() => confirmDelete(kiosk)} disabled={isBusy}><Trash2 className="size-4" aria-hidden="true" />Disable</Button>
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
                    <th className="px-4 py-3 font-semibold">Identity</th>
                    <th className="px-4 py-3 font-semibold">Face picture</th>
                    <th className="px-4 py-3 font-semibold">Grace period</th>
                    <th className="px-4 py-3 font-semibold">State</th>
                    <th className="px-4 py-3 text-right font-semibold">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {kiosksQuery.isLoading ? <LoadingRow colSpan={7} label="Loading reception kiosks..." /> : null}
                  {kiosksQuery.isError ? <ErrorRow colSpan={7} label="Could not load reception kiosks." /> : null}
                  {kiosks.map((kiosk) => (
                    <tr key={kiosk.id}>
                      <td className="px-4 py-4 font-medium text-foreground">{kiosk.name}</td>
                      <td className="px-4 py-4 text-muted-foreground">{getLocationLabel(locationsById.get(kiosk.locationId))}</td>
                      <td className="px-4 py-4 text-muted-foreground">{formatIdentityVerificationMethod(kiosk.identityVerificationMethod)}</td>
                      <td className="px-4 py-4 text-muted-foreground">{kiosk.requireFacePicture ? 'Required' : 'Optional'}</td>
                      <td className="px-4 py-4 text-muted-foreground">{kiosk.onboardingGracePeriodMinutes} min</td>
                      <td className="px-4 py-4"><EnabledBadge enabled={kiosk.enabled} /></td>
                      <td className="px-4 py-4">
                        <div className="flex justify-end gap-2">
                          <Button type="button" variant="outline" size="sm" onClick={() => openEditForm(kiosk)}><Pencil className="size-4" aria-hidden="true" />Edit</Button>
                          <Button type="button" variant="outline" size="sm" onClick={() => rotateKey.mutate(kiosk.id)} disabled={isBusy}><RotateCcw className="size-4" aria-hidden="true" />Rotate key</Button>
                          <Button type="button" variant="destructive" size="sm" onClick={() => confirmDelete(kiosk)} disabled={isBusy}><Trash2 className="size-4" aria-hidden="true" />Disable</Button>
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

function ReceptionKioskKeyPanel({ response, onClose }: { readonly response: ReceptionKioskKeyResponse; readonly onClose: () => void }) {
  async function copyKey() {
    await navigator.clipboard.writeText(response.apiKey);
    toast.success('Reception kiosk key copied.');
  }

  return (
    <div className="rounded-structural border border-success bg-success-background p-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <div className="flex items-center gap-2 text-[14px] font-semibold text-foreground">
            <KeyRound className="size-4" aria-hidden="true" />
            New key for {response.kiosk.name}
          </div>
          <p className="mt-2 text-[14px] text-muted-foreground">Copy this key now. It is only shown once and should be entered on the kiosk device.</p>
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

function InfoRow({ label, value }: { readonly label: string; readonly value: string }) {
  return <div className="flex justify-between gap-4"><dt className="text-muted-foreground">{label}</dt><dd className="text-right text-foreground">{value}</dd></div>;
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

function formatIdentityVerificationMethod(value: components['schemas']['IdentityVerificationMethod']) {
  if (!value) {
    return 'None';
  }

  return value === 'PassportScanner' ? 'Passport Scanner' : value === 'EidReader' ? 'eID Reader' : value;
}
