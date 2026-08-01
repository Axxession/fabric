import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from '@tanstack/react-router';
import { ArrowLeft, Plus, Trash2 } from 'lucide-react';
import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { i18n } from '@/shared/i18n/i18n';
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
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [values, setValues] = useState<FormValues>(emptyValues);
  const [validationError, setValidationError] = useState<string | null>(null);

  const chipDesignQuery = useQuery({
    queryKey: [...chipDesignsQueryKey, chipDesignId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/desfire/chip-designs/{id}', { params: { path: { id: chipDesignId ?? '' } } });
      if (error || !data) {
        throw new Error(t('cardManagement.chipDesignForm.couldNotLoadChipDesign'));
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
        throw new Error(t('cardManagement.chipDesignForm.couldNotLoadKeyGroups'));
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
          throw new Error(t('cardManagement.chipDesignForm.createFailed'));
        }
        return;
      }

      const { error } = await api.PUT('/api/desfire/chip-designs/{id}', { params: { path: { id: chipDesignId ?? '' } }, body: { ...request, version: Number(values.version) } });
      if (error) {
        throw new Error(t('cardManagement.chipDesignForm.updateFailed'));
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: chipDesignsQueryKey });
      if (chipDesignId) {
        await queryClient.invalidateQueries({ queryKey: [...chipDesignsQueryKey, chipDesignId] });
      }
      toast.success(mode === 'create' ? t('cardManagement.chipDesignForm.created') : t('cardManagement.chipDesignForm.updated'));
      window.history.back();
    },
    onError: () => toast.error(mode === 'create' ? t('cardManagement.chipDesignForm.createFailed') : t('cardManagement.chipDesignForm.updateFailed')),
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
        <Button variant="outline" size="icon" aria-label={t('cardManagement.chipDesignForm.back')} onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">{mode === 'create' ? t('cardManagement.chipDesignForm.addTitle') : values.name || t('cardManagement.chipDesignForm.editTitle')}</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{t('cardManagement.chipDesignForm.description')}</p>
        </div>
      </header>

      {chipDesignQuery.isError ? <PanelError>{t('cardManagement.chipDesignForm.couldNotLoadChipDesign')}</PanelError> : null}
      {keyGroupsQuery.isError ? <PanelError>{t('cardManagement.chipDesignForm.couldNotLoadKeyGroups')}</PanelError> : null}
      {validationError ? <PanelError>{validationError}</PanelError> : null}

      <Card className="p-4 sm:p-6">
        {chipDesignQuery.isLoading ? <p className="text-[14px] text-muted-foreground">{t('cardManagement.chipDesignForm.loading')}</p> : null}
        {mode === 'create' || chipDesignQuery.data ? (
          <form className="grid gap-5" onSubmit={handleSubmit}>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium">
                {t('cardManagement.chipDesignForm.name')}
                <Input value={values.name} onChange={(event) => updateValue('name', event.target.value)} required />
              </label>
              <label className="grid gap-2 text-[14px] font-medium">
                {t('cardManagement.chipDesignForm.version')}
                <Input value={values.version} type="number" min={1} placeholder={mode === 'create' ? t('cardManagement.chipDesignForm.versionPlaceholder') : undefined} onChange={(event) => updateValue('version', event.target.value)} required={mode === 'edit'} />
              </label>
              <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
                {t('cardManagement.chipDesignForm.descriptionLabel')}
                <Input value={values.description} onChange={(event) => updateValue('description', event.target.value)} />
              </label>
            </div>

            <PiccEditor value={values.specification.picc} keyGroupNames={keyGroupNames} onChange={(picc) => updateSpecification({ ...values.specification, picc })} />
            <ApplicationsEditor value={values.specification.applications} keyGroupNames={keyGroupNames} onChange={(applications) => updateSpecification({ ...values.specification, applications })} />
            <JsonPreview value={jsonPreview} />

            <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <Button type="button" variant="outline" onClick={() => window.history.back()}>{t('cardManagement.chipDesignForm.cancel')}</Button>
              <Button type="submit" disabled={saveChipDesign.isPending}>{saveChipDesign.isPending ? t('cardManagement.chipDesignForm.saving') : t('cardManagement.chipDesignForm.save')}</Button>
            </div>
          </form>
        ) : null}
      </Card>
    </div>
  );
}

function PiccEditor({ value, keyGroupNames, onChange }: { readonly value: PiccValues; readonly keyGroupNames: string[]; readonly onChange: (value: PiccValues) => void }) {
  const { t } = useTranslation();
  return (
    <section className="grid gap-4 rounded-structural border border-border p-4">
      <div>
        <h3 className="text-[16px] font-semibold tracking-tight">{t('cardManagement.chipDesignForm.picc')}</h3>
        <p className="mt-1 text-[14px] text-muted-foreground">{t('cardManagement.chipDesignForm.piccDescription')}</p>
      </div>

      <Checkbox label={t('cardManagement.chipDesignForm.usePiccKey')} checked={value.useKey} onChange={(useKey) => onChange({ ...value, useKey })} />
      {value.useKey ? <KeyRefEditor value={value.key} keyGroupNames={keyGroupNames} onChange={(key) => onChange({ ...value, key })} /> : null}

      <Checkbox label={t('cardManagement.chipDesignForm.allowCreateDelete')} checked={value.allowCreateDelete} onChange={(allowCreateDelete) => onChange({ ...value, allowCreateDelete })} />
      <KeySettingsEditor title={t('cardManagement.chipDesignForm.piccKeySettings')} value={value.keySettings} onChange={(keySettings) => onChange({ ...value, keySettings })} extra={<Checkbox label={t('cardManagement.chipDesignForm.allowDamKeys')} checked={value.keySettings.allowDamKeys} onChange={(allowDamKeys) => onChange({ ...value, keySettings: { ...value.keySettings, allowDamKeys } })} />} />
      <SwitchGrid title={t('cardManagement.chipDesignForm.piccSettings')} values={[
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
  const { t } = useTranslation();
  function updateApplication(index: number, application: ApplicationRow) {
    onChange(value.map((current, currentIndex) => currentIndex === index ? application : current));
  }

  return (
    <section className="grid gap-4 rounded-structural border border-border p-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="text-[16px] font-semibold tracking-tight">{t('cardManagement.chipDesignForm.applications')}</h3>
          <p className="mt-1 text-[14px] text-muted-foreground">{t('cardManagement.chipDesignForm.applicationsDescription')}</p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={() => onChange([...value, createApplication()])}>
          <Plus className="size-4" aria-hidden="true" />{t('cardManagement.chipDesignForm.addApplication')}
        </Button>
      </div>

      {value.length === 0 ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">{t('cardManagement.chipDesignForm.blankTemplateNoApplications')}</p> : null}
      {value.map((application, index) => (
        <div key={index} className="grid gap-4 rounded-structural border border-border p-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-wrap items-center gap-2">
              <h4 className="text-[15px] font-semibold tracking-tight">{t('cardManagement.chipDesignForm.application', { value: application.aid || index + 1 })}</h4>
              {application.aid ? <Badge variant="outline">AID {application.aid}</Badge> : null}
            </div>
            <Button type="button" variant="outline" size="sm" onClick={() => onChange(value.filter((_, currentIndex) => currentIndex !== index))}>
              <Trash2 className="size-4" aria-hidden="true" />{t('cardManagement.chipDesignForm.remove')}
            </Button>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2 text-[14px] font-medium">
              {t('cardManagement.chipDesignForm.aidHex')}
              <Input value={application.aid} pattern="[0-9a-fA-F]+" onChange={(event) => updateApplication(index, { ...application, aid: event.target.value.toUpperCase() })} required />
            </label>
            <label className="grid gap-2 text-[14px] font-medium">
              {t('cardManagement.chipDesignForm.isoDfName')}
              <Input value={application.isoDfName} onChange={(event) => updateApplication(index, { ...application, isoDfName: event.target.value })} />
            </label>
            <KeyGroupSelect value={application.keyGroup} keyGroupNames={keyGroupNames} onChange={(keyGroup) => updateApplication(index, { ...application, keyGroup })} />
            <Checkbox label={t('cardManagement.chipDesignForm.use2ByteFileIds')} checked={application.use2BytesFileIdentifier} onChange={(use2BytesFileIdentifier) => updateApplication(index, { ...application, use2BytesFileIdentifier })} />
          </div>

          <KeySettingsEditor title={t('cardManagement.chipDesignForm.applicationKeySettings')} value={application.keySettings} onChange={(keySettings) => updateApplication(index, { ...application, keySettings })} extra={<label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.chipDesignForm.changeKeyId')}</span><Input value={application.keySettings.changeKey} type="number" min={0} onChange={(event) => updateApplication(index, { ...application, keySettings: { ...application.keySettings, changeKey: event.target.value } })} required /></label>} />
          <SecureMessagingEditor value={application.secureMessaging} onChange={(secureMessaging) => updateApplication(index, { ...application, secureMessaging })} />
          <FilesEditor value={application.files} onChange={(files) => updateApplication(index, { ...application, files })} />
        </div>
      ))}
    </section>
  );
}

function FilesEditor({ value, onChange }: { readonly value: FileRow[]; readonly onChange: (value: FileRow[]) => void }) {
  const { t } = useTranslation();

  function updateFile(index: number, file: FileRow) {
    onChange(value.map((current, currentIndex) => currentIndex === index ? file : current));
  }

  return (
    <section className="grid gap-3">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h5 className="text-[14px] font-semibold tracking-tight">{t('cardManagement.chipDesignForm.files')}</h5>
          <p className="mt-1 text-[13px] text-muted-foreground">{t('cardManagement.chipDesignForm.filesDescription')}</p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={() => onChange([...value, createFile()])}>
          <Plus className="size-4" aria-hidden="true" />{t('cardManagement.chipDesignForm.addFile')}
        </Button>
      </div>

      {value.length === 0 ? <p className="rounded-structural border border-border p-3 text-[14px] text-muted-foreground">{t('cardManagement.chipDesignForm.noFiles')}</p> : null}
      {value.map((file, index) => (
        <div key={index} className="grid gap-4 rounded-interactive border border-border p-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-wrap items-center gap-2">
              <h6 className="text-[14px] font-semibold tracking-tight">{t('cardManagement.chipDesignForm.file', { value: file.id || index + 1 })}</h6>
              {file.id ? <Badge variant="outline">ID {file.id}</Badge> : null}
            </div>
            <Button type="button" variant="outline" size="sm" onClick={() => onChange(value.filter((_, currentIndex) => currentIndex !== index))}>
              <Trash2 className="size-4" aria-hidden="true" />{t('cardManagement.chipDesignForm.remove')}
            </Button>
          </div>
          <div className="grid gap-4 md:grid-cols-3">
            <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.chipDesignForm.fileId')}</span><Input value={file.id} type="number" min={1} max={50} onChange={(event) => updateFile(index, { ...file, id: event.target.value })} required /></label>
            <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.chipDesignForm.mode')}</span><select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={file.mode} onChange={(event) => updateFile(index, { ...file, mode: event.target.value as FileMode })}>{fileModes.map((mode) => <option key={mode} value={mode}>{mode}</option>)}</select></label>
            <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.chipDesignForm.variable')}</span><Input value={file.variable} onChange={(event) => updateFile(index, { ...file, variable: event.target.value })} required /></label>
            <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.chipDesignForm.sizeBytes')}</span><Input value={file.size} type="number" min={0} onChange={(event) => updateFile(index, { ...file, size: event.target.value })} required /></label>
            <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.chipDesignForm.dataOffsetBytes')}</span><Input value={file.dataOffsetBytes} type="number" min={0} onChange={(event) => updateFile(index, { ...file, dataOffsetBytes: event.target.value })} required /></label>
            <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.chipDesignForm.dataLengthBytes')}</span><Input value={file.dataLengthBytes} type="number" min={0} onChange={(event) => updateFile(index, { ...file, dataLengthBytes: event.target.value })} required /></label>
          </div>
          <EncodingEditor value={file} onChange={(next) => updateFile(index, next)} />
          <div className="grid gap-4 md:grid-cols-4">
            <KeyNumber label={t('cardManagement.chipDesignForm.readKeyId')} value={file.readKey} onChange={(readKey) => updateFile(index, { ...file, readKey })} />
            <KeyNumber label={t('cardManagement.chipDesignForm.writeKeyId')} value={file.writeKey} onChange={(writeKey) => updateFile(index, { ...file, writeKey })} />
            <KeyNumber label={t('cardManagement.chipDesignForm.readWriteKeyId')} value={file.readWriteKey} onChange={(readWriteKey) => updateFile(index, { ...file, readWriteKey })} />
            <KeyNumber label={t('cardManagement.chipDesignForm.changeKeyId')} value={file.changeKey} onChange={(changeKey) => updateFile(index, { ...file, changeKey })} />
          </div>
        </div>
      ))}
    </section>
  );
}

function EncodingEditor({ value, onChange }: { readonly value: FileRow; readonly onChange: (value: FileRow) => void }) {
  const { t } = useTranslation();
  return (
    <div className="grid gap-4 md:grid-cols-3">
      <label className="grid gap-2 text-[14px] font-medium">
        {t('cardManagement.chipDesignForm.encoding')}
        <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={value.encodingMode} onChange={(event) => onChange({ ...value, encodingMode: event.target.value as EncodingMode })}>
          <option value="text">{t('cardManagement.chipDesignForm.text')}</option>
          <option value="hex">{t('cardManagement.chipDesignForm.hex')}</option>
          <option value="uint-be">{t('cardManagement.chipDesignForm.uintBe')}</option>
          <option value="uint-le">{t('cardManagement.chipDesignForm.uintLe')}</option>
          <option value="custom">{t('cardManagement.chipDesignForm.custom')}</option>
        </select>
      </label>
      {value.encodingMode === 'uint-be' || value.encodingMode === 'uint-le' ? <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.chipDesignForm.integerByteLength')}</span><Input value={value.integerLength} type="number" min={1} onChange={(event) => onChange({ ...value, integerLength: event.target.value })} required /></label> : null}
      {value.encodingMode === 'custom' ? <label className="grid gap-2 text-[14px] font-medium md:col-span-2"><span>{t('cardManagement.chipDesignForm.customEncoding')}</span><Input value={value.customEncoding} onChange={(event) => onChange({ ...value, customEncoding: event.target.value })} required /></label> : null}
    </div>
  );
}

function KeyRefEditor({ value, keyGroupNames, onChange }: { readonly value: KeyRefValues; readonly keyGroupNames: string[]; readonly onChange: (value: KeyRefValues) => void }) {
  const { t } = useTranslation();
  return (
    <div className="grid gap-4 md:grid-cols-3">
      <KeyGroupSelect value={value.keyGroup} keyGroupNames={keyGroupNames} onChange={(keyGroup) => onChange({ ...value, keyGroup })} />
      <label className="grid gap-2 text-[14px] font-medium"><span>{t('cardManagement.chipDesignForm.keySetId')}</span><Input value={value.keySet} type="number" min={0} onChange={(event) => onChange({ ...value, keySet: event.target.value })} required /></label>
      <KeyNumber label={t('cardManagement.chipDesignForm.keyId')} value={value.key} onChange={(key) => onChange({ ...value, key })} />
    </div>
  );
}

function KeyGroupSelect({ value, keyGroupNames, onChange }: { readonly value: string; readonly keyGroupNames: string[]; readonly onChange: (value: string) => void }) {
  const { t } = useTranslation();
  return (
    <label className="grid gap-2 text-[14px] font-medium">
      {t('cardManagement.chipDesignForm.keyGroup')}
      <select className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary" value={value} onChange={(event) => onChange(event.target.value)} required>
        <option value="">{t('cardManagement.chipDesignForm.selectKeyGroup')}</option>
        {keyGroupNames.map((name) => <option key={name} value={name}>{name}</option>)}
      </select>
    </label>
  );
}

function KeyNumber({ label, value, onChange }: { readonly label: string; readonly value: string; readonly onChange: (value: string) => void }) {
  return <label className="grid gap-2 text-[14px] font-medium"><span>{label}</span><Input value={value} type="number" min={0} onChange={(event) => onChange(event.target.value)} required /></label>;
}

function KeySettingsEditor<TValue extends KeySettingsValues>({ title, value, onChange, extra }: { readonly title: string; readonly value: TValue; readonly onChange: (value: TValue) => void; readonly extra?: ReactNode }) {
  const { t } = useTranslation();
  return (
    <section className="grid gap-3 rounded-interactive border border-border p-3">
      <h4 className="text-[14px] font-semibold tracking-tight">{title}</h4>
      <div className="grid gap-3 md:grid-cols-2">
        <Checkbox label={t('cardManagement.chipDesignForm.changeable')} checked={value.changeable} onChange={(changeable) => onChange({ ...value, changeable })} />
        <Checkbox label={t('cardManagement.chipDesignForm.masterKeyChangeable')} checked={value.masterKeyChangeable} onChange={(masterKeyChangeable) => onChange({ ...value, masterKeyChangeable })} />
        <Checkbox label={t('cardManagement.chipDesignForm.freeDirectoryListing')} checked={value.freeDirectoryListing} onChange={(freeDirectoryListing) => onChange({ ...value, freeDirectoryListing })} />
        <Checkbox label={t('cardManagement.chipDesignForm.allowCreateDelete')} checked={value.allowCreateDelete} onChange={(allowCreateDelete) => onChange({ ...value, allowCreateDelete })} />
        {extra}
      </div>
    </section>
  );
}

function SecureMessagingEditor({ value, onChange }: { readonly value: SecureMessagingValues; readonly onChange: (value: SecureMessagingValues) => void }) {
  const { t } = useTranslation();

  return <SwitchGrid title={t('cardManagement.chipDesignForm.secureMessaging')} values={[
    [t('cardManagement.chipDesignForm.disableD40'), value.disableD40, (checked) => onChange({ ...value, disableD40: checked })],
    [t('cardManagement.chipDesignForm.disableEv1'), value.disableEv1, (checked) => onChange({ ...value, disableEv1: checked })],
    [t('cardManagement.chipDesignForm.disableEv2Chaining'), value.disableEv2Chaining, (checked) => onChange({ ...value, disableEv2Chaining: checked })],
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
  const { t } = useTranslation();
  return (
    <section className="grid gap-3 rounded-structural border border-border p-4">
      <div>
        <h3 className="text-[16px] font-semibold tracking-tight">{t('cardManagement.chipDesignForm.readOnlyJson')}</h3>
        <p className="mt-1 text-[14px] text-muted-foreground">{t('cardManagement.chipDesignForm.readOnlyJsonDescription')}</p>
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
    return i18n.t('cardManagement.chipDesignForm.validationVersionMin');
  }

  if (values.picc.useKey && !values.picc.key.keyGroup) {
    return i18n.t('cardManagement.chipDesignForm.validationPiccKeyGroup');
  }

  const aids = new Set<string>();
  for (const application of values.applications) {
    const aid = application.aid.toUpperCase();
    if (!/^[0-9A-F]+$/.test(aid)) {
      return i18n.t('cardManagement.chipDesignForm.validationAidHex');
    }
    if (aids.has(aid)) {
      return i18n.t('cardManagement.chipDesignForm.validationDuplicateAid', { aid });
    }
    aids.add(aid);
    if (!application.keyGroup) {
      return i18n.t('cardManagement.chipDesignForm.validationApplicationKeyGroup', { aid });
    }

    const fileIds = new Set<number>();
    for (const file of application.files) {
      const fileId = Number(file.id);
      if (!Number.isInteger(fileId) || fileId < 1 || fileId > 50) {
        return i18n.t('cardManagement.chipDesignForm.validationFileIdRange', { aid });
      }
      if (fileIds.has(fileId)) {
        return i18n.t('cardManagement.chipDesignForm.validationDuplicateFileId', { fileId, aid });
      }
      fileIds.add(fileId);
      if (file.encodingMode === 'custom' && !file.customEncoding.trim()) {
        return i18n.t('cardManagement.chipDesignForm.validationCustomEncoding', { fileId, aid });
      }
      if ((file.encodingMode === 'uint-be' || file.encodingMode === 'uint-le') && Number(file.integerLength) < 1) {
        return i18n.t('cardManagement.chipDesignForm.validationIntegerLength', { fileId, aid });
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
