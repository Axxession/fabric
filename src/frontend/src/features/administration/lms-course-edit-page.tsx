import { useMutation, useQuery } from '@tanstack/react-query';
import { Link, useLocation, useNavigate, useParams } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button, buttonVariants } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { Textarea } from '@/shared/components/ui/textarea';

type AttemptResponse = components['schemas']['AttemptResponse'];
type CourseLanguageResponse = components['schemas']['CourseLanguageResponse'];
type CourseCompletionReportRowResponse = components['schemas']['CourseCompletionReportRowResponse'];
type CourseResponse = components['schemas']['CourseResponse'];
type EnrollmentResponse = components['schemas']['EnrollmentResponse'];
type IdentityResponse = components['schemas']['IdentityResponse'];
type PageOfCourseLanguageResponse = { items?: CourseLanguageResponse[] };
type PageOfEnrollmentResponse = components['schemas']['PageOfEnrollmentResponse'];
type PageOfAttemptResponse = components['schemas']['PageOfAttemptResponse'];
type LmsCourseDetailTab = 'languages' | 'enrollments' | 'reporting';

export default function LmsCourseEditPage() {
  const location = useLocation();
  const { courseId } = useParams({ from: '/main/administration/lms/courses/$courseId' });
  const navigate = useNavigate();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const activeTab = getActiveTab(location.searchStr);

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
  const identityIds = [...new Set([...(enrollmentsQuery.data?.items ?? []).map((item) => item.identityId), ...(reportingQuery.data ?? []).map((item) => item.identityId)])];
  const identitiesQuery = useQuery({ queryKey: ['administration', 'lms', 'course', courseId, 'identities', identityIds], enabled: identityIds.length > 0, queryFn: async () => { const { data, error } = await api.GET('/api/identities', { params: { query: { query: undefined, status: undefined, affiliationType: undefined, page: 0, pageSize: identityIds.length || 1, ids: identityIds } } }); if (error || !data) throw new Error('Could not load course identities.'); return (data.items ?? []) as IdentityResponse[]; } });
  const identitiesById = new Map((identitiesQuery.data ?? []).map((item) => [item.id, item]));

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

      <Tabs value={activeTab} onValueChange={(value) => void navigate({ to: '/administration/lms/courses/$courseId', params: { courseId }, search: { tab: value } as never, replace: true })}>
        <TabsList className="overflow-x-auto">
          <TabsTrigger value="languages">Languages <span className="text-[12px] font-medium text-muted-foreground">{languagesQuery.data?.length ?? 0}</span></TabsTrigger>
          <TabsTrigger value="enrollments">Enrollments <span className="text-[12px] font-medium text-muted-foreground">{enrollmentsQuery.data?.items?.length ?? 0}</span></TabsTrigger>
          <TabsTrigger value="reporting">Reporting <span className="text-[12px] font-medium text-muted-foreground">{reportingQuery.data?.length ?? 0}</span></TabsTrigger>
        </TabsList>

        <TabsContent value="languages">
          <Card className="p-6">
            <div className="flex items-start justify-between gap-4">
              <div><h3 className="text-[18px] font-semibold tracking-tight">Languages</h3><p className="mt-2 text-[14px] text-muted-foreground">Manage delivery languages for this course and upload versions under each language.</p></div>
              <Link to="/administration/lms/courses/$courseId/languages/new" params={{ courseId }} className={buttonVariants({ variant: 'outline' })}>Add language</Link>
            </div>
            <div className="mt-4 overflow-x-auto rounded-structural border border-border"><table className="w-full min-w-[48rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Language</th><th className="px-4 py-3 font-semibold">Display label</th><th className="px-4 py-3 font-semibold">Status</th><th className="px-4 py-3 text-right font-semibold">Open</th></tr></thead><tbody className="divide-y divide-border">{(languagesQuery.data ?? []).map((item) => <tr key={item.id} className="cursor-pointer transition hover:bg-hover-blue" onClick={() => void navigate({ to: '/administration/lms/courses/$courseId/languages/$languageId', params: { courseId, languageId: item.id } })}><td className="px-4 py-4 text-muted-foreground">{item.languageCode}</td><td className="px-4 py-4 font-medium text-foreground">{item.displayLabel}</td><td className="px-4 py-4 text-muted-foreground">{item.isActive ? 'Active' : 'Inactive'}</td><td className="px-4 py-4 text-right text-muted-foreground">Open</td></tr>)}</tbody></table></div>
          </Card>
        </TabsContent>

        <TabsContent value="enrollments">
          <Card className="p-6">
            <div className="flex items-start justify-between gap-4">
              <div><h3 className="text-[18px] font-semibold tracking-tight">Enrollments</h3><p className="mt-2 text-[14px] text-muted-foreground">View current enrollments and add a new learner assignment.</p></div>
              <Link to="/administration/lms/courses/$courseId/enrollments/new" params={{ courseId }} className={buttonVariants({ variant: 'outline' })}>Add enrollment</Link>
            </div>
            <div className="mt-4 overflow-x-auto rounded-structural border border-border"><table className="w-full min-w-[54rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Identity</th><th className="px-4 py-3 font-semibold">Status</th><th className="px-4 py-3 font-semibold">Assigned</th><th className="px-4 py-3 font-semibold">Completed</th><th className="px-4 py-3 font-semibold">Latest attempt</th></tr></thead><tbody className="divide-y divide-border">{(enrollmentsQuery.data?.items ?? []).map((item) => <EnrollmentRow key={item.id} enrollment={item} identity={identitiesById.get(item.identityId)} />)}</tbody></table></div>
          </Card>
        </TabsContent>

        <TabsContent value="reporting">
          <Card className="p-6">
            <div><h3 className="text-[18px] font-semibold tracking-tight">Reporting</h3><p className="mt-2 text-[14px] text-muted-foreground">Review which identities completed this course and with which version and score.</p></div>
            <div className="mt-4 overflow-x-auto rounded-structural border border-border"><table className="w-full min-w-[56rem] border-collapse text-left text-[14px]"><thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground"><tr><th className="px-4 py-3 font-semibold">Identity</th><th className="px-4 py-3 font-semibold">Completed</th><th className="px-4 py-3 font-semibold">Version</th><th className="px-4 py-3 font-semibold">Score</th><th className="px-4 py-3 font-semibold">Success</th></tr></thead><tbody className="divide-y divide-border">{(reportingQuery.data ?? []).map((item) => <tr key={item.attemptId}><td className="px-4 py-4"><div><p className="font-medium text-foreground">{identitiesById.get(item.identityId)?.displayName ?? 'Unknown identity'}</p><p className="mt-1 text-[13px] text-muted-foreground">{identitiesById.get(item.identityId)?.email ?? item.identityId}</p></div></td><td className="px-4 py-4 text-muted-foreground">{new Date(item.completedAt).toLocaleString()}</td><td className="px-4 py-4 text-muted-foreground">v{item.versionNumber}</td><td className="px-4 py-4 text-muted-foreground">{item.score ?? 'n/a'}</td><td className="px-4 py-4 text-muted-foreground">{item.successStatus ?? 'n/a'}</td></tr>)}</tbody></table></div>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

function getActiveTab(search: string): LmsCourseDetailTab {
  const tab = new URLSearchParams(search).get('tab');
  return tab === 'languages' || tab === 'enrollments' || tab === 'reporting' ? tab : 'languages';
}

function EnrollmentRow({ enrollment, identity }: { readonly enrollment: EnrollmentResponse; readonly identity: IdentityResponse | undefined }) {
  const navigate = useNavigate();
  const attemptsQuery = useQuery({ queryKey: ['administration', 'lms', 'enrollment', enrollment.id, 'attempts'], queryFn: async () => { const { data, error } = await api.GET('/api/learning/enrollments/{id}/attempts', { params: { path: { id: enrollment.id }, query: { Page: 0, PageSize: 50 } } }); if (error || !data) throw new Error('Could not load attempts.'); return data as PageOfAttemptResponse; } });
  const latestAttempt = attemptsQuery.data?.items?.[0];
  return <tr className="cursor-pointer transition hover:bg-hover-blue" onClick={() => void navigate({ to: '/scorm/test/$enrollmentId', params: { enrollmentId: enrollment.id } })}><td className="px-4 py-4"><div><p className="font-medium text-foreground">{identity?.displayName ?? 'Unknown identity'}</p><p className="mt-1 text-[13px] text-muted-foreground">{identity?.email ?? enrollment.identityId}</p></div></td><td className="px-4 py-4 text-muted-foreground">{enrollment.status}</td><td className="px-4 py-4 text-muted-foreground">{new Date(enrollment.assignedAt).toLocaleString()}</td><td className="px-4 py-4 text-muted-foreground">{enrollment.completedAt ? new Date(enrollment.completedAt).toLocaleString() : 'n/a'}</td><td className="px-4 py-4 text-muted-foreground">{latestAttempt ? `${latestAttempt.status}${latestAttempt.score !== null ? ` • ${latestAttempt.score}` : ''}` : 'No attempts'}</td></tr>;
}
