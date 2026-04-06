import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
} from '@microsoft/signalr';
import { NextRequest, NextResponse } from 'next/server';
import { getServerSession } from '@/lib/auth';
import { getBackendApiBaseUrl } from '@/lib/env';
import type { OperationalRealtimeEvent, StaffRole } from '@/types/api';

export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';

const STREAM_VERSION = '1.0';
const HEARTBEAT_INTERVAL_MS = 25_000;
const RETRY_INTERVAL_MS = 3_000;

const DASHBOARD_ROLES = new Set<StaffRole>(['Supervisor', 'Support']);
const QUEUE_ROLES = new Set<StaffRole>(['Receptionist', 'Doctor', 'Supervisor']);
const TRAJECTORY_ROLES = new Set<StaffRole>(['Supervisor', 'Support']);

const REALTIME_METHODS = [
  'PatientCheckedIn',
  'PatientCalled',
  'PatientAtConsultation',
  'PatientAttentionCompleted',
] as const;

interface RequestedScopes {
  dashboard: boolean;
  queueIds: string[];
  trajectoryId?: string;
}

function readString(source: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = source[key];
    if (typeof value === 'string' && value.trim().length > 0) {
      return value;
    }
  }

  return undefined;
}

function readRequestedScopes(request: NextRequest): RequestedScopes {
  const queueIds = Array.from(
    new Set(
      request.nextUrl.searchParams
        .getAll('queueId')
        .map((value) => value.trim())
        .filter((value) => value.length > 0)
    )
  );

  const trajectoryId = request.nextUrl.searchParams.get('trajectoryId')?.trim() || undefined;

  return {
    dashboard: request.nextUrl.searchParams.get('dashboard') === '1',
    queueIds,
    trajectoryId,
  };
}

function validateScopes(role: StaffRole, scopes: RequestedScopes): NextResponse | null {
  if (!scopes.dashboard && scopes.queueIds.length === 0 && !scopes.trajectoryId) {
    return NextResponse.json(
      { message: 'At least one operational realtime scope must be requested.' },
      { status: 400 }
    );
  }

  if (scopes.dashboard && !DASHBOARD_ROLES.has(role)) {
    return NextResponse.json(
      { message: 'Current role cannot subscribe to operations dashboard invalidations.' },
      { status: 403 }
    );
  }

  if (scopes.queueIds.length > 0 && !QUEUE_ROLES.has(role)) {
    return NextResponse.json(
      { message: 'Current role cannot subscribe to waiting room invalidations.' },
      { status: 403 }
    );
  }

  if (scopes.trajectoryId && !TRAJECTORY_ROLES.has(role)) {
    return NextResponse.json(
      { message: 'Current role cannot subscribe to trajectory invalidations.' },
      { status: 403 }
    );
  }

  return null;
}

function buildHubConnection(accessToken: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${getBackendApiBaseUrl()}/hubs/notifications`, {
      accessTokenFactory: () => accessToken,
      transport: HttpTransportType.LongPolling,
    })
    .configureLogging(LogLevel.None)
    .build();
}

function mapInvalidations(
  payload: Record<string, unknown>,
  scopes: RequestedScopes
): OperationalRealtimeEvent[] {
  const eventType = readString(payload, 'EventType', 'eventType') ?? 'OperationalUpdate';
  const queueId = readString(payload, 'QueueId', 'queueId', 'AggregateId', 'aggregateId');
  const trajectoryId = readString(payload, 'TrajectoryId', 'trajectoryId');
  const correlationId = readString(payload, 'CorrelationId', 'correlationId');
  const occurredAt = readString(payload, 'OccurredAt', 'occurredAt') ?? new Date().toISOString();

  const invalidations: OperationalRealtimeEvent[] = [];

  if (scopes.dashboard) {
    invalidations.push({
      version: STREAM_VERSION,
      eventType,
      scope: 'dashboard',
      queueId,
      trajectoryId,
      correlationId,
      occurredAt,
    });
  }

  if (queueId && scopes.queueIds.includes(queueId)) {
    invalidations.push({
      version: STREAM_VERSION,
      eventType,
      scope: 'queue',
      queueId,
      trajectoryId,
      correlationId,
      occurredAt,
    });
  }

  if (scopes.trajectoryId && trajectoryId === scopes.trajectoryId) {
    invalidations.push({
      version: STREAM_VERSION,
      eventType,
      scope: 'trajectory',
      queueId,
      trajectoryId,
      correlationId,
      occurredAt,
    });
  }

  return invalidations;
}

function encodeSseChunk(encoder: TextEncoder, chunk: string): Uint8Array {
  return encoder.encode(chunk);
}

export async function GET(request: NextRequest) {
  const session = await getServerSession();

  if (!session) {
    return NextResponse.json(
      { message: 'Authentication is required to access operational realtime.' },
      { status: 401 }
    );
  }

  const requestedScopes = readRequestedScopes(request);
  const scopeError = validateScopes(session.role, requestedScopes);
  if (scopeError) {
    return scopeError;
  }

  const effectiveScopes = requestedScopes;

  const connection = buildHubConnection(session.accessToken);
  const encoder = new TextEncoder();

  let streamController: ReadableStreamDefaultController<Uint8Array> | null = null;
  let heartbeat: ReturnType<typeof setInterval> | null = null;
  let closed = false;

  const closeStream = async () => {
    if (closed) {
      return;
    }

    closed = true;

    if (heartbeat) {
      clearInterval(heartbeat);
      heartbeat = null;
    }

    if (connection.state !== HubConnectionState.Disconnected) {
      try {
        await connection.stop();
      } catch {
        // Ignore shutdown failures triggered during disconnect cleanup.
      }
    }

    try {
      streamController?.close();
    } catch {
      // Ignore duplicate close attempts caused by simultaneous disconnect signals.
    }
  };

  const stream = new ReadableStream<Uint8Array>({
    async start(controller) {
      streamController = controller;

      const sendChunk = (chunk: string) => {
        if (closed) {
          return;
        }

        controller.enqueue(encodeSseChunk(encoder, chunk));
      };

      const sendInvalidation = (event: OperationalRealtimeEvent) => {
        sendChunk(`data: ${JSON.stringify(event)}\n\n`);
      };

      request.signal.addEventListener('abort', () => {
        void closeStream();
      });

      connection.onclose(() => {
        void closeStream();
      });

      for (const methodName of REALTIME_METHODS) {
        connection.on(methodName, (payload: Record<string, unknown>) => {
          const invalidations = mapInvalidations(payload, effectiveScopes);
          for (const invalidation of invalidations) {
            sendInvalidation(invalidation);
          }
        });
      }

      try {
        await connection.start();

        if (effectiveScopes.dashboard) {
          await connection.invoke('JoinDashboardGroup');
        }

        for (const queueId of effectiveScopes.queueIds) {
          await connection.invoke('JoinQueueGroup', queueId);
        }

        if (effectiveScopes.trajectoryId) {
          await connection.invoke('JoinTrajectoryGroup', effectiveScopes.trajectoryId);
        }

        sendChunk(`retry: ${RETRY_INTERVAL_MS}\n\n`);
        heartbeat = setInterval(() => {
          sendChunk(': keepalive\n\n');
        }, HEARTBEAT_INTERVAL_MS);
      } catch {
        await closeStream();
      }
    },
    async cancel() {
      await closeStream();
    },
  });

  return new Response(stream, {
    headers: {
      'Cache-Control': 'no-cache, no-transform',
      Connection: 'keep-alive',
      'Content-Type': 'text/event-stream',
      'X-Accel-Buffering': 'no',
    },
  });
}
