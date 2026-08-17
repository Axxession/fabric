import { useEffect, useState } from 'react';

import { Button } from '@/shared/components/ui/button';

export type ContractorJobTypeFormValues = {
  code: string;
  name: string;
  description: string;
};

export function ContractorJobTypeForm({
  initialValues,
  isSubmitting,
  submitLabel,
  onSubmit,
}: {
  readonly initialValues: ContractorJobTypeFormValues;
  readonly isSubmitting: boolean;
  readonly submitLabel: string;
  readonly onSubmit: (values: ContractorJobTypeFormValues) => void;
}) {
  const [values, setValues] = useState(initialValues);

  useEffect(() => {
    setValues(initialValues);
  }, [initialValues]);

  return (
    <form
      className="grid gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit(values);
      }}
    >
      <label className="grid gap-2 text-[14px] font-medium">
        <span>Code *</span>
        <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.code} onChange={(event) => setValues((current) => ({ ...current, code: event.target.value }))} required />
      </label>

      <label className="grid gap-2 text-[14px] font-medium">
        <span>Name *</span>
        <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.name} onChange={(event) => setValues((current) => ({ ...current, name: event.target.value }))} required />
      </label>

      <label className="grid gap-2 text-[14px] font-medium">
        <span>Description</span>
        <textarea className="min-h-28 rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.description} onChange={(event) => setValues((current) => ({ ...current, description: event.target.value }))} />
      </label>

      <div className="flex justify-end">
        <Button type="submit" disabled={isSubmitting}>{submitLabel}</Button>
      </div>
    </form>
  );
}
