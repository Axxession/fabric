import { useEffect, useState } from 'react';

import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';

type CatalogStatus = components['schemas']['CatalogStatus'];

export type CatalogueFormValues = {
  name: string;
  description: string;
  status: CatalogStatus;
};

export function CatalogueForm({
  initialValues,
  isSubmitting,
  submitLabel,
  includeStatus,
  onSubmit,
}: {
  readonly initialValues: CatalogueFormValues;
  readonly isSubmitting: boolean;
  readonly submitLabel: string;
  readonly includeStatus: boolean;
  readonly onSubmit: (values: CatalogueFormValues) => void;
}) {
  const [values, setValues] = useState(initialValues);

  useEffect(() => {
    setValues(initialValues);
  }, [initialValues]);

  function updateValue<TKey extends keyof CatalogueFormValues>(key: TKey, value: CatalogueFormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  return (
    <form className="grid gap-5" onSubmit={(event) => { event.preventDefault(); onSubmit(values); }}>
      <div className="grid gap-4 md:grid-cols-2">
        <label className="grid gap-2 text-[14px] font-medium">
          Name
          <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.name} onChange={(event) => updateValue('name', event.target.value)} required />
        </label>

        {includeStatus ? (
          <label className="grid gap-2 text-[14px] font-medium">
            Status
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.status} onChange={(event) => updateValue('status', event.target.value as CatalogStatus)}>
              <option value="Active">Active</option>
              <option value="Inactive">Inactive</option>
            </select>
          </label>
        ) : null}

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
