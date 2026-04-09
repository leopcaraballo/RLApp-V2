'use client';

import { useQuery } from '@tanstack/react-query';
import { rlappApi } from '@/services/rlapp-api';

export const operationalQueryKeys = {
  dashboard: ['operations-dashboard'] as const,
  waitingRoomMonitor: (queueId: string) => ['waiting-room-monitor', queueId] as const,
};

export function useOperationsDashboard(enabled = true) {
  return useQuery({
    queryKey: operationalQueryKeys.dashboard,
    queryFn: () => rlappApi.getOperationsDashboard(),
    enabled,
    staleTime: 15_000,
    refetchOnReconnect: true,
  });
}

export function useWaitingRoomMonitor(queueId: string, enabled = true) {
  return useQuery({
    queryKey: operationalQueryKeys.waitingRoomMonitor(queueId),
    queryFn: () => rlappApi.getWaitingRoomMonitor(queueId),
    enabled: enabled && queueId.trim().length > 0,
    staleTime: 15_000,
    refetchOnReconnect: true,
  });
}
