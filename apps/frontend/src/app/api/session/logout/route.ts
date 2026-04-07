import { NextRequest, NextResponse } from 'next/server';
import { shouldUseSecureSessionCookie } from '@/lib/session-cookie';
import { SESSION_COOKIE_NAME } from '@/lib/session';

export async function POST(request: NextRequest) {
  const response = NextResponse.json({ success: true });
  response.cookies.set(SESSION_COOKIE_NAME, '', {
    httpOnly: true,
    sameSite: 'lax',
    secure: shouldUseSecureSessionCookie(request),
    path: '/',
    maxAge: 0,
  });

  return response;
}
