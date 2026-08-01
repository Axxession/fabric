import { useEffect, useId, useState } from 'react';
import { useQuery } from '@tanstack/react-query';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';

type Building = components['schemas']['BuildingResponse'];
type LocationResponse = components['schemas']['LocationResponse'];
type Room = components['schemas']['RoomResponse'];
type Site = components['schemas']['SiteResponse'];

export type LocationDepth = 'None' | 'Site' | 'Building' | 'Room';

type LocationSelectorProps = {
  readonly value: string | null;
  readonly onChange: (value: string | null) => void;
  readonly level: Exclude<LocationDepth, 'None'>;
  readonly disabled?: boolean;
};

const depthOrder: Record<LocationDepth, number> = {
  None: 0,
  Site: 1,
  Building: 2,
  Room: 3,
};

export function LocationSelector({ value, onChange, level, disabled }: LocationSelectorProps) {
  const siteId = useId();
  const buildingId = useId();
  const roomId = useId();
  const [selectedSiteId, setSelectedSiteId] = useState('');
  const [selectedBuildingId, setSelectedBuildingId] = useState('');
  const [selectedRoomId, setSelectedRoomId] = useState('');

  const showBuildings = depthOrder[level] >= depthOrder.Building;
  const showRooms = depthOrder[level] >= depthOrder.Room;

  const sitesQuery = useQuery({
    queryKey: ['locations', 'sites'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/sites');

      if (error) {
        throw new Error('Could not load sites.');
      }

      return data;
    },
  });

  const selectedLocationQuery = useQuery({
    queryKey: ['locations', 'location', value],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/locations/{id}', {
        params: { path: { id: value ?? '' } },
      });

      if (error) {
        throw new Error('Could not load location.');
      }

      return data;
    },
    enabled: !!value,
  });

  const buildingsQuery = useQuery({
    queryKey: ['locations', 'site-buildings', selectedSiteId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/sites/{siteId}/buildings', {
        params: { path: { siteId: selectedSiteId } },
      });

      if (error) {
        throw new Error('Could not load buildings.');
      }

      return data;
    },
    enabled: showBuildings && !!selectedSiteId,
  });

  const roomsQuery = useQuery({
    queryKey: ['locations', 'building-rooms', selectedSiteId, selectedBuildingId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/sites/{siteId}/buildings/{buildingId}/rooms', {
        params: { path: { siteId: selectedSiteId, buildingId: selectedBuildingId } },
      });

      if (error) {
        throw new Error('Could not load rooms.');
      }

      return data;
    },
    enabled: showRooms && !!selectedSiteId && !!selectedBuildingId,
  });

  useEffect(() => {
    if (!value) {
      setSelectedSiteId('');
      setSelectedBuildingId('');
      setSelectedRoomId('');
      return;
    }

    if (!selectedLocationQuery.data) {
      return;
    }

    const location = selectedLocationQuery.data;
    setSelectedSiteId(location.site.id);
    setSelectedBuildingId(location.building?.id ?? '');
    setSelectedRoomId(location.room?.id ?? '');
  }, [selectedLocationQuery.data, value]);

  const sites = sitesQuery.data?.items ?? [];
  const buildings = buildingsQuery.data ?? [];
  const rooms = roomsQuery.data ?? [];
  const isLoading = sitesQuery.isLoading || selectedLocationQuery.isLoading || buildingsQuery.isLoading || roomsQuery.isLoading;
  const isError = sitesQuery.isError || selectedLocationQuery.isError || buildingsQuery.isError || roomsQuery.isError;

  function handleSiteChange(nextSiteId: string) {
    setSelectedSiteId(nextSiteId);
    setSelectedBuildingId('');
    setSelectedRoomId('');
    onChange(nextSiteId || null);
  }

  function handleBuildingChange(nextBuildingId: string) {
    setSelectedBuildingId(nextBuildingId);
    setSelectedRoomId('');
    onChange(nextBuildingId || selectedSiteId || null);
  }

  function handleRoomChange(nextRoomId: string) {
    setSelectedRoomId(nextRoomId);
    onChange(nextRoomId || selectedBuildingId || selectedSiteId || null);
  }

  return (
    <div className="grid gap-4 md:grid-cols-3">
      <div className="grid gap-2">
        <label className="text-[14px] font-medium" htmlFor={siteId}>Site</label>
        <select
          id={siteId}
          className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20 disabled:cursor-not-allowed disabled:opacity-60"
          value={selectedSiteId}
          onChange={(event) => handleSiteChange(event.target.value)}
          disabled={disabled || sitesQuery.isLoading}
        >
          <option value="">No site</option>
          {sites.map((site) => (
            <option key={site.id} value={site.id}>{site.name}</option>
          ))}
        </select>
      </div>

      {showBuildings ? (
        <div className="grid gap-2">
          <label className="text-[14px] font-medium" htmlFor={buildingId}>Building</label>
          <select
            id={buildingId}
            className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20 disabled:cursor-not-allowed disabled:opacity-60"
            value={selectedBuildingId}
            onChange={(event) => handleBuildingChange(event.target.value)}
            disabled={disabled || !selectedSiteId || buildingsQuery.isLoading}
          >
            <option value="">No building</option>
            {buildings.map((building) => (
              <option key={building.id} value={building.id}>{building.name}</option>
            ))}
          </select>
        </div>
      ) : null}

      {showRooms ? (
        <div className="grid gap-2">
          <label className="text-[14px] font-medium" htmlFor={roomId}>Room</label>
          <select
            id={roomId}
            className="h-9 w-full rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20 disabled:cursor-not-allowed disabled:opacity-60"
            value={selectedRoomId}
            onChange={(event) => handleRoomChange(event.target.value)}
            disabled={disabled || !selectedBuildingId || roomsQuery.isLoading}
          >
            <option value="">No room</option>
            {rooms.map((room) => (
              <option key={room.id} value={room.id}>{room.name}</option>
            ))}
          </select>
        </div>
      ) : null}

      {isLoading ? <p className="text-[13px] text-muted-foreground md:col-span-3">Loading locations...</p> : null}
      {isError ? <p className="text-[13px] text-error md:col-span-3">Could not load locations.</p> : null}
    </div>
  );
}

export function getLocationLabel(location: LocationResponse | null | undefined) {
  if (!location) {
    return 'Unassigned';
  }

  return [location.site.name, location.building?.name, location.room?.name].filter(Boolean).join(' / ');
}

export type { Building, LocationResponse, Room, Site };
