import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import {
  ArrowLeft,
  Copy,
  KeyRound,
  Plus,
  RotateCcw,
  Save,
  Trash2,
} from "lucide-react";
import { useEffect, useId, useRef, useState, type FormEvent } from "react";
import { useAuth } from "react-oidc-context";
import { toast } from "sonner";

import { allWorkflowDefinitionsQueryKey } from './workflow-query-keys';
import { api, apiBaseUrl } from '@/shared/api/client';
import type { components } from "@/shared/api/generated/schema";
import { Badge, type BadgeVariant } from "@/shared/components/ui/badge";
import { Button } from "@/shared/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/shared/components/ui/card";
import {
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
} from "@/shared/components/ui/combobox";
import { Input } from "@/shared/components/ui/input";
import { Label } from "@/shared/components/ui/label";

type Kiosk = components["schemas"]["KioskResponse"];
type KioskWithSession = Kiosk & { readonly activeSessionId?: string | null; readonly activeSessionStatus?: string | null };
type KioskProfile = components["schemas"]["KioskProfileResponse"];
type KioskKeyResponse = components["schemas"]["KioskKeyResponse"];
type KioskMode = components["schemas"]["KioskMode"];
type KioskDeviceType = components["schemas"]["KioskDeviceType"];
type KioskDevice = components["schemas"]["KioskDeviceResponse"];
type HardwareAgent = components["schemas"]["HardwareAgentResponse"];
type HardwareDevice = components["schemas"]["HardwareDeviceResponse"];
type WorkflowDefinition =
  components["schemas"]["LinkedWorkflowDefinitionSummary"];

type KioskForm = {
  readonly name: string;
  readonly profileId: string;
  readonly showDetailedErrors: boolean;
  readonly workflowDefinitionId: string;
};
type DeviceRow = {
  readonly name: string;
  readonly type: KioskDeviceType;
  readonly slotNumber: string;
  readonly agentId: string;
  readonly deviceId: string;
  readonly enabled: boolean;
  readonly cleanupOnSessionEnd: boolean;
  readonly sortOrder: string;
};

const pageSize = 100;
const profilesQueryKey = ["automation", "kiosk", "profiles"] as const;
const kiosksQueryKey = ["automation", "kiosk", "kiosks"] as const;
const hardwareAgentsQueryKey = [
  "automation",
  "kiosk",
  "hardware-agents",
] as const;
const deviceTypes: readonly KioskDeviceType[] = [
  "QrReader",
  "RfidReader",
  "Dispenser",
  "Collector",
  "Encoder",
  "EidReader",
  "PassportReader",
  "LabelPrinter",
];
const capabilityByType: Record<KioskDeviceType, string> = {
  QrReader: "qr.scan",
  RfidReader: "rfid.read",
  Dispenser: "card.dispense",
  Collector: "card.present, card.collect, card.eject",
  Encoder: "card.present, rfid.apdu.exchange, card.eject",
  EidReader: "eid.read",
  PassportReader: "passport.scan",
  LabelPrinter: "label.print",
};

export default function KioskEditPage() {
  const { kioskId } = useParams({
    from: "/main/administration/automation/kiosk/$kioskId/edit",
  });
  const auth = useAuth();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<KioskForm>({
    name: "",
    profileId: "",
    showDetailedErrors: false,
    workflowDefinitionId: "",
  });
  const [latestKey, setLatestKey] = useState<KioskKeyResponse | null>(null);
  const [devices, setDevices] = useState<DeviceRow[]>([]);
  const nameId = useId();
  const profileId = useId();
  const workflowDefinitionId = useId();

  const kioskQuery = useQuery({
    queryKey: [...kiosksQueryKey, kioskId],
    queryFn: async () => {
      const { data, error } = await api.GET("/api/kiosks/{id}", {
        params: { path: { id: kioskId } },
      });
      if (error || !data) throw new Error("Could not load kiosk.");
      return data;
    },
  });
  const profilesQuery = useQuery({
    queryKey: profilesQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET("/api/kiosk-profiles", {
        params: { query: { Page: 0, PageSize: pageSize } },
      });
      if (error || !data) throw new Error("Could not load profiles.");
      return data.items ?? [];
    },
  });
  const devicesQuery = useQuery({
    queryKey: [...kiosksQueryKey, kioskId, "devices"],
    queryFn: async () => {
      const { data, error } = await api.GET("/api/kiosks/{id}/devices", {
        params: { path: { id: kioskId } },
      });
      if (error || !data) throw new Error("Could not load devices.");
      return data;
    },
  });
  const agentsQuery = useQuery({
    queryKey: hardwareAgentsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET("/api/hardware/agents", {
        params: { query: { Page: 0, PageSize: pageSize } },
      });
      if (error || !data) throw new Error("Could not load hardware agents.");
      return data.items ?? [];
    },
  });
  const hardwareDevicesQuery = useQuery({
    queryKey: [
      ...hardwareAgentsQueryKey,
      "devices",
      agentsQuery.data?.map((agent) => agent.id).join(","),
    ],
    enabled: Boolean(agentsQuery.data?.length),
    queryFn: async () =>
      (
        await Promise.all(
          (agentsQuery.data ?? []).map(async (agent) => {
            const { data } = await api.GET(
              "/api/hardware/agents/{agentId}/devices",
              { params: { path: { agentId: agent.id } } },
            );
            return data ?? [];
          }),
        )
      ).flat(),
  });
  const workflowDefinitionsQuery = useQuery({
    queryKey: allWorkflowDefinitionsQueryKey,
    queryFn: () => fetchElsaWorkflowDefinitions(auth.user?.access_token),
  });

  const kiosk = kioskQuery.data as KioskWithSession | undefined;
  const profiles = profilesQuery.data ?? [];
  const workflowDefinitions = workflowDefinitionsQuery.data?.items ?? [];
  const selectedWorkflowDefinition = getSelectedWorkflowDefinition(
    workflowDefinitions,
    form.workflowDefinitionId,
  );

  useEffect(() => {
    if (kiosk)
      setForm({
        name: kiosk.name,
        profileId: kiosk.profileId,
        showDetailedErrors: kiosk.showDetailedErrors,
        workflowDefinitionId: kiosk.workflowDefinitionId ?? "",
      });
  }, [kiosk]);
  useEffect(
    () =>
      setDevices(
        (devicesQuery.data as KioskDevice[] | undefined)?.map((item) => ({
          name: item.name,
          type: item.type,
          slotNumber: String(item.slotNumber),
          agentId: item.agentId,
          deviceId: item.deviceId,
          enabled: item.enabled,
          cleanupOnSessionEnd: item.cleanupOnSessionEnd,
          sortOrder: String(item.sortOrder),
        })) ?? [],
      ),
    [devicesQuery.data],
  );

  const updateKiosk = useMutation({
    mutationFn: async () => {
      const { error } = await api.PUT("/api/kiosks/{id}", {
        params: { path: { id: kioskId } },
        body: { name: form.name, profileId: form.profileId, showDetailedErrors: form.showDetailedErrors },
      });
      if (error) throw new Error("Could not save kiosk.");
      const workflow = await api.PUT("/api/kiosks/{id}/workflow", {
        params: { path: { id: kioskId } },
        body: { workflowDefinitionId: form.workflowDefinitionId },
      });
      if (workflow.error) throw new Error("Could not save workflow.");
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      await queryClient.invalidateQueries({
        queryKey: [...kiosksQueryKey, kioskId],
      });
      toast.success("Kiosk saved.");
    },
    onError: () => toast.error("Could not save kiosk."),
  });
  const rotateKey = useMutation({
    mutationFn: async () => {
      const { data, error } = await api.POST("/api/kiosks/{id}/rotate-key", {
        params: { path: { id: kioskId } },
      });
      if (error || !data) throw new Error("Could not rotate key.");
      return data;
    },
    onSuccess: (response) => {
      setLatestKey(response);
      toast.success("Kiosk key rotated. Copy it now.");
    },
    onError: () => toast.error("Could not rotate kiosk key."),
  });
  const setMode = useMutation({
    mutationFn: async (mode: KioskMode) => {
      const path =
        mode === "Active"
          ? "/api/kiosks/{id}/activate"
          : mode === "Maintenance"
            ? "/api/kiosks/{id}/maintenance"
            : "/api/kiosks/{id}/disable";
      const { error } = await api.POST(path, {
        params: { path: { id: kioskId } },
      });
      if (error) throw new Error("Could not update mode.");
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: [...kiosksQueryKey, kioskId],
      });
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      toast.success("Kiosk mode updated.");
    },
    onError: () => toast.error("Could not update kiosk mode."),
  });
  const cancelSession = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST("/api/kiosks/{id}/cancel-session" as never, {
        params: { path: { id: kioskId } },
      } as never);
      if (error) throw new Error("Could not cancel kiosk session.");
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: kiosksQueryKey });
      await queryClient.invalidateQueries({ queryKey: [...kiosksQueryKey, kioskId] });
      toast.success("Kiosk session cancelled.");
    },
    onError: () => toast.error("Could not cancel kiosk session."),
  });
  const saveDevices = useMutation({
    mutationFn: async () => {
      const { error } = await api.PUT("/api/kiosks/{id}/devices", {
        params: { path: { id: kioskId } },
        body: {
          devices: devices.map((row) => ({
            name: row.name,
            type: row.type,
            slotNumber: Number(row.slotNumber || 0),
            agentId: row.agentId,
            deviceId: row.deviceId,
            enabled: row.enabled,
            cleanupOnSessionEnd: row.cleanupOnSessionEnd,
            sortOrder: Number(row.sortOrder || 0),
          })),
        },
      });
      if (error) throw new Error("Could not save device mappings.");
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: [...kiosksQueryKey, kioskId, "devices"],
      });
      toast.success("Device mappings saved.");
    },
    onError: () => toast.error("Could not save device mappings."),
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    updateKiosk.mutate();
  }

  return (
    <section className="grid gap-6">
      <Link
        to="/administration/automation/kiosk"
        className="inline-flex w-fit items-center gap-2 text-[14px] font-medium text-muted-foreground transition hover:text-foreground"
      >
        <ArrowLeft className="size-4" />
        Back to kiosks
      </Link>
      {latestKey ? (
        <KioskKeyPanel
          response={latestKey}
          onClose={() => setLatestKey(null)}
        />
      ) : null}
      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <CardTitle>{kiosk?.name ?? "Kiosk"}</CardTitle>
              <CardDescription>
                Edit kiosk details, workflow assignment, and mode.
              </CardDescription>
            </div>
            {kiosk ? <ModeBadge mode={kiosk.mode} /> : null}
          </div>
        </CardHeader>
        <CardContent className="grid gap-4">
          <form className="grid gap-4" onSubmit={submit}>
            <div className="grid gap-3 md:grid-cols-3">
              <div className="grid gap-2">
                <Label htmlFor={nameId} className="text-[14px] font-medium">
                  Kiosk name
                </Label>
                <Input
                  id={nameId}
                  value={form.name}
                  onChange={(event) =>
                    setForm({ ...form, name: event.target.value })
                  }
                  placeholder="Kiosk name"
                  required
                />
              </div>
              <div className="grid gap-2">
                <Label htmlFor={profileId} className="text-[14px] font-medium">
                  Profile
                </Label>
                <select
                  id={profileId}
                  className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px]"
                  value={form.profileId}
                  onChange={(event) =>
                    setForm({ ...form, profileId: event.target.value })
                  }
                  required
                >
                  {profiles.map((profile) => (
                    <option key={profile.id} value={profile.id}>
                      {profile.name}
                    </option>
                  ))}
                </select>
              </div>
              <WorkflowDefinitionAutocomplete
                inputId={workflowDefinitionId}
                definitions={workflowDefinitions}
                selectedDefinition={selectedWorkflowDefinition}
                unavailableDefinitionId={
                  form.workflowDefinitionId &&
                  !selectedWorkflowDefinition?.definitionId
                    ? form.workflowDefinitionId
                    : null
                }
                disabled={
                  workflowDefinitionsQuery.isLoading ||
                  workflowDefinitionsQuery.isError
                }
                onChange={(definition) =>
                  setForm({
                    ...form,
                    workflowDefinitionId: definition?.definitionId ?? "",
                  })
                }
              />
            </div>
            <label className="flex items-center gap-3 rounded-interactive border border-border px-3 py-2 text-[14px] font-medium text-foreground">
              <input
                type="checkbox"
                checked={form.showDetailedErrors}
                onChange={(event) =>
                  setForm({ ...form, showDetailedErrors: event.target.checked })
                }
              />
              Show detailed errors on kiosk
            </label>
            {workflowDefinitionsQuery.isError ? (
              <p className="text-[12px] text-error">
                Could not load workflow definitions.
              </p>
            ) : null}
            <div className="flex flex-wrap justify-end gap-2">
              <Button type="submit">
                <Save className="size-4" />
                Save kiosk
              </Button>
            </div>
          </form>
          <div className="flex flex-wrap gap-2 rounded-structural border border-border p-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => rotateKey.mutate()}
            >
              <RotateCcw className="size-4" />
              Rotate key
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => setMode.mutate("Active")}
            >
              Activate
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => setMode.mutate("Maintenance")}
            >
              Maintenance
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => setMode.mutate("Disabled")}
            >
              Disable
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={!kiosk?.activeSessionId || cancelSession.isPending}
              onClick={() => cancelSession.mutate()}
            >
              Cancel session
            </Button>
          </div>
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <CardTitle>Device mappings</CardTitle>
              <CardDescription>
                Map workflow device slots to this kiosk's physical hardware.
              </CardDescription>
            </div>
            <Button
              type="button"
              variant="outline"
              onClick={() =>
                setDevices([
                  ...devices,
                  {
                    name: "",
                    type: "QrReader",
                    slotNumber: String(nextSlotNumber(devices, "QrReader")),
                    agentId: "",
                    deviceId: "",
                    enabled: true,
                    cleanupOnSessionEnd: false,
                    sortOrder: String(devices.length),
                  },
                ])
              }
            >
              <Plus className="size-4" />
              Add mapping
            </Button>
          </div>
        </CardHeader>
        <CardContent className="grid gap-3">
          {devices.map((row, index) => (
            <DeviceRowEditor
              key={index}
              row={row}
              agents={agentsQuery.data ?? []}
              hardwareDevices={hardwareDevicesQuery.data ?? []}
              onChange={(next) => setDevices(replaceAt(devices, index, next))}
              onRemove={() =>
                setDevices(devices.filter((_, rowIndex) => rowIndex !== index))
              }
            />
          ))}
          <div className="flex justify-end">
            <Button type="button" onClick={() => saveDevices.mutate()}>
              <Save className="size-4" />
              Save mappings
            </Button>
          </div>
        </CardContent>
      </Card>
    </section>
  );
}

function WorkflowDefinitionAutocomplete({
  inputId,
  definitions,
  selectedDefinition,
  unavailableDefinitionId,
  disabled,
  onChange,
}: {
  readonly inputId: string;
  readonly definitions: WorkflowDefinition[];
  readonly selectedDefinition: WorkflowDefinition | null;
  readonly unavailableDefinitionId: string | null;
  readonly disabled: boolean;
  readonly onChange: (definition: WorkflowDefinition | null) => void;
}) {
  const anchorRef = useRef<HTMLDivElement | null>(null);

  return (
    <div className="grid gap-2">
      <Label htmlFor={inputId} className="text-[14px] font-medium">
        Workflow definition
      </Label>
      <div ref={anchorRef}>
        <Combobox
          value={selectedDefinition}
          onValueChange={onChange}
          items={definitions}
          itemToStringLabel={getWorkflowDefinitionLabel}
        >
          <ComboboxInput
            id={inputId}
            placeholder={
              disabled
                ? "Workflow definitions unavailable"
                : "Search workflow definitions..."
            }
            showClear
            disabled={disabled}
          />
          <ComboboxContent anchor={anchorRef.current}>
            <ComboboxEmpty>
              {disabled
                ? "Workflow definitions unavailable."
                : "No workflow definitions found."}
            </ComboboxEmpty>
            <ComboboxList>
              {(definition) => (
                <ComboboxItem
                  key={
                    definition.definitionId ?? definition.id ?? definition.name
                  }
                  value={definition}
                >
                  <div className="min-w-0">
                    <p className="truncate font-medium text-foreground">
                      {getWorkflowDefinitionLabel(definition)}
                    </p>
                    <p className="truncate text-[12px] text-muted-foreground">
                      {definition.description ||
                        definition.definitionId ||
                        "No description"}
                    </p>
                  </div>
                </ComboboxItem>
              )}
            </ComboboxList>
          </ComboboxContent>
        </Combobox>
      </div>
      {unavailableDefinitionId ? (
        <p className="text-[12px] text-muted-foreground">
          Current workflow is no longer available:{" "}
          <code>{unavailableDefinitionId}</code>
        </p>
      ) : null}
    </div>
  );
}

function DeviceRowEditor({
  row,
  agents,
  hardwareDevices,
  onChange,
  onRemove,
}: {
  readonly row: DeviceRow;
  readonly agents: HardwareAgent[];
  readonly hardwareDevices: HardwareDevice[];
  readonly onChange: (row: DeviceRow) => void;
  readonly onRemove: () => void;
}) {
  const nameId = useId();
  const typeId = useId();
  const slotNumberId = useId();
  const agentId = useId();
  const deviceId = useId();
  const agentDevices = hardwareDevices.filter(
    (device) => !row.agentId || device.agentId === row.agentId,
  );
  const selectedDevice = hardwareDevices.find(
    (device) =>
      device.agentId === row.agentId && device.deviceId === row.deviceId,
  );
  const requiredCapability = capabilityByType[row.type];
  const mismatch =
    selectedDevice && !selectedDevice.capabilities.includes(requiredCapability);
  return (
    <div className="grid gap-3 rounded-interactive border border-border p-3">
      <div className="rounded-interactive bg-hover-gray px-3 py-2 text-[12px] text-muted-foreground">
        Elsa reference:{" "}
        <code>
          {row.type} slot {row.slotNumber || "?"}
        </code>
      </div>
      <div className="grid gap-3 md:grid-cols-3">
        <div className="grid gap-2">
          <Label htmlFor={nameId} className="text-[14px] font-medium">
            Display name
          </Label>
          <Input
            id={nameId}
            value={row.name}
            onChange={(event) => onChange({ ...row, name: event.target.value })}
            placeholder="Display name"
          />
        </div>
        <div className="grid gap-2">
          <Label htmlFor={typeId} className="text-[14px] font-medium">
            Device type
          </Label>
          <select
            id={typeId}
            className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px]"
            value={row.type}
            onChange={(event) =>
              onChange({ ...row, type: event.target.value as KioskDeviceType })
            }
          >
            {deviceTypes.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </div>
        <div className="grid gap-2">
          <Label htmlFor={slotNumberId} className="text-[14px] font-medium">
            Slot number
          </Label>
          <Input
            id={slotNumberId}
            type="number"
            min={1}
            value={row.slotNumber}
            onChange={(event) =>
              onChange({ ...row, slotNumber: event.target.value })
            }
            placeholder="Slot"
          />
        </div>
      </div>
      <div className="grid gap-3 md:grid-cols-2">
        <div className="grid gap-2">
          <Label htmlFor={agentId} className="text-[14px] font-medium">
            Hardware agent
          </Label>
          <select
            id={agentId}
            className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px]"
            value={row.agentId}
            onChange={(event) =>
              onChange({ ...row, agentId: event.target.value, deviceId: "" })
            }
          >
            <option value="">Select agent</option>
            {agents.map((agent) => (
              <option key={agent.id} value={agent.id}>
                {agent.name}
              </option>
            ))}
          </select>
        </div>
        <div className="grid gap-2">
          <Label htmlFor={deviceId} className="text-[14px] font-medium">
            Hardware device
          </Label>
          <select
            id={deviceId}
            className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px]"
            value={row.deviceId}
            onChange={(event) =>
              onChange({ ...row, deviceId: event.target.value })
            }
          >
            <option value="">Select device</option>
            {agentDevices.map((device) => (
              <option
                key={`${device.agentId}:${device.deviceId}`}
                value={device.deviceId}
              >
                {device.deviceId} ({device.kind})
              </option>
            ))}
          </select>
        </div>
      </div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap gap-3">
          <label className="flex items-center gap-2 text-[13px]">
            <input
              type="checkbox"
              checked={row.enabled}
              onChange={(event) =>
                onChange({ ...row, enabled: event.target.checked })
              }
            />
            Enabled
          </label>
          <label className="flex items-center gap-2 text-[13px]">
            <input
              type="checkbox"
              disabled={row.type !== "Collector"}
              checked={row.cleanupOnSessionEnd}
              onChange={(event) =>
                onChange({ ...row, cleanupOnSessionEnd: event.target.checked })
              }
            />
            Cleanup on session end
          </label>
        </div>
        <Button type="button" variant="ghost" size="sm" onClick={onRemove}>
          <Trash2 className="size-4" />
          Remove
        </Button>
      </div>
      <p
        className={`text-[13px] ${mismatch ? "text-error" : "text-muted-foreground"}`}
      >
        Requires {requiredCapability}
        {selectedDevice
          ? `; device has ${selectedDevice.capabilities.join(", ")}`
          : ""}
      </p>
    </div>
  );
}

function KioskKeyPanel({
  response,
  onClose,
}: {
  readonly response: KioskKeyResponse;
  readonly onClose: () => void;
}) {
  async function copyKey() {
    await navigator.clipboard.writeText(response.apiKey);
    toast.success("Kiosk key copied.");
  }
  return (
    <div className="rounded-structural border border-success bg-success-background p-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <div className="flex items-center gap-2 text-[14px] font-semibold">
            <KeyRound className="size-4" />
            New key for {response.kiosk.name}
          </div>
          <p className="mt-2 text-[14px] text-muted-foreground">
            Copy this key now. It is shown once.
          </p>
          <code className="mt-3 block overflow-x-auto rounded-interactive border border-border bg-content px-3 py-2 text-[13px]">
            {response.apiKey}
          </code>
        </div>
        <div className="flex gap-2">
          <Button type="button" variant="outline" onClick={copyKey}>
            <Copy className="size-4" />
            Copy
          </Button>
          <Button type="button" variant="ghost" onClick={onClose}>
            Dismiss
          </Button>
        </div>
      </div>
    </div>
  );
}
function ModeBadge({ mode }: { readonly mode: KioskMode }) {
  const variant: BadgeVariant =
    mode === "Active"
      ? "success"
      : mode === "Maintenance"
        ? "warning"
        : "secondary";
  return <Badge variant={variant}>{mode}</Badge>;
}
function replaceAt<T>(items: T[], index: number, item: T) {
  return items.map((current, currentIndex) =>
    currentIndex === index ? item : current,
  );
}
function nextSlotNumber(devices: DeviceRow[], type: KioskDeviceType) {
  return (
    Math.max(
      0,
      ...devices
        .filter((device) => device.type === type)
        .map((device) => Number(device.slotNumber) || 0),
    ) + 1
  );
}
async function fetchElsaWorkflowDefinitions(accessToken: string | undefined) {
  const response = await fetch(`${apiBaseUrl}/elsa/api/workflow-definitions?versionOptions=Published`, {
    headers: {
      Accept: "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
    },
  });

  if (!response.ok) {
    throw new Error(`Elsa API request failed with status ${response.status}.`);
  }

  return (await response.json()) as components["schemas"]["PagedListResponseOfLinkedWorkflowDefinitionSummary"];
}
function getWorkflowDefinitionLabel(definition: WorkflowDefinition | null) {
  return definition?.name || definition?.definitionId || "Untitled workflow";
}
function getSelectedWorkflowDefinition(
  definitions: WorkflowDefinition[],
  workflowDefinitionId: string,
) {
  if (!workflowDefinitionId) return null;
  return (
    definitions.find(
      (definition) => definition.definitionId === workflowDefinitionId,
    ) ?? {
      definitionId: workflowDefinitionId,
      name: workflowDefinitionId,
      description: "Workflow definition no longer available.",
    }
  );
}
