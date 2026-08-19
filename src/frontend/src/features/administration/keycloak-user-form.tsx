import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';

export type KeycloakUserFormValues = {
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
};

export function KeycloakUserForm({
  values,
  isSubmitting,
  submitLabel,
  onChange,
  onSubmit,
}: {
  readonly values: KeycloakUserFormValues;
  readonly isSubmitting: boolean;
  readonly submitLabel: string;
  readonly onChange: (values: KeycloakUserFormValues) => void;
  readonly onSubmit: () => void;
}) {
  return (
    <form className="grid gap-4" onSubmit={(event) => { event.preventDefault(); onSubmit(); }}>
      <Field label="Username"><Input value={values.username} onChange={(event) => onChange({ ...values, username: event.target.value })} /></Field>
      <Field label="First name"><Input value={values.firstName} onChange={(event) => onChange({ ...values, firstName: event.target.value })} /></Field>
      <Field label="Last name"><Input value={values.lastName} onChange={(event) => onChange({ ...values, lastName: event.target.value })} /></Field>
      <Field label="Email"><Input type="email" value={values.email} onChange={(event) => onChange({ ...values, email: event.target.value })} /></Field>
      <label className="flex items-center gap-3 rounded-interactive border border-border px-3 py-2 text-[14px] font-medium text-foreground">
        <input type="checkbox" checked={values.isActive} onChange={(event) => onChange({ ...values, isActive: event.target.checked })} />
        Active user
      </label>
      <div className="flex justify-end border-t border-border pt-4">
        <Button type="submit" disabled={isSubmitting}>{isSubmitting ? 'Saving...' : submitLabel}</Button>
      </div>
    </form>
  );
}

function Field({ label, children }: { readonly label: string; readonly children: React.ReactNode; }) {
  return <label className="grid gap-2 text-[14px] font-medium text-foreground"><span>{label}</span>{children}</label>;
}
