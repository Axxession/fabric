import { Navigate, useNavigate } from '@tanstack/react-router';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef, useState, type MutableRefObject } from 'react';

import { Button } from '@/shared/components/ui/button';

import { useReceptionKioskCamera } from './reception-kiosk-camera';
import { ReceptionKioskCameraSettings, ReceptionKioskCancelLink, ReceptionKioskCaptureReview, ReceptionKioskCaptureShell, ReceptionKioskFooterActions, ReceptionKioskPreviewStage } from './reception-kiosk-capture-ui';
import { advanceReceptionKioskSession, stopReceptionKioskSession, storeReceptionKioskSessionIdentityDocument } from './reception-kiosk-api';
import { receptionKioskCurrentSessionQueryKey, useReceptionKioskCurrentSession } from './reception-kiosk-session';
import { saveReceptionKioskResult } from './reception-kiosk-result';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

const cardAspectRatio = 1.586;
const sampleWidth = 240;
const sampleHeight = 180;

export default function ReceptionKioskSessionDocumentPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const sessionQuery = useReceptionKioskCurrentSession();
  const [capture, setCapture] = useState<import('./reception-kiosk-onboarding').ReceptionKioskCapturedImage | null>(null);
  const [status, setStatus] = useState('Place the identity document inside the frame.');
  const [isDocumentReady, setIsDocumentReady] = useState(false);
  const [countdown, setCountdown] = useState<number | null>(null);
  const sampleCanvasRef = useRef<HTMLCanvasElement | null>(null);
  const previousSampleRef = useRef<Uint8ClampedArray | null>(null);
  const { availableDevices, captureStillFrame, error, isStarting, isStreamReady, orientation, selectedDeviceId, setOrientation, setSelectedDeviceId, videoRef } = useReceptionKioskCamera('document');

  const continueMutation = useMutation({
    mutationFn: async (imageBase64: string) => {
      await storeReceptionKioskSessionIdentityDocument(imageBase64);
      return await advanceReceptionKioskSession();
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: receptionKioskCurrentSessionQueryKey });
      await navigate({ to: '/reception-kiosk/session' });
    },
    onError: async (error) => {
      saveReceptionKioskResult('action-failed', error instanceof Error ? error.message : undefined);
      await navigate({ to: '/reception-kiosk/failed' });
    },
  });

  useEffect(() => {
    if (!isStreamReady || !videoRef.current || capture) return;
    let frameId = 0;
    let lastProcessedAt = 0;
    const analyzeFrame = (now: number) => {
      if (!videoRef.current || capture) return;
      if (now - lastProcessedAt >= 180) {
        lastProcessedAt = now;
        const result = evaluateDocumentFrame(videoRef.current, sampleCanvasRef, previousSampleRef);
        setIsDocumentReady(result.ready);
        setStatus(result.message);
      }
      frameId = window.requestAnimationFrame(analyzeFrame);
    };
    frameId = window.requestAnimationFrame(analyzeFrame);
    return () => window.cancelAnimationFrame(frameId);
  }, [capture, isStreamReady, videoRef]);

  useEffect(() => {
    if (!isDocumentReady || capture) {
      setCountdown(null);
      return;
    }
    setCountdown((current) => current ?? 3);
  }, [capture, isDocumentReady]);

  useEffect(() => {
    if (countdown === null || capture || !isDocumentReady) return;
    if (countdown === 0) {
      const videoElement = videoRef.current;
      if (!videoElement) return;
      const stillFrame = captureStillFrame({ sourceRect: getDocumentFrameRect(videoElement.videoWidth, videoElement.videoHeight) });
      if (stillFrame) setCapture(stillFrame);
      return;
    }
    const timeoutId = window.setTimeout(() => setCountdown((current) => current === null ? null : current - 1), 1000);
    return () => window.clearTimeout(timeoutId);
  }, [capture, captureStillFrame, countdown, isDocumentReady]);

  if (!hasReceptionKioskSettings()) return <Navigate to="/reception-kiosk/setup" replace />;
  if (sessionQuery.isLoading) return null;
  if (sessionQuery.isError || !sessionQuery.data) return <Navigate to="/reception-kiosk" replace />;
  if (sessionQuery.data.status !== 'Active' || sessionQuery.data.currentStep !== 'IdentityDocumentCheck') return <Navigate to="/reception-kiosk/session" replace />;

  async function handleConfirm() {
    if (!capture) return;
    continueMutation.mutate(capture.base64);
  }

  async function handleCancel() {
    await stopReceptionKioskSession('HomeRedirect', 'User returned home.');
    await queryClient.invalidateQueries({ queryKey: receptionKioskCurrentSessionQueryKey });
    await navigate({ to: '/reception-kiosk/session/terminal' });
  }

  const statusLabel = capture
    ? 'Document picture ready. Review before continuing.'
    : countdown !== null
      ? `Taking photo in ${countdown}...`
      : status;

  return (
    <ReceptionKioskCaptureShell backTo="/reception-kiosk" progressLabel="Identity document" title="Scan identity document" description="Hold the identity document inside the frame. We will capture a picture automatically once it is stable.">
      <ReceptionKioskCameraSettings availableDevices={availableDevices} orientation={orientation} selectedDeviceId={selectedDeviceId} setOrientation={setOrientation} setSelectedDeviceId={setSelectedDeviceId} />
      {capture ? (
        <ReceptionKioskCaptureReview capture={capture} title="Identity document preview" onConfirm={() => void handleConfirm()} onRetake={() => { setCapture(null); setCountdown(null); setStatus('Place the identity document inside the frame.'); }} confirmLabel={continueMutation.isPending ? 'Continuing...' : 'Continue'} />
      ) : (
        <>
          <ReceptionKioskPreviewStage error={error} isStarting={isStarting} status={statusLabel}>
            <div className="relative h-full w-full">
              <video ref={videoRef} className="h-full w-full object-cover" muted playsInline aria-label="Document camera preview" />
              <div className="pointer-events-none absolute inset-0 bg-black/20" aria-hidden="true" />
              <div className="pointer-events-none absolute inset-0 flex items-center justify-center px-[8%]" aria-hidden="true">
                <div className="w-full max-w-[82%] rounded-[1.5rem] border-4 border-white/85 shadow-[0_0_0_9999px_rgba(0,0,0,0.3)] aspect-[1.586/1]" />
              </div>
              {countdown !== null ? <div className="absolute inset-0 flex items-center justify-center text-[96px] font-semibold text-white drop-shadow-lg">{countdown}</div> : null}
            </div>
          </ReceptionKioskPreviewStage>
          <ReceptionKioskFooterActions>
            <Button type="button" variant="outline" className="h-14 rounded-[1rem] text-[18px]" onClick={() => { const videoElement = videoRef.current; if (videoElement) setCapture(captureStillFrame({ sourceRect: getDocumentFrameRect(videoElement.videoWidth, videoElement.videoHeight) })); }}>Capture now</Button>
            <ReceptionKioskCancelLink to="/reception-kiosk" label="Cancel session" onClick={() => void handleCancel()} />
          </ReceptionKioskFooterActions>
        </>
      )}
    </ReceptionKioskCaptureShell>
  );
}

function evaluateDocumentFrame(videoElement: HTMLVideoElement, sampleCanvasRef: MutableRefObject<HTMLCanvasElement | null>, previousSampleRef: MutableRefObject<Uint8ClampedArray | null>) {
  const sampleCanvas = sampleCanvasRef.current ?? document.createElement('canvas');
  sampleCanvasRef.current = sampleCanvas;
  sampleCanvas.width = sampleWidth;
  sampleCanvas.height = sampleHeight;
  const context = sampleCanvas.getContext('2d', { willReadFrequently: true });
  if (!context) return { ready: false, message: 'Place the identity document inside the frame.' };
  context.drawImage(videoElement, 0, 0, sampleCanvas.width, sampleCanvas.height);
  const frameRect = getDocumentFrameRect(sampleCanvas.width, sampleCanvas.height);
  const imageData = context.getImageData(frameRect.x, frameRect.y, frameRect.width, frameRect.height);
  const grayscale = toGrayscale(imageData.data);
  const variance = calculateVariance(grayscale);
  const edgeDensity = calculateEdgeDensity(grayscale, frameRect.width, frameRect.height);
  const motion = calculateMotion(previousSampleRef.current, grayscale);
  const borderStrength = calculateBorderStrength(grayscale, frameRect.width, frameRect.height);
  previousSampleRef.current = grayscale;
  if (variance < 160 || edgeDensity < 7) return { ready: false, message: 'Move the document closer and fill the frame.' };
  if (borderStrength < 16) return { ready: false, message: 'Keep all card edges inside the frame.' };
  if (motion > 9) return { ready: false, message: 'Hold the document still and avoid glare.' };
  return { ready: true, message: 'Hold still. We are preparing the capture.' };
}

function getDocumentFrameRect(width: number, height: number) {
  if (width === 0 || height === 0) return { x: 0, y: 0, width: 0, height: 0 };
  const maxWidth = width * 0.82;
  const maxHeight = height * 0.56;
  const widthFromHeight = maxHeight * cardAspectRatio;
  const frameWidth = Math.round(Math.min(maxWidth, widthFromHeight));
  const frameHeight = Math.round(frameWidth / cardAspectRatio);
  return { x: Math.round((width - frameWidth) / 2), y: Math.round((height - frameHeight) / 2), width: frameWidth, height: frameHeight };
}

function toGrayscale(data: Uint8ClampedArray) {
  const grayscale = new Uint8ClampedArray(data.length / 4);
  for (let index = 0; index < data.length; index += 4) grayscale[index / 4] = Math.round((data[index] + data[index + 1] + data[index + 2]) / 3);
  return grayscale;
}

function calculateVariance(grayscale: Uint8ClampedArray) {
  let total = 0;
  grayscale.forEach((value) => { total += value; });
  const mean = total / grayscale.length;
  let squaredDiff = 0;
  grayscale.forEach((value) => { squaredDiff += (value - mean) * (value - mean); });
  return squaredDiff / grayscale.length;
}

function calculateEdgeDensity(grayscale: Uint8ClampedArray, width: number, height: number) {
  let edges = 0;
  for (let y = 1; y < height - 1; y++) {
    for (let x = 1; x < width - 1; x++) {
      const index = y * width + x;
      const dx = Math.abs(grayscale[index - 1] - grayscale[index + 1]);
      const dy = Math.abs(grayscale[index - width] - grayscale[index + width]);
      if (dx + dy > 70) edges++;
    }
  }
  return (edges / (width * height)) * 100;
}

function calculateMotion(previous: Uint8ClampedArray | null, current: Uint8ClampedArray) {
  if (!previous || previous.length !== current.length) return 0;
  let total = 0;
  for (let index = 0; index < current.length; index++) total += Math.abs(current[index] - previous[index]);
  return total / current.length;
}

function calculateBorderStrength(grayscale: Uint8ClampedArray, width: number, height: number) {
  const insetX = Math.max(1, Math.round(width * 0.08));
  const insetY = Math.max(1, Math.round(height * 0.08));
  let score = 0;
  for (let x = insetX; x < width - insetX; x++) {
    score += Math.abs(grayscale[insetY * width + x] - grayscale[(insetY + 2) * width + x]);
    score += Math.abs(grayscale[(height - insetY - 1) * width + x] - grayscale[(height - insetY - 3) * width + x]);
  }
  for (let y = insetY; y < height - insetY; y++) {
    score += Math.abs(grayscale[y * width + insetX] - grayscale[y * width + insetX + 2]);
    score += Math.abs(grayscale[y * width + (width - insetX - 1)] - grayscale[y * width + (width - insetX - 3)]);
  }
  return score / (2 * (width + height));
}
