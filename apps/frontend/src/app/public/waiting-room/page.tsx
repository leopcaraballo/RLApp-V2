import { PublicWaitingRoomDisplay } from '@/components/display/WaitingRoomMonitor/public-waiting-room-display';
import { getPublicWaitingRoomDefaultQueueId } from '@/lib/env';

export default async function PublicWaitingRoomPage({
  searchParams,
}: {
  searchParams: Promise<{ queueId?: string }>;
}) {
  const resolvedSearchParams = await searchParams;
  const queueId = resolvedSearchParams.queueId?.trim() || getPublicWaitingRoomDefaultQueueId();

  return <PublicWaitingRoomDisplay initialQueueId={queueId} />;
}
