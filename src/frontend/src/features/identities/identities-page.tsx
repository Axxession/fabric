import { useQuery } from '@tanstack/react-query';
import { useNavigate } from '@tanstack/react-router';
import { ChevronRight } from 'lucide-react';
import { useState } from 'react';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Card } from '@/shared/components/ui/card';
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from '@/shared/components/ui/pagination';

type IdentityAffiliationSummaryResponse = components['schemas']['IdentityAffiliationSummaryResponse'];
type IdentityResponse = components['schemas']['IdentityResponse'];
type IdentityStatus = components['schemas']['IdentityStatus'];

type PaginationState = {
  readonly currentPage: number;
  readonly firstItem: number;
  readonly lastItem: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly visiblePages: readonly (number | 'ellipsis')[];
};

const pageSize = 10;

export default function IdentitiesPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(0);
  const [query, setQuery] = useState('');

  const identitiesQuery = useQuery({
    queryKey: ['security-officer', 'identities', page, query],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/identities', {
        params: { query: { query: query || undefined, status: undefined, affiliationType: undefined, page, pageSize } },
      });

      if (error) {
        throw new Error('Could not load identities.');
      }

      return data;
    },
  });

  const items = identitiesQuery.data?.items ?? [];
  const pagination = getPaginationState(identitiesQuery.data, items.length, page, pageSize);

  function openIdentity(identityId: string) {
    void navigate({ to: '/security-officer/identities/$identityId', params: { identityId } });
  }

  return (
    <Card className="p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Identity 360</h2>
          <p className="mt-2 text-[14px] text-muted-foreground">Review canonical identities and drill into a full identity record.</p>
        </div>
      </div>

      <div className="mt-6 grid gap-2 sm:max-w-sm">
        <label className="text-[14px] font-medium" htmlFor="identity-query">Search identities</label>
        <input id="identity-query" className="h-9 rounded-interactive border border-border bg-content px-3 text-[14px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20" value={query} onChange={(event) => { setQuery(event.target.value); setPage(0); }} placeholder="Search by name or email" />
      </div>

      {identitiesQuery.isError ? <p className="mt-6 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load identities.</p> : null}
      {identitiesQuery.isLoading ? <p className="mt-6 text-[14px] text-muted-foreground">Loading identities...</p> : null}
      {!identitiesQuery.isLoading && items.length === 0 ? <p className="mt-6 rounded-structural border border-dashed border-border p-6 text-[14px] text-muted-foreground">No identities found.</p> : null}

      {items.length > 0 ? (
        <>
          <div className="mt-6 hidden overflow-x-auto md:block">
            <table className="w-full min-w-[64rem] border-collapse text-left text-[14px]">
              <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                <tr>
                  <th className="px-4 py-3 font-semibold">Name</th>
                  <th className="px-4 py-3 font-semibold">Email</th>
                  <th className="px-4 py-3 font-semibold">Status</th>
                  <th className="px-4 py-3 font-semibold">Affiliations</th>
                  <th className="px-4 py-3 text-right font-semibold">Open</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {items.map((item) => (
                  <tr key={item.id} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => openIdentity(item.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openIdentity(item.id); } }}>
                    <td className="px-4 py-4 font-medium text-foreground">{item.displayName}</td>
                    <td className="px-4 py-4 text-muted-foreground">{item.email ?? '-'}</td>
                    <td className="px-4 py-4"><Badge variant={getStatusVariant(item.status)}>{item.status}</Badge></td>
                    <td className="px-4 py-4 text-muted-foreground">{formatAffiliations(item)}</td>
                    <td className="px-4 py-4 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="mt-6 grid gap-3 md:hidden">
            {items.map((item) => (
              <article key={item.id} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => openIdentity(item.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openIdentity(item.id); } }}>
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-medium text-foreground">{item.displayName}</p>
                    <p className="mt-1 text-[13px] text-muted-foreground">{item.email ?? 'No email'}</p>
                  </div>
                  <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
                </div>
                <dl className="mt-4 grid gap-2 text-[14px]">
                  <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Status</dt><dd><Badge variant={getStatusVariant(item.status)}>{item.status}</Badge></dd></div>
                  <div className="flex items-center justify-between gap-3"><dt className="text-muted-foreground">Affiliations</dt><dd className="text-right text-foreground">{formatAffiliations(item)}</dd></div>
                </dl>
              </article>
            ))}
          </div>

          {pagination.totalPages > 1 ? (
            <div className="mt-6 flex flex-col gap-3 border-t border-border pt-4 text-[14px] text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
              <p>Showing {pagination.firstItem}-{pagination.lastItem} of {pagination.totalItems}</p>
              <Pagination>
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious disabled={pagination.currentPage === 0} onClick={() => { if (pagination.currentPage > 0) setPage(pagination.currentPage - 1); }} />
                  </PaginationItem>
                  {pagination.visiblePages.map((item, index) => (
                    item === 'ellipsis' ? <PaginationItem key={`ellipsis-${index}`}><PaginationEllipsis /></PaginationItem> : <PaginationItem key={item}><PaginationLink isActive={item === pagination.currentPage} onClick={() => setPage(item)}>{item + 1}</PaginationLink></PaginationItem>
                  ))}
                  <PaginationItem>
                    <PaginationNext disabled={pagination.currentPage >= pagination.totalPages - 1} onClick={() => { if (pagination.currentPage < pagination.totalPages - 1) setPage(pagination.currentPage + 1); }} />
                  </PaginationItem>
                </PaginationContent>
              </Pagination>
            </div>
          ) : null}
        </>
      ) : null}
    </Card>
  );
}

function formatAffiliations(identity: IdentityResponse) {
  const parts = [
    ...identity.employeeAffiliations.map((item) => formatAffiliation('Employee', item)),
    ...identity.contractorAffiliations.map((item) => formatAffiliation('Contractor', item)),
    ...identity.visitorAffiliations.map((item) => formatAffiliation('Visitor', item)),
  ];

  return parts.length > 0 ? parts.join(', ') : 'None';
}

function formatAffiliation(label: string, affiliation: IdentityAffiliationSummaryResponse) {
  return affiliation.status === 'Active' ? label : `${label} (${affiliation.status})`;
}

function getStatusVariant(status: IdentityStatus) {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Suspended':
      return 'secondary';
    default:
      return 'error';
  }
}

function getPaginationState(response: components['schemas']['PageOfIdentityResponse'] | undefined, itemCount: number, page: number, currentPageSize: number): PaginationState {
  const totalItems = Number(response?.totalItems ?? itemCount);
  const totalPages = Math.max(1, Number(response?.totalPages ?? (currentPageSize > 0 ? Math.ceil(totalItems / currentPageSize) : 1)));
  const currentPage = Math.min(page, totalPages - 1);
  const firstItem = itemCount === 0 ? 0 : currentPage * currentPageSize + 1;
  const lastItem = itemCount === 0 ? 0 : firstItem + itemCount - 1;

  return {
    currentPage,
    firstItem,
    lastItem,
    totalItems,
    totalPages,
    visiblePages: buildVisiblePages(currentPage, totalPages),
  };
}

function buildVisiblePages(currentPage: number, totalPages: number) {
  if (totalPages <= 5) {
    return Array.from({ length: totalPages }, (_, index) => index);
  }

  if (currentPage <= 2) {
    return [0, 1, 2, 'ellipsis', totalPages - 1] as const;
  }

  if (currentPage >= totalPages - 3) {
    return [0, 'ellipsis', totalPages - 3, totalPages - 2, totalPages - 1] as const;
  }

  return [0, 'ellipsis', currentPage, 'ellipsis', totalPages - 1] as const;
}
