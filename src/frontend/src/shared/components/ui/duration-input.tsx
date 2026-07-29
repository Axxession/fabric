export type DurationInputValue = {
  days: string;
  hours: string;
  minutes: string;
};

export function DurationInput({
  value,
  onChange,
  disabled,
}: {
  readonly value: DurationInputValue;
  readonly onChange: (value: DurationInputValue) => void;
  readonly disabled?: boolean;
}) {
  function update<K extends keyof DurationInputValue>(key: K, nextValue: DurationInputValue[K]) {
    onChange({ ...value, [key]: nextValue });
  }

  return (
    <div className="grid gap-3 sm:grid-cols-3">
      <label className="grid gap-2 text-[13px] font-medium">
        <span>Days</span>
        <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" type="number" min="0" step="1" value={value.days} onChange={(event) => update('days', event.target.value)} disabled={disabled} />
      </label>
      <label className="grid gap-2 text-[13px] font-medium">
        <span>Hours</span>
        <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" type="number" min="0" max="23" step="1" value={value.hours} onChange={(event) => update('hours', event.target.value)} disabled={disabled} />
      </label>
      <label className="grid gap-2 text-[13px] font-medium">
        <span>Minutes</span>
        <input className="rounded-interactive border border-border bg-content px-3 py-2 text-[14px] outline-none transition focus:border-primary disabled:cursor-not-allowed disabled:opacity-60" type="number" min="0" max="59" step="1" value={value.minutes} onChange={(event) => update('minutes', event.target.value)} disabled={disabled} />
      </label>
    </div>
  );
}

export function getDefaultDurationInputValue(): DurationInputValue {
  return { days: '0', hours: '0', minutes: '0' };
}

export function toTimeSpan(value: DurationInputValue) {
  const days = normalizePart(value.days);
  const hours = normalizePart(value.hours, 23);
  const minutes = normalizePart(value.minutes, 59);

  if (days === 0) {
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:00`;
  }

  return `${days}.${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:00`;
}

export function fromTimeSpan(value: string): DurationInputValue {
  const match = /^(?:(\d+)\.)?(\d{2}):(\d{2}):(\d{2})$/.exec(value);
  if (!match) {
    return getDefaultDurationInputValue();
  }

  return {
    days: String(Number(match[1] ?? '0')),
    hours: String(Number(match[2])),
    minutes: String(Number(match[3])),
  };
}

function normalizePart(value: string, maxValue?: number) {
  const parsed = Number(value);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return 0;
  }

  const whole = Math.floor(parsed);
  return typeof maxValue === 'number' ? Math.min(whole, maxValue) : whole;
}
