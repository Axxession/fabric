import { useEffect, useState } from 'react';

import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';

export type OrganizationUnitFormValues = {
  name: string;
  code: string;
  type: string;
  parentId: string;
};

type OrganizationUnit = components['schemas']['OrganizationUnitResponse'];

export function OrganizationUnitForm({
  initialValues,
  parentOptions,
  isSubmitting,
  submitLabel,
  onSubmit,
}: {
  readonly initialValues: OrganizationUnitFormValues;
  readonly parentOptions: readonly OrganizationUnit[];
  readonly isSubmitting: boolean;
  readonly submitLabel: string;
  readonly onSubmit: (values: OrganizationUnitFormValues) => void;
}) {
  const [values, setValues] = useState(initialValues);

  useEffect(() => {
    setValues(initialValues);
  }, [initialValues]);

  function updateValue<TKey extends keyof OrganizationUnitFormValues>(key: TKey, value: OrganizationUnitFormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  return (
    <form
      className="grid gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit(values);
      }}
    >
      <div className="grid gap-4 md:grid-cols-2">
        <Field label="Name" required>
          <Input value={values.name} onChange={(value) => updateValue('name', value)} />
        </Field>
        <Field label="Code">
          <Input value={values.code} onChange={(value) => updateValue('code', value)} />
        </Field>
        <Field label="Type" required>
          <Input value={values.type} onChange={(value) => updateValue('type', value)} />
        </Field>
        <Field label="Parent organizational unit">
          <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.parentId} onChange={(event) => updateValue('parentId', event.target.value)}>
            <option value="">No parent (root)</option>
            {parentOptions.map((option) => <option key={option.id} value={option.id}>{option.name}</option>)}
          </select>
        </Field>
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={isSubmitting}>{submitLabel}</Button>
      </div>
    </form>
  );
}

function Field({ label, required, children }: { readonly label: string; readonly required?: boolean; readonly children: React.ReactNode }) {
  return (
    <label className="grid gap-2 text-[14px] font-medium">
      <span>{label}{required ? ' *' : ''}</span>
      {children}
    </label>
  );
}

function Input({ value, onChange }: { readonly value: string; readonly onChange: (value: string) => void }) {
  return <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={value} onChange={(event) => onChange(event.target.value)} />;
}
