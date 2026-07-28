import { type ReactNode, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery } from '@tanstack/react-query';
import { format, parseISO } from 'date-fns';
import { CalendarIcon } from 'lucide-react';
import { z } from 'zod';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';
import { Button } from '@/shared/components/ui/button';
import { Calendar } from '@/shared/components/ui/calendar';
import {
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
} from '@/shared/components/ui/combobox';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/shared/components/ui/form';
import { Input } from '@/shared/components/ui/input';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/shared/components/ui/popover';
import { LocationSelector } from '@/shared/components/location-selector';

type Host = components['schemas']['HostResponse'];

const formSchema = z.object({
  hostEmployeeId: z.string().min(1, 'Host is required'),
  summary: z.string().min(1, 'Summary is required'),
  start: z.string().min(1, 'Start time is required'),
  stop: z.string().min(1, 'End time is required'),
  locationId: z.string().nullable(),
});

export type VisitFormValues = z.infer<typeof formSchema>;

type VisitFormProps = {
  readonly initialValues: VisitFormValues;
  readonly isSubmitting: boolean;
  readonly submitLabel: string;
  readonly onSubmit: (values: VisitFormValues) => void;
  readonly disabledFields?: ('host' | 'summary' | 'start' | 'stop' | 'location')[];
  readonly disableSubmit?: boolean;
  readonly footerLeft?: ReactNode;
};

function getNextHour() {
  const now = new Date();
  now.setHours(now.getHours() + 1, 0, 0, 0);
  return now;
}

function toDatetimeLocal(date: Date) {
  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60_000);
  return local.toISOString().slice(0, 16);
}

function getHostName(host: Host) {
  return [host.firstName, host.lastName].filter(Boolean).join(' ') || host.email || 'Unnamed host';
}

function splitDatetime(datetime: string): { date: string; time: string } {
  const [date = '', time = ''] = datetime.split('T');
  return { date, time };
}

function combineDatetime(date: string, time: string): string {
  return `${date}T${time}`;
}

export function getDefaultVisitFormValues(): VisitFormValues {
  const start = getNextHour();
  const stop = new Date(start.getTime() + 60 * 60_000);
  return {
    hostEmployeeId: '',
    summary: '',
    start: toDatetimeLocal(start),
    stop: toDatetimeLocal(stop),
    locationId: null,
  };
}

export function VisitForm({ initialValues, isSubmitting, submitLabel, onSubmit, disabledFields, disableSubmit, footerLeft }: VisitFormProps) {
  const anchorRef = useRef<HTMLDivElement | null>(null);

  const form = useForm<VisitFormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: initialValues,
  });

  const hostsQuery = useQuery({
    queryKey: ['visitors-management', 'hosts', 'all'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/visitors/hosts', {
        params: { query: {} },
      });

      if (error) {
        throw new Error('Could not load hosts.');
      }

      return data;
    },
  });

  const hosts = hostsQuery.data?.items ?? [];

  return (
    <Form {...form}>
      <form className="grid gap-5" onSubmit={form.handleSubmit(onSubmit)}>
        <FormField
          control={form.control}
          name="hostEmployeeId"
          render={({ field }) => {
            const selectedHost = hosts.find((host) => host.employeeId === field.value) ?? null;

            return (
              <FormItem>
                <FormLabel>Host</FormLabel>
                <FormControl>
                  <div ref={anchorRef}>
                    <Combobox
                      value={selectedHost}
                      onValueChange={(host) => field.onChange(host?.employeeId ?? '')}
                      items={hosts}
                      itemToStringLabel={(host) => getHostName(host)}
                    >
            <ComboboxInput
              placeholder="Search hosts..."
              showClear
              disabled={disabledFields?.includes('host')}
            />
                      <ComboboxContent anchor={anchorRef.current}>
                        <ComboboxEmpty>No hosts found.</ComboboxEmpty>
                        <ComboboxList>
                          {(host) => (
                            <ComboboxItem key={host.employeeId} value={host}>
                              <div>
                                <p className="font-medium text-foreground">{getHostName(host)}</p>
                                {host.email ? <p className="text-[12px] text-muted-foreground">{host.email}</p> : null}
                              </div>
                            </ComboboxItem>
                          )}
                        </ComboboxList>
                      </ComboboxContent>
                    </Combobox>
                  </div>
                </FormControl>
                <FormMessage />
              </FormItem>
            );
          }}
        />

        <FormField
          control={form.control}
          name="summary"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Summary</FormLabel>
              <FormControl>
                <Input {...field} disabled={disabledFields?.includes('summary')} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="locationId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Location</FormLabel>
              <FormControl>
                <LocationSelector
                  value={field.value}
                  onChange={field.onChange}
                  level="Room"
                  disabled={disabledFields?.includes('location')}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid gap-5 md:grid-cols-2">
          <FormField
            control={form.control}
            name="start"
            render={({ field }) => {
              const { date, time } = splitDatetime(field.value);
              const selectedDate = date ? parseISO(date) : undefined;
              const [open, setOpen] = useState(false);
              const disabled = disabledFields?.includes('start');

              return (
                <FormItem>
                  <FormLabel>Start</FormLabel>
                  <div className="flex flex-col gap-2 sm:flex-row">
                    <div className="flex-[7]">
                      <Popover open={open} onOpenChange={setOpen}>
                        <PopoverTrigger render={<Button variant="outline" className="w-full justify-start text-left font-normal" disabled={disabled} />}>
                          <CalendarIcon className="size-4" />
                          {selectedDate ? format(selectedDate, 'MMM d, yyyy') : <span className="text-muted-foreground">Pick date</span>}
                        </PopoverTrigger>
                        <PopoverContent align="start">
                          <Calendar
                            mode="single"
                            selected={selectedDate}
                            onSelect={(nextDate) => {
                              if (nextDate) {
                                field.onChange(combineDatetime(format(nextDate, 'yyyy-MM-dd'), time));
                                setOpen(false);
                              }
                            }}
                            autoFocus
                          />
                        </PopoverContent>
                      </Popover>
                    </div>
                    <Input
                      type="time"
                      value={time}
                      onChange={(e) => field.onChange(combineDatetime(date, e.target.value))}
                      className="flex-[3]"
                      disabled={disabled}
                    />
                  </div>
                  <FormMessage />
                </FormItem>
              );
            }}
          />

          <FormField
            control={form.control}
            name="stop"
            render={({ field }) => {
              const { date, time } = splitDatetime(field.value);
              const selectedDate = date ? parseISO(date) : undefined;
              const [open, setOpen] = useState(false);
              const disabled = disabledFields?.includes('stop');

              return (
                <FormItem>
                  <FormLabel>End</FormLabel>
                  <div className="flex flex-col gap-2 sm:flex-row">
                    <div className="flex-[7]">
                      <Popover open={open} onOpenChange={setOpen}>
                        <PopoverTrigger render={<Button variant="outline" className="w-full justify-start text-left font-normal" disabled={disabled} />}>
                          <CalendarIcon className="size-4" />
                          {selectedDate ? format(selectedDate, 'MMM d, yyyy') : <span className="text-muted-foreground">Pick date</span>}
                        </PopoverTrigger>
                        <PopoverContent align="start">
                          <Calendar
                            mode="single"
                            selected={selectedDate}
                            onSelect={(nextDate) => {
                              if (nextDate) {
                                field.onChange(combineDatetime(format(nextDate, 'yyyy-MM-dd'), time));
                                setOpen(false);
                              }
                            }}
                            autoFocus
                          />
                        </PopoverContent>
                      </Popover>
                    </div>
                    <Input
                      type="time"
                      value={time}
                      onChange={(e) => field.onChange(combineDatetime(date, e.target.value))}
                      className="flex-[3]"
                      disabled={disabled}
                    />
                  </div>
                  <FormMessage />
                </FormItem>
              );
            }}
          />
        </div>

        <div className={footerLeft ? 'flex flex-col-reverse gap-2 sm:flex-row sm:items-center sm:justify-between' : 'flex justify-end'}>
          {footerLeft ? <div className="[&>*]:w-full sm:[&>*]:w-auto">{footerLeft}</div> : null}
          <Button type="submit" className="w-full sm:w-auto" disabled={isSubmitting || disableSubmit}>
            {isSubmitting ? 'Saving...' : submitLabel}
          </Button>
        </div>
      </form>
    </Form>
  );
}
