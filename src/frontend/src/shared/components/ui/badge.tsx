import * as React from 'react';

import { cn } from '@/shared/utils/cn';

type BadgeVariant = 'default' | 'secondary' | 'outline' | 'success' | 'warning' | 'error';

const variantStyles: Record<BadgeVariant, { badge: string; dot?: string }> = {
  default: {
    badge: 'border-primary/12 bg-primary/8 text-primary',
    dot: 'bg-primary',
  },
  secondary: {
    badge: 'border-[#d9e2ec] bg-[#eef3f8] text-[#365a80]',
  },
  outline: {
    badge: 'border-border bg-content text-foreground',
  },
  success: {
    badge: 'border-[#d5e8d6] bg-[#edf7ee] text-[#2f7d32]',
    dot: 'bg-[#54a857]',
  },
  warning: {
    badge: 'border-[#f0e0b8] bg-[#fbf4df] text-[#9a7413]',
    dot: 'bg-[#d5a328]',
  },
  error: {
    badge: 'border-[#efd2dc] bg-[#fdf0f5] text-[#c14a86]',
    dot: 'bg-[#dd5b9c]',
  },
};

interface BadgeProps extends React.ComponentProps<'span'> {
  variant?: BadgeVariant;
}

function Badge({ className, variant = 'default', ...props }: BadgeProps) {
  const styles = variantStyles[variant];

  return (
    <span
      data-slot="badge"
      className={cn(
        'inline-flex items-center gap-2 rounded-[10px] border px-3 py-1 text-[12px] font-semibold whitespace-nowrap transition-colors',
        styles.badge,
        className,
      )}
      {...props}
    >
      {styles.dot ? <span className={cn('size-2 rounded-full', styles.dot)} aria-hidden="true" /> : null}
      <span>{props.children}</span>
    </span>
  );
}

export { Badge, type BadgeProps, type BadgeVariant };
