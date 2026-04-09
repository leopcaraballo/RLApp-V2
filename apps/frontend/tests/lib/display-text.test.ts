import { describe, it, expect } from 'vitest';
import {
  getRoleDisplayName,
  getOperationalStatusDisplayName,
  getHealthStatusDisplayName,
  getJournalStatusDisplayName,
  formatDisplayNumber,
} from '@/lib/display-text';

describe('getRoleDisplayName', () => {
  it('returns correct Spanish name for known roles', () => {
    expect(getRoleDisplayName('Receptionist')).toBe('Recepcion');
    expect(getRoleDisplayName('Cashier')).toBe('Caja');
    expect(getRoleDisplayName('Doctor')).toBe('Medico');
    expect(getRoleDisplayName('Supervisor')).toBe('Supervisor');
    expect(getRoleDisplayName('Support')).toBe('Soporte');
  });

  it('returns role as-is for unknown role', () => {
    expect(getRoleDisplayName('UnknownRole' as never)).toBe('UnknownRole');
  });
});

describe('getOperationalStatusDisplayName', () => {
  it('maps known operational statuses', () => {
    expect(getOperationalStatusDisplayName('Waiting')).toBe('En espera');
    expect(getOperationalStatusDisplayName('AtCashier')).toBe('En caja');
    expect(getOperationalStatusDisplayName('InConsultation')).toBe('En consulta');
    expect(getOperationalStatusDisplayName('Completed')).toBe('Finalizado');
    expect(getOperationalStatusDisplayName('Absent')).toBe('Ausente');
  });

  it('maps trajectory states', () => {
    expect(getOperationalStatusDisplayName('TrayectoriaActiva')).toBe('Trayectoria activa');
    expect(getOperationalStatusDisplayName('TrayectoriaFinalizada')).toBe('Trayectoria finalizada');
    expect(getOperationalStatusDisplayName('TrayectoriaCancelada')).toBe('Trayectoria cancelada');
  });

  it('returns status as-is for unknown status', () => {
    expect(getOperationalStatusDisplayName('SomeUnknown')).toBe('SomeUnknown');
  });
});

describe('getHealthStatusDisplayName', () => {
  it('maps known health statuses', () => {
    expect(getHealthStatusDisplayName('healthy')).toBe('Saludable');
    expect(getHealthStatusDisplayName('degraded')).toBe('Degradado');
    expect(getHealthStatusDisplayName('unhealthy')).toBe('Con fallas');
  });

  it('is case-insensitive', () => {
    expect(getHealthStatusDisplayName('Healthy')).toBe('Saludable');
    expect(getHealthStatusDisplayName('DEGRADED')).toBe('Degradado');
  });

  it('returns "Sin datos" for undefined/null', () => {
    expect(getHealthStatusDisplayName(undefined)).toBe('Sin datos');
  });

  it('returns unknown status as-is', () => {
    expect(getHealthStatusDisplayName('critical')).toBe('critical');
  });
});

describe('getJournalStatusDisplayName', () => {
  it('maps success and error', () => {
    expect(getJournalStatusDisplayName('success')).toBe('Correcto');
    expect(getJournalStatusDisplayName('error')).toBe('Error');
  });
});

describe('formatDisplayNumber', () => {
  it('formats numbers with locale separators', () => {
    const result = formatDisplayNumber(1234567);
    expect(result).toBeTruthy();
    // es-CO uses period as thousands separator
    expect(result).toContain('.');
  });

  it('formats zero', () => {
    expect(formatDisplayNumber(0)).toBe('0');
  });
});
