import type { ReceptionKioskComplianceCourseLaunch } from './reception-kiosk-api';

type ReceptionKioskActiveCourseState = {
  readonly requirementDefinitionId: string;
  readonly courseId: string;
  readonly courseTitle: string;
  readonly token: string;
};

const complianceLaunchKey = 'fabric.reception-kiosk.compliance-launch';
const activeCourseKey = 'fabric.reception-kiosk.compliance-course';

export function saveComplianceLaunch(launch: ReceptionKioskComplianceCourseLaunch) {
  window.sessionStorage.setItem(complianceLaunchKey, JSON.stringify(launch));
}

export function getComplianceLaunch(): ReceptionKioskComplianceCourseLaunch | null {
  const raw = window.sessionStorage.getItem(complianceLaunchKey);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as ReceptionKioskComplianceCourseLaunch;
  } catch {
    return null;
  }
}

export function clearComplianceLaunch() {
  window.sessionStorage.removeItem(complianceLaunchKey);
}

export function saveActiveCourse(state: ReceptionKioskActiveCourseState) {
  window.sessionStorage.setItem(activeCourseKey, JSON.stringify(state));
}

export function getActiveCourse(): ReceptionKioskActiveCourseState | null {
  const raw = window.sessionStorage.getItem(activeCourseKey);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as ReceptionKioskActiveCourseState;
  } catch {
    return null;
  }
}

export function clearActiveCourse() {
  window.sessionStorage.removeItem(activeCourseKey);
}
