import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Pencil, Plus } from 'lucide-react';
import { useState } from 'react';

import { api } from '@/shared/api/client';
import { Badge } from '@/shared/components/ui/badge';
import { buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Input } from '@/shared/components/ui/input';
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from '@/shared/components/ui/pagination';

import { formatDateTime, printDesignsQueryKey, type PrintDesignSummary, type PrintSurfaceKind } from './card-management-types';

const pageSize = 12;

export function PrintDesignList({
  surfaceKind,
  title,
  description,
  createTo,
  editTo,
  emptyTitle,
  emptyDescription,
}: {
  readonly surfaceKind?: PrintSurfaceKind;
  readonly title: string;
  readonly description: string;
  readonly createTo: string;
  readonly editTo: (designId: string) => string;
  readonly emptyTitle: string;
  readonly emptyDescription: string;
}) {
  const [page, setPage] = useState(0);
  const [name, setName] = useState('');

  const printDesignsQuery = useQuery({
    queryKey: [...printDesignsQueryKey, { page, name, surfaceKind }],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/printing/designs', {
        params: {
          query: {
            Name: name.trim() || undefined,
            MediaLabel: undefined,
            SurfaceKind: surfaceKind,
            ids: [],
            Page: page,
            PageSize: pageSize,
          } as never,
        },
      });

      if (error || !data) {
        throw new Error('Could not load print designs.');
      }

      return data;
    },
  });

  const designs = printDesignsQuery.data?.items ?? [];
  const pagination = getPaginationState(printDesignsQuery.data, designs.length, page, pageSize);

  return (
    <section className="rounded-structural border border-border bg-content">
      <div className="flex flex-col gap-4 border-b border-border p-4 sm:flex-row sm:items-end sm:justify-between sm:p-6">
        <div>
          <h1 className="text-[20px] font-semibold tracking-tight">{title}</h1>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{description}</p>
        </div>
        <Link to={createTo} className={buttonVariants({ className: 'w-full sm:w-fit' })}>
          <Plus className="size-4" aria-hidden="true" />
          Create design
        </Link>
      </div>

      <div className="grid gap-4 p-4 sm:p-6">
        <label className="grid gap-2 text-[14px] font-medium sm:max-w-sm">
          <span>Search by name</span>
          <Input
            value={name}
            onChange={(event) => {
              setName(event.target.value);
              setPage(0);
            }}
            placeholder="Search print designs"
          />
        </label>

        {printDesignsQuery.isError ? <PanelError>Could not load print designs.</PanelError> : null}
        {printDesignsQuery.isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading print designs...</p> : null}
        {!printDesignsQuery.isLoading && !printDesignsQuery.isError && designs.length === 0 ? (
          <Empty>
            <EmptyHeader>
              <EmptyTitle>{emptyTitle}</EmptyTitle>
              <EmptyDescription>{emptyDescription}</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : null}

        {designs.length > 0 ? <PrintDesignTable designs={designs} editTo={editTo} /> : null}

        {!printDesignsQuery.isLoading && !printDesignsQuery.isError && pagination.totalItems > 0 ? (
          <div className="flex flex-col gap-3 text-[14px] text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
            <p>Showing {pagination.firstItem}-{pagination.lastItem} of {pagination.totalItems}</p>
            <Pagination className="sm:mx-0 sm:w-auto">
              <PaginationContent>
                <PaginationItem>
                  <PaginationPrevious disabled={pagination.currentPage === 0} onClick={() => setPage(Math.max(0, pagination.currentPage - 1))} />
                </PaginationItem>
                {pagination.visiblePages.map((visiblePage, index) =>
                  visiblePage === 'ellipsis' ? (
                    <PaginationItem key={`${visiblePage}-${index}`}>
                      <PaginationEllipsis />
                    </PaginationItem>
                  ) : (
                    <PaginationItem key={visiblePage}>
                      <PaginationLink isActive={visiblePage === pagination.currentPage} onClick={() => setPage(visiblePage)}>
                        {visiblePage + 1}
                      </PaginationLink>
                    </PaginationItem>
                  ),
                )}
                <PaginationItem>
                  <PaginationNext disabled={pagination.currentPage >= pagination.totalPages - 1} onClick={() => setPage(Math.min(pagination.totalPages - 1, pagination.currentPage + 1))} />
                </PaginationItem>
              </PaginationContent>
            </Pagination>
          </div>
        ) : null}
      </div>
    </section>
  );
}

function PrintDesignTable({ designs, editTo }: { readonly designs: PrintDesignSummary[]; readonly editTo: (designId: string) => string }) {
  return (
    <div className="overflow-x-auto rounded-structural border border-border">
      <table className="w-full min-w-[68rem] border-collapse text-left text-[14px]">
        <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
          <tr>
            <th className="px-4 py-3 font-semibold">Name</th>
            <th className="px-4 py-3 font-semibold">Version</th>
            <th className="px-4 py-3 font-semibold">Surface</th>
            <th className="px-4 py-3 font-semibold">Media</th>
            <th className="px-4 py-3 font-semibold">DPI</th>
            <th className="px-4 py-3 font-semibold">Updated</th>
            <th className="px-4 py-3 text-right font-semibold">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {designs.map((design) => (
            <tr key={design.id}>
              <td className="px-4 py-4">
                <div className="font-medium text-foreground">{design.name}</div>
                {design.description ? <div className="mt-1 max-w-[20rem] truncate text-muted-foreground">{design.description}</div> : null}
              </td>
              <td className="px-4 py-4 text-muted-foreground">v{design.version}</td>
              <td className="px-4 py-4"><Badge variant="secondary">{design.surfaceKind}</Badge></td>
              <td className="px-4 py-4 text-muted-foreground">{design.media.label || `${design.media.width} x ${design.media.height}`}</td>
              <td className="px-4 py-4 text-muted-foreground">{design.dpi}</td>
              <td className="px-4 py-4 text-muted-foreground">{formatDateTime(design.updatedAt)}</td>
              <td className="px-4 py-4">
                <div className="flex justify-end gap-2">
                  <Link to={editTo(design.id)} className={buttonVariants({ variant: 'outline', size: 'sm' })}>
                    <Pencil className="size-4" aria-hidden="true" />
                    Edit
                  </Link>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function PanelError({ children }: { readonly children: React.ReactNode }) {
  return <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{children}</p>;
}

function getPaginationState(page: { totalItems?: number | string | null; totalPages?: number | string | null; currentPage?: number | string | null } | undefined, itemCount: number, requestedPage: number, resolvedPageSize: number) {
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
