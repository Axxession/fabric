import { useMutation, useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Textarea } from '@/shared/components/ui/textarea';

type AttemptResponse = components['schemas']['AttemptResponse'];
type CourseLanguageResponse = components['schemas']['CourseLanguageResponse'];
type CourseCompletionReportRowResponse = components['schemas']['CourseCompletionReportRowResponse'];
type CourseResponse = components['schemas']['CourseResponse'];
type EnrollmentResponse = components['schemas']['EnrollmentResponse'];
type PageOfCourseLanguageResponse = { items?: CourseLanguageResponse[] };
type PageOfEnrollmentResponse = components['schemas']['PageOfEnrollmentResponse'];
type PageOfAttemptResponse = components['schemas']['PageOfAttemptResponse'];

export default function LmsCourseEditPage() {
  const { courseId } = useParams({ from: '/main/administration/lms/courses/$courseId' });
  const navigate = useNavigate();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');

  const courseQuery = useQuery({
    queryKey: ['administration', 'lms', 'course', courseId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/learning/courses/{id}', { params: { path: { id: courseId } } });
      if (error || !data) {
        throw new Error('Could not load course.');
      }
      setTitle(data.title);
      setDescription(data.description ?? '');
      return data as CourseResponse;
    },
  });

  const languagesQuery = useQuery({ queryKey: ['administration', 'lms', 'course', courseId, 'languages'], queryFn: async () => { const { data, error } = await api.GET('/api/learning/courses/{id}/languages', { params: { path: { id: courseId } } }); if (error || !data) throw new Error('Could not load course languages.'); return data as CourseLanguageResponse[]; } });
  const enrollmentsQuery = useQuery({ queryKey: ['administration', 'lms', 'course', courseId, 'enrollments'], queryFn: async () => { const { data, error } = await api.GET('/api/learning/enrollments', { params: { query: { CourseId: courseId, IdentityId: undefined, Status: undefined, Page: 0, PageSize: 200 } as never } }); if (error || !data) throw new Error('Could not load enrollments.'); return data as PageOfEnrollmentResponse; } });
  const reportingQuery = useQuery({ queryKey: ['administration', 'lms', 'course', courseId, 'reporting'], queryFn: async () => { const { data, error } = await api.GET('/api/learning/courses/{id}/reporting', { params: { path: { id: courseId } } }); if (error || !data) throw new Error('Could not load reporting.'); return data as CourseCompletionReportRowResponse[]; } });

  const updateCourse = useMutation({ mutationFn: async () => { const { data, error } = await api.PUT('/api/learning/courses/{id}', { params: { path: { id: courseId } }, body: { title, description: description.trim() === '' ? null : description } }); if (error || !data) throw new Error('Could not save course.'); return data as CourseResponse; }, onSuccess: () => toast.success('Course saved.'), onError: () => toast.error('Could not save course.') });
  const toggleCourse = useMutation({ mutationFn: async () => { const path = courseQuery.data?.isActive ? '/api/learning/courses/{id}/deactivate' : '/api/learning/courses/{id}/activate'; const { data, error } = await api.POST(path, { params: { path: { id: courseId } } } as never); if (error || !data) throw new Error('Could not update course status.'); return data as CourseResponse; }, onSuccess: () => toast.success('Course status updated.'), onError: () => toast.error('Could not update course status.') });

  return (
    <div className="grid gap-6">
      <header className="flex items-start gap-4">
        <Button variant="outline" size="icon" aria-label="Go back" onClick={() => window.history.back()}><ArrowLeft className="size-4" aria-hidden="true" /></Button>
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Course detail</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Edit course metadata, upload new versions, manage enrollments, and review completions.</p>
        </div>
      </header>

      <Card className="p-6">
        {courseQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading course...</p> : null}
        {courseQuery.data ? <form className="grid gap-5" onSubmit={(event) => { event.preventDefault(); updateCourse.mutate(); }}>
          <label className="grid gap-2 text-[14px] font-medium"><span>Code</span><Input value={courseQuery.data.code} disabled /></label>
          <label className="grid gap-2 text-[14px] font-medium"><span>Title</span><Input value={title} onChange={(event) => setTitle(event.target.value)} required /></label>
          <label className="grid gap-2 text-[14px] font-medium"><span>Description</span><Textarea value={description} onChange={(event) => setDescription(event.target.value)} rows={4} /></label>
          <div className="flex gap-2 justify-end">
            <Button type="button" variant="outline" onClick={() => toggleCourse.mutate()}>{courseQuery.data.isActive ? 'Deactivate' : 'Activate'}</Button>
            <Button type="submit" disabled={updateCourse.isPending}>Save</Button>
          </div>
        </form> : null}
      </Card>

      <Card className="p-6">
        <div className="flex items-start justify-between gap-4">
          <div><h3 className="text-[18px] font-semibold tracking-tight">Languages</h3><p className="mt-2 text-[14px] text-muted-foreground">Manage delivery languages for this course and upload versions under each language.</p></div>
          <Link to="/administration/lms/courses/$courseId/languages/new" params={{ courseId }} className={buttonVariants({ variant: 'outline' })}>Add language</Link>
        </div>
        <div className="mt-4 overflow-x-auto rounded-structural border border-border"><table className="w-full min-w-[48rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Language</th><th className="px-4 py-3 font-semibold">Display label</th><th className="px-4 py-3 font-semibold">Status</th><th className="px-4 py-3 text-right font-semibold">Open</th></tr></thead><tbody className="divide-y divide-border">{(languagesQuery.data ?? []).map((item) => <tr key={item.id} className="cursor-pointer transition hover:bg-hover-blue" onClick={() => void navigate({ to: '/administration/lms/courses/$courseId/languages/$languageId', params: { courseId, languageId: item.id } })}><td className="px-4 py-4 text-muted-foreground">{item.languageCode}</td><td className="px-4 py-4 font-medium text-foreground">{item.displayLabel}</td><td className="px-4 py-4 text-muted-foreground">{item.isActive ? 'Active' : 'Inactive'}</td><td className="px-4 py-4 text-right text-muted-foreground">Open</td></tr>)}</tbody></table></div>
      </Card>

      <Card className="p-6">
        <div className="flex items-start justify-between gap-4">
          <div><h3 className="text-[18px] font-semibold tracking-tight">Enrollments</h3><p className="mt-2 text-[14px] text-muted-foreground">View current enrollments and add a new learner assignment.</p></div>
          <Link to="/administration/lms/courses/$courseId/enrollments/new" params={{ courseId }} className={buttonVariants({ variant: 'outline' })}>Add enrollment</Link>
        </div>
        <div className="mt-4 overflow-x-auto rounded-structural border border-border"><table className="w-full min-w-[54rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Identity</th><th className="px-4 py-3 font-semibold">Status</th><th className="px-4 py-3 font-semibold">Assigned</th><th className="px-4 py-3 font-semibold">Completed</th><th className="px-4 py-3 font-semibold">Latest attempt</th></tr></thead><tbody className="divide-y divide-border">{(enrollmentsQuery.data?.items ?? []).map((item) => <EnrollmentRow key={item.id} enrollment={item} />)}</tbody></table></div>
      </Card>

      <Card className="p-6">
        <div><h3 className="text-[18px] font-semibold tracking-tight">Reporting</h3><p className="mt-2 text-[14px] text-muted-foreground">Review which identities completed this course and with which version and score.</p></div>
        <div className="mt-4 overflow-x-auto rounded-structural border border-border"><table className="w-full min-w-[56rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Identity</th><th className="px-4 py-3 font-semibold">Completed</th><th className="px-4 py-3 font-semibold">Version</th><th className="px-4 py-3 font-semibold">Score</th><th className="px-4 py-3 font-semibold">Success</th></tr></thead><tbody className="divide-y divide-border">{(reportingQuery.data ?? []).map((item) => <tr key={item.attemptId}><td className="px-4 py-4 text-muted-foreground">{item.identityId}</td><td className="px-4 py-4 text-muted-foreground">{new Date(item.completedAt).toLocaleString()}</td><td className="px-4 py-4 text-muted-foreground">v{item.versionNumber}</td><td className="px-4 py-4 text-muted-foreground">{item.score ?? 'n/a'}</td><td className="px-4 py-4 text-muted-foreground">{item.successStatus ?? 'n/a'}</td></tr>)}</tbody></table></div>
      </Card>
    </div>
  );
}

function EnrollmentRow({ enrollment }: { readonly enrollment: EnrollmentResponse }) {
  const attemptsQuery = useQuery({ queryKey: ['administration', 'lms', 'enrollment', enrollment.id, 'attempts'], queryFn: async () => { const { data, error } = await api.GET('/api/learning/enrollments/{id}/attempts', { params: { path: { id: enrollment.id }, query: { Page: 0, PageSize: 50 } } }); if (error || !data) throw new Error('Could not load attempts.'); return data as PageOfAttemptResponse; } });
  const latestAttempt = attemptsQuery.data?.items?.[0];
  return <tr><td className="px-4 py-4 text-muted-foreground">{enrollment.identityId}</td><td className="px-4 py-4 text-muted-foreground">{enrollment.status}</td><td className="px-4 py-4 text-muted-foreground">{new Date(enrollment.assignedAt).toLocaleString()}</td><td className="px-4 py-4 text-muted-foreground">{enrollment.completedAt ? new Date(enrollment.completedAt).toLocaleString() : 'n/a'}</td><td className="px-4 py-4 text-muted-foreground">{latestAttempt ? `${latestAttempt.status}${latestAttempt.score !== null ? ` • ${latestAttempt.score}` : ''}` : 'No attempts'}</td></tr>;
}
