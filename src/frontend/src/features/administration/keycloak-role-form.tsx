import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';

export type KeycloakRoleFormValues = {
  name: string;
  description: string;
};

export function KeycloakRoleForm({
  values,
  isSubmitting,
  submitLabel,
  onChange,
  onSubmit,
}: {
  readonly values: KeycloakRoleFormValues;
  readonly isSubmitting: boolean;
  readonly submitLabel: string;
  readonly onChange: (values: KeycloakRoleFormValues) => void;
  readonly onSubmit: () => void;
}) {
  return (
    <form className="grid gap-4" onSubmit={(event) => { event.preventDefault(); onSubmit(); }}>
      <Field label="Role name"><Input value={values.name} onChange={(event) => onChange({ ...values, name: event.target.value })} /></Field>
      <Field label="Description"><Input value={values.description} onChange={(event) => onChange({ ...values, description: event.target.value })} placeholder="Optional description" /></Field>
      <div className="flex justify-end border-t border-border pt-4">
        <Button type="submit" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : submitLabel}</Button>
      </div>
    </form>
  );
}

function Field({ label, children }: { readonly label: string; readonly children: React.ReactNode; }) {
  return <label className="grid gap-2 text-[14px] font-medium text-foreground"><span>{label}</span>{children}</label>;
}
