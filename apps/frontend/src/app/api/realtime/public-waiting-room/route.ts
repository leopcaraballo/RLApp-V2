import { NextRequest, NextResponse } from 'next/server';
import {
  PublicWaitingRoomBackendError,
  fetchPublicWaitingRoomDisplayFromBackend,
} from '@/lib/public-waiting-room-server';
import type {
  PublicWaitingRoomDisplaySnapshot,
  PublicWaitingRoomRealtimeEvent,
} from '@/types/public-display';

export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';

const STREAM_VERSION = '1.0';
const HEARTBEAT_INTERVAL_MS = 25_000;
const POLL_INTERVAL_MS = 2_000;
const RETRY_INTERVAL_MS = 3_000;

function encodeSseChunk(encoder: TextEncoder, chunk: string): Uint8Array {
  return encoder.encode(chunk);
}

function collectVisibleTurnNumbers(snapshot: PublicWaitingRoomDisplaySnapshot | null): Set<string> {
  const turnNumbers = new Set<string>();

  if (!snapshot) {
    return turnNumbers;
  }

  if (snapshot.currentTurn?.turnNumber) {
    turnNumbers.add(snapshot.currentTurn.turnNumber);
  }

  for (const activeCall of snapshot.activeCalls) {
    if (activeCall.turnNumber) {
      turnNumbers.add(activeCall.turnNumber);
    }
  }

  for (const turn of snapshot.upcomingTurns) {
    if (turn.turnNumber) {
      turnNumbers.add(turn.turnNumber);
    }
  }

  return turnNumbers;
}

function collectActiveCallSignatures(
  snapshot: PublicWaitingRoomDisplaySnapshot | null
): Set<string> {
  const signatures = new Set<string>();

  if (!snapshot) {
    return signatures;
  }

  for (const activeCall of snapshot.activeCalls) {
    signatures.add(`${activeCall.turnNumber}|${activeCall.destination}|${activeCall.status}`);
  }

  return signatures;
}

function resolveEventType(
  previousSnapshot: PublicWaitingRoomDisplaySnapshot | null,
  nextSnapshot: PublicWaitingRoomDisplaySnapshot
): string {
  const previousActiveCalls = collectActiveCallSignatures(previousSnapshot);
  const nextActiveCalls = collectActiveCallSignatures(nextSnapshot);

  for (const signature of nextActiveCalls) {
    if (!previousActiveCalls.has(signature)) {
      return 'PatientCalled';
    }
  }

  if (nextActiveCalls.size !== previousActiveCalls.size) {
    return 'PatientCalled';
  }

  const previousTurns = collectVisibleTurnNumbers(previousSnapshot);
  const nextTurns = collectVisibleTurnNumbers(nextSnapshot);

  for (const turnNumber of nextTurns) {
    if (!previousTurns.has(turnNumber)) {
      return 'PatientCheckedIn';
    }
  }

  for (const turnNumber of previousTurns) {
    if (!nextTurns.has(turnNumber)) {
      return 'PatientAttentionCompleted';
    }
  }

  return 'PublicDisplayUpdated';
}

export async function GET(request: NextRequest) {
  const queueId = request.nextUrl.searchParams.get('queueId')?.trim() ?? '';

  if (queueId.length === 0) {
    return NextResponse.json(
      { message: 'Debes indicar una cola publica valida.' },
      { status: 400 }
    );
  }

  let initialSnapshot: PublicWaitingRoomDisplaySnapshot;

  try {
    initialSnapshot = await fetchPublicWaitingRoomDisplayFromBackend(queueId);
  } catch (error) {
    if (error instanceof PublicWaitingRoomBackendError) {
      return NextResponse.json({ message: error.message }, { status: error.status });
    }

    throw error;
  }

  const encoder = new TextEncoder();
  let streamController: ReadableStreamDefaultController<Uint8Array> | null = null;
  let heartbeat: ReturnType<typeof setInterval> | null = null;
  let poller: ReturnType<typeof setInterval> | null = null;
  let closed = false;
  let lastSignature = JSON.stringify(initialSnapshot);
  let lastSnapshot: PublicWaitingRoomDisplaySnapshot | null = initialSnapshot;

  const closeStream = () => {
    if (closed) {
      return;
    }

    closed = true;

    if (heartbeat) {
      clearInterval(heartbeat);
      heartbeat = null;
    }

    if (poller) {
      clearInterval(poller);
      poller = null;
    }

    try {
      streamController?.close();
    } catch {
      // Ignore duplicate close attempts during reconnect races.
    }
  };

  const stream = new ReadableStream<Uint8Array>({
    start(controller) {
      streamController = controller;

      const sendChunk = (chunk: string) => {
        if (closed) {
          return;
        }

        controller.enqueue(encodeSseChunk(encoder, chunk));
      };

      const sendSnapshot = (snapshot: PublicWaitingRoomDisplaySnapshot) => {
        const event: PublicWaitingRoomRealtimeEvent = {
          version: STREAM_VERSION,
          eventType: resolveEventType(lastSnapshot, snapshot),
          queueId,
          occurredAt: snapshot.generatedAt,
          payload: snapshot,
        };

        lastSnapshot = snapshot;
        lastSignature = JSON.stringify(snapshot);
        sendChunk(`data: ${JSON.stringify(event)}\n\n`);
      };

      const publishSnapshot = async () => {
        try {
          const nextSnapshot = await fetchPublicWaitingRoomDisplayFromBackend(queueId);
          const nextSignature = JSON.stringify(nextSnapshot);

          if (nextSignature === lastSignature) {
            return;
          }

          sendSnapshot(nextSnapshot);
        } catch {
          closeStream();
        }
      };

      request.signal.addEventListener('abort', closeStream);

      sendChunk(`retry: ${RETRY_INTERVAL_MS}\n\n`);
      sendSnapshot(initialSnapshot);

      heartbeat = setInterval(() => {
        sendChunk(': keepalive\n\n');
      }, HEARTBEAT_INTERVAL_MS);

      poller = setInterval(() => {
        void publishSnapshot();
      }, POLL_INTERVAL_MS);
    },
    cancel() {
      closeStream();
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
