import { useNavigate } from '@tanstack/react-router';
import { KeyRound, MonitorSmartphone } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { useMemo, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { getLocationLabel, type LocationResponse } from '@/shared/components/location-selector';
import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';
import { Label } from '@/shared/components/ui/label';

import { getReceptionDeskWorkstationSettings, getStoredReceptionDeskWorkstationId, saveReceptionDeskWorkstationSettings } from './reception-desk-workstation-settings';

type ReceptionDeskWorkstation = components['schemas']['ReceptionDeskWorkstationResponse'];

const workstationQueryKey = ['reception-desk-workstation', 'setup', 'workstations'] as const;

export default function ReceptionDeskWorkstationSetupPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [workstationId, setWorkstationId] = useState(getStoredReceptionDeskWorkstationId);
  const [workstationApiKey, setWorkstationApiKey] = useState('');
  const [error, setError] = useState<string | null>(null);
  const hasExistingApiKey = getReceptionDeskWorkstationSettings() !== null;

  const workstationsQuery = useQuery({
    queryKey: workstationQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/workstations', {
        params: { query: { Page: 0, PageSize: 100 } },
      });

      if (error || !data) {
        throw new Error(t('receptionDesk.setup.couldNotLoadWorkstations'));
      }

      return data.items ?? [];
    },
  });

  const locationDetailsQuery = useQuery({
    queryKey: [...workstationQueryKey, 'locations', (workstationsQuery.data ?? []).map((item) => item.locationId).sort().join(',')],
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

  const workstations = useMemo(
    () => [...(workstationsQuery.data ?? [])].filter((item) => item.enabled).sort((left, right) => left.name.localeCompare(right.name)),
    [workstationsQuery.data],
  );
  const locationsById = locationDetailsQuery.data ?? new Map<string, LocationResponse>();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    const currentSettings = getReceptionDeskWorkstationSettings();
    const nextWorkstationId = workstationId.trim();
    const nextApiKey = workstationApiKey.trim() || currentSettings?.workstationApiKey || '';

    if (!nextWorkstationId) {
      setError(t('receptionDesk.setup.selectWorkstationFirst'));
      return;
    }

    if (!nextApiKey) {
      setError(t('receptionDesk.setup.apiKeyRequired'));
      return;
    }

    saveReceptionDeskWorkstationSettings({ workstationId: nextWorkstationId, workstationApiKey: nextApiKey });
    await navigate({ to: '/reception-desk-workstation' });
  }

  return (
    <section className="grid w-full gap-6 lg:grid-cols-[0.9fr_1.1fr] lg:items-stretch">
      <div className="rounded-[2rem] border border-border bg-content p-7 shadow-sm sm:p-10">
        <div className="flex size-16 items-center justify-center rounded-full bg-hover-blue text-primary sm:size-20">
          <MonitorSmartphone className="size-8 sm:size-10" aria-hidden="true" />
        </div>
        <p className="mt-8 text-[13px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">{t('receptionDesk.setup.eyebrow')}</p>
        <h2 className="mt-3 text-[34px] font-semibold tracking-tight sm:text-[48px]">{t('receptionDesk.setup.title')}</h2>
        <p className="mt-5 text-[18px] leading-8 text-muted-foreground">
          {t('receptionDesk.setup.description')}
        </p>
      </div>

      <form className="grid gap-6 rounded-[2rem] border border-border bg-content p-7 shadow-sm sm:p-10" onSubmit={handleSubmit}>
        <div className="flex items-center gap-4">
          <div className="flex size-14 items-center justify-center rounded-full bg-hover-gray text-muted-foreground">
            <KeyRound className="size-7" aria-hidden="true" />
          </div>
          <div>
            <h3 className="text-[24px] font-semibold tracking-tight">{t('receptionDesk.setup.credentialsTitle')}</h3>
            <p className="mt-1 text-[15px] text-muted-foreground">{t('receptionDesk.setup.credentialsDescription')}</p>
          </div>
        </div>

        {error ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[15px] font-medium text-error">{error}</p> : null}

        <div className="grid gap-3">
          <Label htmlFor="reception-desk-workstation-id" className="text-[16px]">{t('receptionDesk.setup.workstation')}</Label>
          <select
            id="reception-desk-workstation-id"
            className="h-14 rounded-xl border border-border bg-content px-4 text-[18px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60"
            value={workstationId}
            onChange={(event) => setWorkstationId(event.target.value)}
            disabled={workstationsQuery.isLoading || workstationsQuery.isError}
          >
            <option value="">{t('receptionDesk.setup.selectWorkstation')}</option>
            {workstations.map((workstation) => (
              <option key={workstation.id} value={workstation.id}>{formatWorkstationLabel(workstation, locationsById.get(workstation.locationId))}</option>
            ))}
          </select>
          {workstationsQuery.isLoading ? <p className="text-[14px] text-muted-foreground">{t('receptionDesk.setup.loadingWorkstations')}</p> : null}
          {workstationsQuery.isError ? <p className="text-[14px] text-error">{t('receptionDesk.setup.couldNotLoadWorkstations')}</p> : null}
        </div>

        <div className="grid gap-3">
          <Label htmlFor="reception-desk-workstation-api-key" className="text-[16px]">{t('receptionDesk.setup.apiKeyLabel')}</Label>
          <Input
            id="reception-desk-workstation-api-key"
            className="h-14 rounded-xl px-4 text-[18px] md:text-[18px]"
            type="password"
            value={workstationApiKey}
            autoComplete="new-password"
            placeholder={hasExistingApiKey ? t('receptionDesk.setup.apiKeyConfiguredPlaceholder') : t('receptionDesk.setup.apiKeyPlaceholder')}
            onChange={(event) => setWorkstationApiKey(event.target.value)}
          />
          <p className="text-[14px] leading-6 text-muted-foreground">{t('receptionDesk.setup.apiKeyHint')}</p>
        </div>

        <div className="pt-2">
          <Button type="submit" className="h-14 w-full rounded-xl text-[18px] font-semibold sm:w-auto sm:px-10" disabled={workstationsQuery.isLoading || workstationsQuery.isError}>
            {t('receptionDesk.setup.save')}
          </Button>
        </div>
      </form>
    </section>
  );
}

function formatWorkstationLabel(workstation: ReceptionDeskWorkstation, location: LocationResponse | null | undefined) {
  const locationLabel = getLocationLabel(location);
  return locationLabel === 'Unassigned' ? workstation.name : `${workstation.name} - ${locationLabel}`;
}
