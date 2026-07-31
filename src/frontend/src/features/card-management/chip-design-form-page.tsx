import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft, Plus, Trash2 } from 'lucide-react';
import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Textarea } from '@/shared/components/ui/textarea';

import { chipDesignsQueryKey, fileModes, keyGroupsQueryKey, type ChipDesignRequest, type FileMode, type FileSpecification, type TemplateSpecification } from './card-management-types';

type KeyRefValues = { keyGroup: string; keySet: string; key: string };
type KeySettingsValues = { changeable: boolean; masterKeyChangeable: boolean; freeDirectoryListing: boolean; allowCreateDelete: boolean };
type PiccValues = {
  useKey: boolean;
  key: KeyRefValues;
  allowCreateDelete: boolean;
  keySettings: KeySettingsValues & { allowDamKeys: boolean };
  piccSettings: { enableLegacyRandomId: boolean; isoVirtualCardMandatory: boolean; proximityCheckMandatory: boolean; randomIdEnabled: boolean; disableCardFormat: boolean };
  secureMessaging: SecureMessagingValues;
};
type SecureMessagingValues = { disableD40: boolean; disableEv1: boolean; disableEv2Chaining: boolean };
type ApplicationRow = { aid: string; isoDfName: string; keyGroup: string; use2BytesFileIdentifier: boolean; keySettings: KeySettingsValues & { changeKey: string }; secureMessaging: SecureMessagingValues; files: FileRow[] };
type EncodingMode = 'text' | 'hex' | 'uint-be' | 'uint-le' | 'custom';
type FileRow = { id: string; mode: FileMode; variable: string; size: string; dataOffsetBytes: string; dataLengthBytes: string; encodingMode: EncodingMode; integerLength: string; customEncoding: string; readKey: string; writeKey: string; readWriteKey: string; changeKey: string };
type SpecificationValues = { picc: PiccValues; applications: ApplicationRow[] };
type FormValues = { name: string; version: string; description: string; specification: SpecificationValues };

const defaultKeySettings: KeySettingsValues = { changeable: true, masterKeyChangeable: true, freeDirectoryListing: true, allowCreateDelete: true };
const defaultSecureMessaging: SecureMessagingValues = { disableD40: false, disableEv1: false, disableEv2Chaining: false };
const defaultPiccSettings = { enableLegacyRandomId: false, isoVirtualCardMandatory: false, proximityCheckMandatory: false, randomIdEnabled: false, disableCardFormat: false };

const emptyValues: FormValues = {
  name: '',
  version: '',
  description: '',
  specification: {
    picc: {
      useKey: false,
      key: { keyGroup: '', keySet: '0', key: '0' },
      allowCreateDelete: false,
      keySettings: { ...defaultKeySettings, allowDamKeys: true },
      piccSettings: defaultPiccSettings,
      secureMessaging: defaultSecureMessaging,
    },
    applications: [],
  },
};

export function ChipDesignCreatePage() {
  return <ChipDesignFormPage mode="create" />;
}

export default function ChipDesignEditPage() {
  const { chipDesignId } = useParams({ from: '/desfire-studio/chip-designs/$chipDesignId/edit' });
  return <ChipDesignEditPageContent chipDesignId={chipDesignId} />;
}

export function ChipDesignEditPageContent({ chipDesignId }: { readonly chipDesignId: string }) {
  return <ChipDesignFormPage mode="edit" chipDesignId={chipDesignId} />;
}

function ChipDesignFormPage({ mode, chipDesignId }: { readonly mode: 'create' | 'edit'; readonly chipDesignId?: string }) {
  const queryClient = useQueryClient();
  const [values, setValues] = useState<FormValues>(emptyValues);
  const [validationError, setValidationError] = useState<string | null>(null);

  const chipDesignQuery = useQuery({
    queryKey: [...chipDesignsQueryKey, chipDesignId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/chip-designs/{id}', { params: { path: { id: chipDesignId ?? '' } } });
      if (error || !data) {
        throw new Error('Could not load chip design.');
      }
      return data;
    },
    enabled: mode === 'edit' && !!chipDesignId,
  });

  const keyGroupsQuery = useQuery({
    queryKey: keyGroupsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/key-groups', { params: { query: { Page: 0, PageSize: 100 } } });
      if (error) {
        throw new Error('Could not load key groups.');
      }
      return data;
    },
  });

  useEffect(() => {
    if (!chipDesignQuery.data) {
      return;
    }

    setValues({
      name: chipDesignQuery.data.name,
      version: String(chipDesignQuery.data.version),
      description: chipDesignQuery.data.description ?? '',
      specification: fromSpecification(chipDesignQuery.data.specification),
    });
  }, [chipDesignQuery.data]);

  const saveChipDesign = useMutation({
    mutationFn: async (request: ChipDesignRequest) => {
      if (mode === 'create') {
        const { error } = await api.POST('/api/desfire/chip-designs', { body: request });
        if (error) {
          throw new Error('Could not add chip design.');
        }
        return;
      }

      const { error } = await api.PUT('/api/desfire/chip-designs/{id}', { params: { path: { id: chipDesignId ?? '' } }, body: { ...request, version: Number(values.version) } });
      if (error) {
        throw new Error('Could not update chip design.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: chipDesignsQueryKey });
      if (chipDesignId) {
        await queryClient.invalidateQueries({ queryKey: [...chipDesignsQueryKey, chipDesignId] });
      }
      toast.success(mode === 'create' ? 'Chip design added.' : 'Chip design updated.');
      window.history.back();
    },
    onError: () => toast.error(mode === 'create' ? 'Could not add chip design.' : 'Could not update chip design.'),
  });

  const keyGroupNames = (keyGroupsQuery.data?.items ?? []).map((group) => group.name).sort((left, right) => left.localeCompare(right));
  const jsonPreview = JSON.stringify(toSpecification(values.specification), null, 2);

  function updateValue<TKey extends keyof FormValues>(key: TKey, value: FormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  function updateSpecification(specification: SpecificationValues) {
    updateValue('specification', specification);
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const error = validateSpecification(values.specification, mode, values.version);
    if (error) {
      setValidationError(error);
      return;
    }

    setValidationError(null);
    saveChipDesign.mutate({
      name: values.name,
      version: mode === 'create' && values.version.trim() === '' ? null : Number(values.version),
      description: values.description || null,
      specification: toSpecification(values.specification),
    });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">{mode === 'create' ? 'Add chip design' : values.name || 'Edit chip design'}</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Build a DESFire template with guided PICC, application, and file settings. JSON is generated from the form.</p>
        </div>
      </header>

      {chipDesignQuery.isError ? <PanelError>Could not load chip design.</PanelError> : null}
      {keyGroupsQuery.isError ? <PanelError>Could not load key groups for dropdowns.</PanelError> : null}
      {validationError ? <PanelError>{validationError}</PanelError> : null}

      <Card className="p-4 sm:p-6">
        {chipDesignQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading chip design...</p> : null}
        {mode === 'create' || chipDesignQuery.data ? (
          <form className="grid gap-5" onSubmit={handleSubmit}>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium">
                Name
                <Input value={values.name} onChange={(event) => updateValue('name', event.target.value)} required />
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                Version
                <Input value={values.version} type="number" min={1} placeholder={mode === 'create' ? 'Auto' : undefined} onChange={(event) => updateValue('version', event.target.value)} required={mode === 'edit'} />
              </label>
              <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
                Description
                <Input value={values.description} onChange={(event) => updateValue('description', event.target.value)} />
              </label>
            </div>

            <PiccEditor value={values.specification.picc} keyGroupNames={keyGroupNames} onChange={(picc) => updateSpecification({ ...values.specification, picc })} />
            <ApplicationsEditor value={values.specification.applications} keyGroupNames={keyGroupNames} onChange={(applications) => updateSpecification({ ...values.specification, applications })} />
            <JsonPreview value={jsonPreview} />

            <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <Button type="button" variant="outline" onClick={() => window.history.back()}>Cancel</Button>
              <Button type="submit" disabled={saveChipDesign.isPending}>{saveChipDesign.isPending ? 'Saving...' : 'Save chip design'}</Button>
            </div>
          </form>
        ) : null}
      </Card>
    </div>
  );
}

function PiccEditor({ value, keyGroupNames, onChange }: { readonly value: PiccValues; readonly keyGroupNames: string[]; readonly onChange: (value: PiccValues) => void }) {
  return (
    <section className="grid gap-4 rounded-structural border border-border p-4">
      <div>
        <h3 className="text-[16px] font-semibold tracking-tight">PICC</h3>
        <p className="mt-1 text-[14px] text-muted-foreground">Card-level key, key settings, and configuration.</p>
      </div>

      <Checkbox label="Use PICC key" checked={value.useKey} onChange={(useKey) => onChange({ ...value, useKey })} />
      {value.useKey ? <KeyRefEditor value={value.key} keyGroupNames={keyGroupNames} onChange={(key) => onChange({ ...value, key })} /> : null}

      <Checkbox label="Allow create/delete" checked={value.allowCreateDelete} onChange={(allowCreateDelete) => onChange({ ...value, allowCreateDelete })} />
      <KeySettingsEditor title="PICC key settings" value={value.keySettings} onChange={(keySettings) => onChange({ ...value, keySettings })} extra={<Checkbox label="Allow DAM keys" checked={value.keySettings.allowDamKeys} onChange={(allowDamKeys) => onChange({ ...value, keySettings: { ...value.keySettings, allowDamKeys } })} />} />
      <SwitchGrid title="PICC settings" values={[
        ['Enable legacy random ID', value.piccSettings.enableLegacyRandomId, (checked) => onChange({ ...value, piccSettings: { ...value.piccSettings, enableLegacyRandomId: checked } })],
        ['ISO virtual card mandatory', value.piccSettings.isoVirtualCardMandatory, (checked) => onChange({ ...value, piccSettings: { ...value.piccSettings, isoVirtualCardMandatory: checked } })],
        ['Proximity check mandatory', value.piccSettings.proximityCheckMandatory, (checked) => onChange({ ...value, piccSettings: { ...value.piccSettings, proximityCheckMandatory: checked } })],
        ['Random ID enabled', value.piccSettings.randomIdEnabled, (checked) => onChange({ ...value, piccSettings: { ...value.piccSettings, randomIdEnabled: checked } })],
        ['Disable card format', value.piccSettings.disableCardFormat, (checked) => onChange({ ...value, piccSettings: { ...value.piccSettings, disableCardFormat: checked } })],
      ]} />
      <SecureMessagingEditor value={value.secureMessaging} onChange={(secureMessaging) => onChange({ ...value, secureMessaging })} />
    </section>
  );
}

function ApplicationsEditor({ value, keyGroupNames, onChange }: { readonly value: ApplicationRow[]; readonly keyGroupNames: string[]; readonly onChange: (value: ApplicationRow[]) => void }) {
  function updateApplication(index: number, application: ApplicationRow) {
    onChange(value.map((current, currentIndex) => currentIndex === index ? application : current));
  }

  return (
    <section className="grid gap-4 rounded-structural border border-border p-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="text-[16px] font-semibold tracking-tight">Applications</h3>
          <p className="mt-1 text-[14px] text-muted-foreground">Application dictionary keys are derived from AID and hidden.</p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={() => onChange([...value, createApplication()])}>
          <Plus className="size-4" aria-hidden="true" />Add application
        </Button>
      </div>

      {value.length === 0 ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Blank template: no applications.</p> : null}
      {value.map((application, index) => (
        <div key={index} className="grid gap-4 rounded-structural border border-border p-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-wrap items-center gap-2">
              <h4 className="text-[15px] font-semibold tracking-tight">Application {application.aid || index + 1}</h4>
              {application.aid ? <Badge variant="outline">AID {application.aid}</Badge> : null}
            </div>
            <Button type="button" variant="outline" size="sm" onClick={() => onChange(value.filter((_, currentIndex) => currentIndex !== index))}>
              <Trash2 className="size-4" aria-hidden="true" />Remove
            </Button>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              AID hex
              <Input value={application.aid} pattern="[0-9a-fA-F]+" onChange={(event) => updateApplication(index, { ...application, aid: event.target.value.toUpperCase() })} required />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              ISO DF name
              <Input value={application.isoDfName} onChange={(event) => updateApplication(index, { ...application, isoDfName: event.target.value })} />
            </label>
            <KeyGroupSelect value={application.keyGroup} keyGroupNames={keyGroupNames} onChange={(keyGroup) => updateApplication(index, { ...application, keyGroup })} />
            <Checkbox label="Use 2-byte file identifiers" checked={application.use2BytesFileIdentifier} onChange={(use2BytesFileIdentifier) => updateApplication(index, { ...application, use2BytesFileIdentifier })} />
          </div>

          <KeySettingsEditor title="Application key settings" value={application.keySettings} onChange={(keySettings) => updateApplication(index, { ...application, keySettings })} extra={<label className="grid gap-2 text-[14px] font-medium"><span>Change key ID</span><Input value={application.keySettings.changeKey} type="number" min={0} onChange={(event) => updateApplication(index, { ...application, keySettings: { ...application.keySettings, changeKey: event.target.value } })} required /></label>} />
          <SecureMessagingEditor value={application.secureMessaging} onChange={(secureMessaging) => updateApplication(index, { ...application, secureMessaging })} />
          <FilesEditor value={application.files} onChange={(files) => updateApplication(index, { ...application, files })} />
        </div>
      ))}
    </section>
  );
}

function FilesEditor({ value, onChange }: { readonly value: FileRow[]; readonly onChange: (value: FileRow[]) => void }) {
  function updateFile(index: number, file: FileRow) {
    onChange(value.map((current, currentIndex) => currentIndex === index ? file : current));
  }

  return (
    <section className="grid gap-3">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h5 className="text-[14px] font-semibold tracking-tight">Files</h5>
          <p className="mt-1 text-[13px] text-muted-foreground">File dictionary keys are derived from file ID.</p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={() => onChange([...value, createFile()])}>
          <Plus className="size-4" aria-hidden="true" />Add file
        </Button>
      </div>

      {value.length === 0 ? <p className="rounded-structural border border-border p-3 text-[14px] text-muted-foreground">No files in this application.</p> : null}
      {value.map((file, index) => (
        <div key={index} className="grid gap-4 rounded-interactive border border-border p-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-wrap items-center gap-2">
              <h6 className="text-[14px] font-semibold tracking-tight">File {file.id || index + 1}</h6>
              {file.id ? <Badge variant="outline">ID {file.id}</Badge> : null}
            </div>
            <Button type="button" variant="outline" size="sm" onClick={() => onChange(value.filter((_, currentIndex) => currentIndex !== index))}>
              <Trash2 className="size-4" aria-hidden="true" />Remove
            </Button>
          </div>
          <div className="grid gap-4 md:grid-cols-3">
            <label className="grid gap-2 text-[14px] font-medium"><span>File ID</span><Input value={file.id} type="number" min={1} max={50} onChange={(event) => updateFile(index, { ...file, id: event.target.value })} required /></label>
            <label className="grid gap-2 text-[14px] font-medium"><span>Mode</span><select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={file.mode} onChange={(event) => updateFile(index, { ...file, mode: event.target.value as FileMode })}>{fileModes.map((mode) => <option key={mode} value={mode}>{mode}</option>)}</select></label>
            <label className="grid gap-2 text-[14px] font-medium"><span>Variable</span><Input value={file.variable} onChange={(event) => updateFile(index, { ...file, variable: event.target.value })} required /></label>
            <label className="grid gap-2 text-[14px] font-medium"><span>Size bytes</span><Input value={file.size} type="number" min={0} onChange={(event) => updateFile(index, { ...file, size: event.target.value })} required /></label>
            <label className="grid gap-2 text-[14px] font-medium"><span>Data offset bytes</span><Input value={file.dataOffsetBytes} type="number" min={0} onChange={(event) => updateFile(index, { ...file, dataOffsetBytes: event.target.value })} required /></label>
            <label className="grid gap-2 text-[14px] font-medium"><span>Data length bytes</span><Input value={file.dataLengthBytes} type="number" min={0} onChange={(event) => updateFile(index, { ...file, dataLengthBytes: event.target.value })} required /></label>
          </div>
          <EncodingEditor value={file} onChange={(next) => updateFile(index, next)} />
          <div className="grid gap-4 md:grid-cols-4">
            <KeyNumber label="Read key ID" value={file.readKey} onChange={(readKey) => updateFile(index, { ...file, readKey })} />
            <KeyNumber label="Write key ID" value={file.writeKey} onChange={(writeKey) => updateFile(index, { ...file, writeKey })} />
            <KeyNumber label="Read/write key ID" value={file.readWriteKey} onChange={(readWriteKey) => updateFile(index, { ...file, readWriteKey })} />
            <KeyNumber label="Change key ID" value={file.changeKey} onChange={(changeKey) => updateFile(index, { ...file, changeKey })} />
          </div>
        </div>
      ))}
    </section>
  );
}

function EncodingEditor({ value, onChange }: { readonly value: FileRow; readonly onChange: (value: FileRow) => void }) {
  return (
    <div className="grid gap-4 md:grid-cols-3">
      <label className="grid gap-2 text-[14px] font-medium">
        Encoding
        <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={value.encodingMode} onChange={(event) => onChange({ ...value, encodingMode: event.target.value as EncodingMode })}>
          <option value="text">Text</option>
          <option value="hex">Hex</option>
          <option value="uint-be">Unsigned integer, big-endian</option>
          <option value="uint-le">Unsigned integer, little-endian</option>
          <option value="custom">Custom</option>
        </select>
      </label>
      {value.encodingMode === 'uint-be' || value.encodingMode === 'uint-le' ? <label className="grid gap-2 text-[14px] font-medium"><span>Integer byte length</span><Input value={value.integerLength} type="number" min={1} onChange={(event) => onChange({ ...value, integerLength: event.target.value })} required /></label> : null}
      {value.encodingMode === 'custom' ? <label className="grid gap-2 text-[14px] font-medium md:col-span-2"><span>Custom encoding</span><Input value={value.customEncoding} onChange={(event) => onChange({ ...value, customEncoding: event.target.value })} required /></label> : null}
    </div>
  );
}

function KeyRefEditor({ value, keyGroupNames, onChange }: { readonly value: KeyRefValues; readonly keyGroupNames: string[]; readonly onChange: (value: KeyRefValues) => void }) {
  return (
    <div className="grid gap-4 md:grid-cols-3">
      <KeyGroupSelect value={value.keyGroup} keyGroupNames={keyGroupNames} onChange={(keyGroup) => onChange({ ...value, keyGroup })} />
      <label className="grid gap-2 text-[14px] font-medium"><span>Key set ID</span><Input value={value.keySet} type="number" min={0} onChange={(event) => onChange({ ...value, keySet: event.target.value })} required /></label>
      <KeyNumber label="Key ID" value={value.key} onChange={(key) => onChange({ ...value, key })} />
    </div>
  );
}

function KeyGroupSelect({ value, keyGroupNames, onChange }: { readonly value: string; readonly keyGroupNames: string[]; readonly onChange: (value: string) => void }) {
  return (
    <label className="grid gap-2 text-[14px] font-medium">
      Key group
      <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={value} onChange={(event) => onChange(event.target.value)} required>
        <option value="">Select key group</option>
        {keyGroupNames.map((name) => <option key={name} value={name}>{name}</option>)}
      </select>
    </label>
  );
}

function KeyNumber({ label, value, onChange }: { readonly label: string; readonly value: string; readonly onChange: (value: string) => void }) {
  return <label className="grid gap-2 text-[14px] font-medium"><span>{label}</span><Input value={value} type="number" min={0} onChange={(event) => onChange(event.target.value)} required /></label>;
}

function KeySettingsEditor<TValue extends KeySettingsValues>({ title, value, onChange, extra }: { readonly title: string; readonly value: TValue; readonly onChange: (value: TValue) => void; readonly extra?: ReactNode }) {
  return (
    <section className="grid gap-3 rounded-interactive border border-border p-3">
      <h4 className="text-[14px] font-semibold tracking-tight">{title}</h4>
      <div className="grid gap-3 md:grid-cols-2">
        <Checkbox label="Changeable" checked={value.changeable} onChange={(changeable) => onChange({ ...value, changeable })} />
        <Checkbox label="Master key changeable" checked={value.masterKeyChangeable} onChange={(masterKeyChangeable) => onChange({ ...value, masterKeyChangeable })} />
        <Checkbox label="Free directory listing" checked={value.freeDirectoryListing} onChange={(freeDirectoryListing) => onChange({ ...value, freeDirectoryListing })} />
        <Checkbox label="Allow create/delete" checked={value.allowCreateDelete} onChange={(allowCreateDelete) => onChange({ ...value, allowCreateDelete })} />
        {extra}
      </div>
    </section>
  );
}

function SecureMessagingEditor({ value, onChange }: { readonly value: SecureMessagingValues; readonly onChange: (value: SecureMessagingValues) => void }) {
  return <SwitchGrid title="Secure messaging" values={[
    ['Disable D40', value.disableD40, (checked) => onChange({ ...value, disableD40: checked })],
    ['Disable EV1', value.disableEv1, (checked) => onChange({ ...value, disableEv1: checked })],
    ['Disable EV2 chaining', value.disableEv2Chaining, (checked) => onChange({ ...value, disableEv2Chaining: checked })],
  ]} />;
}

function SwitchGrid({ title, values }: { readonly title: string; readonly values: readonly (readonly [string, boolean, (checked: boolean) => void])[] }) {
  return (
    <section className="grid gap-3 rounded-interactive border border-border p-3">
      <h4 className="text-[14px] font-semibold tracking-tight">{title}</h4>
      <div className="grid gap-3 md:grid-cols-2">
        {values.map(([label, checked, onChange]) => <Checkbox key={label} label={label} checked={checked} onChange={onChange} />)}
      </div>
    </section>
  );
}

function Checkbox({ label, checked, onChange }: { readonly label: string; readonly checked: boolean; readonly onChange: (checked: boolean) => void }) {
  return <label className="flex min-h-9 items-center gap-2 rounded-interactive border border-border px-3 py-2 text-[14px] font-medium"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} />{label}</label>;
}

function JsonPreview({ value }: { readonly value: string }) {
  return (
    <section className="grid gap-3 rounded-structural border border-border p-4">
      <div>
        <h3 className="text-[16px] font-semibold tracking-tight">Read-only JSON</h3>
        <p className="mt-1 text-[14px] text-muted-foreground">Generated from the form. Dictionary keys are derived from AID and file ID.</p>
      </div>
      <Textarea value={value} className="min-h-[22rem] font-mono text-[13px]" spellCheck={false} readOnly />
    </section>
  );
}

function fromSpecification(specification: TemplateSpecification): SpecificationValues {
  const picc = specification.picc ?? {};
  return {
    picc: {
      useKey: !!picc.key,
      key: { keyGroup: picc.key?.keyGroup ?? '', keySet: String(picc.key?.keySet ?? 0), key: String(picc.key?.key ?? 0) },
      allowCreateDelete: picc.allowCreateDelete ?? false,
      keySettings: { ...defaultKeySettings, ...picc.keySettings, allowDamKeys: picc.keySettings?.allowDamKeys ?? true },
      piccSettings: { ...defaultPiccSettings, ...picc.config?.piccSettings },
      secureMessaging: { ...defaultSecureMessaging, ...picc.config?.secureMessaging },
    },
    applications: Object.values(specification.applications ?? {}).map((application) => ({
      aid: application.aid ?? '',
      isoDfName: application.isoDfName ?? '',
      keyGroup: application.keyGroup ?? '',
      use2BytesFileIdentifier: application.use2BytesFileIdentifier ?? false,
      keySettings: { ...defaultKeySettings, ...application.keySettings, changeKey: application.keySettings?.changeKey ?? '0' },
      secureMessaging: { ...defaultSecureMessaging, ...application.secureMessing },
      files: Object.values(application.files ?? {}).map(fromFileSpecification),
    })),
  };
}

function fromFileSpecification(file: FileSpecification): FileRow {
  const encoding = parseEncoding(file.encoding ?? 'text');
  return {
    id: String(file.id ?? ''),
    mode: file.mode ?? 'Plain',
    variable: file.variable ?? '',
    size: String(file.size ?? 0),
    dataOffsetBytes: String(file.dataOffsetBytes ?? 0),
    dataLengthBytes: String(file.dataLengthBytes ?? 0),
    encodingMode: encoding.mode,
    integerLength: encoding.integerLength,
    customEncoding: encoding.customEncoding,
    readKey: file.readKey ?? '0',
    writeKey: file.writeKey ?? '0',
    readWriteKey: file.readWriteKey ?? '0',
    changeKey: file.changeKey ?? '0',
  };
}

function toSpecification(values: SpecificationValues): TemplateSpecification {
  return {
    picc: {
      key: values.picc.useKey ? { keyGroupName: values.picc.key.keyGroup, keyGroup: values.picc.key.keyGroup, keySet: Number(values.picc.key.keySet), key: Number(values.picc.key.key) } : null,
      allowCreateDelete: values.picc.allowCreateDelete,
      keySettings: values.picc.keySettings,
      config: { piccSettings: values.picc.piccSettings, secureMessaging: values.picc.secureMessaging },
    },
    applications: Object.fromEntries(values.applications.map((application) => [application.aid.toUpperCase(), {
      aid: application.aid.toUpperCase(),
      isoDfName: application.isoDfName,
      keyGroupName: application.keyGroup,
      keyGroup: application.keyGroup,
      keySettings: application.keySettings,
      secureMessing: application.secureMessaging,
      use2BytesFileIdentifier: application.use2BytesFileIdentifier,
      files: Object.fromEntries(application.files.map((file) => [String(Number(file.id)), {
        id: Number(file.id),
        mode: file.mode,
        variable: file.variable,
        size: Number(file.size),
        dataOffsetBytes: Number(file.dataOffsetBytes),
        dataLengthBytes: Number(file.dataLengthBytes),
        encoding: formatEncoding(file),
        readKey: file.readKey,
        writeKey: file.writeKey,
        readWriteKey: file.readWriteKey,
        changeKey: file.changeKey,
      }]))
    }])),
  };
}

function validateSpecification(values: SpecificationValues, mode: 'create' | 'edit', version: string) {
  if (mode === 'edit' && Number(version) < 1) {
    return 'Version must be at least 1.';
  }

  if (values.picc.useKey && !values.picc.key.keyGroup) {
    return 'PICC key requires a key group.';
  }

  const aids = new Set<string>();
  for (const application of values.applications) {
    const aid = application.aid.toUpperCase();
    if (!/^[0-9A-F]+$/.test(aid)) {
      return 'Application AID must be hexadecimal.';
    }
    if (aids.has(aid)) {
      return `Duplicate application AID ${aid}.`;
    }
    aids.add(aid);
    if (!application.keyGroup) {
      return `Application ${aid} requires a key group.`;
    }

    const fileIds = new Set<number>();
    for (const file of application.files) {
      const fileId = Number(file.id);
      if (!Number.isInteger(fileId) || fileId < 1 || fileId > 50) {
        return `File ID in application ${aid} must be between 1 and 50.`;
      }
      if (fileIds.has(fileId)) {
        return `Duplicate file ID ${fileId} in application ${aid}.`;
      }
      fileIds.add(fileId);
      if (file.encodingMode === 'custom' && !file.customEncoding.trim()) {
        return `File ${fileId} in application ${aid} requires a custom encoding value.`;
      }
      if ((file.encodingMode === 'uint-be' || file.encodingMode === 'uint-le') && Number(file.integerLength) < 1) {
        return `File ${fileId} in application ${aid} requires an integer byte length.`;
      }
    }
  }

  return null;
}

function createApplication(): ApplicationRow {
  return { aid: '', isoDfName: '', keyGroup: '', use2BytesFileIdentifier: false, keySettings: { ...defaultKeySettings, changeKey: '0' }, secureMessaging: defaultSecureMessaging, files: [] };
}

function createFile(): FileRow {
  return { id: '', mode: 'Plain', variable: '', size: '0', dataOffsetBytes: '0', dataLengthBytes: '0', encodingMode: 'text', integerLength: '1', customEncoding: '', readKey: '0', writeKey: '0', readWriteKey: '0', changeKey: '0' };
}

function parseEncoding(value: string): { mode: EncodingMode; integerLength: string; customEncoding: string } {
  if (value === 'text' || value === 'hex') {
    return { mode: value, integerLength: '1', customEncoding: '' };
  }

  const match = /^uint:(\d+):(be|le)$/.exec(value);
  if (match) {
    return { mode: match[2] === 'be' ? 'uint-be' : 'uint-le', integerLength: match[1], customEncoding: '' };
  }

  return { mode: 'custom', integerLength: '1', customEncoding: value };
}

function formatEncoding(file: FileRow) {
  if (file.encodingMode === 'uint-be') {
    return `uint:${Number(file.integerLength)}:be`;
  }
  if (file.encodingMode === 'uint-le') {
    return `uint:${Number(file.integerLength)}:le`;
  }
  if (file.encodingMode === 'custom') {
    return file.customEncoding;
  }
  return file.encodingMode;
}

function PanelError({ children }: { readonly children: ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}
