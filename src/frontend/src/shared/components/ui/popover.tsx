'use client';

import * as React from 'react';
import { Popover as PopoverPrimitive } from '@base-ui/react';

import { cn } from '@/shared/utils/cn';

function PopoverRoot(props: PopoverPrimitive.Root.Props) {
  return <PopoverPrimitive.Root data-slot="popover-root" {...props} />;
}

function PopoverTrigger({
  className,
  ...props
}: PopoverPrimitive.Trigger.Props) {
  return (
    <PopoverPrimitive.Trigger
      data-slot="popover-trigger"
      className={cn(className)}
      {...props}
    />
  );
}

function PopoverPortal(props: PopoverPrimitive.Portal.Props) {
  return <PopoverPrimitive.Portal data-slot="popover-portal" {...props} />;
}

function PopoverPositioner({
  className,
  side = 'bottom',
  sideOffset = 6,
  align = 'start',
  alignOffset = 0,
  ...props
}: PopoverPrimitive.Positioner.Props) {
  return (
    <PopoverPrimitive.Positioner
      side={side}
      sideOffset={sideOffset}
      align={align}
      alignOffset={alignOffset}
      data-slot="popover-positioner"
      className={cn('isolate z-50', className)}
      {...props}
    />
  );
}

function PopoverPopup({
  className,
  ...props
}: PopoverPrimitive.Popup.Props) {
  return (
    <PopoverPrimitive.Popup
      data-slot="popover-popup"
      className={cn(
        'max-w-[calc(100vw-1.5rem)] origin-(--transform-origin) overflow-hidden rounded-structural border border-border bg-content text-foreground shadow-[0_18px_50px_rgba(17,24,39,0.14)]',
        className,
      )}
      {...props}
    />
  );
}

function PopoverContent({
  className,
  side = 'bottom',
  sideOffset = 6,
  align = 'start',
  alignOffset = 0,
  ...props
}: PopoverPrimitive.Popup.Props &
  Pick<PopoverPrimitive.Positioner.Props, 'side' | 'align' | 'sideOffset' | 'alignOffset'>) {
  return (
    <PopoverPortal>
      <PopoverPositioner
        side={side}
        sideOffset={sideOffset}
        align={align}
        alignOffset={alignOffset}
      >
        <PopoverPopup
          className={className}
          {...props}
        />
      </PopoverPositioner>
    </PopoverPortal>
  );
}

function PopoverArrow({
  className,
  ...props
}: PopoverPrimitive.Arrow.Props) {
  return (
    <PopoverPrimitive.Arrow
      data-slot="popover-arrow"
      className={cn('fill-content stroke-border', className)}
      {...props}
    />
  );
}

function PopoverClose({
  className,
  ...props
}: PopoverPrimitive.Close.Props) {
  return (
    <PopoverPrimitive.Close
      data-slot="popover-close"
      className={cn(className)}
      {...props}
    />
  );
}

export {
  PopoverRoot as Popover,
  PopoverTrigger,
  PopoverPortal,
  PopoverPositioner,
  PopoverPopup,
  PopoverContent,
  PopoverArrow,
  PopoverClose,
};
