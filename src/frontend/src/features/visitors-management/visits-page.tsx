import { useQuery } from '@tanstack/react-query';
import { ChevronLeft, ChevronRight, Plus, Users } from 'lucide-react';
import { Link } from '@tanstack/react-router';
import { useEffect, useMemo, useState } from 'react';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { VisitStatusBadge } from '@/shared/components/visit-status-badge';
import { cn } from '@/shared/utils/cn';

type Visit = components['schemas']['VisitResponse'];
type VisitStatus = components['schemas']['VisitStatus'];
type VisitsPageProps = {
  readonly title?: string;
  readonly description?: string;
  readonly createTo?: '/employee/visitors/new';
  readonly editTo?: '/employee/visitors/$visitId/edit';
  readonly storageKey?: string;
};

type CalendarView = 'today' | 'work-week' | 'week' | 'month';

const visitsCalendarStorageKey = 'fabric.visits.calendar';
const visitStatuses: VisitStatus[] = ['Scheduled', 'Cancelled', 'Completed'];
const viewOptions: { readonly label: string; readonly value: CalendarView }[] = [
  { label: 'Today', value: 'today' },
  { label: 'Work Week', value: 'work-week' },
  { label: 'Week', value: 'week' },
  { label: 'Month', value: 'month' },
];

type StoredCalendarState = {
  readonly view?: CalendarView;
  readonly statuses?: VisitStatus[];
  readonly status?: VisitStatus | 'all';
  readonly anchorDate?: string;
};

export default function VisitsPage(props: VisitsPageProps) {
  const title = props.title ?? 'Visits';
  const description = props.description ?? 'Plan visits, check expected arrivals, and coordinate visitor access workflows.';
  const createTo = props.createTo ?? '/employee/visitors/new';
  const editTo = props.editTo ?? '/employee/visitors/$visitId/edit';
  const storageKey = props.storageKey ?? visitsCalendarStorageKey;

  const [calendarState, setCalendarState] = useState(() => getStoredCalendarState(storageKey));
  const interval = useMemo(() => getCalendarInterval(calendarState.anchorDate, calendarState.view), [calendarState.anchorDate, calendarState.view]);
  const filteredStatuses = calendarState.statuses.length > 0 && calendarState.statuses.length < visitStatuses.length ? calendarState.statuses : [];

  useEffect(() => {
    window.sessionStorage.setItem(storageKey, JSON.stringify(calendarState));
  }, [calendarState, storageKey]);

  const visitsQuery = useQuery({
    queryKey: ['visitors-management', 'visits', interval.start.toISOString(), interval.end.toISOString(), calendarState.statuses.join(',')],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/visitors/visits', {
        params: {
          query: {
            after: interval.start.toISOString(),
            before: interval.end.toISOString(),
            page: 0,
            pageSize: 250,
            withStatus: filteredStatuses,
          },
        },
      });

      if (error) {
        throw new Error('Could not load visits.');
      }

      return data;
    },
  });

  const visits = visitsQuery.data?.items ?? [];
  const days = useMemo(() => getCalendarDays(interval.start, interval.end, calendarState.view), [interval.start, interval.end, calendarState.view]);

  function setView(view: CalendarView) {
    setCalendarState((current) => ({ ...current, view }));
  }

  function toggleStatus(status: VisitStatus) {
    setCalendarState((current) => {
      const statuses = current.statuses.includes(status) ? current.statuses.filter((currentStatus) => currentStatus !== status) : [...current.statuses, status];
      return { ...current, statuses };
    });
  }

  function jumpInterval(direction: -1 | 1) {
    setCalendarState((current) => ({
      ...current,
      anchorDate: addInterval(new Date(current.anchorDate), current.view, direction).toISOString(),
    }));
  }

  return (
    <div className="grid gap-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-[20px] font-semibold tracking-tight">{title}</h2>
          <p className="mt-2 max-w-2xl text-[14px] text-muted-foreground">{description}</p>
        </div>
        <Link
          to={createTo}
          className="inline-flex w-full items-center justify-center gap-2 rounded-interactive bg-primary px-4 py-2 text-[14px] font-semibold text-white transition hover:opacity-90 sm:w-fit"
        >
          <Plus className="size-4" aria-hidden="true" />
          Add visit
        </Link>
      </div>

      {visitsQuery.isError ? (
        <p className="rounded-interactive border border-error bg-error-background px-4 py-3 text-[14px] text-error" role="alert">
          Could not load visits.
        </p>
      ) : null}

      <section className="rounded-structural border border-border bg-content">
        <div className="border-b border-border px-4 py-4 sm:px-6">
          <div className="flex items-center justify-between">
            <h3 className="text-[16px] font-semibold tracking-tight">{interval.label}</h3>
            <p className="text-[14px] text-muted-foreground">{visitsQuery.isLoading ? 'Loading visits...' : `${visits.length} visits`}</p>
          </div>
          <div className="mt-3 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
                  aria-label={`Previous ${getViewLabel(calendarState.view)}`}
                  onClick={() => jumpInterval(-1)}
                >
                  <ChevronLeft className="size-4" aria-hidden="true" />
                </button>
                <button
                  type="button"
                  className="inline-flex size-10 items-center justify-center rounded-interactive border border-border text-muted-foreground transition hover:bg-hover-blue hover:text-foreground"
                  aria-label={`Next ${getViewLabel(calendarState.view)}`}
                  onClick={() => jumpInterval(1)}
                >
                  <ChevronRight className="size-4" aria-hidden="true" />
                </button>
              </div>
              <div className="flex flex-wrap items-center gap-1 rounded-interactive border border-border bg-hover-gray p-1" aria-label="Calendar view">
                {viewOptions.map((option) => (
                  <button
                    key={option.value}
                    type="button"
                    className={cn(
                      'rounded-interactive px-3 py-2 text-[13px] font-semibold text-muted-foreground transition hover:text-foreground',
                      calendarState.view === option.value && 'bg-content text-foreground shadow-sm',
                    )}
                    aria-pressed={calendarState.view === option.value}
                    onClick={() => setView(option.value)}
                  >
                    {option.label}
                  </button>
                ))}
              </div>
            </div>
            <fieldset className="flex flex-wrap items-center gap-2 text-[13px] font-semibold text-muted-foreground" aria-label="Status filter">
              <legend className="mr-1">Status</legend>
              {visitStatuses.map((status) => (
                <label
                  key={status}
                  className={cn(
                    'inline-flex cursor-pointer items-center gap-2 rounded-interactive border border-border bg-content px-3 py-2 transition hover:bg-hover-blue hover:text-foreground',
                    calendarState.statuses.includes(status) && 'border-primary/40 bg-hover-blue text-foreground',
                  )}
                >
                  <input
                    type="checkbox"
                    className="size-4 accent-primary"
                    checked={calendarState.statuses.includes(status)}
                    onChange={() => toggleStatus(status)}
                  />
                  {status}
                </label>
              ))}
              <span className="text-[12px] font-medium text-muted-foreground">{calendarState.statuses.length === 0 ? 'All statuses' : 'Multiple allowed'}</span>
            </fieldset>
          </div>
        </div>

        <CalendarGrid days={days} visits={visits} view={calendarState.view} isLoading={visitsQuery.isLoading} editTo={editTo} />
      </section>
    </div>
  );
}

function CalendarGrid({ days, visits, view, isLoading, editTo }: { readonly days: Date[]; readonly visits: Visit[]; readonly view: CalendarView; readonly isLoading: boolean; readonly editTo: VisitsPageProps['editTo'] }) {
  const columnsClassName = view === 'today' ? 'grid-cols-1' : view === 'work-week' ? 'lg:grid-cols-5' : 'lg:grid-cols-7';
  const desktopColumns = view === 'today' ? 1 : view === 'work-week' ? 5 : 7;

  return (
    <div className={cn('grid overflow-hidden rounded-b-structural', columnsClassName)}>
      {days.map((day, index) => {
        const dayVisits = visits.filter((visit) => visit.start && isSameDay(new Date(visit.start), day));
        const isCurrentDay = isToday(day);
        const isDesktopColumnEnd = (index + 1) % desktopColumns === 0;
        const isLastRow = index >= days.length - desktopColumns;

        return (
          <article
            key={day.toISOString()}
            className={cn(
              'min-h-56 border-b border-border lg:border-r',
              view === 'month' && 'min-h-52',
              isDesktopColumnEnd && 'lg:border-r-0',
              isLastRow && 'lg:border-b-0',
              index === days.length - 1 && 'border-b-0',
              isCurrentDay && 'bg-hover-blue/40',
            )}
          >
            <header className={cn('border-b border-border px-4 py-3', isCurrentDay && 'border-primary/30')}>
              <div className="flex items-center justify-between gap-2">
                <div>
                  <p className={cn('text-[12px] font-semibold uppercase text-muted-foreground', isCurrentDay && 'text-primary')}>{formatWeekday(day)}</p>
                  <h4 className={cn('text-[15px] font-semibold text-foreground', isCurrentDay && 'text-primary')}>{formatDayHeading(day)}</h4>
                </div>
                {isCurrentDay ? <span className="rounded-status bg-primary px-2 py-0.5 text-[11px] font-semibold uppercase text-white">Today</span> : null}
              </div>
            </header>
            <div className="grid gap-2 p-3">
              {isLoading ? <p className="px-1 py-2 text-[14px] text-muted-foreground">Loading...</p> : null}
              {!isLoading && dayVisits.length === 0 ? <p className="px-1 py-2 text-[13px] text-muted-foreground">No visits</p> : null}
              {dayVisits.map((visit) => (
                <VisitCard key={visit.id ?? `${visit.summary}-${visit.start}`} visit={visit} editTo={editTo} />
              ))}
            </div>
          </article>
        );
      })}
    </div>
  );
}

function VisitCard({ visit, editTo }: { readonly visit: Visit; readonly editTo: VisitsPageProps['editTo'] }) {
  const participantCount = visit.invitations?.length ?? 0;

  return (
      <Link
        to={editTo ?? '/employee/visitors/$visitId/edit'}
        params={{ visitId: visit.id ?? '' }}
        className="grid gap-2 rounded-interactive border border-border bg-content p-3 shadow-sm transition hover:border-primary/40 hover:shadow-md"
      >
      <div className="flex items-start justify-between gap-3">
        <h5 className="text-[14px] font-semibold leading-5 text-foreground">{visit.summary || 'Untitled visit'}</h5>
        <VisitStatusBadge status={visit.status} />
      </div>
      <p className="text-[13px] font-medium text-muted-foreground">
        {formatTime(visit.start)}-{formatTime(visit.stop)}
      </p>
      <p className="inline-flex items-center gap-1.5 text-[13px] text-muted-foreground">
        <Users className="size-3.5" aria-hidden="true" />
        {participantCount} {participantCount === 1 ? 'participant' : 'participants'}
      </p>
    </Link>
  );
}

function getStoredCalendarState(storageKey: string): Required<StoredCalendarState> {
  const fallback = { view: 'work-week' as const, statuses: [], status: 'all' as const, anchorDate: new Date().toISOString() };

  try {
    const stored = window.sessionStorage.getItem(storageKey);
    if (!stored) {
      return fallback;
    }

    const parsed = JSON.parse(stored) as StoredCalendarState;
    return {
      view: isCalendarView(parsed.view) ? parsed.view : fallback.view,
      statuses: getStoredStatuses(parsed),
      status: fallback.status,
      anchorDate: parsed.anchorDate && !Number.isNaN(new Date(parsed.anchorDate).getTime()) ? parsed.anchorDate : fallback.anchorDate,
    };
  } catch {
    return fallback;
  }
}

function isCalendarView(value: unknown): value is CalendarView {
  return value === 'today' || value === 'work-week' || value === 'week' || value === 'month';
}

function isVisitStatusFilter(value: unknown): value is VisitStatus | 'all' {
  return value === 'all' || visitStatuses.includes(value as VisitStatus);
}

function getStoredStatuses(parsed: StoredCalendarState) {
  if (Array.isArray(parsed.statuses)) {
    return parsed.statuses.filter((status): status is VisitStatus => visitStatuses.includes(status));
  }

  return isVisitStatusFilter(parsed.status) && parsed.status !== 'all' ? [parsed.status] : [];
}

function getCalendarInterval(anchorDate: string, view: CalendarView) {
  const anchor = new Date(anchorDate);
  const start = startOfDay(anchor);
  let intervalStart = start;
  let intervalEnd = addDays(start, 1);

  if (view === 'work-week') {
    intervalStart = startOfWorkWeek(anchor);
    intervalEnd = addDays(intervalStart, 5);
  }

  if (view === 'week') {
    intervalStart = startOfWeek(anchor);
    intervalEnd = addDays(intervalStart, 7);
  }

  if (view === 'month') {
    intervalStart = new Date(anchor.getFullYear(), anchor.getMonth(), 1);
    intervalEnd = new Date(anchor.getFullYear(), anchor.getMonth() + 1, 1);
  }

  return {
    start: intervalStart,
    end: intervalEnd,
    label: formatIntervalLabel(intervalStart, intervalEnd, view),
  };
}

function getCalendarDays(start: Date, end: Date, view: CalendarView) {
  const days: Date[] = [];
  const monthGridStart = view === 'month' ? startOfWeek(start) : start;
  const monthGridEnd = view === 'month' ? addDays(startOfWeek(addDays(end, -1)), 7) : end;

  for (let cursor = monthGridStart; cursor < monthGridEnd; cursor = addDays(cursor, 1)) {
    days.push(cursor);
  }

  return days;
}

function addInterval(date: Date, view: CalendarView, direction: -1 | 1) {
  if (view === 'month') {
    return new Date(date.getFullYear(), date.getMonth() + direction, date.getDate());
  }

  if (view === 'today') {
    return addDays(date, direction);
  }

  return addDays(date, direction * 7);
}

function startOfDay(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function startOfWeek(date: Date) {
  const start = startOfDay(date);
  return addDays(start, -start.getDay());
}

function startOfWorkWeek(date: Date) {
  const start = startOfDay(date);
  const day = start.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  return addDays(start, diff);
}

function addDays(date: Date, days: number) {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
}

function isSameDay(first: Date, second: Date) {
  return first.getFullYear() === second.getFullYear() && first.getMonth() === second.getMonth() && first.getDate() === second.getDate();
}

function isToday(date: Date) {
  return isSameDay(date, new Date());
}

function formatIntervalLabel(start: Date, end: Date, view: CalendarView) {
  if (view === 'today') {
    return start.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
  }

  if (view === 'month') {
    return start.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
  }

  return `${start.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} - ${addDays(end, -1).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })}`;
}

function getViewLabel(view: CalendarView) {
  return viewOptions.find((option) => option.value === view)?.label ?? 'interval';
}

function formatWeekday(date: Date) {
  return date.toLocaleDateString(undefined, { weekday: 'short' });
}

function formatDayHeading(date: Date) {
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

function formatTime(value?: string | null) {
  if (!value) {
    return '--:--';
  }

  return new Date(value).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
}
