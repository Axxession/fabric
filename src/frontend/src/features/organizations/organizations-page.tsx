import { useTranslation } from 'react-i18next';

export default function OrganizationsPage() {
  const { t } = useTranslation();

  return (
    <section className="rounded-structural border border-border bg-content p-4 sm:p-6 md:p-8">
      <h1 className="text-[32px] font-semibold tracking-tight">{t('placeholderPages.organizations.title')}</h1>
      <p className="mt-3 max-w-2xl text-[14px] text-muted-foreground">{t('placeholderPages.organizations.description')}</p>
    </section>
  );
}
