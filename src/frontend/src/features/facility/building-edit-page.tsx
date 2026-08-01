import { type FormEvent, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, Pencil, Plus, Trash2, X } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { BuildingForm, type BuildingFormValues } from './building-form';

type Room = components['schemas']['RoomResponse'];

const locationsQueryKey = ['facility', 'locations'] as const;
const emptyBuilding: BuildingFormValues = { name: '', address: '' };

export default function BuildingEditPage() {
  const { siteId, buildingId } = useParams({ from: '/main/administration/sites/$siteId/buildings/$buildingId/edit' });
  const queryClient = useQueryClient();
  const [isAddingRoom, setIsAddingRoom] = useState(false);
  const [roomName, setRoomName] = useState('');
  const [roomCapacity, setRoomCapacity] = useState('0');
  const [roomWheelchairAccessible, setRoomWheelchairAccessible] = useState(false);

  const buildingQuery = useQuery({
    queryKey: [...locationsQueryKey, siteId, 'buildings', buildingId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/locations/{id}', {
        params: { path: { id: buildingId } },
      });

      if (error || !data || data.type !== 'Building' || !data.building) {
        throw new Error('Could not load building.');
      }

      return data.building;
    },
  });

  const updateBuilding = useMutation({
    mutationFn: async (values: BuildingFormValues) => {
      const { error } = await api.PUT('/api/locations/sites/{siteId}/buildings/{buildingId}', {
        params: { path: { siteId, buildingId } },
        body: { name: values.name },
      });

      if (error) {
        throw new Error('Could not save building.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [...locationsQueryKey, siteId, 'buildings'] }),
        queryClient.invalidateQueries({ queryKey: [...locationsQueryKey, siteId, 'buildings', buildingId] }),
      ]);
      toast.success('Building saved.');
    },
  });

  const roomsQuery = useQuery({
    queryKey: [...locationsQueryKey, siteId, 'buildings', buildingId, 'rooms'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/sites/{siteId}/buildings/{buildingId}/rooms', {
        params: { path: { siteId, buildingId } },
      });

      if (error || !data) {
        throw new Error('Could not load rooms.');
      }

      return data;
    },
  });

  const addRoom = useMutation({
    mutationFn: async (values: { name: string; capacity: string; wheelchairAccessible: boolean }) => {
      const { error } = await api.POST('/api/locations/sites/{siteId}/buildings/{buildingId}/rooms', {
        params: { path: { siteId, buildingId } },
        body: {
          name: values.name,
          capacity: values.capacity,
          wheelchairAccessible: values.wheelchairAccessible,
        },
      });

      if (error) {
        throw new Error('Could not add room.');
      }
    },
    onSuccess: async () => {
      setRoomName('');
      setRoomCapacity('0');
      setRoomWheelchairAccessible(false);
      setIsAddingRoom(false);
      await queryClient.invalidateQueries({ queryKey: [...locationsQueryKey, siteId, 'buildings', buildingId, 'rooms'] });
      toast.success('Room added.');
    },
  });

  const deleteRoom = useMutation({
    mutationFn: async (roomId: string) => {
      const { error } = await api.DELETE('/api/locations/sites/{siteId}/buildings/{buildingId}/rooms/{roomId}', {
        params: { path: { siteId, buildingId, roomId } },
      });

      if (error) {
        throw new Error('Could not delete room.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: [...locationsQueryKey, siteId, 'buildings', buildingId, 'rooms'] });
      toast.success('Room deleted.');
    },
  });

  function handleSubmit(values: BuildingFormValues) {
    updateBuilding.mutate(values);
  }

  function handleAddRoom(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    addRoom.mutate({ name: roomName, capacity: roomCapacity, wheelchairAccessible: roomWheelchairAccessible });
  }

  function handleCancelAddRoom() {
    setRoomName('');
    setRoomCapacity('0');
    setRoomWheelchairAccessible(false);
    setIsAddingRoom(false);
  }

  function handleDeleteRoom(room: Room) {
    const confirmed = window.confirm(`Delete room ${room.name}?`);

    if (confirmed) {
      deleteRoom.mutate(room.id);
    }
  }

  const initialValues: BuildingFormValues = buildingQuery.data
    ? {
        name: buildingQuery.data.name ?? '',
        address: buildingQuery.data.address ?? '',
      }
    : emptyBuilding;

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>

        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Edit building</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update building details within this site.</p>
        </div>
      </header>

      <Card className="p-6">
        {buildingQuery.isError || updateBuilding.isError ? (
          <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {buildingQuery.isError ? 'Could not load building.' : 'Could not save building.'}
          </p>
        ) : null}

        {buildingQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading building...</p> : null}

        {!buildingQuery.isLoading && !buildingQuery.isError ? (
          <BuildingForm initialValues={initialValues} isSubmitting={updateBuilding.isPending} submitLabel="Save" onSubmit={handleSubmit} />
        ) : null}
      </Card>

      <Card className="p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h3 className="text-[18px] font-semibold tracking-tight">Rooms</h3>
            <p className="mt-2 text-[14px] text-muted-foreground">Manage rooms within this building.</p>
          </div>
          <Button type="button" onClick={() => setIsAddingRoom((current) => !current)}>
            {isAddingRoom ? <X className="size-4" aria-hidden="true" /> : <Plus className="size-4" aria-hidden="true" />}
            {isAddingRoom ? 'Cancel' : 'Add room'}
          </Button>
        </div>

        {roomsQuery.isError || addRoom.isError || deleteRoom.isError ? (
          <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {roomsQuery.isError ? 'Could not load rooms.' : addRoom.isError ? 'Could not add room.' : 'Could not delete room.'}
          </p>
        ) : null}

        {isAddingRoom ? (
          <form className="grid gap-3 rounded-structural border border-border p-4 md:grid-cols-[1fr_10rem_auto_auto] md:items-end" onSubmit={handleAddRoom}>
            <label className="grid gap-2 text-[14px] font-medium">
              Room name
              <input
                className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary"
                value={roomName}
                onChange={(event) => setRoomName(event.target.value)}
                required
              />
            </label>

            <label className="grid gap-2 text-[14px] font-medium">
              Capacity
              <input
                className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary"
                type="number"
                min="0"
                value={roomCapacity}
                onChange={(event) => setRoomCapacity(event.target.value)}
                required
              />
            </label>

            <label className="inline-flex h-9 items-center gap-2 text-[14px] font-medium md:mb-0">
              <input
                type="checkbox"
                className="size-4 accent-primary"
                checked={roomWheelchairAccessible}
                onChange={(event) => setRoomWheelchairAccessible(event.target.checked)}
              />
              Wheelchair accessible
            </label>

            <div className="flex gap-2 md:justify-end">
              <Button type="submit" disabled={addRoom.isPending}>
                <Plus className="size-4" aria-hidden="true" />
                {addRoom.isPending ? 'Adding...' : 'Create'}
              </Button>
              <Button type="button" variant="outline" onClick={handleCancelAddRoom} disabled={addRoom.isPending}>
                Cancel
              </Button>
            </div>
          </form>
        ) : null}

        <RoomsList rooms={roomsQuery.data ?? []} isDeleting={deleteRoom.isPending} isLoading={roomsQuery.isLoading} siteId={siteId} buildingId={buildingId} onDelete={handleDeleteRoom} />
      </Card>
    </div>
  );
}

function RoomsList({
  rooms,
  isDeleting,
  isLoading,
  siteId,
  buildingId,
  onDelete,
}: {
  readonly rooms: Room[];
  readonly isDeleting: boolean;
  readonly isLoading: boolean;
  readonly siteId: string;
  readonly buildingId: string;
  readonly onDelete: (room: Room) => void;
}) {
  if (isLoading) {
    return <p className="text-[14px] text-muted-foreground">Loading rooms...</p>;
  }

  if (rooms.length === 0) {
    return <p className="rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No rooms yet.</p>;
  }

  return (
    <div className="grid gap-3">
      <div className="grid gap-3 md:hidden">
        {rooms.map((room) => (
          <article key={room.id} className="rounded-structural border border-border p-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <h4 className="truncate text-[15px] font-semibold text-foreground">{room.name}</h4>
                <p className="mt-1 text-[14px] text-muted-foreground">Capacity: {room.capacity}</p>
                <p className="mt-1 text-[14px] text-muted-foreground">Accessible: {room.wheelchairAccessible ? 'Yes' : 'No'}</p>
              </div>
              <div className="flex shrink-0 gap-2">
                <Link
                  to="/administration/sites/$siteId/buildings/$buildingId/rooms/$roomId/edit"
                  params={{ siteId, buildingId, roomId: room.id }}
                  className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
                  aria-label={`Edit ${room.name}`}
                >
                  <Pencil className="size-4" aria-hidden="true" />
                </Link>
                <button
                  type="button"
                  className="inline-flex size-10 items-center justify-center rounded-interactive border border-error text-error transition hover:bg-error-background disabled:cursor-not-allowed disabled:opacity-60"
                  aria-label={`Delete ${room.name}`}
                  disabled={isDeleting}
                  onClick={() => onDelete(room)}
                >
                  <Trash2 className="size-4" aria-hidden="true" />
                </button>
              </div>
            </div>
          </article>
        ))}
      </div>
      <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
      <table className="w-full min-w-[44rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr>
            <th className="px-4 py-3 font-semibold">Room</th>
            <th className="px-4 py-3 font-semibold">Capacity</th>
            <th className="px-4 py-3 font-semibold">Accessible</th>
            <th className="px-4 py-3 text-right font-semibold">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rooms.map((room) => (
            <tr key={room.id}>
              <td className="px-4 py-4 font-medium text-foreground">{room.name}</td>
              <td className="px-4 py-4 text-muted-foreground">{room.capacity}</td>
              <td className="px-4 py-4 text-muted-foreground">{room.wheelchairAccessible ? 'Yes' : 'No'}</td>
              <td className="px-4 py-4">
                <div className="flex justify-end gap-2">
                  <Link
                    to="/administration/sites/$siteId/buildings/$buildingId/rooms/$roomId/edit"
                    params={{ siteId, buildingId, roomId: room.id }}
                    className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
                    aria-label={`Edit ${room.name}`}
                  >
                    <Pencil className="size-4" aria-hidden="true" />
                  </Link>
                  <button
                    type="button"
                    className="inline-flex size-9 items-center justify-center rounded-interactive border border-error text-error transition hover:bg-error-background disabled:cursor-not-allowed disabled:opacity-60"
                    aria-label={`Delete ${room.name}`}
                    disabled={isDeleting}
                    onClick={() => onDelete(room)}
                  >
                    <Trash2 className="size-4" aria-hidden="true" />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      </div>
    </div>
  );
}
