import { Navigate, useNavigate } from '@tanstack/react-router';
import { CheckCircle2 } from 'lucide-react';
import { useState } from 'react';

import { ScormPlayer } from '@/features/learning/scorm-player';
import { Button } from '@/shared/components/ui/button';

import { clearActiveCourse, clearComplianceLaunch, getActiveCourse } from './reception-kiosk-compliance';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskSessionComplianceCoursePage() {
  const navigate = useNavigate();
  const [completed, setCompleted] = useState(false);
  const activeCourse = getActiveCourse();

  if (!hasReceptionKioskSettings()) return <Navigate to="/reception-kiosk/setup" replace />;
  if (!activeCourse) return <Navigate to="/reception-kiosk/session/compliance" replace />;

  async function goBackToOverview() {
    clearActiveCourse();
    if (completed) clearComplianceLaunch();
    await navigate({ to: '/reception-kiosk/session/compliance' });
  }

  if (completed) {
    return (
      <section className="w-full rounded-[2rem] border border-border bg-content p-8 text-center shadow-sm sm:p-12">
        <div className="mx-auto flex size-24 items-center justify-center rounded-full bg-success-background text-success"><CheckCircle2 className="size-12" aria-hidden="true" /></div>
        <p className="mt-8 text-[14px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">Course completed</p>
        <h2 className="mt-3 text-[36px] font-semibold tracking-tight sm:text-[56px]">{activeCourse.courseTitle}</h2>
        <p className="mx-auto mt-5 max-w-2xl text-[18px] leading-8 text-muted-foreground sm:text-[22px] sm:leading-9">Your progress has been saved. Return to the compliance overview to refresh your status.</p>
        <Button className="mt-10 h-16 rounded-[1rem] px-10 text-[20px]" onClick={() => void goBackToOverview()}>Next</Button>
      </section>
    );
  }

  return (
    <section className="w-full">
      <ScormPlayer
        token={activeCourse.token}
        onExit={() => void goBackToOverview()}
        onComplete={() => setCompleted(true)}
        showSessionHeader={false}
        iframeHeightClassName="h-[calc(100vh-10rem)] min-h-[42rem]"
      />
    </section>
  );
}
