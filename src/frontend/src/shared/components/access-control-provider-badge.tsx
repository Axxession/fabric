import type { components } from '@/shared/api/generated/schema';
import { Badge } from '@/shared/components/ui/badge';

type AccessControlProviderKind = components['schemas']['AccessControlProviderKind'];

export function AccessControlProviderBadge({ providerKind }: { readonly providerKind: AccessControlProviderKind }) {
  return <Badge variant="outline">{getAccessControlProviderLabel(providerKind)}</Badge>;
}

function getAccessControlProviderLabel(providerKind: AccessControlProviderKind) {
  return providerKind === 'Unipass' ? 'Unipass' : providerKind;
}
