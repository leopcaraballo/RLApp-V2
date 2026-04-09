import { getBackendApiBaseUrl } from '@/lib/env';
import type { PublicWaitingRoomDisplaySnapshot } from '@/types/public-display';

export class PublicWaitingRoomBackendError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = 'PublicWaitingRoomBackendError';
    this.status = status;
  }
}

function readPayloadMessage(payload: unknown, status: number): string {
  if (typeof payload === 'object' && payload !== null) {
    if ('message' in payload && typeof payload.message === 'string') {
      return payload.message;
    }

    if ('error' in payload && typeof payload.error === 'string') {
      return payload.error;
    }

    if ('detail' in payload && typeof payload.detail === 'string') {
      return payload.detail;
    }

    if ('title' in payload && typeof payload.title === 'string') {
      return payload.title;
    }
  }

  return `Request failed with status ${status}`;
}

export async function fetchPublicWaitingRoomDisplayFromBackend(
  queueId: string
): Promise<PublicWaitingRoomDisplaySnapshot> {
  const backendUrl = new URL(
    `/api/v1/waiting-room/${encodeURIComponent(queueId)}/public-display`,
    getBackendApiBaseUrl()
  );

  const response = await fetch(backendUrl, {
    headers: {
      'X-Correlation-Id': crypto.randomUUID(),
    },
    cache: 'no-store',
  });

  const contentType = response.headers.get('content-type') ?? '';
  const isJson =
    contentType.includes('application/json') || contentType.includes('application/problem+json');

  const payload = isJson
    ? ((await response.json()) as PublicWaitingRoomDisplaySnapshot | { message?: string })
    : await response.text();

  if (!response.ok) {
    throw new PublicWaitingRoomBackendError(
      response.status,
      readPayloadMessage(payload, response.status)
    );
  }

  return payload as PublicWaitingRoomDisplaySnapshot;
}
