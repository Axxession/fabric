import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { ArrowLeft, Save } from 'lucide-react';
import { useEffect, useState, type FormEvent } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

import { encodersQueryKey, type CreateEncoderRequest, type HardwareAgent, type HardwareDevice, type UpdateEncoderRequest } from './card-management-types';

type FormValues = { readonly name: string; readonly hardwareRef: string; readonly enabled: boolean };

const emptyValues: FormValues = { name: '', hardwareRef: '', enabled: true };

export default function EncoderFormPage() {
  return <EncoderFormPageContent encoderId={getEncoderIdFromPath()} />;
}

export function EncoderFormPageContent({ encoderId = null }: { readonly encoderId?: string | null }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const mode = encoderId ? 'edit' : 'create';
  const [values, setValues] = useState<FormValues>(emptyValues);

  const encoderQuery = useQuery({
    queryKey: [...encodersQueryKey, encoderId],
    enabled: Boolean(encoderId),
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/encoders/{id}', { params: { path: { id: encoderId ?? '' } } });
      if (error || !data) {
        throw new Error('Could not load encoder.');
      }
      return data;
    },
  });

  const agentsQuery = useQuery({
    queryKey: ['hardware', 'agents'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/hardware/agents', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error || !data) {
        throw new Error('Could not load hardware agents.');
      }
      return data.items ?? [];
    },
  });

  const devicesQuery = useQuery({
    queryKey: ['hardware', 'devices', agentsQuery.data?.map((agent) => agent.id).join(',')],
    enabled: Boolean(agentsQuery.data?.length),
    queryFn: async () => {
      const devices = await Promise.all((agentsQuery.data ?? []).map(async (agent) => {
        const { data } = await api.GET('/api/hardware/agents/{agentId}/devices', { params: { path: { agentId: agent.id } } });
        return data ?? [];
      }));
      return devices.flat();
    },
  });

  useEffect(() => {
    if (!encoderQuery.data) {
      return;
    }
    setValues({ name: encoderQuery.data.name, hardwareRef: `${encoderQuery.data.agentId}|${encoderQuery.data.deviceId}`, enabled: encoderQuery.data.enabled });
  }, [encoderQuery.data]);

  const saveEncoder = useMutation({
    mutationFn: async (request: CreateEncoderRequest | UpdateEncoderRequest) => {
      if (mode === 'create') {
        const { error } = await api.POST('/api/desfire/encoders', { body: request });
        if (error) {
          throw new Error('Could not create encoder.');
        }
        return;
      }

      const { error } = await api.PUT('/api/desfire/encoders/{id}', { params: { path: { id: encoderId ?? '' } }, body: request });
      if (error) {
        throw new Error('Could not update encoder.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: encodersQueryKey });
      toast.success(mode === 'create' ? 'Encoder created.' : 'Encoder updated.');
      await navigate({ to: '/desfire-studio/printing' });
    },
    onError: () => toast.error(mode === 'create' ? 'Could not create encoder.' : 'Could not update encoder.'),
  });

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const [agentId, deviceId] = values.hardwareRef.split('|');
    if (!agentId || !deviceId) {
      toast.error('Select a hardware device.');
      return;
    }
    saveEncoder.mutate({ name: values.name.trim(), agentId, deviceId, enabled: values.enabled });
  };

  const encodingDevices = (devicesQuery.data ?? []).filter(supportsEncoding);
  const selectedDevice = encodingDevices.find((device) => `${device.agentId}|${device.deviceId}` === values.hardwareRef);

  return (
    <section className="grid gap-6">
      <Link to="/desfire-studio/printing" className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground"><ArrowLeft className="size-4" />Back to printing</Link>
      <Card>
        <CardHeader>
          <CardTitle>{mode === 'create' ? 'Add Encoder' : 'Edit Encoder'}</CardTitle>
          <CardDescription>Bind a DESFire encoder to an existing hardware device. Capabilities are managed by backend rules.</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="grid gap-5" onSubmit={submit}>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium"><span>Name</span><Input value={values.name} onChange={(event) => setValues({ ...values, name: event.target.value })} placeholder="Back office encoder" required /></label>
              <label className="grid gap-2 text-[14px] font-medium"><span>Hardware device</span><DeviceSelect value={values.hardwareRef} agents={agentsQuery.data ?? []} devices={encodingDevices} onChange={(hardwareRef) => setValues({ ...values, hardwareRef })} /></label>
            </div>
            <label className="flex items-center gap-2 text-[14px] font-medium"><input type="checkbox" checked={values.enabled} onChange={(event) => setValues({ ...values, enabled: event.target.checked })} />Enabled</label>
            <div className="flex flex-wrap gap-2"><Badge variant={selectedDevice ? 'success' : 'secondary'}>{selectedDevice ? 'Supports encoding workflow' : 'Select encoding-capable device'}</Badge><Badge variant="secondary">No printing support</Badge></div>
            <div className="flex justify-end"><Button type="submit" disabled={saveEncoder.isPending}><Save className="size-4" aria-hidden="true" />Save encoder</Button></div>
          </form>
        </CardContent>
      </Card>
    </section>
  );
}

function DeviceSelect({ value, agents, devices, onChange }: { readonly value: string; readonly agents: HardwareAgent[]; readonly devices: HardwareDevice[]; readonly onChange: (value: string) => void }) {
  return <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={value} onChange={(event) => onChange(event.target.value)} required><option value="">Select encoding-capable hardware device</option>{devices.map((device) => <option key={`${device.agentId}|${device.deviceId}`} value={`${device.agentId}|${device.deviceId}`}>{agents.find((agent) => agent.id === device.agentId)?.name ?? device.agentId} / {device.deviceId} ({device.kind}, {device.state})</option>)}</select>;
}

function supportsEncoding(device: HardwareDevice) {
  return ['card.present', 'rfid.apdu.exchange', 'card.eject'].every((required) => device.capabilities.some((capability) => capability.toLowerCase() === required));
}

function getEncoderIdFromPath() {
  const match = window.location.pathname.match(/\/old\/card-management\/printing\/encoders\/([^/]+)\/edit$/);
  return match?.[1] ?? null;
}
