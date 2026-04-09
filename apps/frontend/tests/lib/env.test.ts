import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  getBackendApiBaseUrl,
  getPublicWaitingRoomDefaultQueueId,
  getSessionSecret,
} from '@/lib/env';

describe('getBackendApiBaseUrl', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    vi.stubEnv('BACKEND_API_BASE_URL', '');
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('returns env variable when set', () => {
    vi.stubEnv('BACKEND_API_BASE_URL', 'http://custom:9090');
    expect(getBackendApiBaseUrl()).toBe('http://custom:9090');
  });

  it('returns default when env is not set', () => {
    vi.stubEnv('BACKEND_API_BASE_URL', '');
    const result = getBackendApiBaseUrl();
    expect(result).toContain('backend');
  });
});

describe('getPublicWaitingRoomDefaultQueueId', () => {
  it('returns the default queue ID', () => {
    // The queue ID is captured at module load time from process.env
    const result = getPublicWaitingRoomDefaultQueueId();
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });

  it('trims whitespace', () => {
    const result = getPublicWaitingRoomDefaultQueueId();
    expect(result).toBe(result.trim());
  });
});

describe('getSessionSecret', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('returns env variable when set', () => {
    vi.stubEnv('FRONTEND_SESSION_SECRET', 'my-secret');
    expect(getSessionSecret()).toBe('my-secret');
  });

  it('returns dev default when not in production', () => {
    vi.stubEnv('FRONTEND_SESSION_SECRET', '');
    vi.stubEnv('NODE_ENV', 'development');
    const result = getSessionSecret();
    expect(result).toBe('rlapp-dev-session-secret-change-me');
  });
});
