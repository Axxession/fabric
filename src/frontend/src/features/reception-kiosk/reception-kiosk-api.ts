import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';

import { clearActiveCourse, clearComplianceLaunch } from './reception-kiosk-compliance';
import { clearOnboardingState } from './reception-kiosk-onboarding';
import { getReceptionKioskSettings } from './reception-kiosk-settings';

export type ReceptionKioskExpectedArrival = components['schemas']['ReceptionKioskExpectedArrivalResponse'];
type IdentityVerificationMethod = components['schemas']['IdentityVerificationMethod'];
type ProblemDetails = components['schemas']['ProblemDetails'];
export type ReceptionKioskComplianceStatus = 'Compliant' | 'TemporarilyCompliant' | 'NonCompliant';
export type ReceptionKioskRequirementStatus = 'Fulfilled' | 'Missing' | 'Failed' | 'Expired';

export type ReceptionKioskComplianceOverview = {
  readonly status: ReceptionKioskComplianceStatus;
  readonly requirements: readonly ReceptionKioskComplianceRequirement[];
};

export type ReceptionKioskComplianceRequirement = {
  readonly requirementDefinitionId: string;
  readonly code: string;
  readonly name: string;
  readonly isBlocking: boolean;
  readonly status: ReceptionKioskRequirementStatus;
  readonly reason: string;
  readonly validUntil: string | null;
  readonly course: ReceptionKioskComplianceCourse | null;
};

export type ReceptionKioskComplianceCourse = {
  readonly courseId: string;
  readonly courseCode: string;
  readonly courseTitle: string;
};

export type ReceptionKioskComplianceCourseLaunch = {
  readonly requirementDefinitionId: string;
  readonly courseId: string;
  readonly courseTitle: string;
  readonly languages: readonly ReceptionKioskCourseLanguage[];
  readonly token: string | null;
};

export type ReceptionKioskCourseLanguage = {
  readonly id: string;
  readonly languageCode: string;
  readonly displayLabel: string;
};

export type ReceptionKioskSessionStatus = 'Active' | 'Completed' | 'Stopped' | 'Failed';
export type ReceptionKioskSessionStep = 'FacePicture' | 'IdentityDocumentCheck' | 'ComplianceCheck' | 'Onboard';
export type ReceptionKioskSessionStepStatus = 'Pending' | 'Active' | 'Completed' | 'Skipped';
export type ReceptionKioskSessionStopReason = 'HomeRedirect' | 'NotCompliant' | 'Timeout' | 'Superseded' | 'Failed';

export type ReceptionKioskSession = {
  readonly id: string;
  readonly kioskId: string;
  readonly arrivalId: string;
  readonly arrival: ReceptionKioskExpectedArrival;
  readonly status: ReceptionKioskSessionStatus;
  readonly currentStep: ReceptionKioskSessionStep | null;
  readonly stopReason: ReceptionKioskSessionStopReason | null;
  readonly stopMessage: string | null;
  readonly startedAt: string;
  readonly lastInteractionAt: string;
  readonly completedAt: string | null;
  readonly steps: readonly ReceptionKioskSessionStepState[];
};

export type ReceptionKioskSessionStepState = {
  readonly step: ReceptionKioskSessionStep;
  readonly status: ReceptionKioskSessionStepStatus;
};

type ProblemDetailsWithCode = ProblemDetails & {
  readonly code?: string;
};

const receptionKioskArrivalKey = 'fabric.reception-kiosk.arrival';
const receptionKioskMissedCodeKey = 'fabric.reception-kiosk.missed-code';

export class ReceptionKioskArrivalNotFoundError extends Error {
  constructor(readonly code: string) {
    super('No expected arrival found for this QR code.');
    this.name = 'ReceptionKioskArrivalNotFoundError';
  }
}

export class ReceptionKioskArrivalLookupError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ReceptionKioskArrivalLookupError';
  }
}

export class ReceptionKioskWrongLocationError extends Error {
  constructor(
    readonly title: string,
    readonly detail: string,
  ) {
    super(detail);
    this.name = 'ReceptionKioskWrongLocationError';
  }
}

export async function lookupReceptionKioskArrival(code: string): Promise<ReceptionKioskExpectedArrival> {
  const settings = getReceptionKioskSettings();
  if (!settings) {
    throw new Error('Reception kiosk setup is required.');
  }

  const { data, error, response } = await api.GET('/api/reception/kiosk/arrivals/lookup', {
    params: { query: { code } },
    headers: {
      'reception-kiosk-id': settings.kioskId,
      'reception-kiosk-key': settings.kioskApiKey,
    },
  });

  if (response.status === 404) {
    throw new ReceptionKioskArrivalNotFoundError(code);
  }

  const problem = error as ProblemDetailsWithCode | undefined;
  if (response.status === 409 && problem?.detail) {
    if (problem.code === 'arrival-assigned-to-different-location') {
      throw new ReceptionKioskWrongLocationError(problem.title ?? 'This kiosk cannot serve your location.', problem.detail);
    }

    throw new ReceptionKioskArrivalLookupError(problem.detail);
  }

  if (error || !data) {
    throw new Error('Could not look up expected arrival.');
  }

  return data;
}

export function saveReceptionKioskArrival(arrival: ReceptionKioskExpectedArrival) {
  clearOnboardingState();
  clearActiveCourse();
  clearComplianceLaunch();
  window.sessionStorage.setItem(receptionKioskArrivalKey, JSON.stringify(arrival));
}

export function clearReceptionKioskArrival() {
  clearActiveCourse();
  clearComplianceLaunch();
  window.sessionStorage.removeItem(receptionKioskArrivalKey);
}

export function getReceptionKioskArrival(): ReceptionKioskExpectedArrival | null {
  const rawArrival = window.sessionStorage.getItem(receptionKioskArrivalKey);
  if (!rawArrival) {
    return null;
  }

  try {
    return JSON.parse(rawArrival) as ReceptionKioskExpectedArrival;
  } catch {
    return null;
  }
}

export function saveReceptionKioskMissedCode(code: string) {
  window.sessionStorage.setItem(receptionKioskMissedCodeKey, code);
}

export async function onboardReceptionKioskArrival(
  arrivalId: string,
  request: {
    readonly facePicture?: string;
    readonly identityVerification?: {
      readonly method: IdentityVerificationMethod;
      readonly content: string;
    };
  },
) {
  const settings = getReceptionKioskSettings();
  if (!settings) {
    throw new Error('Reception kiosk setup is required.');
  }

  const { error } = await api.POST('/api/reception/kiosk/arrivals/{id}/onboard', {
    params: { path: { id: arrivalId } },
    headers: {
      'reception-kiosk-id': settings.kioskId,
      'reception-kiosk-key': settings.kioskApiKey,
    },
    body: {
      facePicture: request.facePicture ?? null,
      identityVerification: request.identityVerification ?? null,
    },
  });

  if (error) {
    throw new Error('Could not complete self-onboarding.');
  }
}

export async function checkInReceptionKioskArrival(arrivalId: string) {
  const settings = getReceptionKioskSettings();
  if (!settings) {
    throw new Error('Reception kiosk setup is required.');
  }

  const { error } = await api.POST('/api/reception/kiosk/arrivals/{id}/check-in', {
    params: { path: { id: arrivalId } },
    headers: {
      'reception-kiosk-id': settings.kioskId,
      'reception-kiosk-key': settings.kioskApiKey,
    },
  });

  if (error) {
    throw new Error('Could not check in arrival.');
  }
}

export async function checkOutReceptionKioskArrival(arrivalId: string) {
  const settings = getReceptionKioskSettings();
  if (!settings) {
    throw new Error('Reception kiosk setup is required.');
  }

  const { error } = await api.POST('/api/reception/kiosk/arrivals/{id}/check-out', {
    params: { path: { id: arrivalId } },
    headers: {
      'reception-kiosk-id': settings.kioskId,
      'reception-kiosk-key': settings.kioskApiKey,
    },
  });

  if (error) {
    throw new Error('Could not check out arrival.');
  }
}

export function getReceptionKioskMissedCode(): string {
  return window.sessionStorage.getItem(receptionKioskMissedCodeKey) ?? '';
}

export async function getReceptionKioskCompliance(arrivalId: string): Promise<ReceptionKioskComplianceOverview> {
  return await kioskFetch<ReceptionKioskComplianceOverview>(`/api/reception/kiosk/arrivals/${encodeURIComponent(arrivalId)}/compliance`);
}

export async function launchReceptionKioskComplianceCourse(arrivalId: string, requirementDefinitionId: string, languageId?: string): Promise<ReceptionKioskComplianceCourseLaunch> {
  return await kioskFetch<ReceptionKioskComplianceCourseLaunch>(
    `/api/reception/kiosk/arrivals/${encodeURIComponent(arrivalId)}/compliance/requirements/${encodeURIComponent(requirementDefinitionId)}/launch`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ languageId: languageId ?? null }),
    },
  );
}

export async function startReceptionKioskSession(code: string): Promise<ReceptionKioskSession> {
  return await kioskFetch<ReceptionKioskSession>('/api/reception/kiosk/sessions', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ code }),
  });
}

export async function getCurrentReceptionKioskSession(): Promise<ReceptionKioskSession> {
  return await kioskFetch<ReceptionKioskSession>('/api/reception/kiosk/sessions/current');
}

export async function advanceReceptionKioskSession(): Promise<ReceptionKioskSession> {
  return await kioskFetch<ReceptionKioskSession>('/api/reception/kiosk/sessions/current/next', { method: 'POST' });
}

export async function stopReceptionKioskSession(reason: ReceptionKioskSessionStopReason, message?: string): Promise<ReceptionKioskSession> {
  return await kioskFetch<ReceptionKioskSession>('/api/reception/kiosk/sessions/current/stop', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ reason, message: message ?? null }),
  });
}

export async function storeReceptionKioskSessionFacePicture(content: string): Promise<ReceptionKioskSession> {
  return await storeReceptionKioskSessionCapture('/api/reception/kiosk/sessions/current/face-picture/store', content);
}

export async function storeReceptionKioskSessionIdentityDocument(content: string): Promise<ReceptionKioskSession> {
  return await storeReceptionKioskSessionCapture('/api/reception/kiosk/sessions/current/identity-document/store', content);
}

export async function getCurrentReceptionKioskSessionCompliance(): Promise<ReceptionKioskComplianceOverview> {
  return await kioskFetch<ReceptionKioskComplianceOverview>('/api/reception/kiosk/sessions/current/compliance');
}

export async function launchCurrentReceptionKioskSessionComplianceCourse(requirementDefinitionId: string, languageId?: string): Promise<ReceptionKioskComplianceCourseLaunch> {
  return await kioskFetch<ReceptionKioskComplianceCourseLaunch>(
    `/api/reception/kiosk/sessions/current/compliance/requirements/${encodeURIComponent(requirementDefinitionId)}/launch`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ languageId: languageId ?? null }),
    },
  );
}

export async function markCurrentReceptionKioskSessionNonCompliant(message?: string): Promise<ReceptionKioskSession> {
  return await kioskFetch<ReceptionKioskSession>('/api/reception/kiosk/sessions/current/compliance/non-compliant', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message: message ?? null }),
  });
}

export async function finalizeReceptionKioskSession(): Promise<ReceptionKioskSession> {
  return await kioskFetch<ReceptionKioskSession>('/api/reception/kiosk/sessions/current/finalize', { method: 'POST' });
}

async function storeReceptionKioskSessionCapture(path: string, content: string) {
  return await kioskFetch<ReceptionKioskSession>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ content }),
  });
}

async function kioskFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const settings = getReceptionKioskSettings();
  if (!settings) {
    throw new Error('Reception kiosk setup is required.');
  }

  const response = await fetch(path, {
    ...init,
    headers: {
      'reception-kiosk-id': settings.kioskId,
      'reception-kiosk-key': settings.kioskApiKey,
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    let detail = 'Request failed.';
    try {
      const problem = await response.json() as ProblemDetailsWithCode;
      detail = problem.detail ?? detail;
    } catch {
      // Ignore invalid error payloads and fall back to generic detail.
    }

    throw new Error(detail);
  }

  return await response.json() as T;
}
