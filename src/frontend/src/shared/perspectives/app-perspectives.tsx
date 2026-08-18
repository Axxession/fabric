import { Briefcase, Building2, PlugZap, ShieldCheck, UsersRound } from 'lucide-react';
import type { ComponentType } from 'react';

import type { CurrentActor } from '@/shared/actors/current-actor';
import { i18n } from '@/shared/i18n/i18n';

export type PerspectiveId = 'employee' | 'manager' | 'security-officer' | 'integrations' | 'administration';

export type AppPerspective = {
  id: PerspectiveId;
  label: string;
  shortLabel: string;
  description: string;
  to: string;
  icon: ComponentType<{ className?: string }>;
  priority: number;
  menuItems: readonly PerspectiveMenuItem[] | ((actor: CurrentActor) => readonly PerspectiveMenuItem[]);
  isAvailable: (actor: CurrentActor) => boolean;
};

export type PerspectiveMenuItem = {
    label: string;
    description: string;
  to: string;
};

export type ResolvedAppPerspective = Omit<AppPerspective, 'menuItems'> & {
  menuItems: readonly PerspectiveMenuItem[];
};

function getAppPerspectives(): readonly AppPerspective[] {
  return [
    {
      id: 'employee',
      label: i18n.t('perspectives.employee.label'),
      shortLabel: i18n.t('perspectives.employee.shortLabel'),
      description: i18n.t('perspectives.employee.description'),
      to: '/employee',
      icon: Briefcase,
      priority: 1,
      menuItems: (actor) => [
        { label: i18n.t('perspectives.employee.menu.overview.label'), description: i18n.t('perspectives.employee.menu.overview.description'), to: '/employee' },
        { label: i18n.t('perspectives.employee.menu.requestAccess.label'), description: i18n.t('perspectives.employee.menu.requestAccess.description'), to: '/employee/request-access' },
        ...(actor.roles.includes('contractor-planning') || actor.roles.includes('contractor-enrollment') ? [{ label: i18n.t('perspectives.employee.menu.contractors.label'), description: i18n.t('perspectives.employee.menu.contractors.description'), to: '/employee/contractors' }] : []),
        ...(actor.isHost ? [{ label: i18n.t('perspectives.employee.menu.visitors.label'), description: i18n.t('perspectives.employee.menu.visitors.description'), to: '/employee/visitors' }] : []),
      ],
      isAvailable: (actor) => actor.isEmployee,
    },
    {
      id: 'manager',
      label: i18n.t('perspectives.manager.label'),
      shortLabel: i18n.t('perspectives.manager.shortLabel'),
      description: i18n.t('perspectives.manager.description'),
      to: '/manager',
      icon: UsersRound,
      priority: 2,
      menuItems: [
        { label: i18n.t('perspectives.manager.menu.overview.label'), description: i18n.t('perspectives.manager.menu.overview.description'), to: '/manager' },
        { label: i18n.t('perspectives.manager.menu.myTeam.label'), description: i18n.t('perspectives.manager.menu.myTeam.description'), to: '/manager/my-team' },
        { label: i18n.t('perspectives.manager.menu.approvalInbox.label'), description: i18n.t('perspectives.manager.menu.approvalInbox.description'), to: '/manager/approval-inbox' },
      ],
      isAvailable: (actor) => actor.isManager,
    },
    {
      id: 'security-officer',
      label: i18n.t('perspectives.securityOfficer.label'),
      shortLabel: i18n.t('perspectives.securityOfficer.shortLabel'),
      description: i18n.t('perspectives.securityOfficer.description'),
      to: '/security-officer',
      icon: ShieldCheck,
      priority: 3,
      menuItems: [
        { label: i18n.t('perspectives.securityOfficer.menu.overview.label'), description: i18n.t('perspectives.securityOfficer.menu.overview.description'), to: '/security-officer' },
        { label: i18n.t('perspectives.securityOfficer.menu.identity360.label'), description: i18n.t('perspectives.securityOfficer.menu.identity360.description'), to: '/security-officer/identities' },
      ],
      isAvailable: (actor) => actor.isSecurityOfficer,
    },
    {
      id: 'integrations',
      label: i18n.t('perspectives.integrations.label'),
      shortLabel: i18n.t('perspectives.integrations.shortLabel'),
      description: i18n.t('perspectives.integrations.description'),
      to: '/integrations',
      icon: PlugZap,
      priority: 4,
      menuItems: [
        { label: i18n.t('perspectives.integrations.menu.microsoftGraph.label'), description: i18n.t('perspectives.integrations.menu.microsoftGraph.description'), to: '/integrations/microsoft-graph' },
        { label: i18n.t('perspectives.integrations.menu.keycloak.label'), description: i18n.t('perspectives.integrations.menu.keycloak.description'), to: '/integrations/keycloak' },
      ],
      isAvailable: (actor) => actor.roles.includes('integrator'),
    },
    {
      id: 'administration',
      label: i18n.t('perspectives.administration.label'),
      shortLabel: i18n.t('perspectives.administration.shortLabel'),
      description: i18n.t('perspectives.administration.description'),
      to: '/administration',
      icon: Building2,
      priority: 5,
      menuItems: [
        { label: i18n.t('perspectives.administration.menu.sites.label'), description: i18n.t('perspectives.administration.menu.sites.description'), to: '/administration/sites' },
        { label: i18n.t('perspectives.administration.menu.myOrganization.label'), description: i18n.t('perspectives.administration.menu.myOrganization.description'), to: '/administration/my-organization' },
        { label: i18n.t('perspectives.administration.menu.accessModel.label'), description: i18n.t('perspectives.administration.menu.accessModel.description'), to: '/administration/access-model' },
        { label: i18n.t('perspectives.administration.menu.credentialTypes.label'), description: i18n.t('perspectives.administration.menu.credentialTypes.description'), to: '/administration/credential-types' },
        { label: i18n.t('perspectives.administration.menu.accessControl.label'), description: i18n.t('perspectives.administration.menu.accessControl.description'), to: '/administration/access-control' },
        { label: i18n.t('perspectives.administration.menu.clients.label'), description: i18n.t('perspectives.administration.menu.clients.description'), to: '/administration/clients' },
        { label: i18n.t('perspectives.administration.menu.automation.label'), description: i18n.t('perspectives.administration.menu.automation.description'), to: '/administration/automation' },
        { label: i18n.t('perspectives.administration.menu.notifications.label'), description: i18n.t('perspectives.administration.menu.notifications.description'), to: '/administration/notifications' },
      ],
      isAvailable: (actor) => actor.isAdmin,
    },
  ] as const;
}

export function getAvailablePerspectives(actor: CurrentActor | undefined): ResolvedAppPerspective[] {
  if (!actor) {
    return [];
  }

  return getAppPerspectives()
    .filter((perspective) => perspective.isAvailable(actor))
    .map((perspective) => ({
      ...perspective,
      menuItems: typeof perspective.menuItems === 'function' ? perspective.menuItems(actor) : perspective.menuItems,
    }))
    .sort((left, right) => left.priority - right.priority);
}

export function getPerspectiveByPathname(pathname: string) {
  return getAppPerspectives().find((perspective) => pathname === perspective.to || pathname.startsWith(`${perspective.to}/`));
}

export function getPerspectiveById(id: PerspectiveId) {
  return getAppPerspectives().find((perspective) => perspective.id === id);
}

export function getDefaultPerspective(actor: CurrentActor | undefined) {
  return getAvailablePerspectives(actor)[0];
}
