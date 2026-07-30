import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ChevronLeft, ChevronRight, Clock, X } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { toast } from 'sonner';

import { api, apiBaseUrl, getAccessToken } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { Pagination, PaginationContent, PaginationEllipsis, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from '@/shared/components/ui/pagination';
import { cn } from '@/shared/utils/cn';

import { getReceptionDeskWorkstationHeaders } from './reception-desk-workstation-settings';

type Arrival = components['schemas']['ArrivalResponse'];
type ArrivalEntry = components['schemas']['ArrivalEntryResponse'];
type ReceptionActor = components['schemas']['ReceptionActorResponse'];
type Location = components['schemas']['LocationResponse'];
type Visit = components['schemas']['VisitResponse'];
type VisitInvitation = components['schemas']['VisitInvitationResponse'];
type VisitorPreOnboardingSaga = components['schemas']['VisitorPreOnboardingSaga'];
type CheckInDocument = components['schemas']['CheckInDocumentResponse'];
type ArrivalIntervalView = 'today' | 'week';
type ArrivalListMode = 'expected' | 'onboarded' | 'history';

const arrivalIntervalStorageKey = 'fabric.reception-desk.expected-arrivals';
const historyIntervalStorageKey = 'fabric.reception-desk.history';
const pageSize = 10;
const onboardingSagaRefetchIntervalMs = 10_000;
const intervalOptions: { readonly label: string; readonly value: ArrivalIntervalView }[] = [
  { label: 'Today', value: 'today' },
  { label: 'Week', value: 'week' },
];

type StoredArrivalIntervalState = {
  readonly view?: ArrivalIntervalView;
  readonly anchorDate?: string;
};

export function ReceptionDeskExpectedArrivalsPage() {
  return (
    <div className="grid gap-4">
      <div>
        <h2 className="text-[18px] font-semibold tracking-tight">Expected arrivals</h2>
        <p className="mt-1 text-[14px] text-muted-foreground">Review upcoming arrivals for this workstation location scope.</p>
      </div>
      <div className="rounded-structural border border-border bg-content">
        <ExpectedArrivalsTab />
      </div>
    </div>
  );
}

export function ReceptionDeskArrivalsPage() {
  return (
    <div className="grid gap-4">
      <div>
        <h2 className="text-[18px] font-semibold tracking-tight">Arrivals</h2>
        <p className="mt-1 text-[14px] text-muted-foreground">Manage onboarded arrivals and front desk check-in state.</p>
      </div>
      <div className="rounded-structural border border-border bg-content">
        <ArrivalsTab />
      </div>
    </div>
  );
}

export function ReceptionDeskHistoryPage() {
  return (
    <div className="grid gap-4">
      <div>
        <h2 className="text-[18px] font-semibold tracking-tight">History</h2>
        <p className="mt-1 text-[14px] text-muted-foreground">Review offboarded arrivals and check-in history for this workstation scope.</p>
      </div>
      <div className="rounded-structural border border-border bg-content">
        <HistoryTab />
      </div>
    </div>
  );
}

function ExpectedArrivalsTab() {
  const [page, setPage] = useState(0);
  const [selectedArrivalId, setSelectedArrivalId] = useState<string | null>(null);
  const [intervalState, setIntervalState] = useState(() => getStoredIntervalState(arrivalIntervalStorageKey));
  const interval = useMemo(() => getArrivalInterval(intervalState.anchorDate, intervalState.view), [intervalState.anchorDate, intervalState.view]);

  useEffect(() => {
    window.sessionStorage.setItem(arrivalIntervalStorageKey, JSON.stringify(intervalState));
  }, [intervalState]);

  const expectedArrivalsQuery = useQuery({
    queryKey: ['reception-desk', 'expected-arrivals', interval.start.toISOString(), interval.end.toISOString(), page, pageSize],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/arrivals', {
        headers: getReceptionDeskWorkstationHeaders(),
        params: {
          query: {
            Status: 'NotYetOnboarded',
            expectedArrivalAfter: interval.start.toISOString(),
            expectedArrivalBefore: interval.end.toISOString(),
            page,
            pageSize,
          },
        },
      });

      if (error) {
        throw new Error('Could not load expected arrivals.');
      }

      return data;
    },
  });

  const pagedArrivals = expectedArrivalsQuery.data;
  const arrivals = pagedArrivals?.items ?? [];
  const pagination = getPaginationState(pagedArrivals, arrivals.length, page);

  function setView(view: ArrivalIntervalView) {
    setPage(0);
    setSelectedArrivalId(null);
    setIntervalState((current) => ({ ...current, view }));
  }

  function jumpInterval(direction: -1 | 1) {
    setPage(0);
    setSelectedArrivalId(null);
    setIntervalState((current) => ({
      ...current,
      anchorDate: addInterval(new Date(current.anchorDate), current.view, direction).toISOString(),
    }));
  }

  return (
    <section className="grid gap-4 p-4 sm:p-6" role="tabpanel" aria-label="Expected Arrivals">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="text-[16px] font-semibold tracking-tight">{interval.label}</h3>
          <p className="mt-1 text-[14px] text-muted-foreground">
            {expectedArrivalsQuery.isLoading ? 'Loading expected arrivals...' : `${pagination.totalItems} expected ${pagination.totalItems === 1 ? 'arrival' : 'arrivals'}`}
          </p>
        </div>
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          <div className="flex items-center gap-2">
            <button
              type="button"
              className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
              aria-label={`Previous ${getViewLabel(intervalState.view)}`}
              onClick={() => jumpInterval(-1)}
            >
              <ChevronLeft className="size-4" aria-hidden="true" />
            </button>
            <button
              type="button"
              className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
              aria-label={`Next ${getViewLabel(intervalState.view)}`}
              onClick={() => jumpInterval(1)}
            >
              <ChevronRight className="size-4" aria-hidden="true" />
            </button>
          </div>
          <div className="flex flex-wrap items-center gap-1 rounded-interactive border border-border bg-hover-gray p-1" aria-label="Arrival interval">
            {intervalOptions.map((option) => (
              <button
                key={option.value}
                type="button"
                className={cn(
                  'rounded-interactive px-3 py-2 text-[13px] font-semibold text-muted-foreground transition hover:text-foreground',
                  intervalState.view === option.value && 'bg-content text-foreground shadow-sm',
                )}
                aria-pressed={intervalState.view === option.value}
                onClick={() => setView(option.value)}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      <ArrivalList
        arrivals={arrivals}
        emptyText="No expected arrivals in this interval."
        errorText="Could not load expected arrivals."
        isError={expectedArrivalsQuery.isError}
        isLoading={expectedArrivalsQuery.isLoading}
        loadingText="Loading expected arrivals..."
        mode="expected"
        selectedArrivalId={selectedArrivalId}
        onSelectArrival={setSelectedArrivalId}
      />

      {selectedArrivalId ? <ExpectedArrivalDetails arrivalId={selectedArrivalId} onClose={() => setSelectedArrivalId(null)} /> : null}

      <ArrivalPagination label="expected arrivals" pagination={pagination} isVisible={!expectedArrivalsQuery.isLoading && !expectedArrivalsQuery.isError && pagination.totalItems > 0} setPage={setPage} />
    </section>
  );
}

function ArrivalsTab() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(0);

  const arrivalsQuery = useQuery({
    queryKey: ['reception-desk', 'arrivals', page, pageSize],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/arrivals', {
        headers: getReceptionDeskWorkstationHeaders(),
        params: {
          query: {
            Status: 'Onboarded',
            page,
            pageSize,
          },
        },
      });

      if (error) {
        throw new Error('Could not load arrivals.');
      }

      return data;
    },
  });

  const pagedArrivals = arrivalsQuery.data;
  const arrivals = pagedArrivals?.items ?? [];
  const pagination = getPaginationState(pagedArrivals, arrivals.length, page);

  const toggleCheckIn = useMutation({
    mutationFn: async (arrival: Arrival) => {
      const path = arrival.checkedIn ? '/api/reception/arrivals/{id}/check-out' : '/api/reception/arrivals/{id}/check-in';
      const { error } = await api.POST(path, {
        headers: getReceptionDeskWorkstationHeaders(),
        params: { path: { id: arrival.id } },
      });

      if (error) {
        throw new Error(arrival.checkedIn ? 'Could not check out arrival.' : 'Could not check in arrival.');
      }

      return arrival.checkedIn ? 'checked out' : 'checked in';
    },
    onSuccess: async (action) => {
      await queryClient.invalidateQueries({ queryKey: ['reception-desk'] });
      toast.success(`Arrival ${action}.`);
    },
    onError: (_error, arrival) => {
      toast.error(arrival.checkedIn ? 'Could not check out arrival.' : 'Could not check in arrival.');
    },
  });

  const offboardArrival = useMutation({
    mutationFn: async (arrival: Arrival) => {
      const { error } = await api.POST('/api/reception/arrivals/{id}/offboard', {
        headers: getReceptionDeskWorkstationHeaders(),
        params: { path: { id: arrival.id } },
      });

      if (error) {
        throw new Error('Could not offboard arrival.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['reception-desk'] });
      toast.success('Arrival offboarded.');
    },
    onError: () => {
      toast.error('Could not offboard arrival.');
    },
  });

  function confirmOffboard(arrival: Arrival) {
    const confirmed = window.confirm(`Offboard ${getArrivalName(arrival)}?`);

    if (confirmed) {
      offboardArrival.mutate(arrival);
    }
  }

  return (
    <section className="grid gap-4 p-4 sm:p-6" role="tabpanel" aria-label="Arrivals">
      <div>
        <h3 className="text-[16px] font-semibold tracking-tight">Arrivals</h3>
        <p className="mt-1 text-[14px] text-muted-foreground">
          {arrivalsQuery.isLoading ? 'Loading arrivals...' : `${pagination.totalItems} onboarded ${pagination.totalItems === 1 ? 'arrival' : 'arrivals'}`}
        </p>
      </div>

      <ArrivalList
        arrivals={arrivals}
        emptyText="No onboarded arrivals."
        errorText="Could not load arrivals."
        isError={arrivalsQuery.isError}
        isLoading={arrivalsQuery.isLoading}
        loadingText="Loading arrivals..."
        mode="onboarded"
        checkInActionArrivalId={toggleCheckIn.isPending ? toggleCheckIn.variables?.id : null}
        offboardActionArrivalId={offboardArrival.isPending ? offboardArrival.variables?.id : null}
        onOffboard={confirmOffboard}
        onToggleCheckIn={(arrival) => toggleCheckIn.mutate(arrival)}
      />

      <ArrivalPagination label="arrivals" pagination={pagination} isVisible={!arrivalsQuery.isLoading && !arrivalsQuery.isError && pagination.totalItems > 0} setPage={setPage} />
    </section>
  );
}

function HistoryTab() {
  const [page, setPage] = useState(0);
  const [selectedArrivalId, setSelectedArrivalId] = useState<string | null>(null);
  const [intervalState, setIntervalState] = useState(() => getStoredIntervalState(historyIntervalStorageKey));
  const interval = useMemo(() => getArrivalInterval(intervalState.anchorDate, intervalState.view), [intervalState.anchorDate, intervalState.view]);

  useEffect(() => {
    window.sessionStorage.setItem(historyIntervalStorageKey, JSON.stringify(intervalState));
  }, [intervalState]);

  const historyQuery = useQuery({
    queryKey: ['reception-desk', 'history', interval.start.toISOString(), interval.end.toISOString(), page, pageSize],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/arrivals', {
        headers: getReceptionDeskWorkstationHeaders(),
        params: {
          query: {
            Status: 'Offboarded',
            onboardedBefore: interval.end.toISOString(),
            offboardedAfter: interval.start.toISOString(),
            page,
            pageSize,
          },
        },
      });

      if (error) {
        throw new Error('Could not load history.');
      }

      return data;
    },
  });

  const pagedArrivals = historyQuery.data;
  const arrivals = pagedArrivals?.items ?? [];
  const pagination = getPaginationState(pagedArrivals, arrivals.length, page);

  function setView(view: ArrivalIntervalView) {
    setPage(0);
    setSelectedArrivalId(null);
    setIntervalState((current) => ({ ...current, view }));
  }

  function jumpInterval(direction: -1 | 1) {
    setPage(0);
    setSelectedArrivalId(null);
    setIntervalState((current) => ({
      ...current,
      anchorDate: addInterval(new Date(current.anchorDate), current.view, direction).toISOString(),
    }));
  }

  return (
    <section className="grid gap-4 p-4 sm:p-6" role="tabpanel" aria-label="History">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="text-[16px] font-semibold tracking-tight">{interval.label}</h3>
          <p className="mt-1 text-[14px] text-muted-foreground">
            {historyQuery.isLoading ? 'Loading history...' : `${pagination.totalItems} offboarded ${pagination.totalItems === 1 ? 'arrival' : 'arrivals'}`}
          </p>
        </div>
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          <div className="flex items-center gap-2">
            <button
              type="button"
              className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
              aria-label={`Previous ${getViewLabel(intervalState.view)}`}
              onClick={() => jumpInterval(-1)}
            >
              <ChevronLeft className="size-4" aria-hidden="true" />
            </button>
            <button
              type="button"
              className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
              aria-label={`Next ${getViewLabel(intervalState.view)}`}
              onClick={() => jumpInterval(1)}
            >
              <ChevronRight className="size-4" aria-hidden="true" />
            </button>
          </div>
          <div className="flex flex-wrap items-center gap-1 rounded-interactive border border-border bg-hover-gray p-1" aria-label="History interval">
            {intervalOptions.map((option) => (
              <button
                key={option.value}
                type="button"
                className={cn(
                  'rounded-interactive px-3 py-2 text-[13px] font-semibold text-muted-foreground transition hover:text-foreground',
                  intervalState.view === option.value && 'bg-content text-foreground shadow-sm',
                )}
                aria-pressed={intervalState.view === option.value}
                onClick={() => setView(option.value)}
              >
                {option.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      <ArrivalList
        arrivals={arrivals}
        emptyText="No offboarded arrivals in this interval."
        errorText="Could not load history."
        isError={historyQuery.isError}
        isLoading={historyQuery.isLoading}
        loadingText="Loading history..."
        mode="history"
        selectedArrivalId={selectedArrivalId}
        onSelectArrival={setSelectedArrivalId}
      />

      {selectedArrivalId ? <HistoryArrivalDetails arrivalId={selectedArrivalId} onClose={() => setSelectedArrivalId(null)} /> : null}

      <ArrivalPagination label="history entries" pagination={pagination} isVisible={!historyQuery.isLoading && !historyQuery.isError && pagination.totalItems > 0} setPage={setPage} />
    </section>
  );
}

function ArrivalList({
  arrivals,
  checkInActionArrivalId,
  emptyText,
  errorText,
  isError,
  isLoading,
  loadingText,
  mode,
  offboardActionArrivalId,
  onToggleCheckIn,
  onOffboard,
  onSelectArrival,
  selectedArrivalId,
}: {
  readonly arrivals: Arrival[];
  readonly checkInActionArrivalId?: string | null;
  readonly emptyText: string;
  readonly errorText: string;
  readonly isError: boolean;
  readonly isLoading: boolean;
  readonly loadingText: string;
  readonly mode: ArrivalListMode;
  readonly offboardActionArrivalId?: string | null;
  readonly onToggleCheckIn?: (arrival: Arrival) => void;
  readonly onOffboard?: (arrival: Arrival) => void;
  readonly selectedArrivalId?: string | null;
  readonly onSelectArrival?: (arrivalId: string) => void;
}) {
  if (isError) {
    return (
      <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
        {errorText}
      </p>
    );
  }

  return (
    <div className="grid gap-4">
      <div className="grid gap-3 md:hidden">
        {isLoading ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">{loadingText}</p> : null}
        {!isLoading && arrivals.length === 0 ? <p className="rounded-structural border border-border p-4 text-[14px] text-muted-foreground">{emptyText}</p> : null}
        {arrivals.map((arrival) => (
          <ArrivalCard key={arrival.id} arrival={arrival} checkInActionArrivalId={checkInActionArrivalId} mode={mode} offboardActionArrivalId={offboardActionArrivalId} isSelected={selectedArrivalId === arrival.id} onOffboard={onOffboard} onSelect={onSelectArrival} onToggleCheckIn={onToggleCheckIn} />
        ))}
      </div>

      <div className="hidden overflow-x-auto rounded-structural border border-border md:block">
        <table className="w-full min-w-[52rem] border-collapse text-left text-[14px]">
          <thead className="bg-hover-gray text-[12px] uppercase text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-semibold">Name</th>
              <th className="px-4 py-3 font-semibold">Company</th>
              <th className="px-4 py-3 font-semibold">{getArrivalPrimaryTimeHeader(mode)}</th>
              <th className="px-4 py-3 font-semibold">{getArrivalSecondaryTimeHeader(mode)}</th>
              <th className="px-4 py-3 font-semibold">Type</th>
              {mode === 'onboarded' ? <th className="px-4 py-3 text-right font-semibold">Actions</th> : null}
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {isLoading ? (
              <tr>
                <td className="px-4 py-5 text-muted-foreground" colSpan={mode === 'onboarded' ? 6 : 5}>
                  {loadingText}
                </td>
              </tr>
            ) : null}

            {!isLoading && arrivals.length === 0 ? (
              <tr>
                <td className="px-4 py-5 text-muted-foreground" colSpan={mode === 'onboarded' ? 6 : 5}>
                  {emptyText}
                </td>
              </tr>
            ) : null}

            {arrivals.map((arrival) => (
              <ArrivalTableRow key={arrival.id} arrival={arrival} checkInActionArrivalId={checkInActionArrivalId} mode={mode} offboardActionArrivalId={offboardActionArrivalId} isSelected={selectedArrivalId === arrival.id} onOffboard={onOffboard} onSelect={onSelectArrival} onToggleCheckIn={onToggleCheckIn} />
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function ArrivalCard({
  arrival,
  checkInActionArrivalId,
  isSelected = false,
  mode,
  offboardActionArrivalId,
  onOffboard,
  onSelect,
  onToggleCheckIn,
}: {
  readonly arrival: Arrival;
  readonly checkInActionArrivalId?: string | null;
  readonly isSelected?: boolean;
  readonly mode: ArrivalListMode;
  readonly offboardActionArrivalId?: string | null;
  readonly onOffboard?: (arrival: Arrival) => void;
  readonly onSelect?: (arrivalId: string) => void;
  readonly onToggleCheckIn?: (arrival: Arrival) => void;
}) {
  const isClickable = (mode === 'expected' || mode === 'history') && !!onSelect;
  const isCheckInActionPending = checkInActionArrivalId === arrival.id;
  const isOffboardActionPending = offboardActionArrivalId === arrival.id;
  const isActionPending = isCheckInActionPending || isOffboardActionPending;

  return (
    <article
      className={cn(
        'rounded-structural border border-border p-4',
        isClickable && 'cursor-pointer transition hover:border-primary/40 hover:bg-hover-blue/40',
        isSelected && 'border-primary/50 bg-hover-blue/60',
      )}
      role={isClickable ? 'button' : undefined}
      tabIndex={isClickable ? 0 : undefined}
      onClick={isClickable ? () => onSelect(arrival.id) : undefined}
      onKeyDown={isClickable ? (event) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          onSelect(arrival.id);
        }
      } : undefined}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="truncate text-[15px] font-semibold text-foreground">{getArrivalName(arrival)}</h3>
          <p className="mt-1 truncate text-[14px] text-muted-foreground">{arrival.company || 'No company'}</p>
        </div>
        {mode === 'onboarded' ? <SiteStatusBadge checkedIn={arrival.checkedIn} /> : <Badge variant="outline">{formatStatus(arrival.status)}</Badge>}
      </div>
      <div className="mt-4 grid gap-2 text-[13px] text-muted-foreground">
        <span className="inline-flex items-center gap-2">
          <Clock className="size-3.5" aria-hidden="true" />
          {getArrivalCardTimeText(arrival, mode)}
        </span>
        {mode !== 'expected' && arrival.onboardedBy ? <span>Onboarded by {formatReceptionActor(arrival.onboardedBy)}</span> : null}
        <span>Type: {arrival.type}</span>
      </div>
      {mode === 'onboarded' ? (
        <div className="mt-4 grid gap-2 sm:grid-cols-2">
          {onToggleCheckIn ? (
            <Button type="button" variant="outline" className="w-full" disabled={isActionPending} onClick={() => onToggleCheckIn(arrival)}>
              {getCheckToggleLabel(arrival, isCheckInActionPending)}
            </Button>
          ) : null}
          {onOffboard ? (
            <Button type="button" variant="outline" className="w-full" disabled={isActionPending} onClick={() => onOffboard(arrival)}>
              {isOffboardActionPending ? 'Offboarding...' : 'Offboard'}
            </Button>
          ) : null}
        </div>
      ) : null}
    </article>
  );
}

function ArrivalTableRow({
  arrival,
  checkInActionArrivalId,
  isSelected = false,
  mode,
  offboardActionArrivalId,
  onOffboard,
  onSelect,
  onToggleCheckIn,
}: {
  readonly arrival: Arrival;
  readonly checkInActionArrivalId?: string | null;
  readonly isSelected?: boolean;
  readonly mode: ArrivalListMode;
  readonly offboardActionArrivalId?: string | null;
  readonly onOffboard?: (arrival: Arrival) => void;
  readonly onSelect?: (arrivalId: string) => void;
  readonly onToggleCheckIn?: (arrival: Arrival) => void;
}) {
  const isClickable = (mode === 'expected' || mode === 'history') && !!onSelect;
  const isCheckInActionPending = checkInActionArrivalId === arrival.id;
  const isOffboardActionPending = offboardActionArrivalId === arrival.id;
  const isActionPending = isCheckInActionPending || isOffboardActionPending;

  return (
    <tr
      className={cn('align-middle', isClickable && 'cursor-pointer transition hover:bg-hover-blue/40', isSelected && 'bg-hover-blue/60')}
      onClick={isClickable ? () => onSelect(arrival.id) : undefined}
    >
      <td className="px-4 py-4">
        <span className="font-medium text-foreground">{getArrivalName(arrival)}</span>
        {mode !== 'expected' && arrival.onboardedBy ? <span className="mt-1 block text-[12px] text-muted-foreground">Onboarded by {formatReceptionActor(arrival.onboardedBy)}</span> : null}
      </td>
      <td className="px-4 py-4 text-muted-foreground">{arrival.company || 'No company'}</td>
      <td className="px-4 py-4 text-muted-foreground">{getArrivalPrimaryTimeValue(arrival, mode)}</td>
      <td className="px-4 py-4 text-muted-foreground">{getArrivalSecondaryTimeValue(arrival, mode)}</td>
      <td className="px-4 py-4">
        <Badge variant="secondary">{arrival.type}</Badge>
      </td>
      {mode === 'onboarded' ? (
        <td className="px-4 py-4 text-right">
          <div className="flex justify-end gap-2">
            {onToggleCheckIn ? (
              <Button type="button" variant="outline" size="sm" disabled={isActionPending} onClick={() => onToggleCheckIn(arrival)}>
                {getCheckToggleLabel(arrival, isCheckInActionPending)}
              </Button>
            ) : null}
            {onOffboard ? (
              <Button type="button" variant="outline" size="sm" disabled={isActionPending} onClick={() => onOffboard(arrival)}>
                {isOffboardActionPending ? 'Offboarding...' : 'Offboard'}
              </Button>
            ) : null}
          </div>
        </td>
      ) : null}
    </tr>
  );
}

function HistoryArrivalDetails({ arrivalId, onClose }: { readonly arrivalId: string; readonly onClose: () => void }) {
  const arrivalQuery = useQuery({
    queryKey: ['reception-desk', 'arrival', arrivalId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/arrivals/{id}', {
        headers: getReceptionDeskWorkstationHeaders(),
        params: { path: { id: arrivalId } },
      });

      if (error) {
        throw new Error('Could not load arrival history.');
      }

      return data;
    },
  });

  const arrival = arrivalQuery.data;
  const entries = [...(arrival?.entries ?? [])].sort((first, second) => new Date(first.timestamp).getTime() - new Date(second.timestamp).getTime());
  const [documentsExpanded, setDocumentsExpanded] = useState(false);

  useEffect(() => {
    setDocumentsExpanded(false);
  }, [arrivalId]);

  return (
    <aside className="rounded-structural border border-border bg-content p-4 shadow-sm sm:p-6" aria-label="Arrival history details">
      <div className="mb-5 flex items-start justify-between gap-3">
        <div>
          <h3 className="text-[16px] font-semibold tracking-tight">History details</h3>
          <p className="mt-1 text-[14px] text-muted-foreground">{arrival ? getArrivalName(arrival) : 'Loading arrival...'}</p>
        </div>
        <button
          type="button"
          className="inline-flex size-9 shrink-0 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
          aria-label="Close history details"
          onClick={onClose}
        >
          <X className="size-4" aria-hidden="true" />
        </button>
      </div>

      {arrivalQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading history details...</p> : null}

      {arrivalQuery.isError ? (
        <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
          Could not load history details.
        </p>
      ) : null}

      {arrival ? (
        <div className="grid gap-5">
          <section className="grid gap-3">
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant="secondary">{arrival.type}</Badge>
              <Badge variant="outline">{formatStatus(arrival.status)}</Badge>
            </div>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <DetailField label="Onboarded" value={formatNullableDateTime(arrival.onboardedAt)} />
              <DetailField label="Onboarded by" value={formatReceptionActor(arrival.onboardedBy)} />
              <DetailField label="Offboarded" value={formatNullableDateTime(arrival.offboardedAt)} />
              <DetailField label="Offboarded by" value={formatReceptionActor(arrival.offboardedBy)} />
              <DetailField label="Expected arrival" value={formatDateTime(arrival.expectedArrivalTime)} />
              <DetailField label="Expected leave" value={formatDateTime(arrival.expectedOffboardTime)} />
            </div>
          </section>

          <section className="grid gap-3 rounded-structural border border-border p-4">
            <div>
              <h4 className="text-[15px] font-semibold tracking-tight">Check-in history</h4>
              <p className="mt-1 text-[14px] text-muted-foreground">All recorded check-in and check-out timestamps.</p>
            </div>
            {entries.length > 0 ? (
              <ol className="grid gap-2">
                {entries.map((entry) => (
                  <li key={entry.id} className="flex flex-col gap-1 rounded-interactive border border-border bg-hover-gray px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
                    <span>
                      <span className="block text-[14px] font-medium text-foreground">{formatArrivalEntryType(entry.type)}</span>
                      <span className="block text-[13px] text-muted-foreground">By {formatReceptionActor(entry.actor)}</span>
                    </span>
                    <span className="text-[13px] text-muted-foreground">{formatDateTime(entry.timestamp)}</span>
                  </li>
                ))}
              </ol>
            ) : (
              <p className="rounded-interactive border border-border bg-hover-gray px-4 py-3 text-[14px] text-muted-foreground">No check-in history recorded.</p>
            )}
          </section>

          <section className="grid gap-3 rounded-structural border border-border p-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h4 className="text-[15px] font-semibold tracking-tight">Documents</h4>
                <p className="mt-1 text-[14px] text-muted-foreground">Captured kiosk onboarding documents saved with this arrival.</p>
              </div>
              {arrival.documents.length > 0 ? (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  aria-expanded={documentsExpanded}
                  onClick={() => setDocumentsExpanded((current) => !current)}
                >
                  {documentsExpanded ? 'Hide documents' : 'See documents'}
                </Button>
              ) : null}
            </div>

            {arrival.documents.length === 0 ? (
              <p className="rounded-interactive border border-border bg-hover-gray px-4 py-3 text-[14px] text-muted-foreground">No documents were stored for this arrival.</p>
            ) : null}

            {documentsExpanded ? (
              <div className="grid gap-4 lg:grid-cols-2">
                {arrival.documents.map((document) => (
                  <ArrivalDocumentPreview key={document.id} arrivalId={arrival.id} arrivalName={getArrivalName(arrival)} document={document} />
                ))}
              </div>
            ) : null}
          </section>
        </div>
      ) : null}
    </aside>
  );
}

function ExpectedArrivalDetails({ arrivalId, onClose }: { readonly arrivalId: string; readonly onClose: () => void }) {
  const queryClient = useQueryClient();

  const arrivalQuery = useQuery({
    queryKey: ['reception-desk', 'arrival', arrivalId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/reception/arrivals/{id}', {
        headers: getReceptionDeskWorkstationHeaders(),
        params: { path: { id: arrivalId } },
      });

      if (error) {
        throw new Error('Could not load arrival details.');
      }

      return data;
    },
  });

  const arrival = arrivalQuery.data;

  const locationQuery = useQuery({
    queryKey: ['locations', 'location', arrival?.locationId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/locations/locations/{id}', {
        params: { path: { id: arrival?.locationId ?? '' } },
      });

      if (error) {
        throw new Error('Could not load location.');
      }

      return data;
    },
    enabled: !!arrival?.locationId,
  });

  const visitQuery = useQuery({
    queryKey: ['visitors-management', 'invitation-visit', arrival?.invitationId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/visitors/invitations/{invitationId}/visit', {
        params: { path: { invitationId: arrival?.invitationId ?? '' } },
      });

      if (error) {
        return null;
      }

      return data;
    },
    enabled: arrival?.type === 'Visitor' && !!arrival.invitationId,
  });

  const visit = visitQuery.data ?? null;
  const invitation = visit?.invitations.find((item) => item.id === arrival?.invitationId) ?? null;

  const sagaQuery = useQuery({
    queryKey: ['visitors-management', 'invitation-saga', visit?.id, arrival?.invitationId],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/sagas/visitor-pre-onboarding/{visitId}/{invitationId}', {
        params: { path: { visitId: visit?.id ?? '', invitationId: arrival?.invitationId ?? '' } },
      });

      if (error) {
        return null;
      }

      return data;
    },
    enabled: !!visit?.id && !!arrival?.invitationId,
    refetchInterval: (query) => {
      const saga = query.state.data;
      if (!saga) {
        return false;
      }

      return isSagaStillProcessing(saga)
        ? onboardingSagaRefetchIntervalMs
        : false;
    },
  });

  const onboardArrival = useMutation({
    mutationFn: async () => {
      const { error } = await api.POST('/api/reception/arrivals/{id}/onboard', {
        headers: getReceptionDeskWorkstationHeaders(),
        params: { path: { id: arrivalId } },
        body: {},
      });

      if (error) {
        throw new Error('Could not onboard arrival.');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['reception-desk'] });
      toast.success('Arrival onboarded.');
    },
    onError: () => {
      toast.error('Could not onboard arrival.');
    },
  });

  const canOnboard = arrival?.status === 'NotYetOnboarded';

  return (
    <aside className="rounded-structural border border-border bg-content p-4 shadow-sm sm:p-6" aria-label="Expected arrival details">
      <div className="mb-5 flex items-start justify-between gap-3">
        <div>
          <h3 className="text-[16px] font-semibold tracking-tight">Arrival details</h3>
          <p className="mt-1 text-[14px] text-muted-foreground">{arrival ? getArrivalName(arrival) : 'Loading arrival...'}</p>
        </div>
        <button
          type="button"
          className="inline-flex size-9 shrink-0 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
          aria-label="Close arrival details"
          onClick={onClose}
        >
          <X className="size-4" aria-hidden="true" />
        </button>
      </div>

      {arrivalQuery.isLoading ? <p className="text-[14px] text-muted-foreground">Loading arrival details...</p> : null}

      {arrivalQuery.isError ? (
        <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
          Could not load arrival details.
        </p>
      ) : null}

      {arrival ? (
        <div className="grid gap-5">
          <section className="grid gap-3">
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant="secondary">{arrival.type}</Badge>
              <Badge variant="outline">{formatStatus(arrival.status)}</Badge>
              {arrival.confirmed !== null ? <ConfirmationBadge confirmed={arrival.confirmed} /> : null}
            </div>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <DetailField label="Expected arrival" value={formatDateTime(arrival.expectedArrivalTime)} />
              <DetailField label="Expected leave" value={formatDateTime(arrival.expectedOffboardTime)} />
              <DetailField label="Company" value={arrival.company || 'No company'} />
              <DetailField label="Location" value={locationQuery.isLoading ? 'Loading location...' : formatLocation(locationQuery.data ?? null)} />
            </div>
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-[13px] text-muted-foreground">Onboard confirms this expected arrival and moves it to Arrivals.</p>
              <Button type="button" disabled={!canOnboard || onboardArrival.isPending} onClick={() => onboardArrival.mutate()}>
                {onboardArrival.isPending ? 'Onboarding...' : 'Onboard'}
              </Button>
            </div>
          </section>

          {arrival.type === 'Visitor' ? (
            <VisitorArrivalDetails arrival={arrival} invitation={invitation} isLoading={visitQuery.isLoading || sagaQuery.isLoading} saga={sagaQuery.data ?? null} visit={visit} />
          ) : (
            <p className="rounded-interactive border border-border bg-hover-gray px-4 py-3 text-[14px] text-muted-foreground">Contractor details are not available yet.</p>
          )}
        </div>
      ) : null}
    </aside>
  );
}

function VisitorArrivalDetails({ arrival, invitation, isLoading, saga, visit }: { readonly arrival: Arrival; readonly invitation: VisitInvitation | null; readonly isLoading: boolean; readonly saga: VisitorPreOnboardingSaga | null; readonly visit: Visit | null }) {
  if (isLoading) {
    return <p className="rounded-interactive border border-border bg-hover-gray px-4 py-3 text-[14px] text-muted-foreground">Loading visitor details...</p>;
  }

  if (!visit) {
    return <p className="rounded-interactive border border-border bg-hover-gray px-4 py-3 text-[14px] text-muted-foreground">No visit details found for this invitation.</p>;
  }

  return (
    <section className="grid gap-4 rounded-structural border border-border p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h4 className="text-[15px] font-semibold tracking-tight">Visit details</h4>
          <p className="mt-1 text-[14px] text-muted-foreground">{visit.summary || 'Untitled visit'}</p>
        </div>
        <Badge variant="outline">{visit.status}</Badge>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <DetailField label="Visit starts" value={visit.start ? formatDateTime(visit.start) : 'Not planned'} />
        <DetailField label="Visit ends" value={visit.stop ? formatDateTime(visit.stop) : 'Not planned'} />
        <DetailField label="Host" value={getHostName(visit.host)} />
        <DetailField label="Invitation" value={invitation?.confirmationStatus ?? formatConfirmation(arrival.confirmed)} />
      </div>

      {invitation ? (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <DetailField label="Email" value={invitation.email} />
          <DetailField label="Transport" value={invitation.transport ?? 'Not provided'} />
          <DetailField label="License plate" value={invitation.licensePlate ?? 'Not provided'} />
          <DetailField label="Confirmed at" value={invitation.confirmedAt ? formatDateTime(invitation.confirmedAt) : 'Not confirmed'} />
        </div>
      ) : null}

      <ReceptionDeskJourneyDetails invitation={invitation} saga={saga} />
    </section>
  );
}

function ReceptionDeskJourneyDetails({ invitation, saga }: { readonly invitation: VisitInvitation | null; readonly saga: VisitorPreOnboardingSaga | null }) {
  return (
    <section className="grid gap-3 rounded-structural border border-border p-4">
      <div>
        <h4 className="text-[15px] font-semibold tracking-tight">Visit journey</h4>
        <p className="mt-1 text-[14px] text-muted-foreground">Workflow and invitation milestones for this visitor onboarding flow.</p>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <DetailField label="Workflow state" value={formatSagaState(saga)} />
        <DetailField label="Retry count" value={formatRetryCount(saga?.retryCount)} />
        <DetailField label="Invitation" value={invitation?.confirmationStatus ?? 'No invitation status'} />
        <DetailField label="Confirmed at" value={invitation?.confirmedAt ? formatDateTime(invitation.confirmedAt) : 'Not confirmed'} />
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <DetailField label="Workflow created" value={saga?.createdAt ? formatDateTime(saga.createdAt) : 'Not started'} />
        <DetailField label="Invitation sent" value={saga?.invitationSentAt ? formatDateTime(saga.invitationSentAt) : 'Not sent'} />
        <DetailField label="Arrival notice sent" value={saga?.arrivalNotificationSentAt ? formatDateTime(saga.arrivalNotificationSentAt) : 'Not sent'} />
        <DetailField label="Next retry" value={saga?.nextRetryAt ? formatDateTime(saga.nextRetryAt) : 'No retry scheduled'} />
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <DetailField label="Workflow expires" value={saga?.expiresAt ? formatDateTime(saga.expiresAt) : 'No expiry'} />
        <DetailField label="Arrival link" value={saga?.arrivalId ? 'Linked' : 'Not linked'} />
        <DetailField label="Credential" value={saga?.credentialId ? 'Issued' : 'Not issued'} />
        <DetailField label="Access policy" value={saga?.accessPolicyId ? 'Assigned' : 'Not assigned'} />
      </div>
    </section>
  );
}

function SiteStatusBadge({ checkedIn }: { readonly checkedIn: boolean }) {
  return <Badge variant={checkedIn ? 'success' : 'warning'}>{checkedIn ? 'On site' : 'Not on site'}</Badge>;
}

function getArrivalPrimaryTimeHeader(mode: ArrivalListMode) {
  if (mode === 'expected') {
    return 'Expected arrival';
  }

  return mode === 'history' ? 'Onboarded' : 'Site status';
}

function getArrivalSecondaryTimeHeader(mode: ArrivalListMode) {
  if (mode === 'expected') {
    return 'Status';
  }

  return mode === 'history' ? 'Offboarded' : 'Expected leave';
}

function getArrivalPrimaryTimeValue(arrival: Arrival, mode: ArrivalListMode) {
  if (mode === 'expected') {
    return formatDateTime(arrival.expectedArrivalTime);
  }

  if (mode === 'history') {
    return formatNullableDateTime(arrival.onboardedAt);
  }

  return <SiteStatusBadge checkedIn={arrival.checkedIn} />;
}

function getArrivalSecondaryTimeValue(arrival: Arrival, mode: ArrivalListMode) {
  if (mode === 'expected') {
    return formatStatus(arrival.status);
  }

  return mode === 'history' ? formatNullableDateTime(arrival.offboardedAt) : formatDateTime(arrival.expectedOffboardTime);
}

function getArrivalCardTimeText(arrival: Arrival, mode: ArrivalListMode) {
  if (mode === 'expected') {
    return `Arrives ${formatDateTime(arrival.expectedArrivalTime)}`;
  }

  if (mode === 'history') {
    return `On site ${formatNullableDateTime(arrival.onboardedAt)} - ${formatNullableDateTime(arrival.offboardedAt)}`;
  }

  return `Expected leave ${formatDateTime(arrival.expectedOffboardTime)}`;
}

function getCheckToggleLabel(arrival: Arrival, isPending: boolean) {
  if (arrival.checkedIn) {
    return isPending ? 'Checking out...' : 'Check out';
  }

  return isPending ? 'Checking in...' : 'Check in';
}

function ArrivalPagination({ isVisible, label, pagination, setPage }: { readonly isVisible: boolean; readonly label: string; readonly pagination: PaginationState; readonly setPage: (page: number) => void }) {
  if (!isVisible) {
    return null;
  }

  return (
    <div className="flex flex-col gap-3 text-[14px] text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
      <p>
        Showing {pagination.firstItem}-{pagination.lastItem} of {pagination.totalItems} {label}
      </p>
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
  );
}

type PaginationState = ReturnType<typeof getPaginationState>;

function getPaginationState(pagedArrivals: components['schemas']['PageOfArrivalResponse'] | undefined, itemCount: number, page: number) {
  const totalItems = Number(pagedArrivals?.totalItems ?? itemCount);
  const totalPages = Math.max(Number(pagedArrivals?.totalPages ?? 1), 1);
  const currentPage = Math.min(Number(pagedArrivals?.currentPage ?? page), totalPages - 1);
  const firstItem = totalItems === 0 ? 0 : currentPage * pageSize + 1;
  const lastItem = Math.min((currentPage + 1) * pageSize, totalItems);
  const visiblePages = getVisiblePages(totalPages, currentPage);

  return { currentPage, firstItem, lastItem, totalItems, totalPages, visiblePages };
}

function DetailField({ label, value }: { readonly label: string; readonly value: React.ReactNode }) {
  return (
    <div>
      <p className="text-[12px] font-medium uppercase text-muted-foreground">{label}</p>
      <p className="mt-1 text-[14px] text-foreground">{value}</p>
    </div>
  );
}

function ArrivalDocumentPreview({
  arrivalId,
  arrivalName,
  document,
}: {
  readonly arrivalId: string;
  readonly arrivalName: string;
  readonly document: CheckInDocument;
}) {
  const [imageUrl, setImageUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);

  useEffect(() => {
    let objectUrl: string | null = null;
    let disposed = false;

    async function loadDocument() {
      setIsLoading(true);
      setHasError(false);

      try {
        const accessToken = getAccessToken();
        const response = await fetch(`${apiBaseUrl}/api/reception/arrivals/${arrivalId}/documents/${document.id}`, {
          headers: {
            ...getReceptionDeskWorkstationHeaders(),
            ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
          },
        });

        if (!response.ok) {
          throw new Error('Could not load arrival document.');
        }

        const blob = await response.blob();
        objectUrl = URL.createObjectURL(blob);
        if (!disposed) {
          setImageUrl(objectUrl);
        }
      } catch {
        if (!disposed) {
          setImageUrl(null);
          setHasError(true);
        }
      } finally {
        if (!disposed) {
          setIsLoading(false);
        }
      }
    }

    void loadDocument();

    return () => {
      disposed = true;
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };
  }, [arrivalId, document.id]);

  const label = formatCheckInDocumentType(document.documentType);

  return (
    <article className="grid gap-3 rounded-interactive border border-border bg-hover-gray/40 p-4">
      <div>
        <p className="text-[14px] font-medium text-foreground">{document.name}</p>
        <p className="text-[13px] text-muted-foreground">{label}</p>
      </div>

      <div className="overflow-hidden rounded-interactive border border-border bg-content">
        {isLoading ? <div className="grid aspect-[4/3] place-items-center text-[13px] text-muted-foreground">Loading preview...</div> : null}
        {!isLoading && hasError ? <div className="grid aspect-[4/3] place-items-center px-4 text-center text-[13px] text-muted-foreground">Could not load document preview.</div> : null}
        {!isLoading && imageUrl ? <img src={imageUrl} alt={`${label} for ${arrivalName}`} className="aspect-[4/3] w-full object-contain" /> : null}
      </div>
    </article>
  );
}

function formatSagaState(saga: VisitorPreOnboardingSaga | null) {
  if (!saga) {
    return 'Not started';
  }

  if (saga.cancelledAt) {
    return 'Cancelled';
  }

  if (saga.cancellationRequestedAt) {
    return 'Cancelling';
  }

  if (saga.expiredAt) {
    return 'Expired';
  }

  if (saga.visitorResponseStatus === 'Confirmed') {
    return 'Confirmed';
  }

  if (saga.visitorResponseStatus === 'Rejected') {
    return 'Rejected';
  }

  if (saga.isCompleteOnOurEnd) {
    return 'Awaiting confirmation';
  }

  if (saga.arrivalId) {
    return 'Sending invitation';
  }

  if (saga.credentialId || saga.qrCode) {
    return 'Registering arrival';
  }

  return 'Generating QR';
}

function formatRetryCount(value: VisitorPreOnboardingSaga['retryCount']) {
  if (value === null || value === undefined) {
    return '0';
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? String(parsed) : String(value);
}

function isSagaStillProcessing(saga: VisitorPreOnboardingSaga) {
  const cancelling = Boolean(saga.cancellationRequestedAt && !saga.cancelledAt);
  const activeRegistration = !saga.cancelledAt && !saga.expiredAt && !saga.isCompleteOnOurEnd;
  return cancelling || activeRegistration;
}

function ConfirmationBadge({ confirmed }: { readonly confirmed: boolean }) {
  return <Badge variant={confirmed ? 'success' : 'warning'}>{confirmed ? 'Confirmed' : 'Not confirmed'}</Badge>;
}

function formatConfirmation(value: boolean | null) {
  if (value === null) {
    return 'Unknown';
  }

  return value ? 'Confirmed' : 'Not confirmed';
}

function formatLocation(location: Location | null) {
  if (!location) {
    return 'No location';
  }

  return [location.site.name, location.building?.name, location.room?.name].filter(Boolean).join(' / ');
}

function getStoredIntervalState(storageKey: string): Required<StoredArrivalIntervalState> {
  const fallback = { view: 'today' as const, anchorDate: new Date().toISOString() };

  try {
    const stored = window.sessionStorage.getItem(storageKey);
    if (!stored) {
      return fallback;
    }

    const parsed = JSON.parse(stored) as StoredArrivalIntervalState;
    return {
      view: isArrivalIntervalView(parsed.view) ? parsed.view : fallback.view,
      anchorDate: parsed.anchorDate && !Number.isNaN(new Date(parsed.anchorDate).getTime()) ? parsed.anchorDate : fallback.anchorDate,
    };
  } catch {
    return fallback;
  }
}

function isArrivalIntervalView(value: unknown): value is ArrivalIntervalView {
  return value === 'today' || value === 'week';
}

function getArrivalInterval(anchorDate: string, view: ArrivalIntervalView) {
  const anchor = new Date(anchorDate);
  let intervalStart = startOfDay(anchor);
  let intervalEnd = addDays(intervalStart, 1);

  if (view === 'week') {
    intervalStart = startOfWeek(anchor);
    intervalEnd = addDays(intervalStart, 7);
  }

  return {
    start: intervalStart,
    end: intervalEnd,
    label: formatIntervalLabel(intervalStart, intervalEnd, view),
  };
}

function addInterval(date: Date, view: ArrivalIntervalView, direction: -1 | 1) {
  return addDays(date, view === 'today' ? direction : direction * 7);
}

function startOfDay(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function startOfWeek(date: Date) {
  const start = startOfDay(date);
  return addDays(start, -start.getDay());
}

function addDays(date: Date, days: number) {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
}

function formatIntervalLabel(start: Date, end: Date, view: ArrivalIntervalView) {
  if (view === 'today') {
    return start.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
  }

  return `${start.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} - ${addDays(end, -1).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })}`;
}

function getVisiblePages(totalPages: number, currentPage: number) {
  if (totalPages <= 5) {
    return Array.from({ length: totalPages }, (_, index) => index);
  }

  const pages = new Set([0, totalPages - 1, currentPage - 1, currentPage, currentPage + 1]);
  const sortedPages = [...pages]
    .filter((pageNumber) => pageNumber >= 0 && pageNumber < totalPages)
    .sort((first, second) => first - second);

  return sortedPages.flatMap((pageNumber, index) => {
    const previousPage = sortedPages[index - 1];

    if (previousPage !== undefined && pageNumber - previousPage > 1) {
      return ['ellipsis' as const, pageNumber];
    }

    return [pageNumber];
  });
}

function getArrivalName(arrival: Arrival) {
  return [arrival.firstName, arrival.lastName].filter(Boolean).join(' ') || 'Unnamed arrival';
}

function getHostName(host: components['schemas']['HostResponse']) {
  return [host.firstName, host.lastName].filter(Boolean).join(' ') || 'Unnamed host';
}

function getViewLabel(view: ArrivalIntervalView) {
  return intervalOptions.find((option) => option.value === view)?.label ?? 'interval';
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString(undefined, { weekday: 'short', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function formatNullableDateTime(value: string | null) {
  return value ? formatDateTime(value) : 'Not recorded';
}

function formatArrivalEntryType(value: ArrivalEntry['type']) {
  return value === 'CheckedIn' ? 'Checked in' : 'Checked out';
}

function formatCheckInDocumentType(value: CheckInDocument['documentType']) {
  switch (value) {
    case 'FacePicture':
      return 'Face picture';
    case 'IdentityDocumentImage':
      return 'Identity document';
    case 'GenericPage':
      return 'Document page';
    default:
      return value;
  }
}

function formatReceptionActor(actor: ReceptionActor | null) {
  if (!actor) {
    return 'Not recorded';
  }

  const label = actor.displayName || actor.identifier;
  return actor.type === 'Kiosk' ? `${label} kiosk` : label;
}

function formatStatus(value: string) {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}
