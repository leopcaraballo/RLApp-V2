'use client';

import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { useEffectEvent, useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { ContractAlert } from '@/components/shared/contract-alert';
import { SectionIntro } from '@/components/shared/section-intro';
import { StatusBadge } from '@/components/shared/status-badge';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { useOperationalRealtime } from '@/hooks/use-operational-realtime';
import { ApiError } from '@/services/http-client';
import { rlappApi } from '@/services/rlapp-api';
import type {
  PatientTrajectoryDiscoveryEntry,
  PatientTrajectoryDiscoveryResponse,
  PatientTrajectoryResponse,
  RebuildPatientTrajectoriesRequest,
  RebuildPatientTrajectoriesResult,
} from '@/types/api';
import type { SessionUser } from '@/types/session';

const discoverySchema = z.object({
  patientId: z.string().trim().min(1, 'Patient ID is required.'),
  queueId: z.string().optional(),
});

const querySchema = z.object({
  trajectoryId: z.string().trim().min(1, 'Trajectory ID is required.'),
});

const rebuildSchema = z.object({
  queueId: z.string().optional(),
  patientId: z.string().optional(),
  dryRun: z.enum(['true', 'false']),
});

type DiscoveryFormValues = z.infer<typeof discoverySchema>;
type QueryFormValues = z.infer<typeof querySchema>;
type RebuildFormValues = z.infer<typeof rebuildSchema>;

function normalizeOptional(value: string | undefined): string | undefined {
  const normalized = value?.trim();
  return normalized ? normalized : undefined;
}

function readError(error: unknown): { message: string; correlationId?: string } {
  if (error instanceof ApiError) {
    const correlationId =
      typeof error.payload === 'object' &&
      error.payload !== null &&
      'correlationId' in error.payload &&
      typeof error.payload.correlationId === 'string'
        ? error.payload.correlationId
        : undefined;

    return { message: error.message, correlationId };
  }

  if (error instanceof Error) {
    return { message: error.message };
  }

  return { message: 'Unexpected frontend error while querying patient trajectories.' };
}

function toneFromState(state: string): 'info' | 'success' | 'warning' | 'danger' {
  if (state.includes('Finalizada')) {
    return 'success';
  }

  if (state.includes('Cancelada') || state.includes('Ausente')) {
    return 'danger';
  }

  if (state.includes('Activa')) {
    return 'info';
  }

  return 'warning';
}

function formatTimestamp(value: string | null | undefined): string {
  if (!value) {
    return 'Pending';
  }

  return new Date(value).toLocaleString();
}

function TrajectoryDiscoveryResults({
  discovery,
  isLoading,
  onSelect,
}: {
  discovery: PatientTrajectoryDiscoveryResponse;
  isLoading: boolean;
  onSelect: (trajectoryId: string) => Promise<void>;
}) {
  if (discovery.total === 0) {
    return (
      <div className="response-card" style={{ marginTop: '20px' }}>
        <div className="response-card__title">No persisted trajectories found</div>
        <p>
          No candidate trajectories matched the current filters. Verify the patient has a
          materialized trajectory or narrow the lookup with queueId.
        </p>
      </div>
    );
  }

  return (
    <section className="panel" style={{ marginTop: '20px' }}>
      <div className="panel__header">
        <div>
          <div className="panel__eyebrow">Discovery candidates</div>
          <h2>{discovery.total} candidate trajectory(s)</h2>
          <p>Select the trajectory you want to inspect in full detail.</p>
        </div>
        <StatusBadge tone="info">Projection-backed</StatusBadge>
      </div>

      <div className="history-list" style={{ marginTop: '20px' }}>
        {discovery.items.map((item) => (
          <TrajectoryDiscoveryItem
            entry={item}
            isLoading={isLoading}
            key={`${item.trajectoryId}-${item.openedAt}`}
            onSelect={onSelect}
          />
        ))}
      </div>
    </section>
  );
}

function TrajectoryDiscoveryItem({
  entry,
  isLoading,
  onSelect,
}: {
  entry: PatientTrajectoryDiscoveryEntry;
  isLoading: boolean;
  onSelect: (trajectoryId: string) => Promise<void>;
}) {
  return (
    <article className="history-item">
      <div className="history-item__header">
        <div>
          <h3>{entry.trajectoryId}</h3>
          <p>
            {entry.patientId} in {entry.queueId}
          </p>
        </div>
        <StatusBadge tone={toneFromState(entry.currentState)}>{entry.currentState}</StatusBadge>
      </div>
      <div className="history-item__meta">
        <span>Opened: {formatTimestamp(entry.openedAt)}</span>
        <span>Closed: {formatTimestamp(entry.closedAt)}</span>
      </div>
      <div className="history-item__meta" style={{ marginTop: '8px' }}>
        <span>Last correlation: {entry.lastCorrelationId ?? 'Not captured yet'}</span>
      </div>
      <div className="form-actions" style={{ marginTop: '12px' }}>
        <button
          className="ghost-button"
          disabled={isLoading}
          onClick={() => {
            void onSelect(entry.trajectoryId);
          }}
          type="button"
        >
          {isLoading ? 'Loading...' : 'Load trajectory'}
        </button>
      </div>
    </article>
  );
}

function TrajectorySummary({ trajectory }: { trajectory: PatientTrajectoryResponse }) {
  return (
    <section className="panel">
      <div className="panel__header">
        <div>
          <div className="panel__eyebrow">Persisted read model</div>
          <h2>{trajectory.trajectoryId}</h2>
          <p>
            Projection state reconstructed from domain events and available to support workflows.
          </p>
        </div>
        <StatusBadge tone={toneFromState(trajectory.currentState)}>
          {trajectory.currentState}
        </StatusBadge>
      </div>

      <div className="grid grid--two" style={{ marginTop: '20px' }}>
        <div className="health-details">
          <div className="health-details__row">
            <strong>Patient</strong>
            <small>{trajectory.patientId}</small>
          </div>
          <div className="health-details__row">
            <strong>Queue</strong>
            <small>{trajectory.queueId}</small>
          </div>
          <div className="health-details__row">
            <strong>Opened at</strong>
            <small>{formatTimestamp(trajectory.openedAt)}</small>
          </div>
          <div className="health-details__row">
            <strong>Closed at</strong>
            <small>{formatTimestamp(trajectory.closedAt)}</small>
          </div>
        </div>

        <div className="health-details">
          <div className="health-details__row">
            <strong>Correlation IDs</strong>
            <small>{trajectory.correlationIds.join(', ') || 'No correlation ids recorded.'}</small>
          </div>
          <div className="health-details__row">
            <strong>Stages recorded</strong>
            <small>{trajectory.stages.length}</small>
          </div>
        </div>
      </div>

      <div className="history-list" style={{ marginTop: '20px' }}>
        {trajectory.stages.map((stage, index) => (
          <article
            className="history-item"
            key={`${stage.correlationId}-${stage.occurredAt}-${index}`}
          >
            <div className="history-item__header">
              <h3>{stage.stage}</h3>
              <StatusBadge tone="info">{stage.sourceEvent}</StatusBadge>
            </div>
            <p>
              Source state: {stage.sourceState ?? 'State not captured by the originating event.'}
            </p>
            <div className="history-item__meta">
              <span>{formatTimestamp(stage.occurredAt)}</span>
              <span>Correlation: {stage.correlationId}</span>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

function RebuildResult({ result }: { result: RebuildPatientTrajectoriesResult }) {
  return (
    <div className="health-details">
      <div className="health-details__row">
        <strong>Job</strong>
        <small>{result.jobId}</small>
      </div>
      <div className="health-details__row">
        <strong>Status</strong>
        <small>{result.status}</small>
      </div>
      <div className="health-details__row">
        <strong>Scope</strong>
        <small>{result.scope}</small>
      </div>
      <div className="health-details__row">
        <strong>Mode</strong>
        <small>{result.dryRun ? 'Dry run' : 'Materialized rebuild'}</small>
      </div>
      <div className="health-details__row">
        <strong>Events processed</strong>
        <small>{result.eventsProcessed}</small>
      </div>
      <div className="health-details__row">
        <strong>Trajectories processed</strong>
        <small>{result.trajectoriesProcessed}</small>
      </div>
      <div className="health-details__row">
        <strong>Accepted at</strong>
        <small>{formatTimestamp(result.acceptedAt)}</small>
      </div>
    </div>
  );
}

export function PatientTrajectoryConsole({ session }: { session: SessionUser }) {
  const [activeTrajectory, setActiveTrajectory] = useState<PatientTrajectoryResponse | null>(null);
  const discoveryForm = useForm<DiscoveryFormValues>({
    resolver: zodResolver(discoverySchema),
    defaultValues: {
      patientId: '',
      queueId: '',
    },
  });

  const queryForm = useForm<QueryFormValues>({
    resolver: zodResolver(querySchema),
    defaultValues: {
      trajectoryId: '',
    },
  });

  const { entries, pushEntry, clearEntries } = useOperationJournal('patient-trajectory');

  const discoveryMutation = useMutation({
    mutationFn: ({ patientId, queueId }: DiscoveryFormValues) =>
      rlappApi.discoverPatientTrajectories(patientId.trim(), normalizeOptional(queueId)),
    onSuccess(result, variables) {
      pushEntry({
        title: 'Discover patient trajectories',
        status: 'success',
        message:
          result.total === 0
            ? `No persisted trajectories found for patient ${variables.patientId}.`
            : `${result.total} trajectory candidate(s) discovered for patient ${variables.patientId}.`,
        correlationId: result.items[0]?.lastCorrelationId ?? undefined,
        timestamp: new Date().toISOString(),
      });
    },
    onError(error) {
      const failure = readError(error);
      pushEntry({
        title: 'Discover patient trajectories',
        status: 'error',
        message: failure.message,
        correlationId: failure.correlationId,
        timestamp: new Date().toISOString(),
      });
    },
  });

  const queryMutation = useMutation({
    mutationFn: ({ trajectoryId }: QueryFormValues) => rlappApi.getPatientTrajectory(trajectoryId),
    onSuccess(result) {
      setActiveTrajectory(result);
      const lastCorrelationId = result.correlationIds[result.correlationIds.length - 1];

      pushEntry({
        title: 'Query patient trajectory',
        status: 'success',
        message: `Trajectory ${result.trajectoryId} loaded in state ${result.currentState}.`,
        correlationId: lastCorrelationId,
        timestamp: new Date().toISOString(),
      });
    },
    onError(error) {
      const failure = readError(error);
      pushEntry({
        title: 'Query patient trajectory',
        status: 'error',
        message: failure.message,
        correlationId: failure.correlationId,
        timestamp: new Date().toISOString(),
      });
    },
  });

  const canRebuild = session.role === 'Support';

  async function loadTrajectory(trajectoryId: string) {
    queryForm.setValue('trajectoryId', trajectoryId, {
      shouldDirty: true,
      shouldTouch: true,
      shouldValidate: true,
    });

    await queryMutation.mutateAsync({ trajectoryId });
  }

  const refreshActiveTrajectory = useEffectEvent(async () => {
    if (!activeTrajectory) {
      return;
    }

    try {
      const refreshedTrajectory = await rlappApi.getPatientTrajectory(
        activeTrajectory.trajectoryId
      );
      setActiveTrajectory(refreshedTrajectory);
    } catch {
      // Ignore silent refresh failures and keep the last confirmed snapshot visible.
    }
  });

  useOperationalRealtime({
    role: session.role,
    enabled: Boolean(activeTrajectory),
    trajectoryId: activeTrajectory?.trajectoryId,
    queueId: activeTrajectory?.queueId,
    onTrajectoryInvalidation: () => {
      void refreshActiveTrajectory();
    },
  });

  return (
    <>
      <SectionIntro
        badge={session.role}
        eyebrow="Support diagnostics"
        title="Patient trajectory console"
        description="Docker local now exposes trajectory discovery, audited trajectory query and controlled rebuild flow with silent resync when the active trajectory receives a realtime invalidation."
      />

      <div className="grid grid--two">
        <section className="operation-card">
          <div className="operation-card__header">
            <div>
              <div className="panel__eyebrow">Backend query</div>
              <h2>Discover and inspect a persisted trajectory</h2>
              <p>
                Start with patientId, optionally narrow by queueId, and load the exact event-derived
                longitudinal projection when multiple histories exist.
              </p>
            </div>
            <StatusBadge tone="info">GET</StatusBadge>
          </div>

          <ContractAlert
            title="Contract caveats"
            items={[
              'Discovery requires patientId and can narrow by queueId when the operator knows the active context.',
              'Full trajectory detail remains a second explicit query by trajectoryId after candidate selection.',
              'Access is limited to Support and Supervisor roles.',
            ]}
          />

          <form
            className="form-grid"
            onSubmit={discoveryForm.handleSubmit(async (values) => {
              await discoveryMutation.mutateAsync(values);
            })}
            style={{ marginTop: '20px' }}
          >
            <label className="form-field" htmlFor="discoveryPatientId">
              <span>Patient ID</span>
              <input
                id="discoveryPatientId"
                placeholder="PAT-0045"
                {...discoveryForm.register('patientId')}
              />
              <small>Required. This lookup stays on the persisted read model.</small>
              {discoveryForm.formState.errors.patientId ? (
                <strong className="form-field__error">
                  {discoveryForm.formState.errors.patientId.message}
                </strong>
              ) : null}
            </label>

            <label className="form-field" htmlFor="discoveryQueueId">
              <span>Queue ID</span>
              <input
                id="discoveryQueueId"
                placeholder="Q-2026-03-19-MAIN"
                {...discoveryForm.register('queueId')}
              />
              <small>
                Optional. Use it to narrow the lookup when the patient has multiple histories.
              </small>
            </label>

            <div className="form-actions">
              <button
                className="primary-button"
                disabled={discoveryMutation.isPending}
                type="submit"
              >
                {discoveryMutation.isPending ? 'Searching...' : 'Discover candidates'}
              </button>
            </div>
          </form>

          {discoveryMutation.isError ? (
            <div className="response-card response-card--error" style={{ marginTop: '20px' }}>
              <div className="response-card__title">Backend rejected the discovery</div>
              <p>{readError(discoveryMutation.error).message}</p>
            </div>
          ) : null}

          {discoveryMutation.data ? (
            <TrajectoryDiscoveryResults
              discovery={discoveryMutation.data}
              isLoading={queryMutation.isPending}
              onSelect={loadTrajectory}
            />
          ) : null}

          <form
            className="form-grid"
            onSubmit={queryForm.handleSubmit(async (values) => {
              await loadTrajectory(values.trajectoryId);
            })}
            style={{ marginTop: '24px' }}
          >
            <div className="panel__eyebrow">Direct query</div>
            <label className="form-field" htmlFor="trajectoryId">
              <span>Trajectory ID</span>
              <input
                id="trajectoryId"
                placeholder="TRJ-Q-2026-03-19-MAIN-PAT-0045-20260401091000000"
                {...queryForm.register('trajectoryId')}
              />
              <small>
                Use this when you already know the canonical trajectory identifier from support
                evidence.
              </small>
              {queryForm.formState.errors.trajectoryId ? (
                <strong className="form-field__error">
                  {queryForm.formState.errors.trajectoryId.message}
                </strong>
              ) : null}
            </label>

            <div className="form-actions">
              <button className="primary-button" disabled={queryMutation.isPending} type="submit">
                {queryMutation.isPending ? 'Loading...' : 'Fetch trajectory'}
              </button>
            </div>
          </form>

          {queryMutation.isError ? (
            <div className="response-card response-card--error">
              <div className="response-card__title">Backend rejected the query</div>
              <p>{readError(queryMutation.error).message}</p>
            </div>
          ) : null}
        </section>

        {canRebuild ? (
          <ActionFormCard<RebuildFormValues, RebuildPatientTrajectoriesResult>
            title="Controlled trajectory rebuild"
            description="Replay historical events to validate or reconcile patient trajectory projections."
            schema={rebuildSchema}
            defaultValues={{
              queueId: '',
              patientId: '',
              dryRun: 'true',
            }}
            fields={[
              {
                name: 'queueId',
                label: 'Queue ID',
                description:
                  'Optional. Scope the rebuild to one queue when you do not want a global replay.',
                placeholder: 'Q-2026-03-19-MAIN',
              },
              {
                name: 'patientId',
                label: 'Patient ID',
                description: 'Optional. Combine with queueId for the narrowest replay scope.',
                placeholder: 'PAT-0045',
              },
              {
                name: 'dryRun',
                label: 'Execution mode',
                description: 'Dry run is the safe default before materializing rebuild changes.',
                kind: 'select',
                options: [
                  { label: 'Dry run only', value: 'true' },
                  { label: 'Persist rebuild', value: 'false' },
                ],
              },
            ]}
            submitLabel="Run rebuild"
            notes={[
              'The proxy automatically injects correlation and idempotency headers.',
              'Leave queueId and patientId empty to replay the full historical scope.',
            ]}
            contractWarnings={[
              'Only Support can execute rebuild operations.',
              'Use dryRun first; setting dryRun=false materializes projection changes.',
            ]}
            onSubmit={(values) =>
              rlappApi.rebuildPatientTrajectories({
                queueId: normalizeOptional(values.queueId),
                patientId: normalizeOptional(values.patientId),
                dryRun: values.dryRun === 'true',
              } satisfies RebuildPatientTrajectoriesRequest)
            }
            onSettled={(payload) => {
              pushEntry({
                title: payload.title,
                status: payload.status,
                message: payload.message,
                correlationId: payload.correlationId,
                timestamp: new Date().toISOString(),
              });
            }}
            renderResult={(result) => <RebuildResult result={result} />}
          />
        ) : (
          <section className="panel">
            <div className="panel__header">
              <div>
                <div className="panel__eyebrow">Role boundary</div>
                <h2>Controlled rebuild remains Support-only</h2>
                <p>
                  Supervisors can inspect persisted trajectories, but rebuild execution stays
                  limited to the Support profile defined by the backend authorization policy.
                </p>
              </div>
              <StatusBadge tone="warning">Read-only</StatusBadge>
            </div>
          </section>
        )}
      </div>

      {activeTrajectory ? (
        <TrajectorySummary trajectory={activeTrajectory} />
      ) : (
        <section className="panel">
          <div className="panel__header">
            <div>
              <div className="panel__eyebrow">Projection output</div>
              <h2>No trajectory loaded yet</h2>
              <p>
                Discover candidates by patientId or submit a known trajectoryId to inspect the
                persisted longitudinal state.
              </p>
            </div>
          </div>
          <div className="empty-state" style={{ marginTop: '20px' }}>
            <p>
              This screen now stays backend-aligned by discovering candidate trajectoryIds first and
              loading the full projection only after an explicit selection.
            </p>
          </div>
        </section>
      )}

      <OperationHistory title="Trajectory journal" entries={entries} onClear={clearEntries} />
    </>
  );
}
