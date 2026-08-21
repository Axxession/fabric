import { Navigate, useNavigate } from '@tanstack/react-router';
import { useMutation } from '@tanstack/react-query';
import { ArrowLeft, Languages } from 'lucide-react';

import { Button } from '@/shared/components/ui/button';

import { launchCurrentReceptionKioskSessionComplianceCourse } from './reception-kiosk-api';
import { clearComplianceLaunch, getComplianceLaunch, saveActiveCourse, saveComplianceLaunch } from './reception-kiosk-compliance';
import { saveReceptionKioskResult } from './reception-kiosk-result';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskSessionComplianceLanguagePage() {
  const navigate = useNavigate();
  const launch = getComplianceLaunch();

  if (!hasReceptionKioskSettings()) return <Navigate to="/reception-kiosk/setup" replace />;
  if (!launch) return <Navigate to="/reception-kiosk/session/compliance" replace />;

  const startCourse = useMutation({
    mutationFn: async (languageId: string) => await launchCurrentReceptionKioskSessionComplianceCourse(launch.requirementDefinitionId, languageId),
    onSuccess: async (nextLaunch) => {
      saveComplianceLaunch(nextLaunch);
      if (!nextLaunch.token) return;
      saveActiveCourse({ requirementDefinitionId: nextLaunch.requirementDefinitionId, courseId: nextLaunch.courseId, courseTitle: nextLaunch.courseTitle, token: nextLaunch.token });
      await navigate({ to: '/reception-kiosk/session/compliance/course' });
    },
    onError: async (error) => {
      saveReceptionKioskResult('action-failed', error instanceof Error ? error.message : undefined);
      await navigate({ to: '/reception-kiosk/failed' });
    },
  });

  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-5 shadow-sm sm:p-8 lg:p-10">
      <button type="button" onClick={() => { clearComplianceLaunch(); void navigate({ to: '/reception-kiosk/session/compliance' }); }} className="inline-flex items-center gap-2 text-[16px] font-medium text-muted-foreground hover:text-foreground">
        <ArrowLeft className="size-5" aria-hidden="true" />
        Back
      </button>
      <div className="mt-6 rounded-[2rem] bg-hover-blue p-6 sm:p-8 text-center">
        <div className="mx-auto flex size-18 items-center justify-center rounded-full bg-content text-primary sm:size-20"><Languages className="size-9 sm:size-10" aria-hidden="true" /></div>
        <p className="mt-7 text-[13px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">Language</p>
        <h2 className="mt-3 text-[34px] font-semibold tracking-tight sm:text-[52px]">{launch.courseTitle}</h2>
        <p className="mx-auto mt-5 max-w-2xl text-[18px] leading-8 text-muted-foreground sm:text-[22px] sm:leading-9">Choose the language for this course.</p>
      </div>
      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        {launch.languages.map((language) => (
          <Button key={language.id} type="button" className="h-auto min-h-32 flex-col rounded-[1.5rem] p-6 text-[20px]" disabled={startCourse.isPending} onClick={() => startCourse.mutate(language.id)}>
            <span>{language.displayLabel}</span>
            <span className="text-[14px] font-normal opacity-80">{language.languageCode}</span>
          </Button>
        ))}
      </div>
    </section>
  );
}
