import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';

type SystemType = '' | 'unipass' | 'lenel';
type CreateUnipassAccessControlSystemRequest = components['schemas']['CreateUnipassAccessControlSystemRequest'];

type FormValues = {
  name: string;
  endpoint: string;
  sslValidation: boolean;
  username: string;
  password: string;
};

const systemsQueryKey = ['administration', 'access-control', 'systems'] as const;
const emptyFormValues: FormValues = {
  name: '',
  endpoint: '',
  sslValidation: true,
  username: '',
  password: '',
};

export default function AccessControlSystemCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [systemType, setSystemType] = useState<SystemType>('');
  const [values, setValues] = useState<FormValues>(emptyFormValues);

  const createUnipassSystem = useMutation({
    mutationFn: async (request: CreateUnipassAccessControlSystemRequest) => {
      const { data, error } = await api.POST('/api/access-control/systems/unipass', { body: request });
      if (error || !data) {
        throw new Error('Could not register access control system.');
      }
      return data;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: systemsQueryKey });
      toast.success('Access control system registered.');
      await navigate({ to: '/administration/access-control', search: { tab: 'systems' } as never, replace: true });
    },
    onError: () => {
      toast.error('Could not register access control system.');
    },
  });

  function updateValue<TKey extends keyof FormValues>(key: TKey, value: FormValues[TKey]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (systemType !== 'unipass') {
      return;
    }

    createUnipassSystem.mutate({
      name: values.name,
      endpoint: values.endpoint,
      sslValidation: values.sslValidation,
      username: values.username,
      password: values.password,
    });
  }

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>

        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Register access control system</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Choose an access control system type before entering configuration details.</p>
        </div>
      </header>

      <Card className="p-6">
        {createUnipassSystem.isError ? (
          <p className="mb-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
            Could not register access control system.
          </p>
        ) : null}

        <div className="grid gap-2 text-[14px] font-medium">
          <label htmlFor="system-type">Access control system type</label>
          <select
            id="system-type"
            className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary"
            value={systemType}
            onChange={(event) => setSystemType(event.target.value as SystemType)}
          >
            <option value="">Select system type</option>
            <option value="unipass">Unipass</option>
            <option value="lenel">Lenel</option>
          </select>
        </div>

        {systemType === 'lenel' ? (
          <div className="mt-6 rounded-structural border border-border bg-background p-4 text-[14px] text-muted-foreground">
            Lenel is not supported yet.
          </div>
        ) : null}

        {systemType === 'unipass' ? (
          <form className="mt-6 grid gap-5" onSubmit={handleSubmit}>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="grid gap-2 text-[14px] font-medium">
                Name
                <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.name} onChange={(event) => updateValue('name', event.target.value)} required />
              </label>

              <label className="grid gap-2 text-[14px] font-medium md:col-span-2">
                Endpoint
                <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.endpoint} onChange={(event) => updateValue('endpoint', event.target.value)} required />
              </label>

              <label className="grid gap-2 text-[14px] font-medium">
                Username
                <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={values.username} onChange={(event) => updateValue('username', event.target.value)} required />
              </label>

              <label className="grid gap-2 text-[14px] font-medium">
                Password
                <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" type="password" value={values.password} onChange={(event) => updateValue('password', event.target.value)} required />
              </label>
            </div>

            <label className="flex items-center gap-3 text-[14px] font-medium">
              <input type="checkbox" checked={values.sslValidation} onChange={(event) => updateValue('sslValidation', event.target.checked)} />
              Validate SSL certificate
            </label>

            <div className="flex justify-end">
              <Button type="submit" disabled={createUnipassSystem.isPending}>
                {createUnipassSystem.isPending ? 'Registering...' : 'Register access control system'}
              </Button>
            </div>
          </form>
        ) : null}
      </Card>
    </div>
  );
}
