import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Card } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';

type CourseResponse = components['schemas']['CourseResponse'];
type LearningRequirementRuleResponse = components['schemas']['LearningRequirementRuleResponse'];
type RequirementDefinitionResponse = components['schemas']['RequirementDefinitionResponse'];
type PageOfRequirementDefinitionResponse = components['schemas']['PageOfRequirementDefinitionResponse'];
type PageOfCourseResponse = components['schemas']['PageOfCourseResponse'];

export default function LmsCourseRequirementCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [requirementDefinitionId, setRequirementDefinitionId] = useState('');
  const [courseId, setCourseId] = useState('');
  const [satisfactionMode, setSatisfactionMode] = useState<'Completion' | 'MinimumScore'>('Completion');
  const [minimumScore, setMinimumScore] = useState('');

  const requirementsQuery = useQuery({ queryKey: ['administration', 'lms', 'requirements-options'], queryFn: async () => { const { data, error } = await api.GET('/api/requirements/definitions', { params: { query: { Query: undefined, IsActive: true, LocationId: undefined, Page: 0, PageSize: 200 } as never } }); if (error || !data) throw new Error('Could not load requirements.'); return (data as PageOfRequirementDefinitionResponse).items?.filter((item) => item.fulfillmentKind === 'Learning') ?? []; } });
  const coursesQuery = useQuery({ queryKey: ['administration', 'lms', 'courses-options'], queryFn: async () => { const { data, error } = await api.GET('/api/learning/courses', { params: { query: { Query: undefined, IsActive: true, Page: 0, PageSize: 200 } as never } }); if (error || !data) throw new Error('Could not load courses.'); return (data as PageOfCourseResponse).items ?? []; } });

  const createRule = useMutation({
    mutationFn: async () => {
      const { data, error } = await api.POST('/api/sagas/learning-requirements/course-rules', { body: { requirementDefinitionId, courseId, satisfactionMode, minimumScore: satisfactionMode === 'MinimumScore' && minimumScore.trim() ? Number(minimumScore) : null } });
      if (error || !data) throw new Error('Could not save course requirement.');
      return data as LearningRequirementRuleResponse;
    },
    onSuccess: async (rule) => { await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course-requirements'] }); toast.success('Course requirement saved.'); await navigate({ to: '/administration/lms/course-requirements/$ruleId', params: { ruleId: rule.id }, replace: true }); },
    onError: () => toast.error('Could not save course requirement.'),
  });

  return <RuleFormLayout title="Add course requirement" description="Map a learning-fulfillable requirement to a course." onBack={() => window.history.back()}>
    <RuleFormBody
      requirementDefinitionId={requirementDefinitionId}
      setRequirementDefinitionId={setRequirementDefinitionId}
      courseId={courseId}
      setCourseId={setCourseId}
      satisfactionMode={satisfactionMode}
      setSatisfactionMode={setSatisfactionMode}
      minimumScore={minimumScore}
      setMinimumScore={setMinimumScore}
      requirements={requirementsQuery.data ?? []}
      courses={coursesQuery.data ?? []}
      submitLabel="Create mapping"
      isSubmitting={createRule.isPending}
      onSubmit={() => createRule.mutate()}
    />
  </RuleFormLayout>;
}

export function RuleFormLayout({ title, description, onBack, children }: { readonly title: string; readonly description: string; readonly onBack: () => void; readonly children: React.ReactNode; }) {
  return <div className="grid gap-6"><header className="flex items-start gap-4"><Button variant="outline" size="icon" aria-label="Go back" onClick={onBack}><ArrowLeft className="size-4" aria-hidden="true" /></Button><div><h2 className="text-[20px] font-semibold tracking-tight">{title}</h2><p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{description}</p></div></header><Card className="p-6">{children}</Card></div>;
}

export function RuleFormBody({ requirementDefinitionId, setRequirementDefinitionId, courseId, setCourseId, satisfactionMode, setSatisfactionMode, minimumScore, setMinimumScore, requirements, courses, submitLabel, isSubmitting, onSubmit }: { readonly requirementDefinitionId: string; readonly setRequirementDefinitionId: (value: string) => void; readonly courseId: string; readonly setCourseId: (value: string) => void; readonly satisfactionMode: 'Completion' | 'MinimumScore'; readonly setSatisfactionMode: (value: 'Completion' | 'MinimumScore') => void; readonly minimumScore: string; readonly setMinimumScore: (value: string) => void; readonly requirements: RequirementDefinitionResponse[]; readonly courses: CourseResponse[]; readonly submitLabel: string; readonly isSubmitting: boolean; readonly onSubmit: () => void; }) {
  return <form className="grid gap-5" onSubmit={(event) => { event.preventDefault(); onSubmit(); }}>
    <label className="grid gap-2 text-[14px] font-medium"><span>Requirement</span><select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={requirementDefinitionId} onChange={(event) => setRequirementDefinitionId(event.target.value)} required><option value="">Select requirement</option>{requirements.map((item) => <option key={item.id} value={item.id}>{item.name} ({item.code})</option>)}</select></label>
    <label className="grid gap-2 text-[14px] font-medium"><span>Course</span><select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={courseId} onChange={(event) => setCourseId(event.target.value)} required><option value="">Select course</option>{courses.map((item) => <option key={item.id} value={item.id}>{item.title} ({item.code})</option>)}</select></label>
    <label className="grid gap-2 text-[14px] font-medium"><span>Satisfaction mode</span><select className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={satisfactionMode} onChange={(event) => setSatisfactionMode(event.target.value as 'Completion' | 'MinimumScore')}><option value="Completion">Completion</option><option value="MinimumScore">Minimum score</option></select></label>
    {satisfactionMode === 'MinimumScore' ? <label className="grid gap-2 text-[14px] font-medium"><span>Minimum score</span><Input value={minimumScore} onChange={(event) => setMinimumScore(event.target.value)} type="number" min="0" step="0.01" required /></label> : null}
    <div className="flex justify-end"><Button type="submit" disabled={isSubmitting}>{submitLabel}</Button></div>
  </form>;
}
