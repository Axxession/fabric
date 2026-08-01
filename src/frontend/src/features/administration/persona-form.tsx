import { useEffect, useState } from 'react';

import { Button } from '@/shared/components/ui/button';

export type PersonaFormValues = {
  name: string;
};

export function PersonaForm({
  initialValues,
  isSubmitting,
  submitLabel,
  onSubmit,
}: {
  readonly initialValues: PersonaFormValues;
  readonly isSubmitting: boolean;
  readonly submitLabel: string;
  readonly onSubmit: (values: PersonaFormValues) => void;
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
        <span>Name *</span>
        <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.name} onChange={(event) => setValues({ name: event.target.value })} required />
      </label>

      <div className="flex justify-end">
        <Button type="submit" disabled={isSubmitting}>{submitLabel}</Button>
      </div>
    </form>
  );
}
