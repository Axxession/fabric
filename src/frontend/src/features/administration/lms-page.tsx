import { useQuery } from '@tanstack/react-query';
import { Link, useLocation, useNavigate } from '@tanstack/react-router';
import { ChevronRight } from 'lucide-react';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { buttonVariants } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

type LmsTab = 'courses' | 'course-requirements';
type CourseResponse = components['schemas']['CourseResponse'];
type LearningRequirementRuleResponse = components['schemas']['LearningRequirementRuleResponse'];
type RequirementDefinitionResponse = components['schemas']['RequirementDefinitionResponse'];
type PageOfCourseResponse = components['schemas']['PageOfCourseResponse'];
type PageOfLearningRequirementRuleResponse = components['schemas']['PageOfLearningRequirementRuleResponse'];
type PageOfRequirementDefinitionResponse = components['schemas']['PageOfRequirementDefinitionResponse'];

export default function LmsPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = getActiveTab(location.searchStr);

  const coursesQuery = useQuery({
    queryKey: ['administration', 'lms', 'courses'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/learning/courses', { params: { query: { Query: undefined, IsActive: undefined, Page: 0, PageSize: 200 } as never } });
      if (error || !data) {
        throw new Error('Could not load courses.');
      }
      return data as PageOfCourseResponse;
    },
  });

  const courseRequirementsQuery = useQuery({
    queryKey: ['administration', 'lms', 'course-requirements'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/sagas/learning-requirements/course-rules', { params: { query: { Page: 0, PageSize: 200 } as never } });
      if (error || !data) {
        throw new Error('Could not load course requirements.');
      }
      return data as PageOfLearningRequirementRuleResponse;
    },
  });

  const ruleRequirementIds = Array.from(new Set((courseRequirementsQuery.data?.items ?? []).map((item) => item.requirementDefinitionId)));
  const ruleCourseIds = Array.from(new Set((courseRequirementsQuery.data?.items ?? []).map((item) => item.courseId)));

  const requirementsByIdQuery = useQuery({
    queryKey: ['administration', 'lms', 'course-requirements', 'requirements-by-id', ruleRequirementIds.join(',')],
    enabled: ruleRequirementIds.length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/requirements/definitions', { params: { query: { Ids: ruleRequirementIds, Query: undefined, IsActive: undefined, LocationId: undefined, Page: 0, PageSize: 200 } as never } });
      if (error || !data) throw new Error('Could not load requirement names.');
      return new Map(((data as PageOfRequirementDefinitionResponse).items ?? []).map((item) => [item.id, item]));
    },
  });

  const coursesByIdQuery = useQuery({
    queryKey: ['administration', 'lms', 'course-requirements', 'courses-by-id', ruleCourseIds.join(',')],
    enabled: ruleCourseIds.length > 0,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/learning/courses', { params: { query: { Ids: ruleCourseIds, Query: undefined, IsActive: undefined, Page: 0, PageSize: 200 } as never } });
      if (error || !data) throw new Error('Could not load course names.');
      return new Map(((data as PageOfCourseResponse).items ?? []).map((item) => [item.id, item]));
    },
  });

  return (
    <section className="rounded-structural border border-border bg-content p-4 sm:p-6">
      <Tabs value={activeTab} onValueChange={(value) => void navigate({ to: '/administration/lms', search: { tab: value } as never })}>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h2 className="text-[20px] font-semibold tracking-tight">LMS</h2>
            <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Manage SCORM courses, enrollments, reporting, and course-to-requirement mappings.</p>
          </div>
          <div className="flex gap-2">
            {activeTab === 'courses' ? <Link to="/administration/lms/courses/new" className={buttonVariants()}>Add course</Link> : <Link to="/administration/lms/course-requirements/new" className={buttonVariants()}>Add course requirement</Link>}
          </div>
        </div>

        <div className="mt-5">
          <TabsList>
            <TabsTrigger value="courses">Courses</TabsTrigger>
            <TabsTrigger value="course-requirements">Course Requirements</TabsTrigger>
          </TabsList>
        </div>

        <TabsContent value="courses" className="mt-5">
          <Card className="p-0">
            {coursesQuery.isLoading ? <p className="p-6 text-[14px] text-muted-foreground">Loading courses...</p> : null}
            {coursesQuery.isError ? <p className="p-6 text-[14px] text-error">Could not load courses.</p> : null}
            {!coursesQuery.isLoading && !coursesQuery.isError && (coursesQuery.data?.items ?? []).length === 0 ? (
              <Empty className="p-6">
                  <EmptyHeader>
                    <EmptyTitle>No courses</EmptyTitle>
                    <EmptyDescription>Create the first course to get started.</EmptyDescription>
                  </EmptyHeader>
                </Empty>
            ) : null}
            {!coursesQuery.isLoading && !coursesQuery.isError && (coursesQuery.data?.items ?? []).length > 0 ? (
              <div className="overflow-x-auto rounded-structural border border-border">
                <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
                  <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                    <tr>
                      <th className="px-4 py-3 font-semibold">Title</th>
                      <th className="px-4 py-3 font-semibold">Code</th>
                      <th className="px-4 py-3 font-semibold">Status</th>
                      <th className="px-4 py-3 text-right font-semibold">Open</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {(coursesQuery.data?.items ?? []).map((item) => (
                      <tr key={item.id} className="cursor-pointer transition hover:bg-hover-blue" onClick={() => void navigate({ to: '/administration/lms/courses/$courseId', params: { courseId: item.id } })}>
                        <td className="px-4 py-4"><div><p className="font-medium text-foreground">{item.title}</p>{item.description ? <p className="mt-1 text-[13px] text-muted-foreground">{item.description}</p> : null}</div></td>
                        <td className="px-4 py-4 text-muted-foreground">{item.code}</td>
                        <td className="px-4 py-4">{item.isActive ? <Badge variant="success">Active</Badge> : <Badge variant="secondary">Inactive</Badge>}</td>
                        <td className="px-4 py-4 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : null}
          </Card>
        </TabsContent>

        <TabsContent value="course-requirements" className="mt-5">
          <Card className="p-0">
            {courseRequirementsQuery.isLoading ? <p className="p-6 text-[14px] text-muted-foreground">Loading course requirements...</p> : null}
            {courseRequirementsQuery.isError ? <p className="p-6 text-[14px] text-error">Could not load course requirements.</p> : null}
            {!courseRequirementsQuery.isLoading && !courseRequirementsQuery.isError && (courseRequirementsQuery.data?.items ?? []).length === 0 ? (
              <Empty className="p-6">
                <EmptyHeader>
                  <EmptyTitle>No course requirements</EmptyTitle>
                  <EmptyDescription>Create the first LMS requirement mapping.</EmptyDescription>
                </EmptyHeader>
              </Empty>
            ) : null}
            {!courseRequirementsQuery.isLoading && !courseRequirementsQuery.isError && (courseRequirementsQuery.data?.items ?? []).length > 0 ? (
              <div className="overflow-x-auto rounded-structural border border-border">
                <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
                  <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                    <tr>
                      <th className="px-4 py-3 font-semibold">Requirement</th>
                      <th className="px-4 py-3 font-semibold">Course</th>
                      <th className="px-4 py-3 font-semibold">Mode</th>
                      <th className="px-4 py-3 font-semibold">Enabled</th>
                      <th className="px-4 py-3 text-right font-semibold">Open</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {(courseRequirementsQuery.data?.items ?? []).map((item) => (
                      <tr key={item.id} className="cursor-pointer transition hover:bg-hover-blue" onClick={() => void navigate({ to: '/administration/lms/course-requirements/$ruleId', params: { ruleId: item.id } })}>
                        <td className="px-4 py-4">{requirementsByIdQuery.data?.get(item.requirementDefinitionId) ? <div><p className="font-medium text-foreground">{requirementsByIdQuery.data.get(item.requirementDefinitionId)?.name}</p><p className="mt-1 text-[13px] text-muted-foreground">{requirementsByIdQuery.data.get(item.requirementDefinitionId)?.code}</p></div> : <span className="text-muted-foreground">{item.requirementDefinitionId}</span>}</td>
                        <td className="px-4 py-4">{coursesByIdQuery.data?.get(item.courseId) ? <div><p className="font-medium text-foreground">{coursesByIdQuery.data.get(item.courseId)?.title}</p><p className="mt-1 text-[13px] text-muted-foreground">{coursesByIdQuery.data.get(item.courseId)?.code}</p></div> : <span className="text-muted-foreground">{item.courseId}</span>}</td>
                        <td className="px-4 py-4 text-muted-foreground">{item.satisfactionMode}</td>
                        <td className="px-4 py-4">{item.isEnabled ? <Badge variant="success">Enabled</Badge> : <Badge variant="secondary">Disabled</Badge>}</td>
                        <td className="px-4 py-4 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : null}
          </Card>
        </TabsContent>
      </Tabs>
    </section>
  );
}

function getActiveTab(search: string): LmsTab {
  const params = new URLSearchParams(search);
  return params.get('tab') === 'course-requirements' ? 'course-requirements' : 'courses';
}
