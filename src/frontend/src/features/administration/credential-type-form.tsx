import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { DurationInput, getDefaultDurationInputValue, type DurationInputValue } from '@/shared/components/ui/duration-input';

type CredentialTechnology = components['schemas']['CredentialTechnology'];
type CredentialAllocationMode = components['schemas']['CredentialAllocationMode'];
type CredentialRecyclePolicy = components['schemas']['CredentialRecyclePolicy'];
type CredentialIdentifierPaddingDirection = Exclude<components['schemas']['CredentialIdentifierPaddingDirection'], null>;
type CredentialTypeStatus = components['schemas']['CredentialTypeStatus'];

export type CredentialTypeFormValues = {
  name: string;
  technology: CredentialTechnology;
  allocationMode: CredentialAllocationMode;
  recyclePolicy: CredentialRecyclePolicy;
  recycleGracePeriod: DurationInputValue;
  requiresConfirmedPacsRevocation: boolean;
  nearLimitThreshold: string;
  identifierPrefix: string;
  identifierSuffix: string;
  identifierNumberLength: string;
  identifierPaddingDirection: CredentialIdentifierPaddingDirection;
  identifierPaddingCharacter: string;
  status: CredentialTypeStatus;
};

const technologyOptions: readonly CredentialTechnology[] = ['Qr', 'Desfire', 'LicensePlate'];
const allocationModeOptions: readonly CredentialAllocationMode[] = ['Range', 'Provided'];
const recyclePolicyOptions: readonly CredentialRecyclePolicy[] = ['NeverReuse', 'ReuseAfterExpiry', 'ReuseAfterRevocation', 'ReuseAfterRevocationAndGrace'];
const paddingDirectionOptions: readonly CredentialIdentifierPaddingDirection[] = ['Left', 'Right'];
const statusOptions: readonly CredentialTypeStatus[] = ['Active', 'Disabled'];

export function CredentialTypeForm({
  values,
  onChange,
  onSubmit,
  isSubmitting,
  submitLabel,
  includeStatus,
}: {
  readonly values: CredentialTypeFormValues;
  readonly onChange: (next: CredentialTypeFormValues) => void;
  readonly onSubmit: () => void;
  readonly isSubmitting: boolean;
  readonly submitLabel: string;
  readonly includeStatus: boolean;
}) {
  const recycleControlsDisabled = values.allocationMode === 'Provided';
  const graceDisabled = values.recyclePolicy !== 'ReuseAfterRevocationAndGrace' || recycleControlsDisabled;
  const qrFormattingEnabled = values.technology === 'Qr';

  function update<K extends keyof CredentialTypeFormValues>(key: K, value: CredentialTypeFormValues[K]) {
    const next = { ...values, [key]: value };

    if (key === 'allocationMode' && value === 'Provided') {
      next.recyclePolicy = 'NeverReuse';
      next.recycleGracePeriod = getDefaultDurationInputValue();
      next.requiresConfirmedPacsRevocation = false;
    }

    if (key === 'recyclePolicy' && value !== 'ReuseAfterRevocationAndGrace') {
      next.recycleGracePeriod = getDefaultDurationInputValue();
    }

    if (key === 'technology' && value !== 'Qr') {
      next.identifierPrefix = '';
      next.identifierSuffix = '';
      next.identifierNumberLength = '';
      next.identifierPaddingDirection = 'Left';
      next.identifierPaddingCharacter = '';
    }

    onChange(next);
  }

  return (
    <form className="grid gap-5" onSubmit={(event) => { event.preventDefault(); onSubmit(); }}>
      <div className="grid gap-4 md:grid-cols-2">
        <label className="grid gap-2 text-[14px] font-medium">
          Name
          <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.name} onChange={(event) => update('name', event.target.value)} required />
        </label>

        <label className="grid gap-2 text-[14px] font-medium">
          Technology
          <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.technology} onChange={(event) => update('technology', event.target.value as CredentialTechnology)}>
            {technologyOptions.map((option) => <option key={option} value={option}>{labelForTechnology(option)}</option>)}
          </select>
        </label>

        <label className="grid gap-2 text-[14px] font-medium">
          Allocation Mode
          <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.allocationMode} onChange={(event) => update('allocationMode', event.target.value as CredentialAllocationMode)}>
            {allocationModeOptions.map((option) => <option key={option} value={option}>{labelForAllocationMode(option)}</option>)}
          </select>
        </label>

        <label className="grid gap-2 text-[14px] font-medium">
          Near-limit Threshold
          <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" type="number" min="0" value={values.nearLimitThreshold} onChange={(event) => update('nearLimitThreshold', event.target.value)} placeholder="Optional" />
        </label>

        <label className="grid gap-2 text-[14px] font-medium">
          Prefix / Suffix
          <div className="grid gap-3 md:grid-cols-2">
            <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={values.identifierPrefix} onChange={(event) => update('identifierPrefix', event.target.value)} placeholder="Prefix" disabled={!qrFormattingEnabled} />
            <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={values.identifierSuffix} onChange={(event) => update('identifierSuffix', event.target.value)} placeholder="Suffix" disabled={!qrFormattingEnabled} />
          </div>
          {!qrFormattingEnabled ? <span className="text-[12px] text-muted-foreground">QR formatting options only apply to QR credential types.</span> : null}
        </label>

        <label className="grid gap-2 text-[14px] font-medium">
          Credential Number Length
          <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" type="number" min="1" value={values.identifierNumberLength} onChange={(event) => update('identifierNumberLength', event.target.value)} placeholder="Optional" disabled={!qrFormattingEnabled} />
        </label>

        <label className="grid gap-2 text-[14px] font-medium">
          Padding Direction
          <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={values.identifierPaddingDirection} onChange={(event) => update('identifierPaddingDirection', event.target.value as CredentialIdentifierPaddingDirection)} disabled={!qrFormattingEnabled || !values.identifierNumberLength.trim()}>
            {paddingDirectionOptions.map((option) => <option key={option} value={option}>{option === 'Left' ? 'Padding Left' : 'Padding Right'}</option>)}
          </select>
        </label>

        <label className="grid gap-2 text-[14px] font-medium">
          Padding Character
          <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={values.identifierPaddingCharacter} onChange={(event) => update('identifierPaddingCharacter', event.target.value.slice(0, 1))} placeholder="0" maxLength={1} disabled={!qrFormattingEnabled || !values.identifierNumberLength.trim()} />
        </label>

        <label className="grid gap-2 text-[14px] font-medium">
          Recycle Policy
          <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={values.recyclePolicy} onChange={(event) => update('recyclePolicy', event.target.value as CredentialRecyclePolicy)} disabled={recycleControlsDisabled}>
            {recyclePolicyOptions.map((option) => <option key={option} value={option}>{labelForRecyclePolicy(option)}</option>)}
          </select>
          {recycleControlsDisabled ? <span className="text-[12px] text-muted-foreground">Provided identifiers cannot use recycle policies.</span> : null}
        </label>

        <label className="grid gap-2 text-[14px] font-medium">
          Recycle Grace Period
          <DurationInput value={values.recycleGracePeriod} onChange={(nextValue) => update('recycleGracePeriod', nextValue)} disabled={graceDisabled} />
          {graceDisabled ? <span className="text-[12px] text-muted-foreground">Only used for the grace-based recycle policy.</span> : null}
        </label>

        <label className="grid gap-2 text-[14px] font-medium">
          Revocation Confirmation
          <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" value={values.requiresConfirmedPacsRevocation ? 'required' : 'not-required'} onChange={(event) => update('requiresConfirmedPacsRevocation', event.target.value === 'required')} disabled={recycleControlsDisabled}>
            <option value="not-required">Not required</option>
            <option value="required">Required</option>
          </select>
          <span className="text-[12px] text-muted-foreground">Require linked PACS assignment revocation before a range number can be reused.</span>
        </label>

        {includeStatus ? (
          <label className="grid gap-2 text-[14px] font-medium">
            Status
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.status} onChange={(event) => update('status', event.target.value as CredentialTypeStatus)}>
              {statusOptions.map((option) => <option key={option} value={option}>{option}</option>)}
            </select>
          </label>
        ) : null}
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={isSubmitting || !values.name.trim()}>{isSubmitting ? 'Saving...' : submitLabel}</Button>
      </div>
    </form>
  );
}

function labelForTechnology(value: CredentialTechnology) {
  return value === 'LicensePlate' ? 'License Plate' : value === 'Qr' ? 'QR' : 'Desfire';
}

function labelForAllocationMode(value: CredentialAllocationMode) {
  return value === 'Range' ? 'Range allocated' : 'Provided by caller';
}

function labelForRecyclePolicy(value: CredentialRecyclePolicy) {
  switch (value) {
    case 'NeverReuse':
      return 'Never reuse';
    case 'ReuseAfterExpiry':
      return 'Reuse after expiry';
    case 'ReuseAfterRevocation':
      return 'Reuse after revocation';
    case 'ReuseAfterRevocationAndGrace':
      return 'Reuse after revocation and grace';
  }
}
