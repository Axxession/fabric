import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

import { PersonaForm, type PersonaFormValues } from './persona-form';

type CreatePersonaRequest = components['schemas']['CreatePersonaRequest'];

const personasQueryKey = ['administration', 'my-organization', 'personas'] as const;
const emptyPersona: PersonaFormValues = { name: '' };

export default function PersonaCreatePage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const createPersona = useMutation({
    mutationFn: async (values: PersonaFormValues) => {
      const request: CreatePersonaRequest = { name: values.name };
      const { data, error } = await api.POST('/api/employees/personas', { body: request });
      if (error || !data) throw new Error('Could not create persona.');
      return data;
    },
    onSuccess: async (persona) => {
      await queryClient.invalidateQueries({ queryKey: personasQueryKey });
      toast.success('Persona created.');
      await navigate({ to: '/administration/my-organization/personas/$personaId/edit', params: { personaId: persona.id }, replace: true });
    },
    onError: () => toast.error('Could not create persona.'),
  });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add persona</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new persona in My Organization.</p>
        </div>
      </header>

      <Card className="p-6">
        {createPersona.isError ? <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not create persona.</p> : null}
        <PersonaForm initialValues={emptyPersona} isSubmitting={createPersona.isPending} submitLabel="Create persona" onSubmit={(values) => createPersona.mutate(values)} />
      </Card>
    </div>
  );
}
