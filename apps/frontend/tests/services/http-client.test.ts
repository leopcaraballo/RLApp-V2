import { describe, it, expect, vi, type Mock } from 'vitest';
import { ApiError, httpRequest } from '@/services/http-client';
import type { ApiEnvelopeError } from '@/types/api';

describe('ApiError', () => {
  it('has correct properties', () => {
    const error = new ApiError(404, 'Not found', null);
    expect(error).toBeInstanceOf(Error);
    expect(error.name).toBe('ApiError');
    expect(error.status).toBe(404);
    expect(error.message).toBe('Not found');
    expect(error.payload).toBeNull();
  });

  it('carries error payload', () => {
    const payload = { message: 'Validation failed', code: 'VALIDATION_ERROR' };
    const error = new ApiError(400, 'Validation failed', payload as ApiEnvelopeError);
    expect(error.payload).toEqual(payload);
  });
});

describe('httpRequest', () => {
  it('returns parsed JSON on success', async () => {
    const mockResponse = { data: 'test' };
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: () => Promise.resolve(mockResponse),
    } as Response);

    const result = await httpRequest<{ data: string }>('http://localhost/test');
    expect(result).toEqual(mockResponse);
  });

  it('throws ApiError on non-ok response with JSON', async () => {
    const errorPayload = { message: 'Something went wrong' };
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: () => Promise.resolve(errorPayload),
    } as Response);

    await expect(httpRequest('http://localhost/fail')).rejects.toThrow(ApiError);

    try {
      await httpRequest('http://localhost/fail');
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError);
      expect((error as ApiError).status).toBe(500);
      expect((error as ApiError).message).toBe('Something went wrong');
    }
  });

  it('sends JSON body when json option is provided', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: () => Promise.resolve({ success: true }),
    } as Response);

    await httpRequest('http://localhost/test', {
      method: 'POST',
      json: { key: 'value' },
    });

    const [, options] = (globalThis.fetch as Mock).mock.calls[0];
    expect(options.body).toBe(JSON.stringify({ key: 'value' }));
    expect(options.headers.get('Content-Type')).toBe('application/json');
  });

  it('handles text responses', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      headers: new Headers({ 'content-type': 'text/plain' }),
      text: () => Promise.resolve('plain text'),
    } as Response);

    const result = await httpRequest<string>('http://localhost/text');
    expect(result).toBe('plain text');
  });

  it('extracts error message from payload.error field', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: () => Promise.resolve({ error: 'Bad request detail' }),
    } as Response);

    try {
      await httpRequest('http://localhost/fail');
    } catch (error) {
      expect((error as ApiError).message).toBe('Bad request detail');
    }
  });

  it('falls back to status-based message when no known field', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 503,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: () => Promise.resolve({ unknown: true }),
    } as Response);

    try {
      await httpRequest('http://localhost/fail');
    } catch (error) {
      expect((error as ApiError).message).toBe('Request failed with status 503');
    }
  });
});
