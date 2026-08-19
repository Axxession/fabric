import { useMutation, useQuery } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useRef, useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Combobox, ComboboxContent, ComboboxEmpty, ComboboxInput, ComboboxItem, ComboboxList } from '@/shared/components/ui/combobox';

type EnrollmentResponse = components['schemas']['EnrollmentResponse'];
type IdentityResponse = components['schemas']['IdentityResponse'];

export default function LmsEnrollmentCreatePage() {
  const { courseId } = useParams({ from: '/main/administration/lms/courses/$courseId/enrollments/new' });
  const navigate = useNavigate();
  const identityAnchorRef = useRef<HTMLDivElement | null>(null);
  const [identityId, setIdentityId] = useState('');
  const [identityQuery, setIdentityQuery] = useState('');
  const [selectedIdentity, setSelectedIdentity] = useState<IdentityResponse | null>(null);

  const identitiesQuery = useQuery({
    queryKey: ['administration', 'lms', 'enrollment', 'identities', identityQuery],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/identities', {
        params: { query: { query: identityQuery.trim() || undefined, status: 'Active', affiliationType: undefined, page: 0, pageSize: 25 } },
      });
      if (error) throw new Error('Could not load identities.');
      return (data?.items ?? []) as IdentityResponse[];
    },
  });

  const createEnrollment = useMutation({
    mutationFn: async () => {
      const { data, error } = await api.POST('/api/learning/enrollments/upsert', { body: { courseId, identityId } });
      if (error || !data) {
        throw new Error('Could not save enrollment.');
      }
      return data as EnrollmentResponse;
    },
    onSuccess: async () => {
      toast.success('Enrollment saved.');
      await navigate({ to: '/administration/lms/courses/$courseId', params: { courseId }, replace: true });
    },
    onError: () => toast.error('Could not save enrollment.'),
  });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}><ArrowLeft className="size-4" aria-hidden="true" /></Button>
        <div><h2 className="text-[20px] font-semibold tracking-tight">Add enrollment</h2><p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create or reuse the active enrollment for this course and identity.</p></div>
      </header>
      <Card className="p-6">
        <form className="grid gap-5" onSubmit={(event) => { event.preventDefault(); createEnrollment.mutate(); }}>
          <div className="grid gap-2 text-[14px] font-medium">
            <span>Identity</span>
            <div ref={identityAnchorRef}>
              <Combobox
                value={selectedIdentity}
                onValueChange={(value) => {
                  setSelectedIdentity(value);
                  setIdentityId(value?.id ?? '');
                  setIdentityQuery(value ? formatIdentityLabel(value) : '');
                }}
                inputValue={identityQuery}
                onInputValueChange={(value) => {
                  setIdentityQuery(value);
                  if (selectedIdentity && value !== formatIdentityLabel(selectedIdentity)) {
                    setSelectedIdentity(null);
                    setIdentityId('');
                  }
                }}
                items={identitiesQuery.data ?? []}
                itemToStringLabel={(identity) => identity ? formatIdentityLabel(identity) : ''}
              >
                <ComboboxInput placeholder="Search identities..." showClear disabled={createEnrollment.isPending} />
                <ComboboxContent anchor={identityAnchorRef.current}>
                  <ComboboxEmpty>{identitiesQuery.isLoading ? 'Loading identities...' : 'No active identities found.'}</ComboboxEmpty>
                  <ComboboxList>
                    {(identity) => <ComboboxItem key={identity.id} value={identity}><div className="min-w-0"><p className="truncate font-medium text-foreground">{identity.displayName}</p><p className="truncate text-[12px] text-muted-foreground">{identity.email ?? identity.id}</p></div></ComboboxItem>}
                  </ComboboxList>
                </ComboboxContent>
              </Combobox>
            </div>
          </div>
          <div className="flex justify-end"><Button type="submit" disabled={createEnrollment.isPending || !identityId}>Save enrollment</Button></div>
        </form>
      </Card>
    </div>
  );
}

function formatIdentityLabel(identity: IdentityResponse) {
  return identity.email ? `${identity.displayName} (${identity.email})` : identity.displayName;
}
