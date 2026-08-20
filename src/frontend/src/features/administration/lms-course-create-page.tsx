import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Textarea } from '@/shared/components/ui/textarea';

type CourseResponse = components['schemas']['CourseResponse'];

export default function LmsCourseCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [code, setCode] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');

  const createCourse = useMutation({
    mutationFn: async () => {
      const { data, error } = await api.POST('/api/learning/courses', { body: { code, title: title.trim(), description: description.trim() === '' ? null : description.trim() } });
      if (error || !data) {
        throw new Error('Could not create course.');
      }
      return data as CourseResponse;
    },
    onSuccess: async (course) => {
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'courses'] });
      await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'courses-options'] });
      toast.success('Course created.');
      await navigate({ to: '/administration/lms/courses/$courseId', params: { courseId: course.id }, replace: true });
    },
    onError: () => toast.error('Could not create course.'),
  });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}>
          <ArrowLeft className="size-4" aria-hidden="true" />
        </Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Add course</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Create a new LMS course, then add languages and upload package versions under those languages.</p>
        </div>
      </header>

      <Card className="p-6">
        <form className="grid gap-5" onSubmit={(event) => { event.preventDefault(); createCourse.mutate(); }}>
          <label className="grid gap-2 text-[14px] font-medium"><span>Code</span><Input value={code} onChange={(event) => setCode(event.target.value)} required /></label>
          <label className="grid gap-2 text-[14px] font-medium"><span>Title</span><Input value={title} onChange={(event) => setTitle(event.target.value)} required /></label>
          <label className="grid gap-2 text-[14px] font-medium"><span>Description</span><Textarea value={description} onChange={(event) => setDescription(event.target.value)} rows={4} /></label>
          <div className="flex justify-end"><Button type="submit" disabled={createCourse.isPending}>{createCourse.isPending ? 'Creating...' : 'Create course'}</Button></div>
        </form>
      </Card>
    </div>
  );
}
