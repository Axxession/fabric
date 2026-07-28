import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, Cpu, HeartPulse } from 'lucide-react';
import { useEffect, useState } from 'react';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Separator } from '@/shared/components/ui/separator';

type HardwareAgent = components['schemas']['HardwareAgentResponse'];
type HardwareDevice = components['schemas']['HardwareDeviceResponse'];
type HardwareDeviceHealth = components['schemas']['HardwareDeviceHealthResponse'];
type HardwareConnectionStatus = components['schemas']['HardwareConnectionStatus'];
type HardwareDeviceAvailabilityReason = components['schemas']['HardwareDeviceAvailabilityReason'];

const agentsQueryKey = ['facility', 'hardware-agents'] as const;

export default function HardwareAgentDetailPage() {
  const { agentId } = useParams({ from: '/main/administration/clients/hardware-agents/$agentId' });
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null);

  const agentQuery = useQuery({
    queryKey: [...agentsQueryKey, agentId],
    refetchInterval: 10_000,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/hardware/agents/{agentId}', {
        params: { path: { agentId } },
      });

      if (error || !data) {
        throw new Error('Could not load hardware agent.');
      }

      return data;
    },
  });

  const devicesQuery = useQuery({
    queryKey: [...agentsQueryKey, agentId, 'devices'],
    refetchInterval: 10_000,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/hardware/agents/{agentId}/devices', {
        params: { path: { agentId } },
      });

      if (error || !data) {
        throw new Error('Could not load hardware devices.');
      }

      return data;
    },
  });

  const healthQuery = useQuery({
    queryKey: [...agentsQueryKey, agentId, 'devices', selectedDeviceId, 'health'],
    enabled: selectedDeviceId !== null,
    refetchInterval: 10_000,
    queryFn: async () => {
      if (!selectedDeviceId) {
        throw new Error('No device selected.');
      }

      const { data, error } = await api.GET('/api/hardware/agents/{agentId}/devices/{deviceId}/health', {
        params: { path: { agentId, deviceId: selectedDeviceId } },
      });

      if (error || !data) {
        throw new Error('Could not load hardware device health.');
      }

      return data;
    },
  });

  const agent = agentQuery.data;
  const devices = devicesQuery.data ?? [];
  const hasAgentData = agentQuery.data !== undefined;
  const hasDeviceData = devicesQuery.data !== undefined;
  const isPageDataStale = (hasAgentData && agentQuery.isError) || (hasDeviceData && devicesQuery.isError);
  const isPageDataLive = (hasAgentData || hasDeviceData) && !isPageDataStale;
  const hasHealthData = healthQuery.data !== undefined;
  const isHealthDataStale = hasHealthData && healthQuery.isError;

  useEffect(() => {
    if (!selectedDeviceId) {
      return;
    }

    if (devices.some((device) => device.deviceId === selectedDeviceId)) {
      return;
    }

    setSelectedDeviceId(null);
  }, [devices, selectedDeviceId]);

  return (
    <div className="grid gap-6">
      <Link to="/administration/clients" search={{ tab: 'hardware-agents' } as never} className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground">
        <ArrowLeft className="size-4" aria-hidden="true" />
        Back to hardware
      </Link>

      <section className="rounded-structural border border-border bg-content">
        <div className="border-b border-border p-4 sm:p-6">
          {agentQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading hardware agent...</p> : null}
          {agentQuery.isError && !hasAgentData ? <p className="text-[14px] text-error">Could not load hardware agent.</p> : null}
          {isPageDataLive ? <div className="mb-4"><PollingStatusBadge state="live" /></div> : null}
          {isPageDataStale ? <div className="mb-4 grid gap-2"><PollingStatusBadge state="stale" /><p className="text-[14px] text-warning">Showing last successful update. Live polling is temporarily unavailable.</p></div> : null}
          {agent ? <AgentHeader agent={agent} deviceCount={devices.length} /> : null}
        </div>

        <div className="grid gap-6 p-4 sm:p-6">
          <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_24rem]">
            <DeviceInventory
              devices={devices}
              loading={devicesQuery.isLoading}
              error={devicesQuery.isError}
              selectedDeviceId={selectedDeviceId}
              onSelectDevice={setSelectedDeviceId}
            />
            <DeviceHealthPanel health={healthQuery.data} loading={healthQuery.isLoading} error={healthQuery.isError} isStale={isHealthDataStale} selectedDeviceId={selectedDeviceId} />
          </div>
        </div>
      </section>
    </div>
  );
}

function AgentHeader({ agent, deviceCount }: { readonly agent: HardwareAgent; readonly deviceCount: number }) {
  return (
    <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
      <div>
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="text-[20px] font-semibold tracking-tight">{agent.name}</h1>
          <StatusBadge enabled={agent.enabled} />
          <ConnectionStatusBadge status={agent.connectionStatus} />
        </div>
        <p className="mt-2 text-[14px] text-muted-foreground">{agent.id}</p>
        <AgentConnectionNotice status={agent.connectionStatus} />
      </div>

      <dl className="grid gap-3 rounded-structural border border-border bg-background p-4 text-[13px] sm:grid-cols-2 lg:min-w-[24rem]">
        <InfoItem label="Devices" value={String(deviceCount)} />
        <InfoItem label="Agent" value={formatConnectionStatus(agent.connectionStatus)} />
        <InfoItem label="Last seen" value={formatDate(agent.lastSeenAt)} />
        <InfoItem label="Inventory" value={formatDate(agent.lastInventoryAt)} />
      </dl>
    </div>
  );
}

function DeviceInventory({
  devices,
  loading,
  error,
  selectedDeviceId,
  onSelectDevice,
}: {
  readonly devices: HardwareDevice[];
  readonly loading: boolean;
  readonly error: boolean;
  readonly selectedDeviceId: string | null;
  readonly onSelectDevice: (deviceId: string) => void;
}) {
  if (!loading && !error && devices.length === 0) {
    return (
      <Empty>
        <EmptyHeader>
          <EmptyTitle>No devices reported</EmptyTitle>
          <EmptyDescription>Once agent posts inventory, logical scanners, printers, and readers show here.</EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <div className="grid gap-3">
      <div className="flex items-center gap-2">
        <Cpu className="size-4 text-muted-foreground" aria-hidden="true" />
        <h2 className="text-[16px] font-semibold">Devices</h2>
      </div>

        <div className="grid gap-3 md:hidden">
          {loading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading hardware devices...</p> : null}
          {error && devices.length === 0 ? <p className="rounded-structural border border-border p-4 text-[14px] text-error">Could not load hardware devices.</p> : null}
          {devices.map((device) => (
            <DeviceCard key={device.deviceId} device={device} selected={selectedDeviceId === device.deviceId} onSelect={() => onSelectDevice(device.deviceId)} />
          ))}
      </div>

      <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
        <table className="w-full min-w-[48rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Device</th>
              <th className="px-4 py-3 font-semibold">Kind</th>
              <th className="px-4 py-3 font-semibold">Capabilities</th>
              <th className="px-4 py-3 font-semibold">State</th>
              <th className="px-4 py-3 font-semibold">Last seen</th>
              <th className="px-4 py-3 text-right font-semibold">Health</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {loading ? (
              <tr>
                <td className="px-4 py-5 text-muted-foreground" colSpan={6}>Loading hardware devices...</td>
              </tr>
            ) : null}
            {error && devices.length === 0 ? (
              <tr>
                <td className="px-4 py-5 text-error" colSpan={6}>Could not load hardware devices.</td>
              </tr>
            ) : null}
            {devices.map((device) => (
              <DeviceRow key={device.deviceId} device={device} selected={selectedDeviceId === device.deviceId} onSelect={() => onSelectDevice(device.deviceId)} />
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function DeviceCard({ device, selected, onSelect }: { readonly device: HardwareDevice; readonly selected: boolean; readonly onSelect: () => void }) {
  return (
    <article className="rounded-structural border border-border p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="truncate text-[15px] font-semibold text-foreground">{device.deviceId}</h3>
          <p className="mt-1 text-[13px] text-muted-foreground">{device.kind} via {device.driver}</p>
        </div>
        <div className="flex flex-wrap justify-end gap-2">
          <AvailabilityBadge isAvailable={device.isAvailable} />
        </div>
      </div>
      <div className="mt-3 flex flex-wrap gap-2">
        {device.capabilities.map((capability) => <Badge key={capability} variant="outline">{capability}</Badge>)}
      </div>
      <p className="mt-3 text-[13px] text-muted-foreground">Last seen {formatDate(device.lastSeenAt)}</p>
      <Button type="button" variant={selected ? 'secondary' : 'outline'} size="sm" className="mt-4 w-full" onClick={onSelect}>
        View health
      </Button>
    </article>
  );
}

function DeviceRow({ device, selected, onSelect }: { readonly device: HardwareDevice; readonly selected: boolean; readonly onSelect: () => void }) {
  return (
    <tr className={selected ? 'bg-hover-blue' : undefined}>
      <td className="px-4 py-4">
        <div className="font-medium text-foreground">{device.deviceId}</div>
        <div className="mt-1 text-[13px] text-muted-foreground">{device.driver}</div>
      </td>
      <td className="px-4 py-4 text-muted-foreground">{device.kind}</td>
      <td className="px-4 py-4">
        <div className="flex flex-wrap gap-2">
          {device.capabilities.map((capability) => <Badge key={capability} variant="outline">{capability}</Badge>)}
        </div>
      </td>
      <td className="px-4 py-4"><AvailabilityBadge isAvailable={device.isAvailable} /></td>
      <td className="px-4 py-4 text-muted-foreground">{formatDate(device.lastSeenAt)}</td>
      <td className="px-4 py-4">
        <div className="flex justify-end">
          <Button type="button" variant={selected ? 'secondary' : 'outline'} size="sm" onClick={onSelect}>View health</Button>
        </div>
      </td>
    </tr>
  );
}

function DeviceHealthPanel({
  health,
  loading,
  error,
  isStale,
  selectedDeviceId,
}: {
  readonly health: HardwareDeviceHealth | undefined;
  readonly loading: boolean;
  readonly error: boolean;
  readonly isStale: boolean;
  readonly selectedDeviceId: string | null;
}) {
  return (
    <aside className="rounded-structural border border-border p-4">
      <div className="flex items-center gap-2">
        <HeartPulse className="size-4 text-muted-foreground" aria-hidden="true" />
        <h2 className="text-[16px] font-semibold">Health</h2>
      </div>

      {!selectedDeviceId ? <p className="mt-4 text-[14px] text-muted-foreground">Select a device to inspect health and diagnostics.</p> : null}
      {loading ? <p className="mt-4 text-[14px] text-muted-foreground">Loading health...</p> : null}
      {error && !health ? <p className="mt-4 text-[14px] text-error">Could not load device health.</p> : null}
      {isStale ? <p className="mt-4 text-[14px] text-warning">Showing last successful diagnostics. Live polling is temporarily unavailable.</p> : null}

      {health ? (
        <div className="mt-4 grid gap-4">
          <dl className="grid gap-3 text-[13px]">
            <InfoItem label="Device" value={health.deviceId} />
            <InfoItem label="Availability" value={health.isAvailable ? 'Available' : 'Unavailable'} />
            {!health.isAvailable ? <InfoItem label="Reason" value={formatAvailabilityReason(health.availabilityReason)} /> : null}
            <InfoItem label="Enabled" value={health.enabled ? 'Yes' : 'No'} />
            <InfoItem label="Last seen" value={formatDate(health.lastSeenAt)} />
          </dl>
          <div>
            <h3 className="text-[13px] font-semibold uppercase text-muted-foreground">Diagnostics</h3>
            <DiagnosticsPanel diagnosticsJson={health.diagnosticsJson} />
          </div>
        </div>
      ) : null}
    </aside>
  );
}

function InfoItem({ label, value }: { readonly label: string; readonly value: string }) {
  return (
    <div>
      <dt className="text-muted-foreground">{label}</dt>
      <dd className="mt-1 break-words font-medium text-foreground">{value}</dd>
    </div>
  );
}

function StatusBadge({ enabled }: { readonly enabled: boolean }) {
  return <Badge variant={enabled ? 'success' : 'secondary'}>{enabled ? 'Enabled' : 'Disabled'}</Badge>;
}

function AgentConnectionNotice({ status }: { readonly status: HardwareConnectionStatus }) {
  if (status === 'Online') {
    return null;
  }

  const tone = status === 'Stale' ? 'warning' : 'error';
  const message = status === 'Stale'
    ? 'Hardware devices might face interruption. Device statuses reflect the latest agent update.'
    : 'Hardware devices will not be available. Device statuses reflect the latest agent update.';

  return (
    <div className={`mt-4 rounded-interactive border px-4 py-3 text-[14px] ${tone === 'warning' ? 'border-warning/40 bg-warning/10 text-foreground' : 'border-error/40 bg-error-background text-error'}`}>
      {message}
    </div>
  );
}

function ConnectionStatusBadge({ status }: { readonly status: HardwareConnectionStatus }) {
  const variant = status === 'Online' ? 'success' : status === 'Stale' ? 'warning' : 'secondary';
  return <Badge variant={variant}>{formatConnectionStatus(status)}</Badge>;
}

function AvailabilityBadge({ isAvailable }: { readonly isAvailable: boolean }) {
  return <Badge variant={isAvailable ? 'success' : 'secondary'}>{isAvailable ? 'Available' : 'Unavailable'}</Badge>;
}

function PollingStatusBadge({ state }: { readonly state: 'live' | 'stale' }) {
  return <Badge variant={state === 'live' ? 'success' : 'warning'}>{state === 'live' ? 'Live' : 'Stale'}</Badge>;
}

function formatConnectionStatus(status: HardwareConnectionStatus) {
  return status === 'Online' ? 'Online' : status === 'Stale' ? 'Connection issues' : 'Offline';
}

function formatAvailabilityReason(reason: HardwareDeviceAvailabilityReason | null) {
  if (reason === 'DeviceDisabled') {
    return 'Device disabled';
  }

  if (reason === 'DeviceOffline') {
    return 'Hardware not detected';
  }

  return 'Unavailable';
}

function formatDate(value: string | null) {
  if (!value) {
    return 'Never';
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

function DiagnosticsPanel({ diagnosticsJson }: { readonly diagnosticsJson: string }) {
  const diagnostics = parseDiagnostics(diagnosticsJson);

  return (
    <div className="mt-2 rounded-interactive border border-border bg-background">
      <DiagnosticsRow label="Connection path" value={diagnostics.connection || 'Not reported'} />
      <Separator />
      <DiagnosticsRow label="Configured" value={diagnostics.configured ? 'Yes' : 'No'} />
      <Separator />
      <DiagnosticsRow label="Hardware detected" value={diagnostics.detected ? 'Yes' : 'No'} />
      <Separator />
      <DiagnosticsRow label="Platform" value={diagnostics.platform || 'Not reported'} />
    </div>
  );
}

function DiagnosticsRow({ label, value }: { readonly label: string; readonly value: string }) {
  return (
    <div className="grid gap-1 px-3 py-3 text-[13px] sm:grid-cols-[8rem_minmax(0,1fr)] sm:gap-3">
      <div className="text-muted-foreground">{label}</div>
      <div className="break-words font-medium text-foreground">{value}</div>
    </div>
  );
}

function parseDiagnostics(value: string) {
  try {
    const parsed = JSON.parse(value) as {
      readonly connection?: string | null;
      readonly configured?: boolean;
      readonly detected?: boolean;
      readonly platform?: string | null;
    };

    return {
      connection: parsed.connection ?? null,
      configured: parsed.configured ?? false,
      detected: parsed.detected ?? false,
      platform: parsed.platform ?? null,
    };
  } catch {
    return {
      connection: value,
      configured: false,
      detected: false,
      platform: null,
    };
  }
}
