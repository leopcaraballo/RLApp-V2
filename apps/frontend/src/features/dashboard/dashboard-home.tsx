'use client';

import { HealthPanels } from '@/components/health/health-panels';
import { ContractAlert } from '@/components/shared/contract-alert';
import { SectionIntro } from '@/components/shared/section-intro';
import { StatusBadge } from '@/components/shared/status-badge';
import { useOperationsDashboard } from '@/hooks/use-operational-read-models';
import { useOperationalRealtime } from '@/hooks/use-operational-realtime';
import { ApiError } from '@/services/http-client';
import type { StaffRole } from '@/types/api';
import type { SessionUser } from '@/types/session';

const DASHBOARD_ROLES = new Set<StaffRole>(['Supervisor', 'Support']);

const roleBoundaryCopy: Record<string, { title: string; detail: string; status: string }[]> = {
  Receptionist: [
    {
      title: 'Waiting Room Monitor',
      detail: 'Use the persisted queue monitor from Waiting Room to follow live reception load.',
      status: 'Projection-backed',
    },
    {
      title: 'Reception Flow',
      detail: 'Arrival registration and check-in remain the primary workflow for this role.',
      status: 'Command access',
    },
  ],
  Doctor: [
    {
      title: 'Consultation Queue',
      detail:
        'Waiting Room keeps claim-next and call-patient so turns remain queued and visible before consultation starts.',
      status: 'Command access',
    },
    {
      title: 'Medical Operations',
      detail:
        'Medical now owns call-next shortcut, start-consultation, room lifecycle, and consultation completion.',
      status: 'Command access',
    },
  ],
  Cashier: [
    {
      title: 'Cashier Operations',
      detail: 'Payment validation and cashier absence stay bounded to the Cashier workspace.',
      status: 'Command access',
    },
    {
      title: 'Operational Dashboard',
      detail: 'The aggregated dashboard is restricted to Supervisor and Support by contract.',
      status: 'Role-restricted',
    },
  ],
};

function readErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'The operational dashboard could not be loaded.';
}

function formatTimestamp(value: string): string {
  return new Date(value).toLocaleString();
}

function formatMetric(value: number): string {
  return new Intl.NumberFormat().format(value);
}

function formatMinutes(value: number): string {
  return `${value.toFixed(1)} min`;
}

function realtimeTone(state: 'idle' | 'connecting' | 'live' | 'reconnecting') {
  if (state === 'live') {
    return 'success' as const;
  }

  if (state === 'reconnecting' || state === 'connecting') {
    return 'warning' as const;
  }

  return 'neutral' as const;
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
  const dashboardQuery = useOperationsDashboard(canViewDashboard);
  const realtime = useOperationalRealtime({
    role: session.role,
    enabled: canViewDashboard,
    dashboard: canViewDashboard,
  });

  if (!canViewDashboard) {
    const operationalSurfaces = roleBoundaryCopy[session.role] ?? [
      {
        title: 'Operational Dashboard',
        detail: 'This aggregated snapshot is available only to Supervisor and Support.',
        status: 'Role-restricted',
      },
    ];

    return (
      <>
        <SectionIntro
          badge={session.role}
          description="This workspace keeps route access, but the aggregated operational dashboard is restricted by contract to Supervisor and Support."
          eyebrow="Operational overview"
          title={`Welcome, ${session.username}`}
        />

        <ContractAlert
          title="Dashboard boundary"
          items={[
            'The persisted dashboard snapshot is authorized only for Supervisor and Support.',
            'Reception operational visibility now lives in the Waiting Room monitor for eligible roles.',
            'Cashier and doctor workflows stay available through their bounded command surfaces.',
          ]}
        />

        <section className="grid grid--two">
          {operationalSurfaces.map((surface) => (
            <article className="panel" key={surface.title}>
              <div className="panel__header">
                <div>
                  <div className="panel__eyebrow">Available surface</div>
                  <h2>{surface.title}</h2>
                  <p>{surface.detail}</p>
                </div>
                <StatusBadge tone="info">{surface.status}</StatusBadge>
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
        badge={session.role}
        description="This home view now consumes the persisted operations dashboard and stays in sync through a same-origin invalidation stream mediated by the BFF."
        eyebrow="Synchronized visibility"
        title={`Operations overview for ${session.username}`}
      />

      <section className="panel">
        <div className="panel__header">
          <div>
            <div className="panel__eyebrow">Realtime channel</div>
            <h2>Projection-backed dashboard sync</h2>
            <p>
              Dashboard cards refresh from persisted snapshots, while the stream only carries
              invalidation metadata.
            </p>
          </div>
          <StatusBadge tone={realtimeTone(realtime.connectionState)}>
            {realtime.connectionState === 'live' ? 'Live sync' : 'Reconnecting'}
          </StatusBadge>
        </div>
        <div className="panel__meta">
          <span>
            Last event:{' '}
            {realtime.lastEvent
              ? `${realtime.lastEvent.eventType} at ${formatTimestamp(realtime.lastEvent.occurredAt)}`
              : 'Waiting for the first invalidation.'}
          </span>
        </div>
      </section>

      {dashboardQuery.isPending ? (
        <section className="panel">
          <div className="panel__header">
            <div>
              <div className="panel__eyebrow">Loading snapshot</div>
              <h2>Fetching persisted operational metrics</h2>
              <p>The dashboard is waiting for the latest materialized snapshot.</p>
            </div>
            <StatusBadge tone="warning">Loading</StatusBadge>
          </div>
        </section>
      ) : null}

      {dashboardQuery.isError ? (
        <ContractAlert
          title="Operational dashboard unavailable"
          items={[readErrorMessage(dashboardQuery.error)]}
        />
      ) : null}

      {dashboardQuery.data ? (
        <>
          <section className="grid grid--three">
            <article className="panel">
              <div className="panel__eyebrow">Current waiting</div>
              <div className="metric-value">
                {formatMetric(dashboardQuery.data.currentWaitingCount)}
              </div>
              <p className="metric-caption">Patients still visible in persisted waiting states.</p>
            </article>

            <article className="panel">
              <div className="panel__eyebrow">Average wait</div>
              <div className="metric-value">
                {formatMinutes(dashboardQuery.data.averageWaitTimeMinutes)}
              </div>
              <p className="metric-caption">Projection-backed average across active queues.</p>
            </article>

            <article className="panel">
              <div className="panel__eyebrow">Projection lag</div>
              <div className="metric-value">
                {formatMetric(dashboardQuery.data.projectionLagSeconds)}s
              </div>
              <p className="metric-caption">
                Delay between the latest persisted event and this snapshot.
              </p>
            </article>

            <article className="panel">
              <div className="panel__eyebrow">Patients today</div>
              <div className="metric-value">
                {formatMetric(dashboardQuery.data.totalPatientsToday)}
              </div>
              <p className="metric-caption">
                Aggregate daily volume from the dashboard projection.
              </p>
            </article>

            <article className="panel">
              <div className="panel__eyebrow">Completed</div>
              <div className="metric-value">{formatMetric(dashboardQuery.data.totalCompleted)}</div>
              <p className="metric-caption">
                Closed attentions already materialized in the read model.
              </p>
            </article>

            <article className="panel">
              <div className="panel__eyebrow">Active rooms</div>
              <div className="metric-value">{formatMetric(dashboardQuery.data.activeRooms)}</div>
              <p className="metric-caption">
                Consultation rooms visible as active in the snapshot.
              </p>
            </article>
          </section>

          <section className="grid grid--two">
            <article className="panel">
              <div className="panel__header">
                <div>
                  <div className="panel__eyebrow">Queue snapshots</div>
                  <h2>Queues contributing to the dashboard</h2>
                  <p>
                    Each row comes from the persisted queue projection used to build the aggregate.
                  </p>
                </div>
                <StatusBadge tone="info">
                  {dashboardQuery.data.queueSnapshots.length} queues
                </StatusBadge>
              </div>

              <div className="data-list">
                {dashboardQuery.data.queueSnapshots.map((queue) => (
                  <div className="data-list__row" key={queue.queueId}>
                    <div>
                      <strong>{queue.queueId}</strong>
                      <p>{formatMinutes(queue.averageWaitTimeMinutes)} average wait</p>
                    </div>
                    <div style={{ textAlign: 'right' }}>
                      <strong>{formatMetric(queue.totalPending)} pending</strong>
                      <p>Updated {formatTimestamp(queue.lastUpdatedAt)}</p>
                    </div>
                  </div>
                ))}
              </div>
            </article>

            <article className="panel">
              <div className="panel__header">
                <div>
                  <div className="panel__eyebrow">Visible states</div>
                  <h2>Status breakdown and snapshot integrity</h2>
                  <p>Operational counts are materialized from the same monitor projection set.</p>
                </div>
                <StatusBadge tone={projectionTone(dashboardQuery.data.projectionLagSeconds)}>
                  {dashboardQuery.data.projectionLagSeconds <= 5 ? 'Healthy lag' : 'Watch lag'}
                </StatusBadge>
              </div>

              <div className="data-list">
                {dashboardQuery.data.statusBreakdown.map((entry) => (
                  <div className="data-list__row" key={entry.status}>
                    <strong>{entry.status}</strong>
                    <span>{formatMetric(entry.total)}</span>
                  </div>
                ))}
              </div>

              <div className="panel__meta">
                <span>Generated at {formatTimestamp(dashboardQuery.data.generatedAt)}</span>
              </div>
            </article>
          </section>
        </>
      ) : null}

      <HealthPanels />
    </>
  );
}
