import { useQuery } from '@tanstack/react-query';
import { useAuth } from 'react-oidc-context';

import { api } from '@/shared/api/client';
import type { components } from '@/shared/api/generated/schema';

export type CurrentActor = components['schemas']['CurrentActorResponse'];

export const currentActorQueryKey = ['actors', 'me'] as const;

export function useCurrentActor() {
  const auth = useAuth();

  return useQuery({
    queryKey: currentActorQueryKey,
    enabled: auth.isAuthenticated,
    queryFn: async () => {
      const { data, error } = await api.GET('/api/actors/me');

      if (error || !data) {
        throw new Error('Could not load current actor.');
      }

      return data;
    },
  });
}
