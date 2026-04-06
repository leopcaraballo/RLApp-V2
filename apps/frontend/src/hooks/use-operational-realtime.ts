'use client';

import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useEffectEvent, useRef, useState } from 'react';
import { operationalQueryKeys } from '@/hooks/use-operational-read-models';
import type { OperationalRealtimeEvent, StaffRole } from '@/types/api';

type ConnectionState = 'idle' | 'connecting' | 'live' | 'reconnecting';

interface UseOperationalRealtimeOptions {
  role: StaffRole;
  enabled?: boolean;
  dashboard?: boolean;
  queueId?: string;
  trajectoryId?: string;
  onTrajectoryInvalidation?: (payload?: OperationalRealtimeEvent) => void;
}

const DASHBOARD_ROLES = new Set<StaffRole>(['Supervisor', 'Support']);
const QUEUE_ROLES = new Set<StaffRole>(['Receptionist', 'Doctor', 'Supervisor']);
const TRAJECTORY_ROLES = new Set<StaffRole>(['Supervisor', 'Support']);

function buildRealtimeUrl(options: UseOperationalRealtimeOptions): string | null {
  const params = new URLSearchParams();

  if (options.dashboard) {
    if (!DASHBOARD_ROLES.has(options.role)) {
      return null;
    }

    params.set('dashboard', '1');
  }

  if (options.queueId) {
    if (!QUEUE_ROLES.has(options.role)) {
      return null;
    }

    params.set('queueId', options.queueId);
  }

  if (options.trajectoryId) {
    if (!TRAJECTORY_ROLES.has(options.role)) {
      return null;
    }

    params.set('trajectoryId', options.trajectoryId);
  }

  if (!Array.from(params.keys()).length) {
    return null;
  }

  return `/api/realtime/operations?${params.toString()}`;
}

function parseRealtimeEvent(rawValue: string): OperationalRealtimeEvent | null {
  try {
    return JSON.parse(rawValue) as OperationalRealtimeEvent;
  } catch {
    return null;
  }
}

export function useOperationalRealtime(options: UseOperationalRealtimeOptions) {
  const queryClient = useQueryClient();
  const [connectionState, setConnectionState] = useState<ConnectionState>('idle');
  const [lastEvent, setLastEvent] = useState<OperationalRealtimeEvent | null>(null);
  const hasConnectedOnce = useRef(false);
  const hadConnectionError = useRef(false);

  const url = buildRealtimeUrl(options);

  const invalidateVisibleState = useEffectEvent((payload?: OperationalRealtimeEvent) => {
    if (options.dashboard) {
      void queryClient.invalidateQueries({ queryKey: operationalQueryKeys.dashboard });
    }

    if (options.queueId) {
      void queryClient.invalidateQueries({
        queryKey: operationalQueryKeys.waitingRoomMonitor(options.queueId),
      });
    }

    if (options.trajectoryId) {
      options.onTrajectoryInvalidation?.(payload);
    }
  });

  const handleRealtimeMessage = useEffectEvent((payload: OperationalRealtimeEvent) => {
    setLastEvent(payload);

    if (payload.scope === 'dashboard' && options.dashboard) {
      void queryClient.invalidateQueries({ queryKey: operationalQueryKeys.dashboard });
      return;
    }

    if (payload.scope === 'queue' && options.queueId) {
      if (!payload.queueId || payload.queueId === options.queueId) {
        void queryClient.invalidateQueries({
          queryKey: operationalQueryKeys.waitingRoomMonitor(options.queueId),
        });
      }
      return;
    }

    if (
      payload.scope === 'trajectory' &&
      options.trajectoryId &&
      payload.trajectoryId === options.trajectoryId
    ) {
      options.onTrajectoryInvalidation?.(payload);
    }
  });

  useEffect(() => {
    if (!options.enabled || !url) {
      setConnectionState('idle');
      return;
    }

    setConnectionState(hasConnectedOnce.current ? 'reconnecting' : 'connecting');

    const source = new EventSource(url);

    source.onopen = () => {
      const shouldResync = hasConnectedOnce.current || hadConnectionError.current;
      hasConnectedOnce.current = true;
      hadConnectionError.current = false;
      setConnectionState('live');

      if (shouldResync) {
        invalidateVisibleState();
      }
    };

    source.onmessage = (event) => {
      const payload = parseRealtimeEvent(event.data);
      if (!payload) {
        return;
      }

      handleRealtimeMessage(payload);
    };

    source.onerror = () => {
      hadConnectionError.current = true;
      setConnectionState('reconnecting');
    };

    return () => {
      source.close();
      setConnectionState('idle');
    };
  }, [handleRealtimeMessage, invalidateVisibleState, options.enabled, url]);

  return {
    connectionState,
    lastEvent,
  };
}
