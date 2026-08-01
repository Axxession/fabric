import { useEffect, useState } from 'react';

import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';

export type EmployeeFormValues = {
  firstName: string;
  lastName: string;
  birthDate: string;
  employeeNumber: string;
  directoryId: string;
  email: string;
  organizationUnitId: string;
  managerEmployeeId: string;
  jobTitle: string;
  contractStartDate: string;
  contractEndDate: string;
};

type OrganizationUnit = components['schemas']['OrganizationUnitResponse'];
type Employee = components['schemas']['EmployeeResponse'];

export function EmployeeForm({
  initialValues,
  organizationUnits,
  managers,
  isSubmitting,
  submitLabel,
  onSubmit,
}: {
  readonly initialValues: EmployeeFormValues;
  readonly organizationUnits: readonly OrganizationUnit[];
  readonly managers: readonly Employee[];
  readonly isSubmitting: boolean;
  readonly submitLabel: string;
  readonly onSubmit: (values: EmployeeFormValues) => void;
}) {
  const [values, setValues] = useState(initialValues);

  useEffect(() => {
    setValues(initialValues);
  }, [initialValues]);

  function updateValue<TKey extends keyof EmployeeFormValues>(key: TKey, value: EmployeeFormValues[TKey]) {
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
        <Field label="First name" required>
          <Input value={values.firstName} onChange={(value) => updateValue('firstName', value)} />
        </Field>
        <Field label="Last name" required>
          <Input value={values.lastName} onChange={(value) => updateValue('lastName', value)} />
        </Field>
        <Field label="Birth date">
          <Input type="date" value={values.birthDate} onChange={(value) => updateValue('birthDate', value)} />
        </Field>
        <Field label="Employee number">
          <Input value={values.employeeNumber} onChange={(value) => updateValue('employeeNumber', value)} />
        </Field>
        <Field label="Directory ID">
          <Input value={values.directoryId} onChange={(value) => updateValue('directoryId', value)} />
        </Field>
        <Field label="Email">
          <Input type="email" value={values.email} onChange={(value) => updateValue('email', value)} />
        </Field>
        <Field label="Organizational unit" required>
          <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.organizationUnitId} onChange={(event) => updateValue('organizationUnitId', event.target.value)} required>
            <option value="">Select organizational unit</option>
            {organizationUnits.map((unit) => <option key={unit.id} value={unit.id}>{unit.name}</option>)}
          </select>
        </Field>
        <Field label="Manager">
          <select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.managerEmployeeId} onChange={(event) => updateValue('managerEmployeeId', event.target.value)}>
            <option value="">No manager</option>
            {managers.map((manager) => <option key={manager.id} value={manager.id}>{manager.firstName} {manager.lastName}</option>)}
          </select>
        </Field>
        <Field label="Job title">
          <Input value={values.jobTitle} onChange={(value) => updateValue('jobTitle', value)} />
        </Field>
        <Field label="Contract start date">
          <Input type="date" value={values.contractStartDate} onChange={(value) => updateValue('contractStartDate', value)} />
        </Field>
        <Field label="Contract end date">
          <Input type="date" value={values.contractEndDate} onChange={(value) => updateValue('contractEndDate', value)} />
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

function Input({ value, onChange, type = 'text' }: { readonly value: string; readonly onChange: (value: string) => void; readonly type?: 'text' | 'email' | 'date' }) {
  return <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" type={type} value={value} onChange={(event) => onChange(event.target.value)} />;
}
