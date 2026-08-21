import { FaceDetector, FilesetResolver } from '@mediapipe/tasks-vision';
import { Navigate, useNavigate } from '@tanstack/react-router';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef, useState } from 'react';

import { Button } from '@/shared/components/ui/button';

import { useReceptionKioskCamera } from './reception-kiosk-camera';
import { ReceptionKioskCameraSettings, ReceptionKioskCancelLink, ReceptionKioskCaptureReview, ReceptionKioskCaptureShell, ReceptionKioskFooterActions, ReceptionKioskPreviewStage } from './reception-kiosk-capture-ui';
import { advanceReceptionKioskSession, stopReceptionKioskSession, storeReceptionKioskSessionFacePicture } from './reception-kiosk-api';
import { receptionKioskCurrentSessionQueryKey, useReceptionKioskCurrentSession } from './reception-kiosk-session';
import { saveReceptionKioskResult } from './reception-kiosk-result';
import { hasReceptionKioskSettings } from './reception-kiosk-settings';

const detectorWasmPath = '/mediapipe';
const detectorModelPath = '/mediapipe/blaze_face_short_range.tflite';

export default function ReceptionKioskSessionFacePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const sessionQuery = useReceptionKioskCurrentSession();
  const [capture, setCapture] = useState<import('./reception-kiosk-onboarding').ReceptionKioskCapturedImage | null>(null);
  const [status, setStatus] = useState('Align your face inside the oval.');
  const [countdown, setCountdown] = useState<number | null>(null);
  const [detectionAvailable, setDetectionAvailable] = useState(true);
  const [isFaceReady, setIsFaceReady] = useState(false);
  const detectorRef = useRef<FaceDetector | null>(null);
  const { availableDevices, captureStillFrame, error, isStarting, isStreamReady, orientation, selectedDeviceId, setOrientation, setSelectedDeviceId, videoRef } = useReceptionKioskCamera('face');

  const continueMutation = useMutation({
    mutationFn: async (imageBase64: string) => {
      await storeReceptionKioskSessionFacePicture(imageBase64);
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
    let disposed = false;

    async function loadDetector() {
      try {
        const vision = await FilesetResolver.forVisionTasks(detectorWasmPath);
        if (disposed) return;
        detectorRef.current = await FaceDetector.createFromOptions(vision, {
          baseOptions: { modelAssetPath: detectorModelPath },
          runningMode: 'VIDEO',
          minDetectionConfidence: 0.7,
        });
      } catch {
        if (!disposed) {
          detectorRef.current = null;
          setDetectionAvailable(false);
          setStatus('Face detection unavailable. Capture when your face is centered in the oval.');
        }
      }
    }

    void loadDetector();
    return () => {
      disposed = true;
      detectorRef.current?.close();
      detectorRef.current = null;
    };
  }, []);

  useEffect(() => {
    if (!isStreamReady || !videoRef.current || capture) return;

    let frameId = 0;
    let lastProcessedAt = 0;

    const runDetection = (now: number) => {
      if (!videoRef.current || capture) return;

      if (now - lastProcessedAt >= 160) {
        lastProcessedAt = now;
        if (!detectionAvailable || !detectorRef.current) {
          setIsFaceReady(false);
          setCountdown(null);
          setStatus('Face detection unavailable. Use Capture now when your face is centered in the oval.');
        } else {
          const detections = detectorRef.current.detectForVideo(videoRef.current, now).detections ?? [];
          const nextState = evaluateFaceDetection(detections, videoRef.current.videoWidth, videoRef.current.videoHeight);
          setIsFaceReady(nextState.ready);
          setStatus(nextState.message);
        }
      }

      frameId = window.requestAnimationFrame(runDetection);
    };

    frameId = window.requestAnimationFrame(runDetection);
    return () => window.cancelAnimationFrame(frameId);
  }, [capture, detectionAvailable, isStreamReady, videoRef]);

  useEffect(() => {
    if (!isFaceReady || capture) {
      setCountdown(null);
      return;
    }
    setCountdown((current) => current ?? 3);
  }, [capture, isFaceReady]);

  useEffect(() => {
    if (countdown === null || capture || !isFaceReady) return;
    if (countdown === 0) {
      const stillFrame = captureStillFrame();
      if (stillFrame) setCapture(stillFrame);
      return;
    }

    const timeoutId = window.setTimeout(() => setCountdown((current) => current === null ? null : current - 1), 1000);
    return () => window.clearTimeout(timeoutId);
  }, [capture, captureStillFrame, countdown, isFaceReady]);

  if (!hasReceptionKioskSettings()) return <Navigate to="/reception-kiosk/setup" replace />;
  if (sessionQuery.isLoading) return null;
  if (sessionQuery.isError || !sessionQuery.data) return <Navigate to="/reception-kiosk" replace />;
  if (sessionQuery.data.status !== 'Active' || sessionQuery.data.currentStep !== 'FacePicture') return <Navigate to="/reception-kiosk/session" replace />;

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
    ? 'Face photo ready. Review before continuing.'
    : countdown !== null
      ? countdown > 0
        ? `Taking photo in ${countdown}...`
        : 'Capturing photo...'
      : status;

  return (
    <ReceptionKioskCaptureShell backTo="/reception-kiosk" progressLabel="Step 1 of 4 • Face photo" title="Take face picture" description="Look straight at the tablet. We will capture a face picture automatically after a short countdown.">
      <ReceptionKioskCameraSettings availableDevices={availableDevices} orientation={orientation} selectedDeviceId={selectedDeviceId} setOrientation={setOrientation} setSelectedDeviceId={setSelectedDeviceId} />
      {capture ? (
        <ReceptionKioskCaptureReview capture={capture} title="Face picture preview" onConfirm={() => void handleConfirm()} onRetake={() => { setCapture(null); setCountdown(null); setStatus('Align your face inside the oval.'); }} confirmLabel={continueMutation.isPending ? 'Continuing...' : 'Continue'} />
      ) : (
        <>
          <ReceptionKioskPreviewStage error={error} isStarting={isStarting} status={statusLabel}>
            <div className="relative h-full w-full">
              <video ref={videoRef} className="h-full w-full object-cover [transform:scaleX(-1)]" muted playsInline aria-label="Face camera preview" />
              <div className="pointer-events-none absolute inset-0 bg-black/20" aria-hidden="true" />
              <div className="pointer-events-none absolute inset-[8%] flex items-center justify-center" aria-hidden="true">
                <div className="h-[72%] w-[54%] rounded-[50%] border-4 border-white/85 shadow-[0_0_0_9999px_rgba(0,0,0,0.3)]" />
              </div>
              {countdown !== null ? <div className="absolute inset-0 flex items-center justify-center text-[96px] font-semibold text-white drop-shadow-lg">{countdown}</div> : null}
            </div>
          </ReceptionKioskPreviewStage>
          <ReceptionKioskFooterActions>
            <Button type="button" variant="outline" className="h-14 rounded-[1rem] text-[18px]" onClick={() => setCapture(captureStillFrame())}>Capture now</Button>
            <ReceptionKioskCancelLink to="/reception-kiosk" label="Cancel session" onClick={() => void handleCancel()} />
          </ReceptionKioskFooterActions>
        </>
      )}
    </ReceptionKioskCaptureShell>
  );
}

function evaluateFaceDetection(detections: { boundingBox?: { originX?: number; originY?: number; width?: number; height?: number } }[], videoWidth: number, videoHeight: number) {
  if (detections.length === 0) return { ready: false, message: 'Center your face inside the oval.' };
  if (detections.length > 1) return { ready: false, message: 'Only one person can be in frame.' };

  const box = detections[0]?.boundingBox;
  const width = box?.width ?? 0;
  const height = box?.height ?? 0;
  const centerX = (box?.originX ?? 0) + width / 2;
  const centerY = (box?.originY ?? 0) + height / 2;
  const offsetX = Math.abs(centerX - videoWidth / 2);
  const offsetY = Math.abs(centerY - videoHeight / 2);
  if (width < videoWidth * 0.22 || height < videoHeight * 0.28) return { ready: false, message: 'Move closer to the camera.' };
  if (offsetX > videoWidth * 0.14 || offsetY > videoHeight * 0.16) return { ready: false, message: 'Center your face in the oval.' };
  return { ready: true, message: 'Hold still. We are preparing the capture.' };
}
