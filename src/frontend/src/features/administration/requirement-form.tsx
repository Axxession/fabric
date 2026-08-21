import { useEffect, useState } from 'react';

import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';
import { Textarea } from '@/shared/components/ui/textarea';

type RequirementEvidenceKind = Exclude<components['schemas']['RequirementEvidenceKind'], null>;

export type RequirementFormValues = {
  readonly code: string;
  readonly name: string;
  readonly description: string;
  readonly allowedEvidenceKinds: readonly RequirementEvidenceKind[];
  readonly isSensitive: boolean;
};

const evidenceKindOptions: ReadonlyArray<{ value: RequirementEvidenceKind; label: string; description: string; }> = [
  { value: 'Document', label: 'Document', description: 'Uploaded document or certificate.' },
  { value: 'CourseCompletion', label: 'Course completion', description: 'Satisfied by learning completion.' },
  { value: 'RequirementWaiver', label: 'Requirement waiver', description: 'Satisfied by a manual waiver.' },
];

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
        <fieldset className="grid gap-3 rounded-structural border border-border p-4">
          <legend className="px-1 text-[14px] font-medium">Allowed evidence kinds</legend>
          {evidenceKindOptions.map((option) => {
            const checked = values.allowedEvidenceKinds.includes(option.value);
            return (
              <label key={option.value} className="flex items-start gap-3 rounded-interactive border border-border px-3 py-3 text-[14px] font-medium">
                <input
                  type="checkbox"
                  checked={checked}
                  onChange={(event) => setValues((current) => ({
                    ...current,
                    allowedEvidenceKinds: event.target.checked
                      ? [...current.allowedEvidenceKinds, option.value]
                      : current.allowedEvidenceKinds.filter((item) => item !== option.value),
                  }))}
                />
                <span>
                  <span className="block text-foreground">{option.label}</span>
                  <span className="block text-[13px] font-normal text-muted-foreground">{option.description}</span>
                </span>
              </label>
            );
          })}
        </fieldset>

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
