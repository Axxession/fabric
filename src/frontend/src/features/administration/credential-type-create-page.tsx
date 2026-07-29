import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { getDefaultDurationInputValue, toTimeSpan } from '@/shared/components/ui/duration-input';

import { CredentialTypeForm, type CredentialTypeFormValues } from './credential-type-form';

type CreateCredentialTypeRequest = components['schemas']['CreateCredentialTypeRequest'];

const credentialTypesQueryKey = ['administration', 'credential-types'] as const;
const emptyCredentialType: CredentialTypeFormValues = {
  name: '',
  technology: 'Qr',
  allocationMode: 'Range',
  recyclePolicy: 'NeverReuse',
  recycleGracePeriod: getDefaultDurationInputValue(),
  requiresConfirmedPacsRevocation: false,
  nearLimitThreshold: '',
  identifierPrefix: '',
  identifierSuffix: '',
  identifierNumberLength: '',
  identifierPaddingDirection: 'Left',
  identifierPaddingCharacter: '',
  status: 'Active',
};

export default function CredentialTypeCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [values, setValues] = useState<CredentialTypeFormValues>(emptyCredentialType);

  const createCredentialType = useMutation({
    mutationFn: async (request: CreateCredentialTypeRequest) => {
      const { data, error } = await api.POST('/api/credential-management/credential-types', { body: request });
      if (error || !data) {
        throw new Error('Could not create credential type.');
      }

      return data;
    },
    onSuccess: async (createdCredentialType) => {
      await queryClient.invalidateQueries({ queryKey: credentialTypesQueryKey });
      toast.success('Credential type created.');
      await navigate({ to: '/administration/credential-types/$credentialTypeId/edit', params: { credentialTypeId: createdCredentialType.id }, replace: true });
    },
    onError: () => {
      toast.error('Could not create credential type.');
    },
  });

  function handleSubmit(values: CredentialTypeFormValues) {
    createCredentialType.mutate({
      name: values.name.trim(),
      technology: values.technology,
      allocationMode: values.allocationMode,
      recyclePolicy: values.allocationMode === 'Provided' ? 'NeverReuse' : values.recyclePolicy,
      recycleGracePeriod: toTimeSpan(values.recycleGracePeriod),
      requiresConfirmedPacsRevocation: values.allocationMode === 'Provided' ? false : values.requiresConfirmedPacsRevocation,
      nearLimitThreshold: values.nearLimitThreshold.trim() === '' ? null : Number(values.nearLimitThreshold),
      identifierPrefix: values.technology === 'Qr' ? values.identifierPrefix.trim() || null : null,
      identifierSuffix: values.technology === 'Qr' ? values.identifierSuffix.trim() || null : null,
      identifierNumberLength: values.technology === 'Qr' && values.identifierNumberLength.trim() !== '' ? Number(values.identifierNumberLength) : null,
      identifierPaddingDirection: values.technology === 'Qr' && values.identifierNumberLength.trim() !== '' ? values.identifierPaddingDirection : null,
      identifierPaddingCharacter: values.technology === 'Qr' && values.identifierNumberLength.trim() !== '' ? values.identifierPaddingCharacter || null : null,
    });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add credential type</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new credential type with allocation and recycle settings.</p>
        </div>
      </header>

      <Card className="p-6">
        {createCredentialType.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not create credential type.</p> : null}
        <CredentialTypeForm values={values} onChange={setValues} onSubmit={() => handleSubmit(values)} isSubmitting={createCredentialType.isPending} submitLabel="Create credential type" includeStatus={false} />
      </Card>
    </div>
  );
}
