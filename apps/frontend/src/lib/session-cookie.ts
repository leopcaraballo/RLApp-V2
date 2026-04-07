import type { NextRequest } from 'next/server';

const LOCAL_HOSTNAMES = new Set(['localhost', '127.0.0.1', '::1']);

export function shouldUseSecureSessionCookie(
  request: Pick<NextRequest, 'headers' | 'nextUrl'>
): boolean {
  if (LOCAL_HOSTNAMES.has(request.nextUrl.hostname)) {
    return false;
  }

  const forwardedProto = request.headers.get('x-forwarded-proto');
  if (forwardedProto) {
    return forwardedProto.split(',')[0]?.trim() === 'https';
  }

  return request.nextUrl.protocol === 'https:' || process.env.NODE_ENV === 'production';
}
