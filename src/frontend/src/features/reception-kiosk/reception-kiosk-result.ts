export type ReceptionKioskResultKind = 'onboarding-success' | 'check-in-success' | 'check-out-success' | 'visit-completed' | 'action-failed' | 'wrong-location';

type ReceptionKioskResult = {
  readonly kind: ReceptionKioskResultKind;
  readonly title?: string;
  readonly message?: string;
};

const receptionKioskResultKey = 'fabric.reception-kiosk.result';

export function saveReceptionKioskResult(kind: ReceptionKioskResultKind, message?: string) {
  window.sessionStorage.setItem(receptionKioskResultKey, JSON.stringify({ kind, message } satisfies ReceptionKioskResult));
}

export function saveReceptionKioskWrongLocation(title: string, message: string) {
  window.sessionStorage.setItem(receptionKioskResultKey, JSON.stringify({ kind: 'wrong-location', title, message } satisfies ReceptionKioskResult));
}

export function getReceptionKioskResult(): ReceptionKioskResult | null {
  const rawResult = window.sessionStorage.getItem(receptionKioskResultKey);
  if (!rawResult) {
    return null;
  }

  try {
    const parsed = JSON.parse(rawResult) as Partial<ReceptionKioskResult>;
    return isReceptionKioskResultKind(parsed.kind)
      ? {
          kind: parsed.kind,
          title: typeof parsed.title === 'string' ? parsed.title : undefined,
          message: typeof parsed.message === 'string' ? parsed.message : undefined,
        }
      : null;
  } catch {
    return null;
  }
}

export function clearReceptionKioskResult() {
  window.sessionStorage.removeItem(receptionKioskResultKey);
}

function isReceptionKioskResultKind(value: unknown): value is ReceptionKioskResultKind {
  return value === 'onboarding-success'
    || value === 'check-in-success'
    || value === 'check-out-success'
    || value === 'visit-completed'
    || value === 'action-failed'
    || value === 'wrong-location';
}
