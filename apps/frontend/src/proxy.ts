import { type NextRequest, NextResponse } from 'next/server';
import { canAccessPath } from '@/lib/authorization';
import { SESSION_COOKIE_NAME, unsealSession } from '@/lib/session';

const PUBLIC_PATHS = new Set(['/login']);

export async function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;

  if (PUBLIC_PATHS.has(pathname)) {
    const existingSession = request.cookies.get(SESSION_COOKIE_NAME)?.value;

    if (!existingSession) {
      return NextResponse.next();
    }

    const session = await unsealSession(existingSession);
    if (session) {
      return NextResponse.redirect(new URL('/', request.url));
    }

    return NextResponse.next();
  }

  const rawSession = request.cookies.get(SESSION_COOKIE_NAME)?.value;
  if (!rawSession) {
    return NextResponse.redirect(new URL('/login', request.url));
  }

  const session = await unsealSession(rawSession);
  if (!session) {
    const response = NextResponse.redirect(new URL('/login', request.url));
    response.cookies.delete(SESSION_COOKIE_NAME);
    return response;
  }

  if (!canAccessPath(session.role, pathname)) {
    return NextResponse.redirect(new URL('/', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico|public).*)'],
};
