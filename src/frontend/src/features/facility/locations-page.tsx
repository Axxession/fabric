import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Pencil, Plus } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';

type Site = components['schemas']['SiteResponse'];

const locationsQueryKey = ['facility', 'locations'] as const;

export default function LocationsPage() {
  const { t } = useTranslation();
  const sitesQuery = useQuery({
    queryKey: locationsQueryKey,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/sites');

      if (error) {
        throw new Error(t('administration.locations.couldNotLoad'));
      }

      return data;
    },
  });

  const sites = sitesQuery.data?.items ?? [];
  const totalItems = Number(sitesQuery.data?.totalItems ?? sites.length);

  return (
    <section className="rounded-structural border border-border bg-content">
      <div className="border-b border-border p-4 sm:p-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <h1 className="text-[20px] font-semibold tracking-tight">{t('administration.locations.title')}</h1>
            <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{t('administration.locations.description')}</p>
          </div>
          <Link
            to="/administration/sites/new"
            className="inline-flex w-full items-center justify-center gap-2 rounded-interactive bg-primary px-4 py-2 text-[14px] font-semibold text-white transition hover:opacity-90 sm:w-fit"
          >
            <Plus className="size-4" aria-hidden="true" />
            {t('administration.locations.addSite')}
          </Link>
        </div>
      </div>

      <div className="p-4 sm:p-6">
        {!sitesQuery.isLoading && !sitesQuery.isError && totalItems === 0 ? (
          <Empty>
            <EmptyHeader>
              <EmptyTitle>{t('administration.locations.noLocationsYet')}</EmptyTitle>
              <EmptyDescription>{t('administration.locations.noLocationsDescription')}</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <div className="grid gap-3">
            <div className="grid gap-3 md:hidden">
              {sitesQuery.isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">{t('administration.locations.loading')}</p> : null}
              {sitesQuery.isError ? <p className="rounded-structural border border-border p-4 text-[14px] text-error">{t('administration.locations.couldNotLoad')}</p> : null}
              {sites.map((site) => (
                <SiteCard key={site.id} site={site} />
              ))}
            </div>
            <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
            <table className="w-full min-w-[36rem] border-collapse text-left text-[14px]">
              <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                <tr>
                  <th className="px-4 py-3 font-semibold">{t('administration.locations.site')}</th>
                  <th className="px-4 py-3 font-semibold">{t('administration.locations.address')}</th>
                  <th className="px-4 py-3 text-right font-semibold">{t('administration.locations.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {sitesQuery.isLoading ? (
                  <tr>
                    <td className="px-4 py-5 text-muted-foreground" colSpan={3}>
                      {t('administration.locations.loading')}
                    </td>
                  </tr>
                ) : null}

                {sitesQuery.isError ? (
                  <tr>
                    <td className="px-4 py-5 text-error" colSpan={3}>
                      {t('administration.locations.couldNotLoad')}
                    </td>
                  </tr>
                ) : null}

                {sites.map((site) => (
                  <SiteRow key={site.id} site={site} />
                ))}
              </tbody>
            </table>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

function SiteCard({ site }: { site: Site }) {
  const { t } = useTranslation();

  return (
    <article className="rounded-structural border border-border p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h2 className="truncate text-[15px] font-semibold text-foreground">{site.name}</h2>
          <p className="mt-1 text-[14px] text-muted-foreground">{site.address || t('administration.locations.noAddress')}</p>
        </div>
        <Link
          to="/administration/sites/$siteId/edit"
          params={{ siteId: site.id }}
          className="inline-flex size-10 shrink-0 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
          aria-label={t('administration.locations.editSite', { name: site.name })}
        >
          <Pencil className="size-4" aria-hidden="true" />
        </Link>
      </div>
    </article>
  );
}

function SiteRow({ site }: { site: Site }) {
  const { t } = useTranslation();

  return (
    <tr>
      <td className="px-4 py-4 font-medium text-foreground">{site.name}</td>
      <td className="px-4 py-4 text-muted-foreground">{site.address || t('administration.locations.noAddress')}</td>
      <td className="px-4 py-4">
        <div className="flex justify-end">
          <Link
            to="/administration/sites/$siteId/edit"
            params={{ siteId: site.id }}
            className="inline-flex size-9 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
            aria-label={t('administration.locations.editSite', { name: site.name })}
          >
            <Pencil className="size-4" aria-hidden="true" />
          </Link>
        </div>
      </td>
    </tr>
  );
}
