import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';

import { clearOnboardingState } from './reception-kiosk-onboarding';
import { getReceptionKioskSettings } from './reception-kiosk-settings';

export type ReceptionKioskExpectedArrival = components['schemas']['ReceptionKioskExpectedArrivalResponse'];
type IdentityVerificationMethod = components['schemas']['IdentityVerificationMethod'];
type ProblemDetails = components['schemas']['ProblemDetails'];

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
  window.sessionStorage.setItem(receptionKioskArrivalKey, JSON.stringify(arrival));
}

export function clearReceptionKioskArrival() {
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
