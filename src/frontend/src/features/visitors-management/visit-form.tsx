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

type Organizer = components['schemas']['OrganizerResponse'];

const formSchema = z.object({
  organizer: z.string().min(1, 'Organizer is required'),
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
  readonly disabledFields?: ('organizer' | 'summary' | 'start' | 'stop' | 'location')[];
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

function getOrganizerName(organizer: Organizer) {
  return [organizer.firstName, organizer.lastName].filter(Boolean).join(' ') || organizer.email || 'Unnamed organizer';
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
    organizer: '',
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

  const organizersQuery = useQuery({
    queryKey: ['visitors-management', 'organizers', 'all'],
    queryFn: async () => {
      const { data, error } = await api.GET('/api/visitors/organizers', {
        params: { query: {} },
      });

      if (error) {
        throw new Error('Could not load organizers.');
      }

      return data;
    },
  });

  const organizers = organizersQuery.data?.items ?? [];

  return (
    <Form {...form}>
      <form className="grid gap-5" onSubmit={form.handleSubmit(onSubmit)}>
        <FormField
          control={form.control}
          name="organizer"
          render={({ field }) => {
            const selectedOrganizer = organizers.find((org) => org.id === field.value) ?? null;

            return (
              <FormItem>
                <FormLabel>Organizer</FormLabel>
                <FormControl>
                  <div ref={anchorRef}>
                    <Combobox
                      value={selectedOrganizer}
                      onValueChange={(org) => field.onChange(org?.id ?? '')}
                      items={organizers}
                      itemToStringLabel={(org) => getOrganizerName(org)}
                    >
            <ComboboxInput
              placeholder="Search organizers..."
              showClear
              disabled={disabledFields?.includes('organizer')}
            />
                      <ComboboxContent anchor={anchorRef.current}>
                        <ComboboxEmpty>No organizers found.</ComboboxEmpty>
                        <ComboboxList>
                          {(org) => (
                            <ComboboxItem key={org.id} value={org}>
                              <div>
                                <p className="font-medium text-foreground">{getOrganizerName(org)}</p>
                                {org.email ? <p className="text-[12px] text-muted-foreground">{org.email}</p> : null}
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
