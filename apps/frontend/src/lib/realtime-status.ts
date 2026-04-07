export type RealtimeConnectionState = 'idle' | 'connecting' | 'live' | 'reconnecting';

export function getRealtimeTone(state: RealtimeConnectionState) {
  if (state === 'live') {
    return 'success' as const;
  }

  if (state === 'reconnecting' || state === 'connecting') {
    return 'warning' as const;
  }

  return 'neutral' as const;
}

export function getRealtimeLabel(state: RealtimeConnectionState) {
  if (state === 'live') {
    return 'Sincronizado';
  }

  if (state === 'reconnecting') {
    return 'Reconectando';
  }

  if (state === 'connecting') {
    return 'Conectando';
  }

  return 'En espera';
}
