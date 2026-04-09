import { NextResponse } from 'next/server';
import {
  PublicWaitingRoomBackendError,
  fetchPublicWaitingRoomDisplayFromBackend,
} from '@/lib/public-waiting-room-server';

export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';

export async function GET(_request: Request, context: { params: Promise<{ queueId: string }> }) {
  const { queueId } = await context.params;
  const normalizedQueueId = queueId.trim();

  if (normalizedQueueId.length === 0) {
    return NextResponse.json(
      { message: 'Debes indicar una cola publica valida.' },
      { status: 400 }
    );
  }

  try {
    const snapshot = await fetchPublicWaitingRoomDisplayFromBackend(normalizedQueueId);

    return NextResponse.json(snapshot, {
      headers: {
        'Cache-Control': 'no-store',
      },
    });
  } catch (error) {
    if (error instanceof PublicWaitingRoomBackendError) {
      return NextResponse.json({ message: error.message }, { status: error.status });
    }

    throw error;
  }
}
