// In Docker, use service name 'backend'; locally, use localhost
// Priority: env var > Docker (backend:8080) > localhost
const DEFAULT_BACKEND_API_BASE_URL = process.env.BACKEND_API_BASE_URL || 'http://backend:8080';
const DEFAULT_PUBLIC_WAITING_ROOM_QUEUE_ID =
  process.env.PUBLIC_WAITING_ROOM_DEFAULT_QUEUE_ID || 'CONSULTA-EXTERNA-PRINCIPAL';
const DEFAULT_SESSION_SECRET = 'rlapp-dev-session-secret-change-me';

export function getBackendApiBaseUrl(): string {
  return process.env.BACKEND_API_BASE_URL || DEFAULT_BACKEND_API_BASE_URL;
}

export function getPublicWaitingRoomDefaultQueueId(): string {
  return DEFAULT_PUBLIC_WAITING_ROOM_QUEUE_ID.trim();
}

export function getSessionSecret(): string {
  if (process.env.FRONTEND_SESSION_SECRET) {
    return process.env.FRONTEND_SESSION_SECRET;
  }

  if (process.env.NODE_ENV === 'production') {
    throw new Error('FRONTEND_SESSION_SECRET is required in production.');
  }

  return DEFAULT_SESSION_SECRET;
}
