'use client';

import Link from 'next/link';
import { HealthPanels } from '@/components/health/health-panels';
import { ContractAlert } from '@/components/shared/contract-alert';
import { SectionIntro } from '@/components/shared/section-intro';
import { StatusBadge } from '@/components/shared/status-badge';
import { getVisibleNavigation } from '@/lib/authorization';
import {
  formatDisplayDateTime,
  formatDisplayMinutes,
  formatDisplayNumber,
  getOperationalStatusDisplayName,
  getRoleDisplayName,
} from '@/lib/display-text';
import { getRealtimeLabel, getRealtimeTone } from '@/lib/realtime-status';
import { useOperationsDashboard } from '@/hooks/use-operational-read-models';
import { useOperationalRealtime } from '@/hooks/use-operational-realtime';
import { ApiError } from '@/services/http-client';
import type { StaffRole } from '@/types/api';
import type { SessionUser } from '@/types/session';

const DASHBOARD_ROLES = new Set<StaffRole>(['Supervisor', 'Support']);

function readErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'No fue posible cargar el tablero operativo.';
}

function projectionTone(lagSeconds: number) {
  if (lagSeconds <= 5) {
    return 'success' as const;
  }

  if (lagSeconds <= 15) {
    return 'warning' as const;
  }

  return 'danger' as const;
}

export function DashboardHome({ session }: { session: SessionUser }) {
  const canViewDashboard = DASHBOARD_ROLES.has(session.role);
  const quickActions = getVisibleNavigation(session.role).filter((item) => item.href !== '/');
  const dashboardQuery = useOperationsDashboard(canViewDashboard);
  const realtime = useOperationalRealtime({
    role: session.role,
    enabled: canViewDashboard,
    dashboard: canViewDashboard,
  });

  if (!canViewDashboard) {
    return (
      <div className="clinical-space">
        <SectionIntro
          badge={getRoleDisplayName(session.role)}
          eyebrow="Inicio operativo"
          title="Inicio"
        />

        <ContractAlert
          title="Acceso al tablero agregado"
          items={[
            'El tablero agregado solo esta disponible para Supervisor y Soporte.',
            'Si tu rol no ve el tablero, usa los accesos de abajo para continuar con tu trabajo diario.',
            'Las vistas operativas siguen separadas por rol para evitar errores y acciones no autorizadas.',
          ]}
        />

        <section className="grid grid--two clinical-grid">
          {quickActions.map((surface) => (
            <article className="panel clinical-panel shortcut-card" key={surface.href}>
              <div className="panel__header">
                <div>
                  <div className="panel__eyebrow">Acceso</div>
                  <h2>{surface.label}</h2>
                </div>
                <StatusBadge tone="info">Activo</StatusBadge>
              </div>
              <div className="form-actions shortcut-card__action">
                <Link className="ghost-button" href={surface.href}>
                  Abrir
                </Link>
              </div>
            </article>
          ))}
        </section>

        <HealthPanels />
      </div>
    );
  }

  return (
    <div className="clinical-space">
      <SectionIntro
        badge={getRoleDisplayName(session.role)}
        eyebrow="Visibilidad sincronizada"
        title="Panorama operativo"
      />

      <section className="grid grid--three clinical-grid">
        {quickActions.map((item) => (
          <article className="panel clinical-panel shortcut-card" key={item.href}>
            <div className="panel__eyebrow">Atajo frecuente</div>
            <h2>{item.label}</h2>
            <div className="form-actions shortcut-card__action">
              <Link className="ghost-button" href={item.href}>
                Abrir
              </Link>
            </div>
          </article>
        ))}
      </section>

      <section className="panel clinical-panel clinical-panel--hero">
        <div className="panel__header">
          <div>
            <div className="panel__eyebrow">Sincronizacion</div>
            <h2>Estado en vivo</h2>
          </div>
          <StatusBadge tone={getRealtimeTone(realtime.connectionState)}>
            {getRealtimeLabel(realtime.connectionState)}
          </StatusBadge>
        </div>
        <div className="panel__meta">
          <span>
            Ultimo evento:{' '}
            {realtime.lastEvent
              ? `${realtime.lastEvent.eventType} · ${formatDisplayDateTime(realtime.lastEvent.occurredAt)}`
              : 'Aun no se ha recibido una invalidacion.'}
          </span>
        </div>
      </section>

      {dashboardQuery.isPending ? (
        <section className="panel clinical-panel clinical-panel--soft">
          <div className="panel__header">
            <div>
              <div className="panel__eyebrow">Cargando</div>
              <h2>Consultando tablero</h2>
            </div>
            <StatusBadge tone="warning">Cargando</StatusBadge>
          </div>
        </section>
      ) : null}

      {dashboardQuery.isError ? (
        <ContractAlert
          title="No se pudo abrir el tablero operativo"
          items={[readErrorMessage(dashboardQuery.error)]}
        />
      ) : null}

      {dashboardQuery.data ? (
        <>
          <section className="grid grid--three clinical-grid">
            <article className="panel clinical-panel metric-panel">
              <div className="panel__eyebrow">En espera</div>
              <div className="metric-value">
                {formatDisplayNumber(dashboardQuery.data.currentWaitingCount)}
              </div>
            </article>

            <article className="panel clinical-panel metric-panel">
              <div className="panel__eyebrow">Espera promedio</div>
              <div className="metric-value">
                {formatDisplayMinutes(dashboardQuery.data.averageWaitTimeMinutes)}
              </div>
            </article>

            <article className="panel clinical-panel metric-panel">
              <div className="panel__eyebrow">Lag</div>
              <div className="metric-value">
                {formatDisplayNumber(dashboardQuery.data.projectionLagSeconds)}s
              </div>
            </article>

            <article className="panel clinical-panel metric-panel">
              <div className="panel__eyebrow">Pacientes del dia</div>
              <div className="metric-value">
                {formatDisplayNumber(dashboardQuery.data.totalPatientsToday)}
              </div>
            </article>

            <article className="panel clinical-panel metric-panel">
              <div className="panel__eyebrow">Cierres</div>
              <div className="metric-value">
                {formatDisplayNumber(dashboardQuery.data.totalCompleted)}
              </div>
            </article>

            <article className="panel clinical-panel metric-panel">
              <div className="panel__eyebrow">Consultorios en atencion</div>
              <div className="metric-value">
                {formatDisplayNumber(dashboardQuery.data.activeRooms)}
              </div>
            </article>
          </section>

          <section className="grid grid--two clinical-grid">
            <article className="panel clinical-panel clinical-panel--soft">
              <div className="panel__header">
                <div>
                  <div className="panel__eyebrow">Colas</div>
                  <h2>Panorama por cola</h2>
                </div>
                <StatusBadge tone="info">
                  {dashboardQuery.data.queueSnapshots.length} colas
                </StatusBadge>
              </div>

              <div className="data-list">
                {dashboardQuery.data.queueSnapshots.map((queue) => (
                  <div className="data-list__row" key={queue.queueId}>
                    <div>
                      <strong>{queue.queueId}</strong>
                      <span className="compact-meta">
                        {formatDisplayMinutes(queue.averageWaitTimeMinutes)}
                      </span>
                    </div>
                    <div style={{ textAlign: 'right' }}>
                      <strong>{formatDisplayNumber(queue.totalPending)} pendientes</strong>
                      <span className="compact-meta">
                        {formatDisplayDateTime(queue.lastUpdatedAt)}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </article>

            <article className="panel clinical-panel clinical-panel--soft">
              <div className="panel__header">
                <div>
                  <div className="panel__eyebrow">Estados</div>
                  <h2>Distribucion</h2>
                </div>
                <StatusBadge tone={projectionTone(dashboardQuery.data.projectionLagSeconds)}>
                  {dashboardQuery.data.projectionLagSeconds <= 5 ? 'Saludable' : 'Revisar'}
                </StatusBadge>
              </div>

              <div className="data-list">
                {dashboardQuery.data.statusBreakdown.map((entry) => (
                  <div className="data-list__row" key={entry.status}>
                    <strong>{getOperationalStatusDisplayName(entry.status)}</strong>
                    <span>{formatDisplayNumber(entry.total)}</span>
                  </div>
                ))}
              </div>

              <div className="panel__meta">
                <span>Generado {formatDisplayDateTime(dashboardQuery.data.generatedAt)}</span>
              </div>
            </article>
          </section>
        </>
      ) : null}

      <HealthPanels />
    </div>
  );
}
