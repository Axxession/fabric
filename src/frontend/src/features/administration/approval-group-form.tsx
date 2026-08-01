import { useEffect, useState } from 'react';

import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';

type ApprovalGroupStatus = components['schemas']['ApprovalGroupStatus'];

export type ApprovalGroupFormValues = {
  name: string;
  status: ApprovalGroupStatus;
};

export function ApprovalGroupForm({ initialValues, isSubmitting, submitLabel, includeStatus, onSubmit }: { readonly initialValues: ApprovalGroupFormValues; readonly isSubmitting: boolean; readonly submitLabel: string; readonly includeStatus: boolean; readonly onSubmit: (values: ApprovalGroupFormValues) => void; }) {
  const [values, setValues] = useState(initialValues);

  useEffect(() => {
    setValues(initialValues);
  }, [initialValues]);

  return (
    <form className="grid gap-5" onSubmit={(event) => { event.preventDefault(); onSubmit(values); }}>
      <div className="grid gap-4 md:grid-cols-2">
        <label className="grid gap-2 text-[14px] font-medium">
          Name
          <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.name} onChange={(event) => setValues((current) => ({ ...current, name: event.target.value }))} required />
        </label>

        {includeStatus ? (
          <label className="grid gap-2 text-[14px] font-medium">
            Status
            <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.status} onChange={(event) => setValues((current) => ({ ...current, status: event.target.value as ApprovalGroupStatus }))}>
              <option value="Active">Active</option>
              <option value="Inactive">Inactive</option>
            </select>
          </label>
        ) : null}
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={isSubmitting}>{submitLabel}</Button>
      </div>
    </form>
  );
}
