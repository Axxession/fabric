import { Briefcase, Building2, ShieldCheck, UsersRound } from 'lucide-react';
import type { ComponentType } from 'react';

import type { CurrentActor } from '@/shared/actors/current-actor';

export type PerspectiveId = 'employee' | 'manager' | 'security-officer' | 'administration';

export type AppPerspective = {
  id: PerspectiveId;
  label: string;
  shortLabel: string;
  description: string;
  to: string;
  icon: ComponentType<{ className?: string }>;
  priority: number;
  menuItems: readonly {
    label: string;
    description: string;
    to: string;
  }[];
  isAvailable: (actor: CurrentActor) => boolean;
};

export const appPerspectives: readonly AppPerspective[] = [
  {
    id: 'employee',
    label: 'Employee',
    shortLabel: 'Employee',
    description: 'Personal workspace for employee-facing identity and access tasks.',
    to: '/employee',
    icon: Briefcase,
    priority: 1,
    menuItems: [
      { label: 'Overview', description: 'Employee homepage.', to: '/employee' },
      { label: 'Request Access', description: 'Request access packages and track your requests.', to: '/employee/request-access' },
    ],
    isAvailable: (actor) => actor.isEmployee,
  },
  {
    id: 'manager',
    label: 'Manager',
    shortLabel: 'Manager',
    description: 'Manager workspace for approvals, oversight, and team operations.',
    to: '/manager',
    icon: UsersRound,
    priority: 2,
    menuItems: [
      { label: 'Overview', description: 'Manager homepage.', to: '/manager' },
      { label: 'Approval Inbox', description: 'Review approvals you can give.', to: '/manager/approval-inbox' },
    ],
    isAvailable: (actor) => actor.isManager,
  },
  {
    id: 'security-officer',
    label: 'Security Officer',
    shortLabel: 'Security',
    description: 'Security workspace for access governance and operational control.',
    to: '/security-officer',
    icon: ShieldCheck,
    priority: 3,
    menuItems: [
      { label: 'Overview', description: 'Security Officer homepage.', to: '/security-officer' },
      { label: 'Identity 360', description: 'Review canonical identities and drill into full identity records.', to: '/security-officer/identities' },
    ],
    isAvailable: (actor) => actor.isSecurityOfficer,
  },
  {
    id: 'administration',
    label: 'Administration',
    shortLabel: 'Admin',
    description: 'Administrative workspace for organizational setup and platform control.',
    to: '/administration',
    icon: Building2,
    priority: 4,
    menuItems: [
      { label: 'Sites', description: 'Manage sites, buildings, and rooms.', to: '/administration/sites' },
      { label: 'My Organization', description: 'Review employees, organizational units, and personas.', to: '/administration/my-organization' },
      { label: 'Access Model', description: 'Catalogue, packages and access automation.', to: '/administration/access-model' },
      { label: 'Access Control', description: 'Physical Access Control Configuration.', to: '/administration/access-control' },
    ],
    isAvailable: (actor) => actor.isAdmin,
  },
] as const;

export function getAvailablePerspectives(actor: CurrentActor | undefined) {
  if (!actor) {
    return [];
  }

  return appPerspectives.filter((perspective) => perspective.isAvailable(actor)).sort((left, right) => left.priority - right.priority);
}

export function getPerspectiveByPathname(pathname: string) {
  return appPerspectives.find((perspective) => pathname === perspective.to || pathname.startsWith(`${perspective.to}/`));
}

export function getPerspectiveById(id: PerspectiveId) {
  return appPerspectives.find((perspective) => perspective.id === id);
}

export function getDefaultPerspective(actor: CurrentActor | undefined) {
  return getAvailablePerspectives(actor)[0];
}
