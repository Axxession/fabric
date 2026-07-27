import { Link } from '@tanstack/react-router';

import { useCurrentActor } from '@/shared/actors/current-actor';
import { buttonVariants } from '@/shared/components/ui/button';
import { getPerspectiveById, type PerspectiveId } from '@/shared/perspectives/app-perspectives';

export function PerspectiveHomePage({ perspectiveId }: { perspectiveId: PerspectiveId }) {
  const actorQuery = useCurrentActor();
  const perspective = getPerspectiveById(perspectiveId);

  if (!perspective) {
    return null;
  }

  return (
    <section className="grid gap-6">
      <div className="rounded-structural border border-border bg-content p-6 sm:p-8">
        <p className="text-[14px] font-semibold uppercase text-primary">Perspective</p>
        <h1 className="mt-3 text-[30px] font-semibold tracking-tight">{perspective.label}</h1>
        <p className="mt-3 max-w-2xl text-[14px] leading-6 text-muted-foreground">{perspective.description}</p>
        <p className="mt-6 text-[14px] text-muted-foreground">
          {actorQuery.data?.displayName ? `Signed in as ${actorQuery.data.displayName}.` : 'Perspective shell ready.'}
        </p>
        {perspectiveId === 'employee' ? <Link to="/employee/request-access" className={`${buttonVariants()} mt-6`}>Request Access</Link> : null}
        {perspectiveId === 'manager' ? <Link to="/manager/approval-inbox" className={`${buttonVariants()} mt-6`}>Open Approval Inbox</Link> : null}
      </div>

      <div className="rounded-structural border border-dashed border-border bg-content p-6 text-[14px] text-muted-foreground">
        No pages moved into this perspective yet.
      </div>
    </section>
  );
}
