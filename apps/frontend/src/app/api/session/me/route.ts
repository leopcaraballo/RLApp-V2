import { NextResponse } from 'next/server';
import { getServerSession } from '@/lib/auth';

export async function GET() {
  const session = await getServerSession();

  if (!session) {
    return NextResponse.json(null);
  }

  return NextResponse.json({
    staffId: session.staffId,
    username: session.username,
    email: session.email,
    role: session.role,
    authenticatedAt: session.authenticatedAt,
    expiresAt: session.expiresAt,
  });
}
