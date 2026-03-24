'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { startTransition } from 'react';
import { getVisibleNavigation } from '@/lib/authorization';
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
          <div className="brand-card__eyebrow">RLApp frontend</div>
          <h1>Operational console</h1>
          <p>Next.js 16 app aligned to the backend commands that really exist today.</p>
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
            <div className="topbar__eyebrow">Authenticated staff</div>
            <strong>{session.username}</strong>
            <span>
              {session.role} · expires {new Date(session.expiresAt).toLocaleString()}
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
            Logout
          </button>
        </header>

        <main className="content">{children}</main>
      </div>
    </div>
  );
}
