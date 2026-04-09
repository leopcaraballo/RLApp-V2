import { describe, it, expect } from 'vitest';
import { canAccessPath, getVisibleNavigation } from '@/lib/authorization';

describe('canAccessPath', () => {
  it('allows Supervisor to access all routes', () => {
    const routes = [
      '/',
      '/health',
      '/trajectory',
      '/reception',
      '/waiting-room',
      '/cashier',
      '/medical',
      '/staff',
    ];
    for (const route of routes) {
      expect(canAccessPath('Supervisor', route)).toBe(true);
    }
  });

  it('denies Receptionist access to /cashier', () => {
    expect(canAccessPath('Receptionist', '/cashier')).toBe(false);
  });

  it('denies Cashier access to /medical', () => {
    expect(canAccessPath('Cashier', '/medical')).toBe(false);
  });

  it('denies Doctor access to /staff', () => {
    expect(canAccessPath('Doctor', '/staff')).toBe(false);
  });

  it('allows Receptionist to access /reception', () => {
    expect(canAccessPath('Receptionist', '/reception')).toBe(true);
  });

  it('allows Cashier to access /cashier', () => {
    expect(canAccessPath('Cashier', '/cashier')).toBe(true);
  });

  it('allows Doctor to access /medical', () => {
    expect(canAccessPath('Doctor', '/medical')).toBe(true);
  });

  it('allows access to unknown paths', () => {
    expect(canAccessPath('Receptionist', '/unknown-path')).toBe(true);
  });

  it('matching sub-paths of registered routes', () => {
    expect(canAccessPath('Doctor', '/medical/rooms')).toBe(true);
    expect(canAccessPath('Receptionist', '/medical/rooms')).toBe(false);
  });
});

describe('getVisibleNavigation', () => {
  it('returns only role-authorized items for Receptionist', () => {
    const items = getVisibleNavigation('Receptionist');
    const hrefs = items.map((item) => item.href);

    expect(hrefs).toContain('/');
    expect(hrefs).toContain('/reception');
    expect(hrefs).toContain('/waiting-room');
    expect(hrefs).toContain('/health');
    expect(hrefs).not.toContain('/cashier');
    expect(hrefs).not.toContain('/staff');
  });

  it('returns all navigation items for Supervisor', () => {
    const items = getVisibleNavigation('Supervisor');
    expect(items.length).toBeGreaterThanOrEqual(8);
  });

  it('returns only home and health for Support', () => {
    const items = getVisibleNavigation('Support');
    const hrefs = items.map((item) => item.href);

    expect(hrefs).toContain('/');
    expect(hrefs).toContain('/health');
    expect(hrefs).toContain('/trajectory');
    expect(hrefs).not.toContain('/reception');
    expect(hrefs).not.toContain('/cashier');
  });
});
