'use client';

import { useQuery } from '@tanstack/react-query';
import { rlappApi } from '@/services/rlapp-api';

export function useSession() {
  return useQuery({
    queryKey: ['session'],
    queryFn: () => rlappApi.getSession(),
    staleTime: 60_000,
  });
}
