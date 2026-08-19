import { useMutation } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

type EnrollmentResponse = components['schemas']['EnrollmentResponse'];

export default function LmsEnrollmentCreatePage() {
  const { courseId } = useParams({ from: '/main/administration/lms/courses/$courseId/enrollments/new' });
  const navigate = useNavigate();
  const [identityId, setIdentityId] = useState('');

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
          <label className="grid gap-2 text-[14px] font-medium"><span>Identity ID</span><Input value={identityId} onChange={(event) => setIdentityId(event.target.value)} required /></label>
          <div className="flex justify-end"><Button type="submit" disabled={createEnrollment.isPending}>Save enrollment</Button></div>
        </form>
      </Card>
    </div>
  );
}
