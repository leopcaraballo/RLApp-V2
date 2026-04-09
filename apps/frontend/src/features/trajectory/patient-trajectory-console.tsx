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
import {
  formatDisplayDateTime,
  getOperationalStatusDisplayName,
  getRoleDisplayName,
} from '@/lib/display-text';
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
  patientId: z.string().trim().min(1, 'El identificador del paciente es obligatorio.'),
  queueId: z.string().optional(),
});

const querySchema = z.object({
  trajectoryId: z.string().trim().min(1, 'El identificador de trayectoria es obligatorio.'),
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

  return { message: 'Se produjo un error inesperado al consultar trayectorias.' };
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
        <div className="response-card__title">No se encontraron trayectorias persistidas</div>
        <p>
          Ninguna trayectoria coincide con los filtros actuales. Verifica si el paciente ya tiene
          una trayectoria materializada o acota la busqueda con la cola.
        </p>
      </div>
    );
  }

  return (
    <section className="panel" style={{ marginTop: '20px' }}>
      <div className="panel__header">
        <div>
          <div className="panel__eyebrow">Coincidencias encontradas</div>
          <h2>{discovery.total} trayectoria(s) candidata(s)</h2>
          <p>Elige la trayectoria que quieres revisar en detalle.</p>
        </div>
        <StatusBadge tone="info">Persistida</StatusBadge>
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
            {entry.patientId} en {entry.queueId}
          </p>
        </div>
        <StatusBadge tone={toneFromState(entry.currentState)}>
          {getOperationalStatusDisplayName(entry.currentState)}
        </StatusBadge>
      </div>
      <div className="history-item__meta">
        <span>Apertura: {formatDisplayDateTime(entry.openedAt)}</span>
        <span>Cierre: {formatDisplayDateTime(entry.closedAt)}</span>
      </div>
      <div className="history-item__meta" style={{ marginTop: '8px' }}>
        <span>Ultima correlacion: {entry.lastCorrelationId ?? 'Aun no registrada'}</span>
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
          {isLoading ? 'Cargando...' : 'Cargar trayectoria'}
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
          <div className="panel__eyebrow">Lectura persistida</div>
          <h2>{trajectory.trajectoryId}</h2>
          <p>
            Estado longitudinal reconstruido desde eventos y disponible para consulta operativa.
          </p>
        </div>
        <StatusBadge tone={toneFromState(trajectory.currentState)}>
          {getOperationalStatusDisplayName(trajectory.currentState)}
        </StatusBadge>
      </div>

      <div className="grid grid--two" style={{ marginTop: '20px' }}>
        <div className="health-details">
          <div className="health-details__row">
            <strong>Paciente</strong>
            <small>{trajectory.patientId}</small>
          </div>
          <div className="health-details__row">
            <strong>Cola</strong>
            <small>{trajectory.queueId}</small>
          </div>
          <div className="health-details__row">
            <strong>Apertura</strong>
            <small>{formatDisplayDateTime(trajectory.openedAt)}</small>
          </div>
          <div className="health-details__row">
            <strong>Cierre</strong>
            <small>{formatDisplayDateTime(trajectory.closedAt)}</small>
          </div>
        </div>

        <div className="health-details">
          <div className="health-details__row">
            <strong>Correlaciones</strong>
            <small>
              {trajectory.correlationIds.join(', ') || 'No hay correlaciones registradas.'}
            </small>
          </div>
          <div className="health-details__row">
            <strong>Hitos registrados</strong>
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
              Estado de origen: {stage.sourceState ?? 'El evento original no capturo este dato.'}
            </p>
            <div className="history-item__meta">
              <span>{formatDisplayDateTime(stage.occurredAt)}</span>
              <span>Correlacion: {stage.correlationId}</span>
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
        <strong>Trabajo</strong>
        <small>{result.jobId}</small>
      </div>
      <div className="health-details__row">
        <strong>Estado</strong>
        <small>{result.status}</small>
      </div>
      <div className="health-details__row">
        <strong>Alcance</strong>
        <small>{result.scope}</small>
      </div>
      <div className="health-details__row">
        <strong>Modo</strong>
        <small>{result.dryRun ? 'Simulacion' : 'Reconstruccion materializada'}</small>
      </div>
      <div className="health-details__row">
        <strong>Eventos procesados</strong>
        <small>{result.eventsProcessed}</small>
      </div>
      <div className="health-details__row">
        <strong>Trayectorias procesadas</strong>
        <small>{result.trajectoriesProcessed}</small>
      </div>
      <div className="health-details__row">
        <strong>Aceptado</strong>
        <small>{formatDisplayDateTime(result.acceptedAt)}</small>
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
        title: 'Buscar trayectorias del paciente',
        status: 'success',
        message:
          result.total === 0
            ? `No se encontraron trayectorias para el paciente ${variables.patientId}.`
            : `Se encontraron ${result.total} trayectoria(s) candidata(s) para el paciente ${variables.patientId}.`,
        correlationId: result.items[0]?.lastCorrelationId ?? undefined,
        timestamp: new Date().toISOString(),
      });
    },
    onError(error) {
      const failure = readError(error);
      pushEntry({
        title: 'Buscar trayectorias del paciente',
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
        title: 'Consultar trayectoria del paciente',
        status: 'success',
        message: `La trayectoria ${result.trajectoryId} se cargo en estado ${getOperationalStatusDisplayName(result.currentState)}.`,
        correlationId: lastCorrelationId,
        timestamp: new Date().toISOString(),
      });
    },
    onError(error) {
      const failure = readError(error);
      pushEntry({
        title: 'Consultar trayectoria del paciente',
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
        badge={getRoleDisplayName(session.role)}
        eyebrow="Trayectoria del paciente"
        title="RLApp Clinical Orchestrator"
        description="Consola de trayectoria del paciente para buscar trayectorias persistidas, revisar el historial longitudinal y, si tu rol lo permite, ejecutar reconstrucciones controladas."
      />

      <div className="grid grid--two">
        <section className="operation-card">
          <div className="operation-card__header">
            <div>
              <div className="panel__eyebrow">Consulta</div>
              <h2>Buscar e inspeccionar una trayectoria persistida</h2>
              <p>
                Comienza con el paciente, acota por cola si hace falta y luego carga la trayectoria
                correcta cuando existan varios historiales.
              </p>
            </div>
            <StatusBadge tone="info">GET</StatusBadge>
          </div>

          <ContractAlert
            title="Puntos clave de la consulta"
            items={[
              'La busqueda requiere patientId y puede filtrarse por queueId cuando conoces el contexto activo.',
              'El detalle completo se consulta despues de elegir una trayectoria especifica.',
              'El acceso esta restringido a Supervisor y Soporte.',
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
              <span>Paciente</span>
              <input
                id="discoveryPatientId"
                placeholder="PAT-0045"
                {...discoveryForm.register('patientId')}
              />
              <small>Obligatorio. Esta busqueda se hace sobre la lectura persistida.</small>
              {discoveryForm.formState.errors.patientId ? (
                <strong className="form-field__error">
                  {discoveryForm.formState.errors.patientId.message}
                </strong>
              ) : null}
            </label>

            <label className="form-field" htmlFor="discoveryQueueId">
              <span>Cola</span>
              <input
                id="discoveryQueueId"
                placeholder="Q-2026-03-19-MAIN"
                {...discoveryForm.register('queueId')}
              />
              <small>
                Opcional. Sirve para reducir resultados cuando el paciente tiene varias
                trayectorias.
              </small>
            </label>

            <div className="form-actions">
              <button
                className="primary-button"
                disabled={discoveryMutation.isPending}
                type="submit"
              >
                {discoveryMutation.isPending ? 'Buscando...' : 'Buscar trayectorias'}
              </button>
            </div>
          </form>

          {discoveryMutation.isError ? (
            <div className="response-card response-card--error" style={{ marginTop: '20px' }}>
              <div className="response-card__title">No se pudo completar la busqueda</div>
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
            <div className="panel__eyebrow">Consulta directa</div>
            <label className="form-field" htmlFor="trajectoryId">
              <span>Trayectoria</span>
              <input
                id="trajectoryId"
                placeholder="TRJ-Q-2026-03-19-MAIN-PAT-0045-20260401091000000"
                {...queryForm.register('trajectoryId')}
              />
              <small>
                Usa este campo cuando ya conoces el identificador canonico de la trayectoria.
              </small>
              {queryForm.formState.errors.trajectoryId ? (
                <strong className="form-field__error">
                  {queryForm.formState.errors.trajectoryId.message}
                </strong>
              ) : null}
            </label>

            <div className="form-actions">
              <button className="primary-button" disabled={queryMutation.isPending} type="submit">
                {queryMutation.isPending ? 'Cargando...' : 'Consultar trayectoria'}
              </button>
            </div>
          </form>

          {queryMutation.isError ? (
            <div className="response-card response-card--error">
              <div className="response-card__title">No se pudo completar la consulta</div>
              <p>{readError(queryMutation.error).message}</p>
            </div>
          ) : null}
        </section>

        {canRebuild ? (
          <ActionFormCard<RebuildFormValues, RebuildPatientTrajectoriesResult>
            title="Reconstruccion controlada"
            description="Reprocesa eventos historicos para validar o reconciliar las proyecciones de trayectoria."
            schema={rebuildSchema}
            defaultValues={{
              queueId: '',
              patientId: '',
              dryRun: 'true',
            }}
            fields={[
              {
                name: 'queueId',
                label: 'Cola',
                description:
                  'Opcional. Limita la reconstruccion a una cola cuando no quieras un reproceso global.',
                placeholder: 'Q-2026-03-19-MAIN',
              },
              {
                name: 'patientId',
                label: 'Paciente',
                description: 'Opcional. Combinalo con la cola para acotar al maximo el alcance.',
                placeholder: 'PAT-0045',
              },
              {
                name: 'dryRun',
                label: 'Modo de ejecucion',
                description: 'La simulacion es la opcion segura antes de materializar cambios.',
                kind: 'select',
                options: [
                  { label: 'Solo simulacion', value: 'true' },
                  { label: 'Materializar reconstruccion', value: 'false' },
                ],
              },
            ]}
            submitLabel="Ejecutar reconstruccion"
            notes={[
              'El proxy agrega automaticamente correlation e idempotency headers.',
              'Deja queueId y patientId vacios si necesitas reconstruir todo el historico.',
            ]}
            contractWarnings={[
              'Solo Soporte puede ejecutar reconstrucciones.',
              'Usa simulacion primero; dryRun=false materializa cambios sobre proyecciones.',
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
                <div className="panel__eyebrow">Acceso por rol</div>
                <h2>La reconstruccion sigue siendo exclusiva de Soporte</h2>
                <p>
                  Supervisor puede consultar trayectorias persistidas, pero la reconstruccion queda
                  reservada al perfil de Soporte.
                </p>
              </div>
              <StatusBadge tone="warning">Solo lectura</StatusBadge>
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
              <div className="panel__eyebrow">Resultado longitudinal</div>
              <h2>Aun no hay una trayectoria cargada</h2>
              <p>
                Busca primero por paciente o usa un trajectoryId conocido para revisar el estado
                persistido.
              </p>
            </div>
          </div>
          <div className="empty-state" style={{ marginTop: '20px' }}>
            <p>
              Esta pantalla carga primero coincidencias y luego el detalle completo, para evitar
              errores cuando el paciente tiene varios historiales.
            </p>
          </div>
        </section>
      )}

      <OperationHistory title="Bitacora de trayectoria" entries={entries} onClear={clearEntries} />
    </>
  );
}
