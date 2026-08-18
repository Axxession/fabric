import { Navigate } from '@tanstack/react-router';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

import { useCurrentActor } from '@/shared/actors/current-actor';

export function IntegratorRoute({ children }: { children: ReactNode }) {
  const actorQuery = useCurrentActor();
  const { t } = useTranslation();

  if (actorQuery.isLoading) {
    return <p className="rounded-structural border border-border bg-content p-6 text-[14px] text-muted-foreground">{t('integrationsSettings.common.loading')}</p>;
  }

  if (!actorQuery.data?.roles.includes('integrator')) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
