import { useNavigate } from '@tanstack/react-router';
import { KeyRound, TabletSmartphone } from 'lucide-react';
import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';

import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';
import { Label } from '@/shared/components/ui/label';

import { getReceptionKioskSettings, getStoredReceptionKioskId, saveReceptionKioskSettings } from './reception-kiosk-settings';

export default function ReceptionKioskSetupPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [kioskId, setKioskId] = useState(getStoredReceptionKioskId);
  const [kioskApiKey, setKioskApiKey] = useState('');
  const [error, setError] = useState<string | null>(null);
  const hasExistingApiKey = getReceptionKioskSettings() !== null;

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    const currentSettings = getReceptionKioskSettings();
    const nextKioskId = kioskId.trim();
    const nextApiKey = kioskApiKey.trim() || currentSettings?.kioskApiKey || '';

    if (!nextKioskId) {
      setError(t('receptionKiosk.setup.kioskIdRequired'));
      return;
    }

    if (!nextApiKey) {
      setError(t('receptionKiosk.setup.apiKeyRequired'));
      return;
    }

    saveReceptionKioskSettings({ kioskId: nextKioskId, kioskApiKey: nextApiKey });
    await navigate({ to: '/reception-kiosk' });
  }

  return (
    <section className="grid w-full gap-6 lg:grid-cols-[0.9fr_1.1fr] lg:items-stretch">
      <div className="rounded-[2rem] border border-border bg-content p-7 shadow-sm sm:p-10">
        <div className="flex size-16 items-center justify-center rounded-full bg-hover-blue text-primary sm:size-20">
          <TabletSmartphone className="size-8 sm:size-10" aria-hidden="true" />
        </div>
        <p className="mt-8 text-[13px] font-semibold uppercase tracking-[0.28em] text-muted-foreground">{t('receptionKiosk.setup.eyebrow')}</p>
        <h2 className="mt-3 text-[34px] font-semibold tracking-tight sm:text-[48px]">{t('receptionKiosk.setup.title')}</h2>
        <p className="mt-5 text-[18px] leading-8 text-muted-foreground">
          {t('receptionKiosk.setup.description')}
        </p>
      </div>

      <form className="grid gap-6 rounded-[2rem] border border-border bg-content p-7 shadow-sm sm:p-10" onSubmit={handleSubmit}>
        <div className="flex items-center gap-4">
          <div className="flex size-14 items-center justify-center rounded-full bg-hover-gray text-muted-foreground">
            <KeyRound className="size-7" aria-hidden="true" />
          </div>
          <div>
            <h3 className="text-[24px] font-semibold tracking-tight">{t('receptionKiosk.setup.credentialsTitle')}</h3>
            <p className="mt-1 text-[15px] text-muted-foreground">{t('receptionKiosk.setup.credentialsDescription')}</p>
          </div>
        </div>

        {error ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[15px] font-medium text-error">{error}</p> : null}

        <div className="grid gap-3">
          <Label htmlFor="reception-kiosk-id" className="text-[16px]">{t('receptionKiosk.setup.kioskId')}</Label>
          <Input
            id="reception-kiosk-id"
            className="h-14 rounded-xl px-4 text-[18px] md:text-[18px]"
            value={kioskId}
            autoComplete="off"
            placeholder={t('receptionKiosk.setup.kioskIdPlaceholder')}
            onChange={(event) => setKioskId(event.target.value)}
          />
        </div>

        <div className="grid gap-3">
          <Label htmlFor="reception-kiosk-api-key" className="text-[16px]">{t('receptionKiosk.setup.kioskApiKey')}</Label>
          <Input
            id="reception-kiosk-api-key"
            className="h-14 rounded-xl px-4 text-[18px] md:text-[18px]"
            type="password"
            value={kioskApiKey}
            autoComplete="new-password"
            placeholder={hasExistingApiKey ? t('receptionKiosk.setup.apiKeyConfiguredPlaceholder') : t('receptionKiosk.setup.apiKeyPlaceholder')}
            onChange={(event) => setKioskApiKey(event.target.value)}
          />
          <p className="text-[14px] leading-6 text-muted-foreground">{t('receptionKiosk.setup.apiKeyHint')}</p>
        </div>

        <div className="pt-2">
          <Button type="submit" className="h-14 w-full rounded-xl text-[18px] font-semibold sm:w-auto sm:px-10">
            {t('receptionKiosk.setup.save')}
          </Button>
        </div>
      </form>
    </section>
  );
}
