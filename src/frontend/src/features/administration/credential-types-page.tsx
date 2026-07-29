import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from '@tanstack/react-router';
import { ChevronRight } from 'lucide-react';
import { useState } from 'react';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from '@/shared/components/ui/pagination';

type CredentialTypeResponse = components['schemas']['CredentialTypeResponse'];
type PaginationState = {
  readonly currentPage: number;
  readonly firstItem: number;
  readonly lastItem: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly visiblePages: readonly (number | 'ellipsis')[];
};

const pageSize = 10;

export default function CredentialTypesPage() {
  const navigate = useNavigate();
  const [page, setPage] = useState(0);
  const [name, setName] = useState('');

  const credentialTypesQuery = useQuery({
    queryKey: ['administration', 'credential-types', page, name],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/credential-management/credential-types', {
        params: { query: { Query: name || undefined, Page: page, PageSize: pageSize } as never },
      });

      if (error) {
        throw new Error('Could not load credential types.');
      }

      return data;
    },
  });

  const items = credentialTypesQuery.data?.items ?? [];
  const pagination = getPaginationState(credentialTypesQuery.data, items.length, page, pageSize);

  return (
    <section className="rounded-structural border border-border bg-content p-4 sm:p-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">Credential Types</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">Manage credential technologies, allocation ranges, and recycle policy.</p>
        </div>
        <div>
          <Link to="/administration/credential-types/new" className={buttonVariants()}>Add credential type</Link>
        </div>
      </div>

      <div className="mt-4 grid gap-3 rounded-structural border border-border p-4 md:grid-cols-2">
        <label className="grid gap-2 text-[14px] font-medium md:max-w-md">
          <span>Search credential types</span>
          <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={name} onChange={(event) => { setName(event.target.value); setPage(0); }} placeholder="Search by credential type name" />
        </label>
      </div>

      {credentialTypesQuery.isError ? <p className="mt-4 rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">Could not load credential types.</p> : null}

      {!credentialTypesQuery.isLoading && !credentialTypesQuery.isError && items.length === 0 ? (
        <Empty className="mt-4">
          <EmptyHeader>
            <EmptyTitle>No credential types found</EmptyTitle>
            <EmptyDescription>Try a different search or add a new credential type.</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="mt-4 grid gap-4">
          <div className="md:hidden">
            {credentialTypesQuery.isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading credential types...</p> : null}
            {!credentialTypesQuery.isLoading ? (
              <div className="grid gap-3">
                {items.map((item) => (
                  <article key={item.id} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => void navigate({ to: '/administration/credential-types/$credentialTypeId/edit', params: { credentialTypeId: item.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/credential-types/$credentialTypeId/edit', params: { credentialTypeId: item.id } }); } }}>
                    <div className="flex items-start justify-between gap-3">
                      <h3 className="text-[15px] font-semibold text-foreground">{item.name}</h3>
                      <ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                    </div>
                    <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                      <div><dt className="font-medium text-foreground">Technology</dt><dd>{formatTechnology(item.technology)}</dd></div>
                      <div><dt className="font-medium text-foreground">Allocation</dt><dd>{formatAllocationMode(item.allocationMode)}</dd></div>
                      <div><dt className="font-medium text-foreground">Recycle</dt><dd>{formatRecyclePolicy(item.recyclePolicy)}</dd></div>
                      <div><dt className="font-medium text-foreground">Capacity</dt><dd>{item.usedCount} used / {item.availableCount} available</dd></div>
                      <div><dt className="font-medium text-foreground">Status</dt><dd><StatusBadge status={item.status} /></dd></div>
                    </dl>
                  </article>
                ))}
              </div>
            ) : null}
          </div>

          <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
            {credentialTypesQuery.isLoading ? <p className="px-4 py-5 text-[14px] text-muted-foreground">Loading credential types...</p> : (
              <table className="w-full min-w-[72rem] border-collapse text-left text-[14px]">
                <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
                  <tr>
                    <th className="px-4 py-3 font-semibold">Name</th>
                    <th className="px-4 py-3 font-semibold">Technology</th>
                    <th className="px-4 py-3 font-semibold">Allocation</th>
                    <th className="px-4 py-3 font-semibold">Recycle</th>
                    <th className="px-4 py-3 font-semibold">Capacity</th>
                    <th className="px-4 py-3 font-semibold">Status</th>
                    <th className="px-4 py-3 text-right font-semibold">Open</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {items.map((item) => (
                    <tr key={item.id} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => void navigate({ to: '/administration/credential-types/$credentialTypeId/edit', params: { credentialTypeId: item.id } })} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); void navigate({ to: '/administration/credential-types/$credentialTypeId/edit', params: { credentialTypeId: item.id } }); } }}>
                      <td className="px-4 py-4 font-medium text-foreground">{item.name}</td>
                      <td className="px-4 py-4 text-muted-foreground">{formatTechnology(item.technology)}</td>
                      <td className="px-4 py-4 text-muted-foreground">{formatAllocationMode(item.allocationMode)}</td>
                      <td className="px-4 py-4 text-muted-foreground">{formatRecyclePolicy(item.recyclePolicy)}</td>
                      <td className="px-4 py-4"><CapacityBadge capacityState={item.capacityState} usedCount={item.usedCount} availableCount={item.availableCount} /></td>
                      <td className="px-4 py-4"><StatusBadge status={item.status} /></td>
                      <td className="px-4 py-4 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {!credentialTypesQuery.isLoading && !credentialTypesQuery.isError && pagination.totalItems > 0 ? (
            <div className="flex flex-col gap-3 text-[14px] text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
              <p>Showing {pagination.firstItem}-{pagination.lastItem} of {pagination.totalItems}</p>
              <Pagination className="sm:mx-0 sm:w-auto">
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious disabled={pagination.currentPage === 0} onClick={() => setPage(Math.max(0, pagination.currentPage - 1))} />
                  </PaginationItem>
                  {pagination.visiblePages.map((visiblePage, index) => visiblePage === 'ellipsis' ? (
                    <PaginationItem key={`${visiblePage}-${index}`}>
                      <PaginationEllipsis />
                    </PaginationItem>
                  ) : (
                    <PaginationItem key={visiblePage}>
                      <PaginationLink isActive={visiblePage === pagination.currentPage} onClick={() => setPage(visiblePage)}>{visiblePage + 1}</PaginationLink>
                    </PaginationItem>
                  ))}
                  <PaginationItem>
                    <PaginationNext disabled={pagination.currentPage >= pagination.totalPages - 1} onClick={() => setPage(Math.min(pagination.totalPages - 1, pagination.currentPage + 1))} />
                  </PaginationItem>
                </PaginationContent>
              </Pagination>
            </div>
          ) : null}
        </div>
      )}
    </section>
  );
}

function StatusBadge({ status }: { readonly status: string }) {
  return <Badge variant={status === 'Active' ? 'success' : 'secondary'}>{status}</Badge>;
}

function CapacityBadge({ capacityState, usedCount, availableCount }: { readonly capacityState: components['schemas']['CredentialCapacityState']; readonly usedCount: number | string; readonly availableCount: number | string; }) {
  const variant = capacityState === 'Limit' ? 'error' : capacityState === 'NearLimit' ? 'warning' : 'secondary';
  return <Badge variant={variant}>{usedCount} / {availableCount}</Badge>;
}

function formatTechnology(value: components['schemas']['CredentialTechnology']) {
  return value === 'LicensePlate' ? 'License Plate' : value === 'Qr' ? 'QR' : 'Desfire';
}

function formatAllocationMode(value: components['schemas']['CredentialAllocationMode']) {
  return value === 'Range' ? 'Range allocated' : 'Provided';
}

function formatRecyclePolicy(value: components['schemas']['CredentialRecyclePolicy']) {
  switch (value) {
    case 'NeverReuse':
      return 'Never reuse';
    case 'ReuseAfterExpiry':
      return 'After expiry';
    case 'ReuseAfterRevocation':
      return 'After revocation';
    case 'ReuseAfterRevocationAndGrace':
      return 'After revocation + grace';
  }
}

function getPaginationState(page: { currentPage?: number | string; totalPages?: null | number | string; totalItems?: null | number | string } | undefined, itemCount: number, requestedPage: number, resolvedPageSize: number): PaginationState {
  const totalItems = Number(page?.totalItems ?? itemCount);
  const totalPages = Math.max(Number(page?.totalPages ?? 1), 1);
  const currentPage = Math.min(Number(page?.currentPage ?? requestedPage), totalPages - 1);
  const firstItem = totalItems === 0 ? 0 : currentPage * resolvedPageSize + 1;
  const lastItem = Math.min((currentPage + 1) * resolvedPageSize, totalItems);
  const visiblePages = getVisiblePages(totalPages, currentPage);
  return { currentPage, firstItem, lastItem, totalItems, totalPages, visiblePages };
}

function getVisiblePages(totalPages: number, currentPage: number) {
  if (totalPages <= 5) {
    return Array.from({ length: totalPages }, (_, index) => index);
  }

  const pages = new Set([0, totalPages - 1, currentPage - 1, currentPage, currentPage + 1]);
  const sortedPages = Array.from(pages).filter((pageNumber) => pageNumber >= 0 && pageNumber < totalPages).sort((left, right) => left - right);
  const visiblePages: Array<number | 'ellipsis'> = [];

  sortedPages.forEach((pageNumber, index) => {
    if (index > 0 && pageNumber - sortedPages[index - 1] > 1) {
      visiblePages.push('ellipsis');
    }

    visiblePages.push(pageNumber);
  });

  return visiblePages;
}
