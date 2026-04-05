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
    label: 'Overview',
    description: 'Resumen operativo y riesgos del contrato backend.',
    roles: ['Receptionist', 'Cashier', 'Doctor', 'Supervisor', 'Support'],
  },
  {
    href: '/reception',
    label: 'Reception',
    description: 'Registro inicial y alias operativo de llegada.',
    roles: ['Receptionist', 'Supervisor'],
  },
  {
    href: '/trajectory',
    label: 'Trajectory',
    description: 'Consulta longitudinal y rebuild controlado para soporte.',
    roles: ['Supervisor', 'Support'],
  },
  {
    href: '/waiting-room',
    label: 'Waiting Room',
    description: 'Check-in y flujo de consulta sobre la cola.',
    roles: ['Receptionist', 'Doctor', 'Supervisor'],
  },
  {
    href: '/cashier',
    label: 'Cashier',
    description: 'Caja, pago y ausencias en ventanilla.',
    roles: ['Cashier', 'Supervisor'],
  },
  {
    href: '/medical',
    label: 'Medical',
    description: 'Consultorios, finalización y ausencias clínicas.',
    roles: ['Doctor', 'Supervisor'],
  },
  {
    href: '/staff',
    label: 'Staff',
    description: 'Gestión administrativa de roles internos.',
    roles: ['Supervisor'],
  },
  {
    href: '/health',
    label: 'Health',
    description: 'Health, readiness y liveness del backend.',
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
