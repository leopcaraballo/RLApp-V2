export interface PublicWaitingRoomTurn {
  turnNumber: string;
}

export type PublicWaitingRoomCallStatus = 'Called' | 'AtCashier' | 'InConsultation';

export interface PublicWaitingRoomCall {
  turnNumber: string;
  destination: string;
  status: PublicWaitingRoomCallStatus;
}

export interface PublicWaitingRoomDisplaySnapshot {
  queueId: string;
  generatedAt: string;
  currentTurn: PublicWaitingRoomTurn | null;
  upcomingTurns: PublicWaitingRoomTurn[];
  activeCalls: PublicWaitingRoomCall[];
}

export interface PublicWaitingRoomRealtimeEvent {
  version: string;
  eventType: string;
  queueId: string;
  occurredAt: string;
  payload: PublicWaitingRoomDisplaySnapshot;
}

export type PublicWaitingRoomConnectionState = 'idle' | 'connecting' | 'live' | 'reconnecting';
