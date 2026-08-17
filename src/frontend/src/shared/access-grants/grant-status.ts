import type { components } from '@/shared/api/generated/schema';

type AccessGrantResponse = components['schemas']['AccessGrantResponse'];
type PackageRequestDetailGrantResponse = components['schemas']['PackageRequestDetailGrantResponse'];

type GrantLike = Pick<AccessGrantResponse, 'status' | 'approvalStatus' | 'complianceStatus' | 'compliantUntil'>;

export function getGrantStatusVariant(status: AccessGrantResponse['status']): 'success' | 'secondary' | 'error' {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Revoked':
    case 'Expired':
      return 'error';
    default:
      return 'secondary';
  }
}

export function getGrantApprovalVariant(status: AccessGrantResponse['approvalStatus']): 'success' | 'secondary' | 'error' {
  switch (status) {
    case 'Approved':
    case 'NotRequired':
      return 'success';
    case 'Rejected':
      return 'error';
    default:
      return 'secondary';
  }
}

export function getGrantComplianceVariant(status: AccessGrantResponse['complianceStatus']): 'success' | 'secondary' | 'error' {
  switch (status) {
    case 'Compliant':
      return 'success';
    case 'NonCompliant':
      return 'error';
    default:
      return 'secondary';
  }
}

export function getGrantApprovalLabel(status: AccessGrantResponse['approvalStatus']) {
  return status === 'NotRequired' ? 'Approval not required' : status;
}

export function getGrantComplianceLabel(status: AccessGrantResponse['complianceStatus']) {
  switch (status) {
    case 'TemporarilyCompliant':
      return 'Temporarily compliant';
    case 'NonCompliant':
      return 'Non-compliant';
    default:
      return 'Compliant';
  }
}

export function isGrantBusinessReady(grant: GrantLike) {
  return grant.status === 'Active'
    && (grant.approvalStatus === 'Approved' || grant.approvalStatus === 'NotRequired')
    && (grant.complianceStatus === 'Compliant' || grant.complianceStatus === 'TemporarilyCompliant');
}

export function getGrantBusinessSummary(grant: GrantLike) {
  if (grant.status !== 'Active') {
    return grant.status;
  }

  if (grant.approvalStatus === 'Rejected') {
    return 'Rejected';
  }

  if (grant.approvalStatus === 'Pending') {
    return 'Pending approval';
  }

  if (grant.complianceStatus === 'NonCompliant') {
    return 'Withheld pending compliance';
  }

  if (grant.complianceStatus === 'TemporarilyCompliant') {
    return 'Active until compliance expires';
  }

  return 'Active';
}

export function getGrantComplianceUntilLabel(grant: Pick<GrantLike, 'complianceStatus' | 'compliantUntil'>) {
  return grant.complianceStatus === 'TemporarilyCompliant' ? grant.compliantUntil : null;
}

export type GrantWithCompliance = AccessGrantResponse | PackageRequestDetailGrantResponse;
