import { useQuery } from '@tanstack/react-query';
import { getCurrentReceptionKioskSession, type ReceptionKioskSession, type ReceptionKioskSessionStep, type ReceptionKioskSessionStopReason } from './reception-kiosk-api';

export const receptionKioskCurrentSessionQueryKey = ['reception-kiosk', 'current-session'] as const;

export function useReceptionKioskCurrentSession() {
  return useQuery({
    queryKey: receptionKioskCurrentSessionQueryKey,
    queryFn: async () => await getCurrentReceptionKioskSession(),
    retry: false,
  });
}

export function getReceptionKioskSessionPath(session: Pick<ReceptionKioskSession, 'status' | 'currentStep'>): '/reception-kiosk/session/face' | '/reception-kiosk/session/document' | '/reception-kiosk/session/compliance' | '/reception-kiosk/session/onboard' | '/reception-kiosk/session/terminal' {
  if (session.status !== 'Active' || !session.currentStep) {
    return '/reception-kiosk/session/terminal';
  }

  return session.currentStep === 'FacePicture'
    ? '/reception-kiosk/session/face'
    : session.currentStep === 'IdentityDocumentCheck'
      ? '/reception-kiosk/session/document'
      : session.currentStep === 'ComplianceCheck'
        ? '/reception-kiosk/session/compliance'
        : '/reception-kiosk/session/onboard';
}

export function getReceptionKioskTerminalCopy(reason: ReceptionKioskSessionStopReason | null, message: string | null) {
  if (reason === 'NotCompliant') {
    return {
      title: 'Please contact your contact person',
      message: message ?? 'Your onboarding cannot be completed yet. Please contact your contact person to help you onboard.',
    };
  }

  if (reason === 'Timeout') {
    return {
      title: 'Session timed out',
      message: message ?? 'The session timed out. Please scan your QR code again to continue.',
    };
  }

  if (reason === 'HomeRedirect' || reason === 'Superseded') {
    return {
      title: 'Session ended',
      message: message ?? 'This session was ended before onboarding was completed.',
    };
  }

  return {
    title: 'Session failed',
    message: message ?? 'This kiosk session could not be completed.',
  };
}

export function isReceptionKioskCurrentStep(session: ReceptionKioskSession | undefined, step: ReceptionKioskSessionStep) {
  return session?.status === 'Active' && session.currentStep === step;
}
