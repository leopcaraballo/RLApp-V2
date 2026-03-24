import { NextRequest, NextResponse } from 'next/server';
import { getBackendApiBaseUrl } from '@/lib/env';
import { SESSION_COOKIE_NAME, sealSession } from '@/lib/session';
import type { AuthenticationResult, LoginRequest } from '@/types/api';

function buildWarnings(): string[] {
  return [
    'El backend acepta "identifier", pero hoy autentica realmente por username.',
    'El token devuelve capabilities vacias; la UI usa role como fuente principal de autorizacion visual.',
  ];
}

export async function POST(request: NextRequest) {
  const payload = (await request.json()) as LoginRequest;
  const correlationId = request.headers.get('X-Correlation-Id') ?? crypto.randomUUID();

  const backendResponse = await fetch(`${getBackendApiBaseUrl()}/api/staff/auth/login`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Correlation-Id': correlationId,
    },
    body: JSON.stringify(payload),
    cache: 'no-store',
  });

  const contentType = backendResponse.headers.get('content-type') ?? 'application/json';
  const responseBody = await backendResponse.text();

  if (!backendResponse.ok) {
    return new NextResponse(responseBody, {
      status: backendResponse.status,
      headers: {
        'Content-Type': contentType,
      },
    });
  }

  const result = JSON.parse(responseBody) as AuthenticationResult;
  const expiresAt = new Date(Date.now() + (result.expiresInSeconds ?? 3600) * 1000).toISOString();

  // Ensure role is always a valid string and cast to correct type
  const roleValue = (
    typeof result.role === 'string' ? result.role : String(result.role)
  ) as typeof result.role;

  const cookieValue = await sealSession({
    staffId: result.staffId,
    username: result.username,
    email: result.email,
    role: roleValue,
    authenticatedAt: result.authenticatedAt,
    accessToken: result.accessToken,
    expiresAt,
  });

  const response = NextResponse.json({
    session: {
      staffId: result.staffId,
      username: result.username,
      email: result.email,
      role: roleValue,
      authenticatedAt: result.authenticatedAt,
      expiresAt,
    },
    warnings: buildWarnings(),
  });

  response.cookies.set(SESSION_COOKIE_NAME, cookieValue, {
    httpOnly: true,
    sameSite: 'lax',
    secure: process.env.NODE_ENV === 'production',
    path: '/',
    maxAge: result.expiresInSeconds ?? 3600,
  });

  return response;
}
