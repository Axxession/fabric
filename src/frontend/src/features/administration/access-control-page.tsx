import { useQuery } from '@tanstack/react-query';
import { Link, useLocation, useNavigate } from '@tanstack/react-router';
import { ChevronRight } from 'lucide-react';
import { useState } from 'react';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { AccessControlProviderBadge } from '@/shared/components/access-control-provider-badge';
import { Badge } from '@/shared/components/ui/badge';
import { buttonVariants } from '@/shared/components/ui/button';
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/shared/components/ui/empty';
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from '@/shared/components/ui/pagination';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';

type AccessControlTab = 'access-items' | 'systems';
type AccessItem = components['schemas']['AccessItemResponse'];
type AccessControlSystem = components['schemas']['AccessControlSystemResponse'];
type PaginationState = {
  readonly currentPage: number;
  readonly firstItem: number;
  readonly lastItem: number;
  readonly totalItems: number;
  readonly totalPages: number;
  readonly visiblePages: readonly (number | 'ellipsis')[];
};

const pageSize = 10;

export default function AccessControlPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = getActiveTab(location.searchStr);

  const [accessItemsPage, setAccessItemsPage] = useState(0);
  const [accessItemsName, setAccessItemsName] = useState('');

  const [systemsPage, setSystemsPage] = useState(0);
  const [systemsName, setSystemsName] = useState('');

  const accessItemsQuery = useQuery({
    queryKey: ['administration', 'access-control', 'items', accessItemsPage, accessItemsName],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/items', {
        params: { query: { Name: accessItemsName || undefined, Page: accessItemsPage, PageSize: pageSize } as never },
      });

      if (error) {
        throw new Error('Could not load access items.');
      }

      return data;
    },
  });

  const systemsQuery = useQuery({
    queryKey: ['administration', 'access-control', 'systems', systemsPage, systemsName],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/access-control/systems', {
        params: { query: { Name: systemsName || undefined, Page: systemsPage, PageSize: pageSize } as never },
      });

      if (error) {
        throw new Error('Could not load access control systems.');
      }

      return data;
    },
  });

  function changeTab(nextTab: string) {
    if (!isAccessControlTab(nextTab)) {
      return;
    }

    void navigate({ to: '/administration/access-control', search: { tab: nextTab } as never, replace: true });
  }

  return (
    <section className="rounded-structural border border-border bg-content p-4 sm:p-6">
      <Tabs value={activeTab} onValueChange={changeTab}>
        <TabsList>
          <TabsTrigger value="access-items">Access Items</TabsTrigger>
          <TabsTrigger value="systems">Access Control Systems</TabsTrigger>
        </TabsList>

        <TabsContent value="access-items">
          <AccessItemsPanel
            name={accessItemsName}
            onNameChange={(value) => {
              setAccessItemsName(value);
              setAccessItemsPage(0);
            }}
            onOpenItem={(itemId) => void navigate({ to: '/administration/access-control/items/$itemId/edit', params: { itemId } })}
            response={accessItemsQuery.data}
            isLoading={accessItemsQuery.isLoading}
            isError={accessItemsQuery.isError}
            page={accessItemsPage}
            setPage={setAccessItemsPage}
          />
        </TabsContent>

        <TabsContent value="systems">
          <SystemsPanel
            name={systemsName}
            onNameChange={(value) => {
              setSystemsName(value);
              setSystemsPage(0);
            }}
            onOpenSystem={(systemId) => void navigate({ to: '/administration/access-control/systems/$systemId/edit', params: { systemId } })}
            response={systemsQuery.data}
            isLoading={systemsQuery.isLoading}
            isError={systemsQuery.isError}
            page={systemsPage}
            setPage={setSystemsPage}
          />
        </TabsContent>
      </Tabs>
    </section>
  );
}

function AccessItemsPanel({
  name,
  onNameChange,
  onOpenItem,
  response,
  isLoading,
  isError,
  page,
  setPage,
}: {
  readonly name: string;
  readonly onNameChange: (value: string) => void;
  readonly onOpenItem: (itemId: string) => void;
  readonly response: components['schemas']['PageOfAccessItemResponse'] | undefined;
  readonly isLoading: boolean;
  readonly isError: boolean;
  readonly page: number;
  readonly setPage: (page: number) => void;
}) {
  const items = response?.items ?? [];
  const pagination = getPaginationState(response, items.length, page, pageSize);

  return (
    <ListSection
      title="Access Items"
      description="Review physical access items and their current status."
      isLoading={isLoading}
      isError={isError}
      errorMessage="Could not load access items."
      emptyTitle="No access items found"
      emptyDescription="Try a different search."
      totalItems={pagination.totalItems}
      firstItem={pagination.firstItem}
      lastItem={pagination.lastItem}
      currentPage={pagination.currentPage}
      totalPages={pagination.totalPages}
      visiblePages={pagination.visiblePages}
      setPage={setPage}
      actions={<Link to="/administration/access-control/items/new" className={buttonVariants()}>
        Add access item
      </Link>}
      filters={<FilterInput label="Search access items" value={name} onChange={onNameChange} placeholder="Search by item name" />}
      table={
        <table className="w-full min-w-[56rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Description</th>
              <th className="px-4 py-3 font-semibold">Status</th>
              <th className="px-4 py-3 text-right font-semibold">Open</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {items.map((item) => (
              <tr key={item.id} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => onOpenItem(item.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); onOpenItem(item.id); } }}>
                <td className="px-4 py-4 font-medium text-foreground">{item.name}</td>
                <td className="px-4 py-4 text-muted-foreground">{item.description ?? '-'}</td>
                <td className="px-4 py-4"><StatusBadge status={item.status} /></td>
                <td className="px-4 py-4 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      mobileList={
        <div className="grid gap-3 md:hidden">
          {items.map((item) => (
            <article key={item.id} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => onOpenItem(item.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); onOpenItem(item.id); } }}>
              <div className="flex items-start justify-between gap-3">
                <h3 className="text-[15px] font-semibold text-foreground">{item.name}</h3>
                <ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
              </div>
              <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                <div><dt className="font-medium text-foreground">Description</dt><dd>{item.description ?? '-'}</dd></div>
                <div><dt className="font-medium text-foreground">Status</dt><dd><StatusBadge status={item.status} /></dd></div>
              </dl>
            </article>
          ))}
        </div>
      }
      hasItems={items.length > 0}
    />
  );
}

function SystemsPanel({
  name,
  onNameChange,
  onOpenSystem,
  response,
  isLoading,
  isError,
  page,
  setPage,
}: {
  readonly name: string;
  readonly onNameChange: (value: string) => void;
  readonly onOpenSystem: (systemId: string) => void;
  readonly response: components['schemas']['PageOfAccessControlSystemResponse'] | undefined;
  readonly isLoading: boolean;
  readonly isError: boolean;
  readonly page: number;
  readonly setPage: (page: number) => void;
}) {
  const systems = response?.items ?? [];
  const pagination = getPaginationState(response, systems.length, page, pageSize);

  return (
    <ListSection
      title="Access Control Systems"
      description="Review configured physical access control systems."
      isLoading={isLoading}
      isError={isError}
      errorMessage="Could not load access control systems."
      emptyTitle="No access control systems found"
      emptyDescription="Try a different search."
      totalItems={pagination.totalItems}
      firstItem={pagination.firstItem}
      lastItem={pagination.lastItem}
      currentPage={pagination.currentPage}
      totalPages={pagination.totalPages}
      visiblePages={pagination.visiblePages}
      setPage={setPage}
      actions={<Link to="/administration/access-control/systems/new" className={buttonVariants()}>
        Register system
      </Link>}
      filters={<FilterInput label="Search systems" value={name} onChange={onNameChange} placeholder="Search by system name" />}
      table={
        <table className="w-full min-w-[64rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Provider</th>
              <th className="px-4 py-3 font-semibold">Status</th>
              <th className="px-4 py-3 font-semibold">Endpoint</th>
              <th className="px-4 py-3 text-right font-semibold">Open</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {systems.map((system) => (
              <tr key={system.id} className="cursor-pointer transition hover:bg-hover-blue" role="link" tabIndex={0} onClick={() => onOpenSystem(system.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); onOpenSystem(system.id); } }}>
                <td className="px-4 py-4 font-medium text-foreground">{system.name}</td>
                <td className="px-4 py-4"><AccessControlProviderBadge providerKind={system.providerKind} /></td>
                <td className="px-4 py-4"><StatusBadge status={system.status} /></td>
                <td className="px-4 py-4 text-muted-foreground">{system.endpoint}</td>
                <td className="px-4 py-4 text-right text-muted-foreground"><span className="inline-flex items-center justify-center"><ChevronRight className="size-4" aria-hidden="true" /></span></td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      mobileList={
        <div className="grid gap-3 md:hidden">
          {systems.map((system) => (
            <article key={system.id} className="rounded-structural border border-border p-4 transition hover:bg-hover-blue" role="button" tabIndex={0} onClick={() => onOpenSystem(system.id)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); onOpenSystem(system.id); } }}>
              <div className="flex items-start justify-between gap-3">
                <h3 className="text-[15px] font-semibold text-foreground">{system.name}</h3>
                <ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
              </div>
              <dl className="mt-3 grid gap-2 text-[14px] text-muted-foreground">
                <div><dt className="font-medium text-foreground">Provider</dt><dd><AccessControlProviderBadge providerKind={system.providerKind} /></dd></div>
                <div><dt className="font-medium text-foreground">Status</dt><dd><StatusBadge status={system.status} /></dd></div>
                <div><dt className="font-medium text-foreground">Endpoint</dt><dd>{system.endpoint}</dd></div>
              </dl>
            </article>
          ))}
        </div>
      }
      hasItems={systems.length > 0}
    />
  );
}

function ListSection({
  title,
  description,
  isLoading,
  isError,
  errorMessage,
  emptyTitle,
  emptyDescription,
  totalItems,
  firstItem,
  lastItem,
  currentPage,
  totalPages,
  visiblePages,
  setPage,
  actions,
  filters,
  table,
  mobileList,
  hasItems,
}: {
  readonly title: string;
  readonly description: string;
  readonly isLoading: boolean;
  readonly isError: boolean;
  readonly errorMessage: string;
  readonly emptyTitle: string;
  readonly emptyDescription: string;
  readonly totalItems: number;
  readonly firstItem: number;
  readonly lastItem: number;
  readonly currentPage: number;
  readonly totalPages: number;
  readonly visiblePages: readonly (number | 'ellipsis')[];
  readonly setPage: (page: number) => void;
  readonly actions?: React.ReactNode;
  readonly filters: React.ReactNode;
  readonly table: React.ReactNode;
  readonly mobileList: React.ReactNode;
  readonly hasItems: boolean;
}) {
  return (
    <div className="grid gap-4 pt-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">{title}</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{description}</p>
        </div>
        {actions ? <div>{actions}</div> : null}
      </div>

      <div className="grid gap-3 rounded-structural border border-border p-4 md:grid-cols-2">{filters}</div>

      {isError ? <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">{errorMessage}</p> : null}

      {!isLoading && !isError && !hasItems ? (
        <Empty>
          <EmptyHeader>
            <EmptyTitle>{emptyTitle}</EmptyTitle>
            <EmptyDescription>{emptyDescription}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="grid gap-4">
          <div className="md:hidden">
            {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">Loading...</p> : null}
            {!isLoading ? mobileList : null}
          </div>

          <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
            {isLoading ? <p className="px-4 py-5 text-[14px] text-muted-foreground">Loading...</p> : table}
          </div>

          {!isLoading && !isError && totalItems > 0 ? (
            <div className="flex flex-col gap-3 text-[14px] text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
              <p>Showing {firstItem}-{lastItem} of {totalItems}</p>
              <Pagination className="sm:mx-0 sm:w-auto">
                <PaginationContent>
                  <PaginationItem>
                    <PaginationPrevious disabled={currentPage === 0} onClick={() => setPage(Math.max(0, currentPage - 1))} />
                  </PaginationItem>
                  {visiblePages.map((visiblePage, index) =>
                    visiblePage === 'ellipsis' ? (
                      <PaginationItem key={`${visiblePage}-${index}`}>
                        <PaginationEllipsis />
                      </PaginationItem>
                    ) : (
                      <PaginationItem key={visiblePage}>
                        <PaginationLink isActive={visiblePage === currentPage} onClick={() => setPage(visiblePage)}>
                          {visiblePage + 1}
                        </PaginationLink>
                      </PaginationItem>
                    ),
                  )}
                  <PaginationItem>
                    <PaginationNext disabled={currentPage >= totalPages - 1} onClick={() => setPage(Math.min(totalPages - 1, currentPage + 1))} />
                  </PaginationItem>
                </PaginationContent>
              </Pagination>
            </div>
          ) : null}
        </div>
      )}
    </div>
  );
}

function FilterInput({ label, value, onChange, placeholder }: { readonly label: string; readonly value: string; readonly onChange: (value: string) => void; readonly placeholder: string }) {
  return (
    <label className="grid gap-2 text-[14px] font-medium md:max-w-md">
      <span>{label}</span>
      <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary" value={value} onChange={(event) => onChange(event.target.value)} placeholder={placeholder} />
    </label>
  );
}

function StatusBadge({ status }: { readonly status: string }) {
  return <Badge variant={status === 'Active' ? 'success' : 'secondary'}>{status}</Badge>;
}

function getActiveTab(searchStr: string): AccessControlTab {
  const tab = new URLSearchParams(searchStr).get('tab');
  return isAccessControlTab(tab) ? tab : 'access-items';
}

function isAccessControlTab(value: string | null | undefined): value is AccessControlTab {
  return value === 'access-items' || value === 'systems';
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
