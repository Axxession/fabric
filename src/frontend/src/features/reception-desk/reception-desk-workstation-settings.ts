export type ReceptionDeskWorkstationSettings = {
  readonly workstationId: string;
  readonly workstationApiKey: string;
};

const receptionDeskWorkstationSettingsKey = 'fabric.reception-desk-workstation.settings';

export function getReceptionDeskWorkstationSettings(): ReceptionDeskWorkstationSettings | null {
  if (typeof window === 'undefined') {
    return null;
  }

  const rawSettings = window.localStorage.getItem(receptionDeskWorkstationSettingsKey);
  if (!rawSettings) {
    return null;
  }

  try {
    const settings = JSON.parse(rawSettings) as Partial<ReceptionDeskWorkstationSettings>;
    const workstationId = settings.workstationId?.trim();
    const workstationApiKey = settings.workstationApiKey?.trim();

    if (!workstationId || !workstationApiKey) {
      return null;
    }

    return { workstationId, workstationApiKey };
  } catch {
    return null;
  }
}

export function hasReceptionDeskWorkstationSettings(): boolean {
  return getReceptionDeskWorkstationSettings() !== null;
}

export function saveReceptionDeskWorkstationSettings(settings: ReceptionDeskWorkstationSettings) {
  window.localStorage.setItem(
    receptionDeskWorkstationSettingsKey,
    JSON.stringify({
      workstationId: settings.workstationId.trim(),
      workstationApiKey: settings.workstationApiKey.trim(),
    }),
  );
}

export function getStoredReceptionDeskWorkstationId(): string {
  if (typeof window === 'undefined') {
    return '';
  }

  const rawSettings = window.localStorage.getItem(receptionDeskWorkstationSettingsKey);
  if (!rawSettings) {
    return '';
  }

  try {
    const settings = JSON.parse(rawSettings) as Partial<ReceptionDeskWorkstationSettings>;
    return settings.workstationId?.trim() ?? '';
  } catch {
    return '';
  }
}

export function getReceptionDeskWorkstationHeaders() {
  const settings = getReceptionDeskWorkstationSettings();
  if (!settings) {
    throw new Error('Reception desk workstation setup is required.');
  }

  return {
    'reception-desk-workstation-id': settings.workstationId,
    'reception-desk-workstation-key': settings.workstationApiKey,
  } as const;
}
