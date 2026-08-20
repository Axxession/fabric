import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

type CourseLanguageResponse = components['schemas']['CourseLanguageResponse'];

export default function LmsCourseLanguageCreatePage() {
  const { courseId } = useParams({ from: '/main/administration/lms/courses/$courseId/languages/new' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [languageCode, setLanguageCode] = useState('');
  const [displayLabel, setDisplayLabel] = useState('');

  const createLanguage = useMutation({
    mutationFn: async () => {
      const { data, error } = await api.POST('/api/learning/courses/{id}/languages', { params: { path: { id: courseId } }, body: { languageCode, displayLabel } } as never);
      if (error || !data) {
        throw new Error('Could not create language.');
      }
      return data as CourseLanguageResponse;
    },
    onSuccess: async (language) => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course', courseId, 'languages'] });
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'courses'] });
      toast.success('Language created.');
      await navigate({ to: '/administration/lms/courses/$courseId/languages/$languageId', params: { courseId, languageId: language.id }, replace: true });
    },
    onError: () => toast.error('Could not create language.'),
  });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}><ArrowLeft className="size-4" aria-hidden="true" /></Button>
        <div><h2 className="text-[20px] font-semibold tracking-tight">Add language</h2><p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a delivery language under this course.</p></div>
      </header>
      <Card className="p-6">
        <form className="grid gap-5" onSubmit={(event) => { event.preventDefault(); createLanguage.mutate(); }}>
          <label className="grid gap-2 text-[14px] font-medium"><span>Language code</span><Input value={languageCode} onChange={(event) => setLanguageCode(event.target.value)} required /></label>
          <label className="grid gap-2 text-[14px] font-medium"><span>Display label</span><Input value={displayLabel} onChange={(event) => setDisplayLabel(event.target.value)} required /></label>
          <div className="flex justify-end"><Button type="submit" disabled={createLanguage.isPending}>Create language</Button></div>
        </form>
      </Card>
    </div>
  );
}
