import { useEffect, useState } from 'react';

import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';
import { Textarea } from '@/shared/components/ui/textarea';

type RequirementFulfillmentKind = components['schemas']['RequirementFulfillmentKind'];

export type RequirementFormValues = {
  readonly code: string;
  readonly name: string;
  readonly description: string;
  readonly fulfillmentKind: RequirementFulfillmentKind;
  readonly isSensitive: boolean;
};

const fulfillmentOptions: RequirementFulfillmentKind[] = ['Document', 'Learning'];

export function RequirementForm({ initialValues, isSubmitting, submitLabel, onSubmit }: { readonly initialValues: RequirementFormValues; readonly isSubmitting: boolean; readonly submitLabel: string; readonly onSubmit: (values: RequirementFormValues) => void; }) {
  const [values, setValues] = useState(initialValues);

  useEffect(() => {
    setValues(initialValues);
  }, [initialValues]);

  return (
    <form className="grid gap-5" onSubmit={(event) => { event.preventDefault(); onSubmit(values); }}>
      <div className="grid gap-5 md:grid-cols-2">
        <label className="grid gap-2 text-[14px] font-medium">
          <span>Code</span>
          <Input value={values.code} onChange={(event) => setValues((current) => ({ ...current, code: event.target.value }))} placeholder="site_safety_training" />
        </label>
        <label className="grid gap-2 text-[14px] font-medium">
          <span>Name</span>
          <Input value={values.name} onChange={(event) => setValues((current) => ({ ...current, name: event.target.value }))} placeholder="Site safety training" />
        </label>
      </div>

      <label className="grid gap-2 text-[14px] font-medium">
        <span>Description</span>
        <Textarea value={values.description} onChange={(event) => setValues((current) => ({ ...current, description: event.target.value }))} rows={4} placeholder="Explain what this requirement means." />
      </label>

      <div className="grid gap-5 md:grid-cols-2">
        <label className="grid gap-2 text-[14px] font-medium">
          <span>Fulfillment</span>
          <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.fulfillmentKind} onChange={(event) => setValues((current) => ({ ...current, fulfillmentKind: event.target.value as RequirementFulfillmentKind }))}>
            {fulfillmentOptions.map((option) => <option key={option} value={option}>{option}</option>)}
          </select>
        </label>

        <label className="flex items-center gap-3 rounded-structural border border-border p-4 text-[14px] font-medium">
          <input type="checkbox" checked={values.isSensitive} onChange={(event) => setValues((current) => ({ ...current, isSensitive: event.target.checked }))} />
          Sensitive requirement
        </label>
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : submitLabel}</Button>
      </div>
    </form>
  );
}
