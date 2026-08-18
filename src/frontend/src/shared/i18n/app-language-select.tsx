import { useTranslation } from 'react-i18next';

import { type AppLanguage, getSupportedAppLanguage, saveAppLanguage } from '@/shared/i18n/app-language';
import { i18n } from '@/shared/i18n/i18n';
import { cn } from '@/shared/utils/cn';

const languageLabels: Record<AppLanguage, string> = {
  en: 'common.languages.english',
  nl: 'common.languages.dutch',
};

export function AppLanguageSelect({ className }: { className?: string }) {
  const { t } = useTranslation();
  const currentLanguage = getSupportedAppLanguage(i18n.resolvedLanguage ?? i18n.language);

  return (
    <label className="min-w-0">
      <span className="sr-only">{t('common.language')}</span>
      <select
        className={cn('h-10 min-w-28 rounded-interactive border border-border bg-content px-3 text-[14px] font-medium text-foreground outline-none transition focus:border-primary', className)}
        aria-label={t('common.openLanguageMenu')}
        value={currentLanguage}
        onChange={(event) => {
          const nextLanguage = getSupportedAppLanguage(event.target.value);
          saveAppLanguage(nextLanguage);
          void i18n.changeLanguage(nextLanguage);
        }}
      >
        <option value="en">{t(languageLabels.en)}</option>
        <option value="nl">{t(languageLabels.nl)}</option>
      </select>
    </label>
  );
}
