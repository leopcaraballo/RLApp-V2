import type { ApiEnvelopeError } from '@/types/api';

export class ApiError extends Error {
  status: number;
  payload: ApiEnvelopeError | null;

  constructor(status: number, message: string, payload: ApiEnvelopeError | null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.payload = payload;
  }
}

function buildHeaders(initHeaders?: HeadersInit): Headers {
  return new Headers(initHeaders);
}

export async function httpRequest<TResponse>(
  input: RequestInfo | URL,
  init: RequestInit & { json?: unknown } = {}
): Promise<TResponse> {
  const headers = buildHeaders(init.headers);

  if (init.json !== undefined) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(input, {
    ...init,
    headers,
    body: init.json === undefined ? init.body : JSON.stringify(init.json),
    cache: 'no-store',
  });

  const contentType = response.headers.get('content-type') ?? '';
  const isJson =
    contentType.includes('application/json') || contentType.includes('application/problem+json');

  const payload = isJson
    ? ((await response.json()) as TResponse | ApiEnvelopeError)
    : ((await response.text()) as unknown as TResponse);

  if (!response.ok) {
    const fallbackMessage =
      typeof payload === 'object' && payload !== null && 'message' in payload
        ? String(payload.message)
        : `Request failed with status ${response.status}`;

    throw new ApiError(response.status, fallbackMessage, payload as ApiEnvelopeError);
  }

  return payload as TResponse;
}
