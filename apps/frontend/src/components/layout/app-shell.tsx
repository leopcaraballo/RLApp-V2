'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { startTransition } from 'react';
import { getVisibleNavigation } from '@/lib/authorization';
import { formatDisplayDateTime, getRoleDisplayName } from '@/lib/display-text';
import { rlappApi } from '@/services/rlapp-api';
import type { SessionUser } from '@/types/session';

interface AppShellProps {
  session: SessionUser;
  children: React.ReactNode;
}

export function AppShell({ session, children }: AppShellProps) {
  const pathname = usePathname();
  const router = useRouter();
  const navigation = getVisibleNavigation(session.role);

  return (
    <div className="shell">
      <aside className="shell__sidebar">
        <div className="brand-card">
          <div className="brand-card__eyebrow">Plataforma de staff</div>
          <h1>RLApp Clinical Orchestrator</h1>
          <p>
            Una sola vista para registrar pacientes, seguir la operacion y revisar el estado del
            sistema.
          </p>
          <p>Orquestador de Trayectorias Clínicas Sincronizadas</p>
        </div>

        <nav className="shell__nav">
          {navigation.map((item) => {
            const isActive = pathname === item.href;
            return (
              <Link
                aria-current={isActive ? 'page' : undefined}
                className={`shell__nav-item ${isActive ? 'shell__nav-item--active' : ''}`}
                href={item.href}
                key={item.href}
              >
                <strong>{item.label}</strong>
                <span>{item.description}</span>
              </Link>
            );
          })}
        </nav>
      </aside>

      <div className="shell__content">
        <header className="topbar">
          <div>
            <div className="topbar__eyebrow">Sesion activa</div>
            <strong>{session.username}</strong>
            <span>
              {getRoleDisplayName(session.role)} · vence {formatDisplayDateTime(session.expiresAt)}
            </span>
          </div>

          <button
            className="ghost-button"
            onClick={() => {
              startTransition(async () => {
                await rlappApi.logout();
                router.replace('/login');
                router.refresh();
              });
            }}
            type="button"
          >
            Cerrar sesion
          </button>
        </header>

        <main className="content">{children}</main>
      </div>
    </div>
  );
}
