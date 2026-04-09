import type { StaffRole } from '@/types/api';

export interface NavigationItem {
  href: string;
  label: string;
  description: string;
  roles: StaffRole[];
}

const routeAccess = new Map<string, StaffRole[]>([
  ['/', ['Receptionist', 'Cashier', 'Doctor', 'Supervisor', 'Support']],
  ['/health', ['Receptionist', 'Cashier', 'Doctor', 'Supervisor', 'Support']],
  ['/trajectory', ['Supervisor', 'Support']],
  ['/reception', ['Receptionist', 'Supervisor']],
  ['/waiting-room', ['Receptionist', 'Doctor', 'Supervisor']],
  ['/cashier', ['Cashier', 'Supervisor']],
  ['/medical', ['Doctor', 'Supervisor']],
  ['/staff', ['Supervisor']],
]);

export const navigationItems: NavigationItem[] = [
  {
    href: '/',
    label: 'Inicio',
    description: 'Vista general de la operacion y accesos disponibles.',
    roles: ['Receptionist', 'Cashier', 'Doctor', 'Supervisor', 'Support'],
  },
  {
    href: '/reception',
    label: 'Recepcion',
    description: 'Registrar llegada y dar inicio al flujo del paciente.',
    roles: ['Receptionist', 'Supervisor'],
  },
  {
    href: '/trajectory',
    label: 'Trayectoria',
    description: 'Consultar historial del paciente y soporte de reconstruccion.',
    roles: ['Supervisor', 'Support'],
  },
  {
    href: '/waiting-room',
    label: 'Sala de espera',
    description: 'Ver monitor, llamar pacientes y seguir el flujo hacia consulta.',
    roles: ['Receptionist', 'Doctor', 'Supervisor'],
  },
  {
    href: '/cashier',
    label: 'Caja',
    description: 'Gestionar turnos, pagos y ausencias en ventanilla.',
    roles: ['Cashier', 'Supervisor'],
  },
  {
    href: '/medical',
    label: 'Atencion medica',
    description: 'Operar consultorios, iniciar atenciones y registrar cierres.',
    roles: ['Doctor', 'Supervisor'],
  },
  {
    href: '/staff',
    label: 'Personal',
    description: 'Cambiar roles internos de forma administrativa.',
    roles: ['Supervisor'],
  },
  {
    href: '/health',
    label: 'Estado del sistema',
    description: 'Revisar salud, disponibilidad y dependencias del backend.',
    roles: ['Receptionist', 'Cashier', 'Doctor', 'Supervisor', 'Support'],
  },
];

export function canAccessPath(role: StaffRole, pathname: string): boolean {
  const matchedEntry = [...routeAccess.entries()]
    .sort((left, right) => right[0].length - left[0].length)
    .find(([route]) => pathname === route || pathname.startsWith(`${route}/`));

  if (!matchedEntry) {
    return true;
  }

  const [, allowedRoles] = matchedEntry;
  return allowedRoles.includes(role);
}

export function getVisibleNavigation(role: StaffRole): NavigationItem[] {
  return navigationItems.filter((item) => item.roles.includes(role));
}
