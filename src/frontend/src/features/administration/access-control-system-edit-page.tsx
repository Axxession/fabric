import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft, Plus, Trash2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { AccessControlProviderBadge } from '@/shared/components/access-control-provider-badge';
import { getLocationLabel, LocationSelector, type LocationResponse } from '@/shared/components/location-selector';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

type AccessControlSystemLocationResponse = components['schemas']['AccessControlSystemLocationResponse'];
type AccessControlSystemDetailsResponse = components['schemas']['AccessControlSystemDetailsResponse'];
type AccessControlSystemAuditResponse = components['schemas']['AccessControlSystemAuditResponse'];
type AnomalyBlockMode = components['schemas']['AnomalyBlockMode'];
type AccessControlSystemStatus = components['schemas']['AccessControlSystemStatus'];
type LinkAccessControlSystemLocationRequest = components['schemas']['LinkAccessControlSystemLocationRequest'];
type UpdateUnipassAccessControlSystemRequest = components['schemas']['UpdateUnipassAccessControlSystemRequest'];

type FormValues = {
  name: string;
  endpoint: string;
  sslValidation: boolean;
  username: string;
  password: string;
  status: AccessControlSystemStatus;
  anomalyBlockMode: AnomalyBlockMode;
};

const systemsQueryKey = ['administration', 'access-control', 'systems'] as const;
const emptyFormValues: FormValues = {
  name: '',
  endpoint: '',
  sslValidation: true,
  username: '',
  password: '',
  status: 'Active',
  anomalyBlockMode: 'WarnOnly',
};

export default function AccessControlSystemEditPage() {
  const { systemId } = useParams({ from: '/main/administration/access-control/systems/$systemId/edit' });
  const queryClient = useQueryClient();
  const [values, setValues] = useState<FormValues>(emptyFormValues);
  const [selectedLocationId, setSelectedLocationId] = useState<string | null>(null);

  const detailsQuery = useQuery({
    queryKey: [...systemsQueryKey, systemId, 'details'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/systems/{systemId}/details', { params: { path: { systemId } } });
      if (error || !data) {
        throw new Error('Could not load access control system.');
      }
      return data;
    },
  });

  const locationsQuery = useQuery({
    queryKey: [...systemsQueryKey, systemId, 'locations'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/systems/{systemId}/locations', { params: { path: { systemId }, query: { Page: 0, PageSize: 100 } } });
      if (error) {
        throw new Error('Could not load linked locations.');
      }
      return data;
    },
  });

  const locationDetailsQuery = useQuery({
    queryKey: [...systemsQueryKey, systemId, 'location-details', locationsQuery.data?.items?.map((item) => item.locationId).join(',') ?? ''],
    enabled: Boolean(locationsQuery.data?.items?.length),
    queryFn: async () => {
      const locations = await Promise.all(
        (locationsQuery.data?.items ?? []).map(async (item) => {
          const { data, error } = await api.GET('/api/locations/locations/{id}', { params: { path: { id: item.locationId } } });
          if (error || !data) {
            throw new Error('Could not load linked location details.');
          }
          return data;
        }),
      );
      return new Map(locations.map((location) => [location.id, location]));
    },
  });

  useEffect(() => {
    if (!detailsQuery.data) {
      return;
    }

    const nextValues = toFormValues(detailsQuery.data);
    setValues(nextValues);
  }, [detailsQuery.data]);

  const updateUnipassSystem = useMutation({
    mutationFn: async (request: UpdateUnipassAccessControlSystemRequest) => {
      const { error } = await api.PUT('/api/access-control/systems/{systemId}/unipass', { params: { path: { systemId } }, body: request });
      if (error) {
        throw new Error('Could not save access control system.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: systemsQueryKey }),
        queryClient.invalidateQueries({ queryKey: [...systemsQueryKey, systemId, 'details'] }),
      ]);
      toast.success('Access control system saved.');
    },
    onError: () => {
      toast.error('Could not save access control system.');
    },
  });

  const linkLocation = useMutation({
    mutationFn: async (request: LinkAccessControlSystemLocationRequest) => {
      const { error } = await api.POST('/api/access-control/systems/{systemId}/locations', { params: { path: { systemId } }, body: request });
      if (error) {
        throw new Error('Could not link location.');
      }
    },
    onSuccess: async () => {
      setSelectedLocationId(null);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [...systemsQueryKey, systemId, 'locations'] }),
        queryClient.invalidateQueries({ queryKey: [...systemsQueryKey, systemId, 'location-details'] }),
      ]);
      toast.success('Location linked.');
    },
    onError: () => toast.error('Could not link location.'),
  });

  const unlinkLocation = useMutation({
    mutationFn: async (linkId: string) => {
      const { error } = await api.DELETE('/api/access-control/systems/locations/{linkId}', { params: { path: { linkId } } });
      if (error) {
        throw new Error('Could not unlink location.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [...systemsQueryKey, systemId, 'locations'] }),
        queryClient.invalidateQueries({ queryKey: [...systemsQueryKey, systemId, 'location-details'] }),
      ]);
      toast.success('Location unlinked.');
    },
    onError: () => toast.error('Could not unlink location.'),
  });

  const auditSystemSubjects = useMutation({
    mutationFn: async () => {
      const { data, error } = await api.POST('/api/access-control/systems/{systemId}/audit', { params: { path: { systemId } } });
      if (error || !data) {
        throw new Error('Could not start PACS subject audit.');
      }
      return data as AccessControlSystemAuditResponse;
    },
    onSuccess: (data) => {
      toast.success(`Audit queued for ${data.enqueuedSubjects} subject(s). ${data.recentlyAuditedSubjects} skipped due to recent audit.`);
    },
    onError: () => toast.error('Could not start PACS subject audit.'),
  });

  function updateValue<TKey extends keyof FormValues>(key: TKey, value: FormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!detailsQuery.data) {
      return;
    }

    if (!isUnipassDetails(detailsQuery.data)) {
      return;
    }

    updateUnipassSystem.mutate({
      name: values.name,
      endpoint: values.endpoint,
      sslValidation: values.sslValidation,
      username: values.username,
      password: values.password.trim() === '' ? null : values.password,
      status: values.status,
      anomalyBlockMode: values.anomalyBlockMode,
    });
  }

  const linkedLocations = locationsQuery.data?.items ?? [];
  const linkedLocationIds = new Set(linkedLocations.map((item) => item.locationId));
  const linkedLocationDetails = locationDetailsQuery.data ?? new Map<string, LocationResponse>();

  function handleLinkLocation() {
    if (!selectedLocationId || linkedLocationIds.has(selectedLocationId)) {
      return;
    }

    linkLocation.mutate({ locationId: selectedLocationId });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>

        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Edit access control system</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update access control system configuration. System type cannot be changed.</p>
        </div>
        <Button type="button" variant="outline" disabled={auditSystemSubjects.isPending} onClick={() => auditSystemSubjects.mutate()}>
          {auditSystemSubjects.isPending ? 'Queueing audit...' : 'Audit subjects now'}
        </Button>
      </header>

      <Card className="p-6">
        {detailsQuery.isError || updateUnipassSystem.isError ? (
          <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {detailsQuery.isError ? 'Could not load access control system.' : 'Could not save access control system.'}
          </p>
        ) : null}

        {detailsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading access control system...</p> : null}

        {!detailsQuery.isLoading && detailsQuery.data ? (
          isUnipassDetails(detailsQuery.data) ? (
            <form className="grid gap-5" onSubmit={handleSubmit}>
              <div className="grid gap-4 md:grid-cols-2">
                <label className="grid gap-2 text-[14px] font-medium">
                  Type
                  <div className="h-10 rounded-interactive border border-border bg-background px-3 py-2 text-[14px] text-foreground">
                    <AccessControlProviderBadge providerKind={detailsQuery.data.system.providerKind} />
                  </div>
                </label>

                <label className="grid gap-2 text-[14px] font-medium">
                  Status
                  <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.status} onChange={(event) => updateValue('status', event.target.value as AccessControlSystemStatus)}>
                    <option value="Active">Active</option>
                    <option value="Inactive">Inactive</option>
                  </select>
                </label>

                <label className="grid gap-2 text-[14px] font-medium">
                  Name
                  <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.name} onChange={(event) => updateValue('name', event.target.value)} required />
                </label>

                <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
                  Endpoint
                  <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.endpoint} onChange={(event) => updateValue('endpoint', event.target.value)} required />
                </label>

                <label className="grid gap-2 text-[14px] font-medium">
                  Username
                  <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.username} onChange={(event) => updateValue('username', event.target.value)} required />
                  <span className="text-[12px] text-muted-foreground">Username for the Unipass integration.</span>
                </label>

                <label className="grid gap-2 text-[14px] font-medium">
                  Password
                  <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" type="password" value={values.password} onChange={(event) => updateValue('password', event.target.value)} />
                  <span className="text-[12px] text-muted-foreground">Leave blank to keep current password.</span>
                </label>
              </div>

              <label className="flex items-center gap-3 text-[14px] font-medium">
                <input type="checkbox" checked={values.sslValidation} onChange={(event) => updateValue('sslValidation', event.target.checked)} />
                Validate SSL certificate
              </label>

              <label className="grid gap-2 text-[14px] font-medium md:max-w-sm">
                Anomaly policy
                <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.anomalyBlockMode} onChange={(event) => updateValue('anomalyBlockMode', event.target.value as AnomalyBlockMode)}>
                  <option value="WarnOnly">Warn only</option>
                  <option value="BlockProvisioning">Block provisioning</option>
                </select>
              </label>

              <div className="flex justify-end">
                <Button type="submit" disabled={updateUnipassSystem.isPending}>
                  {updateUnipassSystem.isPending ? 'Saving...' : 'Save'}
                </Button>
              </div>

              <div className="rounded-structural border border-border bg-background p-4 text-[14px] text-muted-foreground">
                System audit uses subject cooldown policy. Subjects audited in the last 5 minutes are skipped.
              </div>
            </form>
          ) : (
            <div className="rounded-structural border border-border bg-background p-4 text-[14px] text-muted-foreground">
              Editing for this system type is not supported yet.
            </div>
          )
        ) : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Linked Locations</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Link physical locations to this access control system.</p>
          </div>
        </div>

        <div className="grid gap-4 rounded-structural border border-border p-4">
          <LocationSelector value={selectedLocationId} onChange={setSelectedLocationId} level="Room" disabled={linkLocation.isPending} />
          <div className="flex justify-end">
            <Button type="button" disabled={!selectedLocationId || linkLocation.isPending || linkedLocationIds.has(selectedLocationId)} onClick={handleLinkLocation}>
              <Plus className="size-4" aria-hidden="true" />
              Link location
            </Button>
          </div>
        </div>

        {locationsQuery.isError || locationDetailsQuery.isError || linkLocation.isError || unlinkLocation.isError ? (
          <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {locationsQuery.isError ? 'Could not load linked locations.' : locationDetailsQuery.isError ? 'Could not load linked location details.' : linkLocation.isError ? 'Could not link location.' : 'Could not unlink location.'}
          </p>
        ) : null}

        {locationsQuery.isLoading || locationDetailsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading linked locations...</p> : null}

        {!locationsQuery.isLoading && !locationDetailsQuery.isLoading && linkedLocations.length === 0 ? <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No locations linked yet.</p> : null}

        {linkedLocations.length > 0 ? (
          <div className="grid gap-3">
            {linkedLocations.map((link) => (
              <div key={link.id} className="flex items-center justify-between gap-4 rounded-structural border border-border p-4">
                <div className="min-w-0">
                  <p className="font-medium text-foreground">{getLocationLabel(linkedLocationDetails.get(link.locationId))}</p>
                  <p className="mt-1 text-[14px] text-muted-foreground">Linked location</p>
                </div>
                <Button type="button" variant="outline" size="sm" disabled={unlinkLocation.isPending} onClick={() => unlinkLocation.mutate(link.id)}>
                  <Trash2 className="size-4" aria-hidden="true" />
                  Unlink
                </Button>
              </div>
            ))}
          </div>
        ) : null}
      </Card>
    </div>
  );
}

function isUnipassDetails(details: AccessControlSystemDetailsResponse) {
  return details.configuration.type === 'unipass';
}

function toFormValues(details: AccessControlSystemDetailsResponse): FormValues {
  return {
    name: details.system.name,
    endpoint: details.system.endpoint,
    sslValidation: details.system.sslValidation,
    username: isUnipassDetails(details) ? details.configuration.username : '',
    password: '',
    status: details.system.status,
    anomalyBlockMode: details.system.anomalyBlockMode,
  };
}
