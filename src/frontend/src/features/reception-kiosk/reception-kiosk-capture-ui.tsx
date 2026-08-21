import { Link } from '@tanstack/react-router';
import { ArrowLeft, Camera, RefreshCw } from 'lucide-react';
import type { ReactNode } from 'react';

import { Button, buttonVariants } from '@/shared/components/ui/button';

import type { ReceptionKioskCameraOrientation, ReceptionKioskCapturedImage, ReceptionKioskOnboardingStep } from './reception-kiosk-onboarding';
import type { VideoInputDevice } from './reception-kiosk-camera';

export function ReceptionKioskCaptureShell({
  backTo,
  children,
  progressLabel,
  title,
  description,
}: {
  readonly backTo: '/reception-kiosk' | '/reception-kiosk/session/terminal';
  readonly children: ReactNode;
  readonly progressLabel: string;
  readonly title: string;
  readonly description: string;
}) {
  return (
    <section className="w-full rounded-[2rem] border border-border bg-content p-5 shadow-sm sm:p-8 lg:p-10">
      <Link to={backTo} className="inline-flex items-center gap-2 text-[16px] font-medium text-muted-foreground hover:text-foreground">
        <ArrowLeft className="size-5" aria-hidden="true" />
        Back
      </Link>

      <div className="mt-6 text-center">
        <div className="mx-auto flex size-20 items-center justify-center rounded-full bg-hover-blue text-primary">
          <Camera className="size-10" aria-hidden="true" />
        </div>
        <p className="mt-5 text-[13px] font-semibold uppercase tracking-[0.24em] text-muted-foreground">{progressLabel}</p>
        <h2 className="mt-3 text-[34px] font-semibold tracking-tight sm:text-[48px]">{title}</h2>
        <p className="mx-auto mt-3 max-w-3xl text-[18px] leading-8 text-muted-foreground">{description}</p>
      </div>

      <div className="mt-8 grid gap-6">{children}</div>
    </section>
  );
}

export function ReceptionKioskCameraSettings({
  availableDevices,
  orientation,
  selectedDeviceId,
  setOrientation,
  setSelectedDeviceId,
}: {
  readonly availableDevices: readonly VideoInputDevice[];
  readonly orientation: ReceptionKioskCameraOrientation;
  readonly selectedDeviceId: string | null;
  readonly setOrientation: (value: ReceptionKioskCameraOrientation) => void;
  readonly setSelectedDeviceId: (value: string) => void;
}) {
  return (
    <div className="grid gap-4 rounded-[1.5rem] border border-border bg-hover-gray/50 p-4 lg:grid-cols-2">
      <div className="grid gap-2">
        <label className="text-[14px] font-medium" htmlFor="reception-kiosk-camera-orientation">Camera orientation</label>
        <select
          id="reception-kiosk-camera-orientation"
          className="h-12 rounded-[1rem] border border-border bg-content px-3 text-[15px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20"
          value={orientation}
          onChange={(event) => setOrientation(event.target.value as ReceptionKioskCameraOrientation)}
        >
          <option value="user">Front / user</option>
          <option value="environment">Rear / environment</option>
        </select>
      </div>

      {availableDevices.length > 1 ? (
        <div className="grid gap-2">
          <label className="text-[14px] font-medium" htmlFor="reception-kiosk-camera-device">Camera</label>
          <select
            id="reception-kiosk-camera-device"
            className="h-12 rounded-[1rem] border border-border bg-content px-3 text-[15px] outline-none transition focus:border-primary focus:ring-[3px] focus:ring-primary/20"
            value={selectedDeviceId ?? ''}
            onChange={(event) => setSelectedDeviceId(event.target.value)}
          >
            <option value="">Use orientation default</option>
            {availableDevices.map((device) => (
              <option key={device.deviceId} value={device.deviceId}>{device.label}</option>
            ))}
          </select>
        </div>
      ) : null}
    </div>
  );
}

export function ReceptionKioskPreviewStage({
  children,
  error,
  isStarting,
  status,
}: {
  readonly children: ReactNode;
  readonly error: string | null;
  readonly isStarting: boolean;
  readonly status: string;
}) {
  return (
    <>
      <div className="relative mx-auto w-full max-w-4xl overflow-hidden rounded-[2rem] border border-border bg-black shadow-sm">
        <div className="aspect-[3/4] w-full sm:aspect-[4/3]">{children}</div>
      </div>

      <div className="mx-auto grid w-full max-w-3xl gap-3 text-center">
        <p className="rounded-[1rem] bg-hover-blue px-4 py-3 text-[17px] font-medium text-primary">{isStarting ? 'Starting camera...' : status}</p>
        {error ? <p className="rounded-[1rem] bg-error-background px-4 py-3 text-[16px] font-medium text-error">{error}</p> : null}
      </div>
    </>
  );
}

export function ReceptionKioskCaptureReview({
  capture,
  title,
  onConfirm,
  onRetake,
  confirmLabel,
}: {
  readonly capture: ReceptionKioskCapturedImage;
  readonly title: string;
  readonly onConfirm: () => void;
  readonly onRetake: () => void;
  readonly confirmLabel: string;
}) {
  return (
    <div className="grid gap-5">
      <div className="overflow-hidden rounded-[2rem] border border-border bg-black">
        <img src={`data:${capture.mimeType};base64,${capture.base64}`} alt={title} className="aspect-[3/4] w-full object-contain sm:aspect-[4/3]" />
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <Button type="button" variant="outline" className="h-14 rounded-[1rem] text-[18px]" onClick={onRetake}>
          <RefreshCw className="size-4" aria-hidden="true" />
          Retake
        </Button>
        <Button type="button" className="h-14 rounded-[1rem] text-[18px]" onClick={onConfirm}>{confirmLabel}</Button>
      </div>
    </div>
  );
}

export function ReceptionKioskFooterActions({
  children,
}: {
  readonly children: ReactNode;
}) {
  return <div className="mx-auto grid w-full max-w-3xl gap-3">{children}</div>;
}

export function ReceptionKioskCancelLink({
  to,
  label,
  onClick,
}: {
  readonly to: '/reception-kiosk' | '/reception-kiosk/session/terminal';
  readonly label: string;
  readonly onClick?: () => void;
}) {
  return <Link to={to} onClick={onClick} className={buttonVariants({ variant: 'outline', size: 'lg', className: 'h-14 rounded-[1rem] text-[18px]' })}>{label}</Link>;
}

export function getStepProgressLabel(step: ReceptionKioskOnboardingStep, totalSteps: number, stepIndex: number): string {
  return totalSteps === 0 ? 'Self-onboarding' : `Step ${stepIndex + 1} of ${totalSteps} • ${step === 'face' ? 'Face photo' : 'Document photo'}`;
}
