export const supportedAppLanguages = ['en', 'nl'] as const;

export type AppLanguage = (typeof supportedAppLanguages)[number];

const appLanguageStorageKey = 'fabric.app.language';

export function isSupportedAppLanguage(value: string | null | undefined): value is AppLanguage {
  return value === 'en' || value === 'nl';
}

export function getStoredAppLanguage() {
  if (typeof window === 'undefined') {
    return undefined;
  }

  const value = window.localStorage.getItem(appLanguageStorageKey)?.trim().toLowerCase();
  return isSupportedAppLanguage(value) ? value : undefined;
}

export function saveAppLanguage(language: AppLanguage) {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.setItem(appLanguageStorageKey, language);
}

export function getSupportedAppLanguage(value: string | null | undefined): AppLanguage {
  const normalized = value?.trim().toLowerCase();

  if (isSupportedAppLanguage(normalized)) {
    return normalized;
  }

  const baseLanguage = normalized?.split('-')[0];
  return isSupportedAppLanguage(baseLanguage) ? baseLanguage : 'en';
}

export function resolveInitialAppLanguage() {
  const storedLanguage = getStoredAppLanguage();
  if (storedLanguage) {
    return storedLanguage;
  }

  if (typeof navigator === 'undefined') {
    return 'en';
  }

  for (const language of navigator.languages) {
    const supportedLanguage = getSupportedAppLanguage(language);

    if (supportedLanguage !== 'en' || language.toLowerCase().startsWith('en')) {
      return supportedLanguage;
    }
  }

  return getSupportedAppLanguage(navigator.language);
}
