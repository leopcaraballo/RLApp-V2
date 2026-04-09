'use client';

import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useEffectEvent, useRef, useState } from 'react';
import { httpRequest } from '@/services/http-client';
import type {
  PublicWaitingRoomConnectionState,
  PublicWaitingRoomDisplaySnapshot,
  PublicWaitingRoomRealtimeEvent,
} from '@/types/public-display';

export const publicWaitingRoomQueryKeys = {
  display: (queueId: string) => ['public-waiting-room-display', queueId] as const,
};

const VISIBLE_RECONNECT_DELAY_MS = 4_000;

function buildDisplayPath(queueId: string): string {
  return `/api/public/waiting-room/${encodeURIComponent(queueId)}`;
}

function buildRealtimeUrl(queueId: string): string {
  const params = new URLSearchParams();
  params.set('queueId', queueId);
  return `/api/realtime/public-waiting-room?${params.toString()}`;
}

function parseRealtimeEvent(rawValue: string): PublicWaitingRoomRealtimeEvent | null {
  try {
    return JSON.parse(rawValue) as PublicWaitingRoomRealtimeEvent;
  } catch {
    return null;
  }
}

export function usePublicWaitingRoomDisplay(queueId: string, enabled = true) {
  return useQuery({
    queryKey: publicWaitingRoomQueryKeys.display(queueId),
    queryFn: () => httpRequest<PublicWaitingRoomDisplaySnapshot>(buildDisplayPath(queueId)),
    enabled: enabled && queueId.trim().length > 0,
    staleTime: 15_000,
    refetchOnReconnect: true,
  });
}

export function usePublicWaitingRoomRealtime({
  queueId,
  enabled = true,
}: {
  queueId: string;
  enabled?: boolean;
}) {
  const queryClient = useQueryClient();
  const [connectionState, setConnectionState] = useState<PublicWaitingRoomConnectionState>('idle');
  const [lastEvent, setLastEvent] = useState<PublicWaitingRoomRealtimeEvent | null>(null);
  const hasConnectedOnce = useRef(false);
  const reconnectIndicatorTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleRealtimeMessage = useEffectEvent((payload: PublicWaitingRoomRealtimeEvent) => {
    setLastEvent(payload);
    queryClient.setQueryData(publicWaitingRoomQueryKeys.display(queueId), payload.payload);
  });

  const clearReconnectIndicatorTimeout = useEffectEvent(() => {
    if (reconnectIndicatorTimeoutRef.current) {
      clearTimeout(reconnectIndicatorTimeoutRef.current);
      reconnectIndicatorTimeoutRef.current = null;
    }
  });

  useEffect(() => {
    const normalizedQueueId = queueId.trim();

    if (!enabled || normalizedQueueId.length === 0) {
      clearReconnectIndicatorTimeout();
      setConnectionState('idle');
      return;
    }

    setConnectionState(hasConnectedOnce.current ? 'reconnecting' : 'connecting');

    const source = new EventSource(buildRealtimeUrl(normalizedQueueId));

    source.onopen = () => {
      clearReconnectIndicatorTimeout();
      hasConnectedOnce.current = true;
      setConnectionState('live');
    };

    source.onmessage = (event) => {
      const payload = parseRealtimeEvent(event.data);
      if (!payload) {
        return;
      }

      handleRealtimeMessage(payload);
    };

    source.onerror = () => {
      if (reconnectIndicatorTimeoutRef.current) {
        return;
      }

      reconnectIndicatorTimeoutRef.current = setTimeout(() => {
        reconnectIndicatorTimeoutRef.current = null;
        setConnectionState('reconnecting');
      }, VISIBLE_RECONNECT_DELAY_MS);
    };

    return () => {
      clearReconnectIndicatorTimeout();
      source.close();
      setConnectionState('idle');
    };
  }, [clearReconnectIndicatorTimeout, enabled, handleRealtimeMessage, queueId]);

  return {
    connectionState,
    lastEvent,
  };
}
