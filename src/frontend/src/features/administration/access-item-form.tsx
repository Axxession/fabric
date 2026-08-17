import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';

type AccessItemStatus = components['schemas']['AccessItemStatus'];

export type AccessItemFormValues = {
  name: string;
  description: string;
  isComplianceRequired: boolean;
  status: AccessItemStatus;
};

export function AccessItemForm({
  initialValues,
  isSubmitting,
  submitLabel,
  includeStatus,
  onSubmit,
}: {
  readonly initialValues: AccessItemFormValues;
  readonly isSubmitting: boolean;
  readonly submitLabel: string;
  readonly includeStatus: boolean;
  readonly onSubmit: (values: AccessItemFormValues) => void;
}) {
  const { t } = useTranslation();
  const [values, setValues] = useState(initialValues);

  useEffect(() => {
    setValues(initialValues);
  }, [initialValues]);

  function updateValue<TKey extends keyof AccessItemFormValues>(key: TKey, value: AccessItemFormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  return (
    <form
      className="grid gap-5"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit(values);
      }}
    >
      <div className="grid gap-4 md:grid-cols-2">
        <label className="grid gap-2 text-[14px] font-medium">
          Name
          <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.name} onChange={(event) => updateValue('name', event.target.value)} required />
        </label>

        {includeStatus ? (
          <label className="grid gap-2 text-[14px] font-medium">
            Status
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.status} onChange={(event) => updateValue('status', event.target.value as AccessItemStatus)}>
              <option value="Active">Active</option>
              <option value="Inactive">Inactive</option>
            </select>
          </label>
        ) : null}

        <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
          <span>{t('administration.accessItems.form.compliance')}</span>
          <span className="flex items-start gap-3 rounded-structural border border-border bg-background px-3 py-3 text-[14px] font-normal">
            <input
              type="checkbox"
              className="mt-0.5 size-4 rounded border border-border"
              checked={values.isComplianceRequired}
              onChange={(event) => updateValue('isComplianceRequired', event.target.checked)}
            />
            <span>
              <span className="block font-medium text-foreground">{t('administration.accessItems.form.requiresCompliance')}</span>
              <span className="mt-1 block text-muted-foreground">{t('administration.accessItems.form.requiresComplianceHelp')}</span>
            </span>
          </span>
        </label>

        <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
          Description
          <textarea className="min-h-28 rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.description} onChange={(event) => updateValue('description', event.target.value)} />
        </label>
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={isSubmitting}>{submitLabel}</Button>
      </div>
    </form>
  );
}
