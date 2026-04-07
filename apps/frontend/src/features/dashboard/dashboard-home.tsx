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
      <>
        <SectionIntro
          badge={getRoleDisplayName(session.role)}
          description="Tu inicio rapido depende del rol asignado. Desde aqui puedes abrir las vistas que necesitas sin entrar en detalles tecnicos del backend."
          eyebrow="Inicio operativo"
          title={`Bienvenido, ${session.username}`}
        />

        <ContractAlert
          title="Acceso al tablero agregado"
          items={[
            'El tablero agregado solo esta disponible para Supervisor y Soporte.',
            'Si tu rol no ve el tablero, usa los accesos de abajo para continuar con tu trabajo diario.',
            'Las vistas operativas siguen separadas por rol para evitar errores y acciones no autorizadas.',
          ]}
        />

        <section className="grid grid--two">
          {quickActions.map((surface) => (
            <article className="panel" key={surface.href}>
              <div className="panel__header">
                <div>
                  <div className="panel__eyebrow">Vista disponible</div>
                  <h2>{surface.label}</h2>
                  <p>{surface.description}</p>
                </div>
                <StatusBadge tone="info">Acceso activo</StatusBadge>
              </div>
              <div className="form-actions" style={{ marginTop: '16px' }}>
                <Link className="ghost-button" href={surface.href}>
                  Abrir vista
                </Link>
              </div>
            </article>
          ))}
        </section>

        <HealthPanels />
      </>
    );
  }

  return (
    <>
      <SectionIntro
        badge={getRoleDisplayName(session.role)}
        description="Esta vista resume la operacion con datos persistidos y se actualiza de forma segura cuando el sistema detecta cambios."
        eyebrow="Visibilidad sincronizada"
        title={`Resumen operativo de ${session.username}`}
      />

      <section className="grid grid--three">
        {quickActions.map((item) => (
          <article className="panel" key={item.href}>
            <div className="panel__eyebrow">Atajo frecuente</div>
            <h2>{item.label}</h2>
            <p>{item.description}</p>
            <div className="form-actions" style={{ marginTop: '16px' }}>
              <Link className="ghost-button" href={item.href}>
                Ir ahora
              </Link>
            </div>
          </article>
        ))}
      </section>

      <section className="panel">
        <div className="panel__header">
          <div>
            <div className="panel__eyebrow">Sincronizacion</div>
            <h2>Tablero actualizado desde datos persistidos</h2>
            <p>
              Los indicadores se refrescan desde proyecciones persistidas. El canal en tiempo real
              solo avisa que hay cambios y dispara la recarga segura.
            </p>
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
        <section className="panel">
          <div className="panel__header">
            <div>
              <div className="panel__eyebrow">Cargando tablero</div>
              <h2>Consultando el ultimo estado disponible</h2>
              <p>
                Estamos trayendo los indicadores mas recientes para que trabajes sobre datos
                consistentes.
              </p>
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
          <section className="grid grid--three">
            <article className="panel">
              <div className="panel__eyebrow">Pacientes en espera</div>
              <div className="metric-value">
                {formatDisplayNumber(dashboardQuery.data.currentWaitingCount)}
              </div>
              <p className="metric-caption">Pacientes que siguen en estados visibles de espera.</p>
            </article>

            <article className="panel">
              <div className="panel__eyebrow">Espera promedio</div>
              <div className="metric-value">
                {formatDisplayMinutes(dashboardQuery.data.averageWaitTimeMinutes)}
              </div>
              <p className="metric-caption">Tiempo medio de espera segun las colas activas.</p>
            </article>

            <article className="panel">
              <div className="panel__eyebrow">Retraso de proyeccion</div>
              <div className="metric-value">
                {formatDisplayNumber(dashboardQuery.data.projectionLagSeconds)}s
              </div>
              <p className="metric-caption">
                Diferencia entre el ultimo evento persistido y este tablero.
              </p>
            </article>

            <article className="panel">
              <div className="panel__eyebrow">Pacientes del dia</div>
              <div className="metric-value">
                {formatDisplayNumber(dashboardQuery.data.totalPatientsToday)}
              </div>
              <p className="metric-caption">Volumen acumulado del dia en el tablero operativo.</p>
            </article>

            <article className="panel">
              <div className="panel__eyebrow">Atenciones cerradas</div>
              <div className="metric-value">
                {formatDisplayNumber(dashboardQuery.data.totalCompleted)}
              </div>
              <p className="metric-caption">
                Procesos que ya quedaron materializados como cerrados.
              </p>
            </article>

            <article className="panel">
              <div className="panel__eyebrow">Consultorios activos</div>
              <div className="metric-value">
                {formatDisplayNumber(dashboardQuery.data.activeRooms)}
              </div>
              <p className="metric-caption">
                Consultorios visibles como activos en la operacion actual.
              </p>
            </article>
          </section>

          <section className="grid grid--two">
            <article className="panel">
              <div className="panel__header">
                <div>
                  <div className="panel__eyebrow">Colas visibles</div>
                  <h2>Colas que alimentan el tablero</h2>
                  <p>
                    Cada fila proviene de una proyeccion persistida y ayuda a construir el resumen
                    general.
                  </p>
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
                      <p>{formatDisplayMinutes(queue.averageWaitTimeMinutes)} de espera promedio</p>
                    </div>
                    <div style={{ textAlign: 'right' }}>
                      <strong>{formatDisplayNumber(queue.totalPending)} pendientes</strong>
                      <p>Actualizado {formatDisplayDateTime(queue.lastUpdatedAt)}</p>
                    </div>
                  </div>
                ))}
              </div>
            </article>

            <article className="panel">
              <div className="panel__header">
                <div>
                  <div className="panel__eyebrow">Estados visibles</div>
                  <h2>Distribucion por estado</h2>
                  <p>
                    Los conteos provienen del mismo conjunto de proyecciones que usa el monitor.
                  </p>
                </div>
                <StatusBadge tone={projectionTone(dashboardQuery.data.projectionLagSeconds)}>
                  {dashboardQuery.data.projectionLagSeconds <= 5 ? 'Lag saludable' : 'Revisar lag'}
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
    </>
  );
}
