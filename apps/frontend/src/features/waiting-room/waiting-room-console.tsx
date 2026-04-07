'use client';

import { startTransition, useDeferredValue, useState } from 'react';
import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { ContractAlert } from '@/components/shared/contract-alert';
import { SectionIntro } from '@/components/shared/section-intro';
import { StatusBadge } from '@/components/shared/status-badge';
import {
  formatDisplayDateTime,
  formatDisplayMinutes,
  getOperationalStatusDisplayName,
  getRoleDisplayName,
} from '@/lib/display-text';
import { getRealtimeLabel, getRealtimeTone } from '@/lib/realtime-status';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { useOperationalRealtime } from '@/hooks/use-operational-realtime';
import { useWaitingRoomMonitor } from '@/hooks/use-operational-read-models';
import { ApiError } from '@/services/http-client';
import { rlappApi } from '@/services/rlapp-api';
import type { StaffRole } from '@/types/api';

const DEFAULT_QUEUE_ID = 'MAIN-QUEUE-001';

const checkInSchema = z.object({
  queueId: z.string().default(DEFAULT_QUEUE_ID),
  appointmentReference: z.string().default('AUTO-APT-' + new Date().getTime()),
  patientId: z.string().min(1, 'El documento o NUIP es obligatorio.'),
  patientName: z.string().optional(),
  consultationType: z.string().default('GeneralMedicine'),
  priority: z.string().default('Standard'),
});

const claimNextSchema = z.object({
  queueId: z.string().default(DEFAULT_QUEUE_ID),
  consultingRoomId: z.string().default('ROOM-01'),
});

const callPatientSchema = z.object({
  queueId: z.string().default(DEFAULT_QUEUE_ID),
  turnId: z.string().min(1, 'Debes indicar el turno.'),
  consultingRoomId: z.string().default('ROOM-01'),
});

function readErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'No fue posible cargar el monitor persistido de sala de espera.';
}

export function WaitingRoomConsole({ role }: { role: StaffRole }) {
  const journal = useOperationJournal('waiting-room');
  const canCheckIn = role === 'Receptionist' || role === 'Supervisor';
  const canConsult = role === 'Doctor' || role === 'Supervisor';
  const canViewMonitor = role === 'Receptionist' || role === 'Supervisor';
  const [requestedQueueId, setRequestedQueueId] = useState(DEFAULT_QUEUE_ID);
  const activeQueueId = useDeferredValue(requestedQueueId.trim() || DEFAULT_QUEUE_ID);
  const monitorQuery = useWaitingRoomMonitor(activeQueueId, canViewMonitor);
  const realtime = useOperationalRealtime({
    role,
    enabled: canViewMonitor,
    queueId: canViewMonitor ? activeQueueId : undefined,
  });

  return (
    <>
      <SectionIntro
        badge={getRoleDisplayName(role)}
        description="Aqui puedes ver la cola, registrar ingresos y preparar el paso del paciente hacia consulta sin perder visibilidad del estado actual."
        eyebrow="Sala de espera"
        title="Monitor de cola y flujo hacia consulta"
      />

      <div className="grid grid--two">
        <section className="panel" style={{ gridColumn: '1 / -1' }}>
          <div className="panel__header">
            <div>
              <div className="panel__eyebrow">Monitor persistido</div>
              <h2>Estado actual de la sala de espera</h2>
              <p>
                Este monitor se alimenta de proyecciones persistidas y se resincroniza cuando llega
                una invalidacion por el canal same-origin.
              </p>
            </div>
            <StatusBadge tone={getRealtimeTone(realtime.connectionState)}>
              {getRealtimeLabel(realtime.connectionState)}
            </StatusBadge>
          </div>

          {canViewMonitor ? (
            <>
              <div className="monitor-toolbar">
                <label className="monitor-toolbar__field" htmlFor="monitorQueueId">
                  <span>Cola</span>
                  <input
                    id="monitorQueueId"
                    onChange={(event) => {
                      const nextQueueId = event.target.value;
                      startTransition(() => {
                        setRequestedQueueId(nextQueueId);
                      });
                    }}
                    placeholder={DEFAULT_QUEUE_ID}
                    value={requestedQueueId}
                  />
                </label>
                <p className="inline-note">
                  El monitor se muestra para la cola seleccionada. Las acciones siguen sus contratos
                  y el cierre medico se hace desde la vista clinica.
                </p>
              </div>

              {monitorQuery.isPending ? (
                <div className="empty-state" style={{ marginTop: '20px' }}>
                  <p>Cargando el monitor persistido para {activeQueueId}.</p>
                </div>
              ) : null}

              {monitorQuery.isError ? (
                <ContractAlert
                  title="No se pudo abrir el monitor de sala de espera"
                  items={[readErrorMessage(monitorQuery.error)]}
                />
              ) : null}

              {monitorQuery.data ? (
                <>
                  <div className="grid grid--three" style={{ marginTop: '20px' }}>
                    <article className="panel">
                      <div className="panel__eyebrow">Pacientes en espera</div>
                      <div className="metric-value">{monitorQuery.data.waitingCount}</div>
                      <p className="metric-caption">
                        Pacientes que siguen pendientes en {monitorQuery.data.queueId}.
                      </p>
                    </article>

                    <article className="panel">
                      <div className="panel__eyebrow">Espera promedio</div>
                      <div className="metric-value">
                        {formatDisplayMinutes(monitorQuery.data.averageWaitTimeMinutes)}
                      </div>
                      <p className="metric-caption">
                        Tiempo medio de espera para la cola seleccionada.
                      </p>
                    </article>

                    <article className="panel">
                      <div className="panel__eyebrow">Consultorios activos</div>
                      <div className="metric-value">
                        {monitorQuery.data.activeConsultationRooms}
                      </div>
                      <p className="metric-caption">
                        Consultorios que ahora mismo aparecen con atencion activa.
                      </p>
                    </article>
                  </div>

                  <div className="grid grid--two" style={{ marginTop: '20px' }}>
                    <section className="panel">
                      <div className="panel__header">
                        <div>
                          <div className="panel__eyebrow">Estados visibles</div>
                          <h2>Distribucion por estado en {monitorQuery.data.queueId}</h2>
                        </div>
                        <StatusBadge tone="info">
                          {monitorQuery.data.statusBreakdown.length} estados
                        </StatusBadge>
                      </div>

                      <div className="data-list">
                        {monitorQuery.data.statusBreakdown.map((entry) => (
                          <div className="data-list__row" key={entry.status}>
                            <strong>{getOperationalStatusDisplayName(entry.status)}</strong>
                            <span>{entry.total}</span>
                          </div>
                        ))}
                      </div>

                      <div className="panel__meta">
                        <span>Generado {formatDisplayDateTime(monitorQuery.data.generatedAt)}</span>
                        <span>
                          Ultimo evento:{' '}
                          {realtime.lastEvent
                            ? `${realtime.lastEvent.eventType} · ${formatDisplayDateTime(realtime.lastEvent.occurredAt)}`
                            : 'Aun no llegan invalidaciones.'}
                        </span>
                      </div>
                    </section>

                    <section className="panel">
                      <div className="panel__header">
                        <div>
                          <div className="panel__eyebrow">Turnos visibles</div>
                          <h2>Listado materializado de pacientes</h2>
                        </div>
                        <StatusBadge tone="info">
                          {monitorQuery.data.entries.length} registros
                        </StatusBadge>
                      </div>

                      {monitorQuery.data.entries.length > 0 ? (
                        <div className="monitor-entry-list">
                          {monitorQuery.data.entries.map((entry) => (
                            <article className="monitor-entry" key={entry.turnId}>
                              <div className="monitor-entry__title">
                                <div>
                                  <strong>{entry.patientName}</strong>
                                  <p>
                                    {entry.ticketNumber} · {entry.turnId}
                                  </p>
                                </div>
                                <StatusBadge tone="info">
                                  {getOperationalStatusDisplayName(entry.status)}
                                </StatusBadge>
                              </div>
                              <div className="monitor-entry__meta">
                                <span>Actualizado {formatDisplayDateTime(entry.updatedAt)}</span>
                                <span>
                                  Consultorio{' '}
                                  {entry.roomAssigned && entry.roomAssigned.length > 0
                                    ? entry.roomAssigned
                                    : 'Por asignar'}
                                </span>
                              </div>
                            </article>
                          ))}
                        </div>
                      ) : (
                        <div className="empty-state" style={{ marginTop: '20px' }}>
                          <p>No hay turnos visibles en este momento para la cola seleccionada.</p>
                        </div>
                      )}
                    </section>
                  </div>
                </>
              ) : null}
            </>
          ) : (
            <ContractAlert
              title="Acceso al monitor"
              items={[
                'El monitor persistido de sala de espera esta disponible solo para Recepcion y Supervisor.',
                'El rol medico conserva acciones operativas en esta vista, pero no accede al monitor por contrato.',
              ]}
            />
          )}
        </section>

        {canCheckIn ? (
          <ActionFormCard
            contractWarnings={[
              'appointmentReference, consultationType y priority aparecen en el contrato, pero hoy el backend no los usa.',
              'Este endpoint reutiliza el mismo comando base del registro por recepcion.',
            ]}
            defaultValues={{
              queueId: DEFAULT_QUEUE_ID,
              appointmentReference: 'AUTO-APT-' + new Date().getTime(),
              patientId: '',
              patientName: '',
              consultationType: 'GeneralMedicine',
              priority: 'Standard',
            }}
            description="POST /api/waiting-room/check-in"
            fields={[
              { name: 'patientId', label: 'Documento o NUIP', placeholder: 'PAT-0045' },
              { name: 'patientName', label: 'Nombre del paciente', placeholder: 'Ana Perez' },
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.checkInPatient(values)}
            schema={checkInSchema}
            submitLabel="Registrar ingreso"
            title="Ingreso a sala de espera"
          />
        ) : null}

        {canConsult ? (
          <ActionFormCard
            defaultValues={{
              queueId: DEFAULT_QUEUE_ID,
              consultingRoomId: 'ROOM-01',
            }}
            description="POST /api/waiting-room/claim-next"
            fields={[
              { name: 'queueId', label: 'Cola', placeholder: DEFAULT_QUEUE_ID },
              {
                name: 'consultingRoomId',
                label: 'Consultorio',
                placeholder: 'ROOM-01',
              },
            ]}
            notes={[
              'Usa este paso para reservar el siguiente turno elegible para el consultorio.',
              'El paciente seguira visible como pendiente hasta que lo llames y luego inicies la atencion.',
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.claimNextPatient(values)}
            schema={claimNextSchema}
            submitLabel="Reservar siguiente"
            title="Reservar siguiente para consulta"
          />
        ) : null}

        {canConsult ? (
          <ActionFormCard
            defaultValues={{
              queueId: DEFAULT_QUEUE_ID,
              turnId: `${DEFAULT_QUEUE_ID}-PAT-0045`,
              consultingRoomId: 'ROOM-01',
            }}
            description="POST /api/waiting-room/call-patient"
            fields={[
              { name: 'queueId', label: 'Cola', placeholder: DEFAULT_QUEUE_ID },
              {
                name: 'turnId',
                label: 'Turno',
                placeholder: `${DEFAULT_QUEUE_ID}-PAT-0045`,
              },
              {
                name: 'consultingRoomId',
                label: 'Consultorio',
                placeholder: 'ROOM-01',
              },
            ]}
            notes={[
              'Puedes tomar el turno desde el monitor o desde la respuesta de reservar siguiente.',
              'La vista medica iniciara la atencion cuando el paciente entre al consultorio.',
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.callPatient(values)}
            schema={callPatientSchema}
            submitLabel="Llamar paciente"
            title="Llamar paciente reservado"
          />
        ) : null}

        <OperationHistory
          entries={journal.entries}
          onClear={journal.clearEntries}
          title="Bitacora de sala de espera"
        />
      </div>
    </>
  );
}
