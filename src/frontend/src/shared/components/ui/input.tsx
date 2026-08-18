import * as React from 'react';

import { cn } from '@/shared/utils/cn';

function Input({ className, type, ...props }: React.ComponentProps<'input'>) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(
        'h-10 w-full min-w-0 rounded-interactive border border-border bg-content px-3.5 py-2 text-[14px] font-medium shadow-xs transition-[color,box-shadow] outline-none selection:bg-primary selection:text-white file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-[13px] file:font-semibold file:text-foreground placeholder:text-faint-foreground disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50',
        'focus-visible:border-primary focus-visible:ring-[3px] focus-visible:ring-primary/20',
        'aria-invalid:border-error aria-invalid:ring-error/20',
        className,
      )}
      {...props}
    />
  );
}

export { Input };
