import type { StaffRole } from '@/types/api';

const locale = 'es-CO';

const roleDisplayNames: Record<StaffRole, string> = {
  Receptionist: 'Recepcion',
  Cashier: 'Caja',
  Doctor: 'Medico',
  Supervisor: 'Supervisor',
  Support: 'Soporte',
};

const statusDisplayNames: Record<string, string> = {
  Waiting: 'En espera',
  AtCashier: 'En caja',
  PaymentPending: 'Pago pendiente',
  WaitingForConsultation: 'Esperando consulta',
  Called: 'Llamado',
  InConsultation: 'En consulta',
  Completed: 'Finalizado',
  Absent: 'Ausente',
  TrayectoriaActiva: 'Trayectoria activa',
  TrayectoriaFinalizada: 'Trayectoria finalizada',
  TrayectoriaCancelada: 'Trayectoria cancelada',
};

const healthStatusDisplayNames: Record<string, string> = {
  healthy: 'Saludable',
  degraded: 'Degradado',
  unhealthy: 'Con fallas',
  unknown: 'Sin datos',
};

export function getRoleDisplayName(role: StaffRole): string {
  return roleDisplayNames[role] ?? role;
}

export function getOperationalStatusDisplayName(status: string): string {
  return statusDisplayNames[status] ?? status;
}

export function getHealthStatusDisplayName(status?: string): string {
  if (!status) {
    return healthStatusDisplayNames.unknown;
  }

  return healthStatusDisplayNames[status.toLowerCase()] ?? status;
}

export function getJournalStatusDisplayName(status: 'success' | 'error'): string {
  return status === 'success' ? 'Correcto' : 'Error';
}

export function formatDisplayDateTime(value: string | Date | null | undefined): string {
  if (!value) {
    return 'Pendiente';
  }

  const date = value instanceof Date ? value : new Date(value);
  return new Intl.DateTimeFormat(locale, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date);
}

export function formatDisplayNumber(value: number): string {
  return new Intl.NumberFormat(locale).format(value);
}

export function formatDisplayMinutes(value: number): string {
  return `${new Intl.NumberFormat(locale, {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  }).format(value)} min`;
}
