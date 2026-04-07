import { NextRequest, NextResponse } from 'next/server';
import { getServerSession } from '@/lib/auth';
import { getBackendApiBaseUrl } from '@/lib/env';

const publicProxyPaths = new Set(['health', 'health/live', 'health/ready']);

function normalizePath(parts: string[]): string {
  const normalizedPath = parts.join('/');
  return publicProxyPaths.has(normalizedPath) ? `/${normalizedPath}` : `/api/${normalizedPath}`;
}

async function proxyRequest(request: NextRequest, path: string[]) {
  const normalizedPath = path.join('/');
  const isPublicRoute = publicProxyPaths.has(normalizedPath);
  const session = isPublicRoute ? null : await getServerSession();

  if (!isPublicRoute && !session) {
    return NextResponse.json(
      { message: 'Debes iniciar sesion para ejecutar operaciones del backend.' },
      { status: 401 }
    );
  }

  const backendUrl = new URL(
    `${getBackendApiBaseUrl()}${normalizePath(path)}${request.nextUrl.search}`
  );

  const headers = new Headers();
  headers.set('X-Correlation-Id', request.headers.get('X-Correlation-Id') ?? crypto.randomUUID());

  if (request.method !== 'GET') {
    headers.set(
      'X-Idempotency-Key',
      request.headers.get('X-Idempotency-Key') ?? crypto.randomUUID()
    );
  }

  if (session) {
    headers.set('Authorization', `Bearer ${session.accessToken}`);
  }

  let body: string | undefined;
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    body = await request.text();
    if (body.length > 0) {
      headers.set('Content-Type', 'application/json');
    }
  }

  const backendResponse = await fetch(backendUrl, {
    method: request.method,
    headers,
    body,
    cache: 'no-store',
  });

  const responseHeaders = new Headers();
  const contentType = backendResponse.headers.get('content-type');
  if (contentType) {
    responseHeaders.set('Content-Type', contentType);
  }

  const wwwAuthenticate = backendResponse.headers.get('www-authenticate');
  if (wwwAuthenticate) {
    responseHeaders.set('www-authenticate', wwwAuthenticate);
  }

  return new NextResponse(await backendResponse.text(), {
    status: backendResponse.status,
    headers: responseHeaders,
  });
}

export async function GET(request: NextRequest, context: { params: Promise<{ path: string[] }> }) {
  const { path } = await context.params;
  return proxyRequest(request, path);
}

export async function POST(request: NextRequest, context: { params: Promise<{ path: string[] }> }) {
  const { path } = await context.params;
  return proxyRequest(request, path);
}
