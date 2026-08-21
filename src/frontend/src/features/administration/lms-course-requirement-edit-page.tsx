import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { useState } from 'react';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';

import { RuleFormBody, RuleFormLayout } from './lms-course-requirement-create-page';
type CourseResponse = components['schemas']['CourseResponse'];
type LearningRequirementRuleResponse = components['schemas']['LearningRequirementRuleResponse'];
type RequirementDefinitionResponse = components['schemas']['RequirementDefinitionResponse'];
type PageOfRequirementDefinitionResponse = components['schemas']['PageOfRequirementDefinitionResponse'];
type PageOfCourseResponse = components['schemas']['PageOfCourseResponse'];

export default function LmsCourseRequirementEditPage() {
  const { ruleId } = useParams({ from: '/main/administration/lms/course-requirements/$ruleId' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [requirementDefinitionId, setRequirementDefinitionId] = useState('');
  const [courseId, setCourseId] = useState('');
  const [satisfactionMode, setSatisfactionMode] = useState<'Completion' | 'MinimumScore'>('Completion');
  const [minimumScore, setMinimumScore] = useState('');

  const ruleQuery = useQuery({ queryKey: ['administration', 'lms', 'course-requirement', ruleId], queryFn: async () => { const { data, error } = await api.GET('/api/sagas/learning-requirements/course-rules/{id}', { params: { path: { id: ruleId } } }); if (error || !data) throw new Error('Could not load rule.'); setRequirementDefinitionId(data.requirementDefinitionId); setCourseId(data.courseId); setSatisfactionMode(data.satisfactionMode); setMinimumScore(data.minimumScore?.toString() ?? ''); return data as LearningRequirementRuleResponse; } });
  const requirementsQuery = useQuery({ queryKey: ['administration', 'lms', 'requirements-options'], queryFn: async () => { const { data, error } = await api.GET('/api/requirements/definitions', { params: { query: { Query: undefined, IsActive: true, LocationId: undefined, Page: 0, PageSize: 200 } as never } }); if (error || !data) throw new Error('Could not load requirements.'); return (data as PageOfRequirementDefinitionResponse).items?.filter((item) => item.allowedEvidenceKinds?.includes('CourseCompletion')) ?? []; } });
  const coursesQuery = useQuery({ queryKey: ['administration', 'lms', 'courses-options'], queryFn: async () => { const { data, error } = await api.GET('/api/learning/courses', { params: { query: { Query: undefined, IsActive: true, Page: 0, PageSize: 200 } as never } }); if (error || !data) throw new Error('Could not load courses.'); return (data as PageOfCourseResponse).items ?? []; } });

  const updateRule = useMutation({ mutationFn: async () => { const { data, error } = await api.PUT('/api/sagas/learning-requirements/course-rules/{id}', { params: { path: { id: ruleId } }, body: { requirementDefinitionId, courseId, satisfactionMode, minimumScore: satisfactionMode === 'MinimumScore' && minimumScore.trim() ? Number(minimumScore) : null } }); if (error || !data) throw new Error('Could not save course requirement.'); return data as LearningRequirementRuleResponse; }, onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course-requirements'] }); await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course-requirement', ruleId] }); toast.success('Course requirement saved.'); }, onError: () => toast.error('Could not save course requirement.') });
  const toggleRule = useMutation({ mutationFn: async () => { const { data, error } = await api.PUT('/api/sagas/learning-requirements/course-rules/{id}/enabled', { params: { path: { id: ruleId } }, body: { isEnabled: !ruleQuery.data?.isEnabled } }); if (error || !data) throw new Error('Could not update rule status.'); return data as LearningRequirementRuleResponse; }, onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course-requirements'] }); await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course-requirement', ruleId] }); toast.success('Rule status updated.'); }, onError: () => toast.error('Could not update rule status.') });
  const deleteRule = useMutation({ mutationFn: async () => { const { error } = await api.DELETE('/api/sagas/learning-requirements/course-rules/{id}', { params: { path: { id: ruleId } } }); if (error) throw new Error('Could not delete course requirement.'); }, onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['administration', 'lms', 'course-requirements'] }); toast.success('Course requirement deleted.'); await navigate({ to: '/administration/lms', search: { tab: 'course-requirements' } as never, replace: true }); }, onError: () => toast.error('Could not delete course requirement.') });

  return <RuleFormLayout title="Edit course requirement" description="Update which course satisfies this learning requirement." onBack={() => window.history.back()}>
    <div className="mb-4 flex justify-end gap-2"><Button type="button" variant="outline" onClick={() => toggleRule.mutate()}>{ruleQuery.data?.isEnabled ? 'Disable' : 'Enable'}</Button><Button type="button" variant="outline" onClick={() => deleteRule.mutate()}><Trash2 className="size-4" aria-hidden="true" />Delete</Button></div>
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
      submitLabel="Save mapping"
      isSubmitting={updateRule.isPending}
      onSubmit={() => updateRule.mutate()}
    />
  </RuleFormLayout>;
}
