import { useEffect, useId, useState, type FormEvent, type ReactNode } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Copy, KeyRound, Pencil, Plus, RotateCw, ShieldCheck, Trash2 } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { LocationSelector, getLocationLabel } from '@/shared/components/location-selector';

type AccessRuleAssignment = components['schemas']['AccessRuleAssignmentResponse'];
type AccessRuleAssignmentRequest = components['schemas']['CreateAccessRuleAssignmentRequest'];
type CreateReceptionKioskRequest = components['schemas']['CreateReceptionKioskRequest'];
type IdentityVerificationMethod = Exclude<components['schemas']['IdentityVerificationMethod'], null>;
type ReceptionKiosk = components['schemas']['ReceptionKioskResponse'];
type ReceptionKioskKeyResponse = components['schemas']['ReceptionKioskKeyResponse'];
type ReceptionAccessPolicyTrigger = components['schemas']['ReceptionAccessPolicyTrigger'];
type UpdateReceptionKioskRequest = components['schemas']['UpdateReceptionKioskRequest'];

type FormValues = {
  readonly locationId: string | null;
  readonly systemId: string;
  readonly accessLevelTypeId: string;
  readonly trigger: ReceptionAccessPolicyTrigger;
  readonly gracePeriodMinutes: string;
};

type KioskFormValues = {
  readonly name: string;
  readonly locationId: string | null;
  readonly enabled: boolean;
  readonly requireFacePicture: boolean;
  readonly identityVerificationMethod: IdentityVerificationMethod | '';
  readonly onboardingGracePeriodMinutes: string;
};

const identityVerificationOptions: { readonly label: string; readonly value: IdentityVerificationMethod }[] = [
  { label: 'Picture', value: 'Picture' },
];

const assignmentsQueryKey = ['settings', 'reception-desk', 'access-rule-assignments'] as const;
const kiosksQueryKey = ['settings', 'reception-desk', 'kiosks'] as const;
const pageSize = 100;

const triggerOptions: { readonly label: string; readonly value: ReceptionAccessPolicyTrigger }[] = [
  { label: 'Expected visitor added', value: 'ExpectedVisitorAdded' },
  { label: 'Visitor confirmed', value: 'VisitorConfirmed' },
  { label: 'Visitor onboarded', value: 'VisitorOnboarded' },
  { label: 'Contractor expected added', value: 'ContractorExpectedAdded' },
  { label: 'Contractor onboarded', value: 'ContractorOnboarded' },
];

export default function ReceptionDeskSettingsPage() {
  const queryClient = useQueryClient();
  const [editingAssignmentId, setEditingAssignmentId] = useState<string | null>(null);
  const [editingKioskId, setEditingKioskId] = useState<string | null>(null);
  const [isKioskFormOpen, setIsKioskFormOpen] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [kioskValues, setKioskValues] = useState<KioskFormValues>(getDefaultKioskFormValues);
  const [lastKioskKey, setLastKioskKey] = useState<ReceptionKioskKeyResponse | null>(null);
  const [values, setValues] = useState<FormValues>(getDefaultFormValues);

  const kiosksQuery = useQuery({
    queryKey: kiosksQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/kiosks', {
        params: { query: { Page: 0, PageSize: pageSize } },
      });

      if (error) {
        throw new Error('Could not load reception kiosks.');
      }

      return data;
    },
  });

  const assignmentsQuery = useQuery({
    queryKey: assignmentsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/access-rule-assignments', {
        params: { query: { Page: 0, PageSize: pageSize } },
      });

      if (error) {
        throw new Error('Could not load access level assignments.');
      }

      return data;
    },
  });

  const assignments = assignmentsQuery.data?.items ?? [];
  const kiosks = kiosksQuery.data?.items ?? [];
  const isLoading = assignmentsQuery.isLoading;
  const isError = assignmentsQuery.isError;
  const areKiosksLoading = kiosksQuery.isLoading;
  const areKiosksError = kiosksQuery.isError;

  const createAssignment = useMutation({
    mutationFn: async (request: AccessRuleAssignmentRequest) => {
      const { error } = await api.POST('/api/reception/access-rule-assignments', { body: request });

      if (error) {
        throw new Error('Could not create access level assignment.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: assignmentsQueryKey });
      resetForm();
      toast.success('Access level assignment created.');
    },
    onError: () => {
      toast.error('Could not create access level assignment.');
    },
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
      resetKioskForm();
      setLastKioskKey(response);
      toast.success('Reception kiosk created. Copy the API key now.');
    },
    onError: () => {
      toast.error('Could not create reception kiosk.');
    },
  });

  const updateKiosk = useMutation({
    mutationFn: async ({ id, request }: { readonly id: string; readonly request: UpdateReceptionKioskRequest }) => {
      const { error } = await api.PUT('/api/reception/kiosks/{id}', {
        params: { path: { id } },
        body: request,
      });

      if (error) {
        throw new Error('Could not update reception kiosk.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      resetKioskForm();
      toast.success('Reception kiosk saved.');
    },
    onError: () => {
      toast.error('Could not save reception kiosk.');
    },
  });

  const rotateKioskKey = useMutation({
    mutationFn: async (kiosk: ReceptionKiosk) => {
      const { data, error } = await api.POST('/api/reception/kiosks/{id}/rotate-key', {
        params: { path: { id: kiosk.id } },
      });

      if (error || !data) {
        throw new Error('Could not rotate reception kiosk key.');
      }

      return data;
    },
    onSuccess: async (response) => {
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      setLastKioskKey(response);
      toast.success('Reception kiosk key rotated. Copy the API key now.');
    },
    onError: () => {
      toast.error('Could not rotate reception kiosk key.');
    },
  });

  const disableKiosk = useMutation({
    mutationFn: async (kiosk: ReceptionKiosk) => {
      const { error } = await api.DELETE('/api/reception/kiosks/{id}', {
        params: { path: { id: kiosk.id } },
      });

      if (error) {
        throw new Error('Could not disable reception kiosk.');
      }
    },
    onSuccess: async (_data, kiosk) => {
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      if (editingKioskId === kiosk.id) {
        resetKioskForm();
      }
      toast.success('Reception kiosk disabled.');
    },
    onError: () => {
      toast.error('Could not disable reception kiosk.');
    },
  });

  const updateAssignment = useMutation({
    mutationFn: async ({ id, request }: { readonly id: string; readonly request: AccessRuleAssignmentRequest }) => {
      const { error } = await api.PUT('/api/reception/access-rule-assignments/{id}', {
        params: { path: { id } },
        body: request,
      });

      if (error) {
        throw new Error('Could not update access level assignment.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: assignmentsQueryKey });
      resetForm();
      toast.success('Access level assignment saved.');
    },
    onError: () => {
      toast.error('Could not save access level assignment.');
    },
  });

  const deleteAssignment = useMutation({
    mutationFn: async (assignment: AccessRuleAssignment) => {
      const { error } = await api.DELETE('/api/reception/access-rule-assignments/{id}', {
        params: { path: { id: assignment.id } },
      });

      if (error) {
        throw new Error('Could not delete access level assignment.');
      }
    },
    onSuccess: async (_data, assignment) => {
      await queryClient.invalidateQueries({ queryKey: assignmentsQueryKey });
      if (editingAssignmentId === assignment.id) {
        resetForm();
      }
      toast.success('Access level assignment deleted.');
    },
    onError: () => {
      toast.error('Could not delete access level assignment.');
    },
  });

  function resetForm() {
    setEditingAssignmentId(null);
    setIsFormOpen(false);
    setValues({
      locationId: null,
      systemId: '',
      accessLevelTypeId: '',
      trigger: 'ExpectedVisitorAdded',
      gracePeriodMinutes: '0',
    });
  }

  function resetKioskForm() {
    setEditingKioskId(null);
    setIsKioskFormOpen(false);
    setKioskValues(getDefaultKioskFormValues());
  }

  function openCreateForm() {
    setEditingAssignmentId(null);
    setIsFormOpen(true);
    setValues({
      locationId: null,
      systemId: '',
      accessLevelTypeId: '',
      trigger: 'ExpectedVisitorAdded',
      gracePeriodMinutes: '0',
    });
  }

  function openCreateKioskForm() {
    setEditingKioskId(null);
    setIsKioskFormOpen(true);
    setKioskValues(getDefaultKioskFormValues());
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const request = toAssignmentRequest(values);

    if (!request) {
      toast.error('Complete access level assignment before saving.');
      return;
    }

    if (editingAssignmentId) {
      updateAssignment.mutate({ id: editingAssignmentId, request });
      return;
    }

    createAssignment.mutate(request);
  }

  function handleKioskSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (editingKioskId) {
      const request = toUpdateKioskRequest(kioskValues);

      if (!request) {
        toast.error('Complete reception kiosk before saving.');
        return;
      }

      updateKiosk.mutate({ id: editingKioskId, request });
      return;
    }

    const request = toCreateKioskRequest(kioskValues);

    if (!request) {
      toast.error('Complete reception kiosk before saving.');
      return;
    }

    createKiosk.mutate(request);
  }

  function editAssignment(assignment: AccessRuleAssignment) {
    setEditingAssignmentId(assignment.id);
    setIsFormOpen(true);
    setValues({
      locationId: assignment.locationId,
      systemId: assignment.systemId,
      accessLevelTypeId: assignment.accessLevelTypeId,
      trigger: assignment.trigger,
      gracePeriodMinutes: String(assignment.gracePeriodMinutes),
    });
  }

  function editKiosk(kiosk: ReceptionKiosk) {
    setEditingKioskId(kiosk.id);
    setIsKioskFormOpen(true);
    setKioskValues({
      name: kiosk.name,
      locationId: kiosk.locationId ?? null,
      enabled: kiosk.enabled,
      requireFacePicture: kiosk.requireFacePicture,
      identityVerificationMethod: kiosk.identityVerificationMethod ?? '',
      onboardingGracePeriodMinutes: String(kiosk.onboardingGracePeriodMinutes),
    });
  }

  function confirmDelete(assignment: AccessRuleAssignment) {
    const confirmed = window.confirm(`Delete assignment for ${assignment.locationId}?`);

    if (confirmed) {
      deleteAssignment.mutate(assignment);
    }
  }

  function confirmDisableKiosk(kiosk: ReceptionKiosk) {
    const confirmed = window.confirm(`Disable kiosk ${kiosk.name}?`);

    if (confirmed) {
      disableKiosk.mutate(kiosk);
    }
  }

  function confirmRotateKioskKey(kiosk: ReceptionKiosk) {
    const confirmed = window.confirm(`Rotate API key for ${kiosk.name}? Existing kiosk clients will stop authenticating until updated.`);

    if (confirmed) {
      rotateKioskKey.mutate(kiosk);
    }
  }

  async function copyKioskKey() {
    if (!lastKioskKey) {
      return;
    }

    await navigator.clipboard.writeText(lastKioskKey.apiKey);
    toast.success('API key copied.');
  }

  async function copyKioskId(kiosk: ReceptionKiosk) {
    await navigator.clipboard.writeText(kiosk.id);
    toast.success('Kiosk ID copied.');
  }

  async function copyKioskHeaders() {
    if (!lastKioskKey) {
      return;
    }

    await navigator.clipboard.writeText(`reception-kiosk-id: ${lastKioskKey.kiosk.id}\nreception-kiosk-key: ${lastKioskKey.apiKey}`);
    toast.success('Kiosk headers copied.');
  }

  return (
    <section className="grid gap-6">
      <div className="rounded-structural border border-border bg-content p-4 sm:p-6">
        <p className="text-[13px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Settings</p>
        <h1 className="mt-3 text-[32px] font-semibold tracking-tight">Reception Desk</h1>
        <p className="mt-3 max-w-2xl text-[14px] text-muted-foreground">Configure reception desk access automation and arrival policy defaults.</p>
      </div>

      <Card>
        <CardHeader>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <CardTitle className="flex items-center gap-2 text-[20px]">
                <span className="rounded-interactive bg-hover-blue p-2 text-primary">
                  <KeyRound className="size-5" aria-hidden="true" />
                </span>
                Reception Kiosks
              </CardTitle>
              <CardDescription className="mt-2 max-w-3xl">
                Manage self onboarding kiosks and their API keys. New and rotated keys are only shown once.
              </CardDescription>
            </div>
            <Button type="button" className="w-full sm:w-auto" disabled={areKiosksLoading || areKiosksError} onClick={openCreateKioskForm}>
              <Plus className="size-4" aria-hidden="true" />
              Create Kiosk
            </Button>
          </div>
        </CardHeader>
        <CardContent className="grid gap-6">
          {areKiosksLoading ? <p className="text-[14px] text-muted-foreground">Loading reception kiosks...</p> : null}
          {areKiosksError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load reception kiosks.</p> : null}

          {lastKioskKey ? (
            <div className="grid gap-3 rounded-structural border border-primary/30 bg-hover-blue p-4">
              <div>
                <h2 className="text-[16px] font-semibold">Kiosk credentials for {lastKioskKey.kiosk.name}</h2>
                <p className="mt-1 text-[13px] text-muted-foreground">Copy these headers now. The API key will not be shown again.</p>
              </div>
              <div className="grid gap-3">
                <CredentialLine label="reception-kiosk-id" value={lastKioskKey.kiosk.id} />
                <CredentialLine label="reception-kiosk-key" value={lastKioskKey.apiKey} />
              </div>
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                <Button type="button" variant="outline" onClick={() => copyKioskId(lastKioskKey.kiosk)}>
                  <Copy className="size-4" aria-hidden="true" />
                  Copy ID
                </Button>
                <Button type="button" variant="outline" onClick={copyKioskKey}>
                  <Copy className="size-4" aria-hidden="true" />
                  Copy key
                </Button>
                <Button type="button" variant="outline" onClick={copyKioskHeaders}>
                  <Copy className="size-4" aria-hidden="true" />
                  Copy headers
                </Button>
                <Button type="button" variant="outline" onClick={() => setLastKioskKey(null)}>Dismiss</Button>
              </div>
            </div>
          ) : null}

          {!areKiosksLoading && !areKiosksError ? (
            <>
              {isKioskFormOpen ? (
                <form className="grid gap-5 rounded-structural border border-border bg-background p-4" onSubmit={handleKioskSubmit}>
                  <div>
                    <h2 className="text-[16px] font-semibold">{editingKioskId ? 'Edit kiosk' : 'New kiosk'}</h2>
                    <p className="mt-1 text-[13px] text-muted-foreground">Set kiosk name and physical location.</p>
                  </div>

                  <div className="grid gap-4 lg:grid-cols-2">
                    <Field label="Kiosk name">
                      <Input value={kioskValues.name} onChange={(event) => setKioskValues((current) => ({ ...current, name: event.target.value }))} required />
                    </Field>
                    <div className="lg:col-span-2">
                      <LocationSelector
                        value={kioskValues.locationId}
                        onChange={(locationId) => setKioskValues((current) => ({ ...current, locationId }))}
                        level="Room"
                      />
                    </div>
                    <label className="inline-flex items-center gap-2 text-[14px] font-medium">
                      <input
                        type="checkbox"
                        checked={kioskValues.requireFacePicture}
                        onChange={(event) => setKioskValues((current) => ({ ...current, requireFacePicture: event.target.checked }))}
                      />
                      Require face picture
                    </label>
                    {editingKioskId ? (
                      <label className="inline-flex items-center gap-2 text-[14px] font-medium">
                        <input
                          type="checkbox"
                          checked={kioskValues.enabled}
                          onChange={(event) => setKioskValues((current) => ({ ...current, enabled: event.target.checked }))}
                        />
                        Enabled
                      </label>
                    ) : null}
                    <div className="lg:col-span-2 grid gap-3 rounded-interactive border border-border bg-content px-4 py-3">
                      <label className="inline-flex items-center gap-2 text-[14px] font-medium">
                        <input
                          type="checkbox"
                          checked={kioskValues.identityVerificationMethod !== ''}
                          onChange={(event) => setKioskValues((current) => ({
                            ...current,
                            identityVerificationMethod: event.target.checked ? 'Picture' : '',
                          }))}
                        />
                        Require identity verification
                      </label>

                      {kioskValues.identityVerificationMethod !== '' ? (
                        <SelectField
                          label="Identity verification method"
                          value={kioskValues.identityVerificationMethod}
                          onChange={(value) => setKioskValues((current) => ({ ...current, identityVerificationMethod: value as IdentityVerificationMethod }))}
                        >
                          {identityVerificationOptions.map((option) => (
                            <option key={option.value} value={option.value}>{option.label}</option>
                          ))}
                        </SelectField>
                      ) : null}

                      <Field label="Onboarding grace period minutes">
                        <Input
                          type="number"
                          min="0"
                          step="1"
                          value={kioskValues.onboardingGracePeriodMinutes}
                          onChange={(event) => setKioskValues((current) => ({ ...current, onboardingGracePeriodMinutes: event.target.value }))}
                          required
                        />
                      </Field>
                    </div>
                  </div>

                  <div className="flex flex-col gap-2 border-t border-border pt-5 sm:flex-row sm:justify-end">
                    {editingKioskId ? <Button type="button" variant="outline" onClick={resetKioskForm}>Cancel edit</Button> : null}
                    <Button type="submit" disabled={createKiosk.isPending || updateKiosk.isPending || !kioskValues.name.trim() || !kioskValues.locationId}>
                      {createKiosk.isPending || updateKiosk.isPending ? 'Saving...' : 'Save kiosk'}
                    </Button>
                  </div>
                </form>
              ) : null}

              <ReceptionKiosksTable
                kiosks={kiosks}
                disableKioskId={disableKiosk.isPending ? disableKiosk.variables?.id : null}
                editingKioskId={editingKioskId}
                rotateKioskId={rotateKioskKey.isPending ? rotateKioskKey.variables?.id : null}
                onDisable={confirmDisableKiosk}
                onEdit={editKiosk}
                onRotateKey={confirmRotateKioskKey}
              />
            </>
          ) : null}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <CardTitle className="flex items-center gap-2 text-[20px]">
                <span className="rounded-interactive bg-hover-blue p-2 text-primary">
                  <ShieldCheck className="size-5" aria-hidden="true" />
                </span>
                Access Level Assignments
              </CardTitle>
              <CardDescription className="mt-2 max-w-3xl">
                Access level assignment setup is temporarily unavailable while legacy AccessPolicies flows are removed.
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="grid gap-6">
          {isLoading ? <p className="text-[14px] text-muted-foreground">Loading reception desk settings...</p> : null}
          {isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error">Could not load reception desk settings.</p> : null}

          {!isLoading && !isError ? (
            <>
              <p className="rounded-interactive border border-border bg-background px-4 py-3 text-[14px] text-muted-foreground">
                Existing assignments remain visible below. Creating or editing PACS-triggered assignments is disabled until reception assignment flows are migrated to `AccessControl`.
              </p>

              <AccessAssignmentsTable
                assignments={assignments}
                deleteAssignmentId={deleteAssignment.isPending ? deleteAssignment.variables?.id : null}
                editingAssignmentId={editingAssignmentId}
                onDelete={confirmDelete}
              />
            </>
          ) : null}
        </CardContent>
      </Card>
    </section>
  );
}

function ReceptionKiosksTable({
  kiosks,
  disableKioskId,
  editingKioskId,
  rotateKioskId,
  onDisable,
  onEdit,
  onRotateKey,
}: {
  readonly kiosks: readonly ReceptionKiosk[];
  readonly disableKioskId: string | null;
  readonly editingKioskId: string | null;
  readonly rotateKioskId: string | null;
  readonly onDisable: (kiosk: ReceptionKiosk) => void;
  readonly onEdit: (kiosk: ReceptionKiosk) => void;
  readonly onRotateKey: (kiosk: ReceptionKiosk) => void;
}) {
  if (kiosks.length === 0) {
    return <p className="rounded-structural border border-border bg-background p-4 text-[14px] text-muted-foreground">No reception kiosks configured.</p>;
  }

  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[48rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr>
            <th className="px-4 py-3 font-semibold">Name</th>
            <th className="px-4 py-3 font-semibold">Location</th>
            <th className="px-4 py-3 font-semibold">Status</th>
            <th className="px-4 py-3 font-semibold">Onboarding</th>
            <th className="px-4 py-3 text-right font-semibold">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {kiosks.map((kiosk) => (
            <tr key={kiosk.id} className={editingKioskId === kiosk.id ? 'bg-hover-blue/60' : undefined}>
              <td className="px-4 py-4 font-medium text-foreground">{kiosk.name}</td>
              <td className="px-4 py-4 text-muted-foreground"><AssignmentLocationLabel locationId={kiosk.locationId} /></td>
              <td className="px-4 py-4 text-muted-foreground">{kiosk.enabled ? 'Enabled' : 'Disabled'}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatKioskOnboarding(kiosk)}</td>
              <td className="px-4 py-4">
                <div className="flex justify-end gap-2">
                  <Button type="button" variant="outline" size="icon-sm" aria-label={`Edit ${kiosk.name}`} onClick={() => onEdit(kiosk)}>
                    <Pencil className="size-4" aria-hidden="true" />
                  </Button>
                  <Button type="button" variant="outline" size="icon-sm" aria-label={`Rotate key for ${kiosk.name}`} disabled={rotateKioskId === kiosk.id} onClick={() => onRotateKey(kiosk)}>
                    <RotateCw className="size-4" aria-hidden="true" />
                  </Button>
                  <Button type="button" variant="outline" size="icon-sm" aria-label={`Disable ${kiosk.name}`} disabled={!kiosk.enabled || disableKioskId === kiosk.id} onClick={() => onDisable(kiosk)}>
                    <Trash2 className="size-4" aria-hidden="true" />
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function CredentialLine({ label, value }: { readonly label: string; readonly value: string }) {
  return (
    <div>
      <p className="mb-1 text-[12px] font-semibold uppercase text-muted-foreground">{label}</p>
      <code className="block min-w-0 overflow-x-auto rounded-interactive border border-border bg-content px-3 py-2 text-[13px]">{value}</code>
    </div>
  );
}

function AccessAssignmentsTable({
  assignments,
  deleteAssignmentId,
  editingAssignmentId,
  onDelete,
}: {
  readonly assignments: readonly AccessRuleAssignment[];
  readonly deleteAssignmentId: string | null;
  readonly editingAssignmentId: string | null;
  readonly onDelete: (assignment: AccessRuleAssignment) => void;
}) {
  if (assignments.length === 0) {
    return <p className="rounded-structural border border-border bg-background p-4 text-[14px] text-muted-foreground">No access level assignments configured.</p>;
  }

  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr>
            <th className="px-4 py-3 font-semibold">Location</th>
            <th className="px-4 py-3 font-semibold">System</th>
            <th className="px-4 py-3 font-semibold">Access level</th>
            <th className="px-4 py-3 font-semibold">Trigger</th>
            <th className="px-4 py-3 font-semibold">Grace</th>
            <th className="px-4 py-3 text-right font-semibold">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {assignments.map((assignment) => (
            <tr key={assignment.id} className={editingAssignmentId === assignment.id ? 'bg-hover-blue/60' : undefined}>
              <td className="px-4 py-4 font-medium text-foreground"><AssignmentLocationLabel locationId={assignment.locationId} /></td>
              <td className="px-4 py-4 text-muted-foreground">{assignment.systemId}</td>
              <td className="px-4 py-4 text-muted-foreground">{assignment.accessLevelTypeId}</td>
              <td className="px-4 py-4 text-muted-foreground">{getTriggerLabel(assignment.trigger)}</td>
              <td className="px-4 py-4 text-muted-foreground">{assignment.gracePeriodMinutes} min</td>
              <td className="px-4 py-4">
                <div className="flex justify-end gap-2">
                  <Button type="button" variant="outline" size="icon-sm" aria-label={`Delete ${getTriggerLabel(assignment.trigger)} assignment`} disabled={deleteAssignmentId === assignment.id} onClick={() => onDelete(assignment)}>
                    <Trash2 className="size-4" aria-hidden="true" />
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function Field({ label, children }: { readonly label: string; readonly children: ReactNode }) {
  const id = useId();

  return (
    <div className="grid gap-2">
      <label className="text-[14px] font-medium" htmlFor={id}>{label}</label>
      <div id={id}>{children}</div>
    </div>
  );
}

function SelectField({ label, value, children, onChange }: { readonly label: string; readonly value: string; readonly children: ReactNode; readonly onChange: (value: string) => void }) {
  const id = useId();

  return (
    <div className="grid gap-2">
      <label className="text-[14px] font-medium" htmlFor={id}>{label}</label>
      <select
        id={id}
        className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required
      >
        {children}
      </select>
    </div>
  );
}

function getDefaultFormValues(): FormValues {
  return {
    locationId: null,
    systemId: '',
    accessLevelTypeId: '',
    trigger: 'ExpectedVisitorAdded',
    gracePeriodMinutes: '0',
  };
}

function getDefaultKioskFormValues(): KioskFormValues {
  return {
    name: '',
    locationId: null,
    enabled: true,
    requireFacePicture: false,
    identityVerificationMethod: '',
    onboardingGracePeriodMinutes: '60',
  };
}

function toAssignmentRequest(values: FormValues): AccessRuleAssignmentRequest | null {
  const gracePeriodMinutes = Number.parseInt(values.gracePeriodMinutes, 10);

  if (!values.locationId || !values.systemId || !values.accessLevelTypeId || Number.isNaN(gracePeriodMinutes) || gracePeriodMinutes < 0) {
    return null;
  }

  return {
    locationId: values.locationId,
    systemId: values.systemId,
    accessLevelTypeId: values.accessLevelTypeId,
    trigger: values.trigger,
    gracePeriodMinutes,
  };
}

function toCreateKioskRequest(values: KioskFormValues): CreateReceptionKioskRequest | null {
  const name = values.name.trim();
  const onboardingGracePeriodMinutes = Number.parseInt(values.onboardingGracePeriodMinutes, 10);

  if (!name || !values.locationId || Number.isNaN(onboardingGracePeriodMinutes) || onboardingGracePeriodMinutes < 0) {
    return null;
  }

  return {
    name,
    locationId: values.locationId,
    requireFacePicture: values.requireFacePicture,
    identityVerificationMethod: values.identityVerificationMethod || null,
    onboardingGracePeriodMinutes,
  };
}

function toUpdateKioskRequest(values: KioskFormValues): UpdateReceptionKioskRequest | null {
  const request = toCreateKioskRequest(values);

  return request ? { ...request, enabled: values.enabled } : null;
}

function formatKioskOnboarding(kiosk: ReceptionKiosk): string {
  const parts = [
    kiosk.requireFacePicture ? 'Face picture' : null,
    kiosk.identityVerificationMethod === 'Picture' ? 'ID picture' : kiosk.identityVerificationMethod,
  ].filter((value): value is string => value !== null);

  const requirements = parts.length > 0 ? parts.join(' + ') : 'None';
  return `${requirements} • ${kiosk.onboardingGracePeriodMinutes} min grace`;
}

function getTriggerLabel(trigger: ReceptionAccessPolicyTrigger) {
  return triggerOptions.find((option) => option.value === trigger)?.label ?? trigger;
}

function AssignmentLocationLabel({ locationId }: { readonly locationId: string }) {
  const locationQuery = useQuery({
    queryKey: ['locations', 'location', locationId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/locations/{id}', {
        params: { path: { id: locationId } },
      });

      if (error) {
        throw new Error('Could not load location.');
      }

      return data;
    },
  });

  if (locationQuery.isLoading) {
    return 'Loading location...';
  }

  if (locationQuery.isError) {
    return locationId;
  }

  return getLocationLabel(locationQuery.data);
}
