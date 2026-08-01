import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { RoomForm, type RoomFormValues } from './room-form';

const locationsQueryKey = ['facility', 'locations'] as const;
const emptyRoom: RoomFormValues = { name: '', capacity: '0', wheelchairAccessible: false };

export default function RoomEditPage() {
  const { siteId, buildingId, roomId } = useParams({ from: '/main/administration/sites/$siteId/buildings/$buildingId/rooms/$roomId/edit' });
  const queryClient = useQueryClient();

  const roomQuery = useQuery({
    queryKey: [...locationsQueryKey, siteId, 'buildings', buildingId, 'rooms', roomId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/locations/{id}', {
        params: { path: { id: roomId } },
      });

      if (error || !data || data.type !== 'Room' || !data.room) {
        throw new Error('Could not load room.');
      }

      return data.room;
    },
  });

  const updateRoom = useMutation({
    mutationFn: async (values: RoomFormValues) => {
      const { error } = await api.PUT('/api/locations/sites/{siteId}/buildings/{buildingId}/rooms/{roomId}', {
        params: { path: { siteId, buildingId, roomId } },
        body: {
          name: values.name,
          capacity: values.capacity,
          wheelchairAccessible: values.wheelchairAccessible,
        },
      });

      if (error) {
        throw new Error('Could not save room.');
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: [...locationsQueryKey, siteId, 'buildings', buildingId, 'rooms'] }),
        queryClient.invalidateQueries({ queryKey: [...locationsQueryKey, siteId, 'buildings', buildingId, 'rooms', roomId] }),
      ]);
      toast.success('Room saved.');
    },
  });

  function handleSubmit(values: RoomFormValues) {
    updateRoom.mutate(values);
  }

  const initialValues: RoomFormValues = roomQuery.data
    ? {
        name: roomQuery.data.name ?? '',
        capacity: String(roomQuery.data.capacity ?? 0),
        wheelchairAccessible: roomQuery.data.wheelchairAccessible ?? false,
      }
    : emptyRoom;

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>

        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Edit room</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Update room details within this building.</p>
        </div>
      </header>

      <Card className="p-6">
        {roomQuery.isError || updateRoom.isError ? (
          <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            {roomQuery.isError ? 'Could not load room.' : 'Could not save room.'}
          </p>
        ) : null}

        {roomQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading room...</p> : null}

        {!roomQuery.isLoading && !roomQuery.isError ? (
          <RoomForm initialValues={initialValues} isSubmitting={updateRoom.isPending} submitLabel="Save" onSubmit={handleSubmit} />
        ) : null}
      </Card>
    </div>
  );
}
