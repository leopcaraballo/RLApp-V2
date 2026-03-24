import { WaitingRoomConsole } from '@/features/waiting-room/waiting-room-console';
import { requireServerSession } from '@/lib/auth';

export default async function WaitingRoomPage() {
  const session = await requireServerSession();

  // Type validation: ensure role is a valid string
  const roleValue = (
    typeof session.role === 'string' ? session.role : String(session.role)
  ) as typeof session.role;

  return <WaitingRoomConsole role={roleValue} />;
}
