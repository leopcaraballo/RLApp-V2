import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';
import { SESSION_COOKIE_NAME, unsealSession } from '@/lib/session';
import type { SessionPayload } from '@/types/session';

export async function getServerSession(): Promise<SessionPayload | null> {
  const cookieStore = await cookies();
  const rawSession = cookieStore.get(SESSION_COOKIE_NAME)?.value;

  if (!rawSession) {
    return null;
  }

  return unsealSession(rawSession);
}

export async function requireServerSession(): Promise<SessionPayload> {
  const session = await getServerSession();

  if (!session) {
    redirect('/login');
  }

  return session;
}
