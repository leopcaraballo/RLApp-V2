'use client';

import { startTransition, useDeferredValue, useState } from 'react';
import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { ContractAlert } from '@/components/shared/contract-alert';
import { SectionIntro } from '@/components/shared/section-intro';
import { StatusBadge } from '@/components/shared/status-badge';
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
  patientId: z.string().min(1),
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
  turnId: z.string().min(1),
  consultingRoomId: z.string().default('ROOM-01'),
});

function readErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'The persisted waiting room monitor could not be loaded.';
}

function formatTimestamp(value: string): string {
  return new Date(value).toLocaleString();
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
        badge={role}
        description="Reception keeps check-in here, doctor flow keeps claim and call here, and active consultation starts only once Medical confirms start-consultation."
        eyebrow="Waiting room"
        title="Queue monitor and consultation flow"
      />

      <div className="grid grid--two">
        <section className="panel" style={{ gridColumn: '1 / -1' }}>
          <div className="panel__header">
            <div>
              <div className="panel__eyebrow">Persisted monitor</div>
              <h2>Operational waiting room snapshot</h2>
              <p>
                This monitor reads from persisted projections and only re-syncs after invalidation
                events arrive through the same-origin realtime channel.
              </p>
            </div>
            <StatusBadge tone={realtimeTone(realtime.connectionState)}>
              {realtime.connectionState === 'live' ? 'Live sync' : 'Reconnecting'}
            </StatusBadge>
          </div>

          {canViewMonitor ? (
            <>
              <div className="monitor-toolbar">
                <label className="monitor-toolbar__field" htmlFor="monitorQueueId">
                  <span>Queue ID</span>
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
                  Snapshot generated for the selected queue only. Commands continue using their own
                  backend contracts, while Medical owns consultation start and completion.
                </p>
              </div>

              {monitorQuery.isPending ? (
                <div className="empty-state" style={{ marginTop: '20px' }}>
                  <p>Loading the persisted waiting room snapshot for {activeQueueId}.</p>
                </div>
              ) : null}

              {monitorQuery.isError ? (
                <ContractAlert
                  title="Waiting room monitor unavailable"
                  items={[readErrorMessage(monitorQuery.error)]}
                />
              ) : null}

              {monitorQuery.data ? (
                <>
                  <div className="grid grid--three" style={{ marginTop: '20px' }}>
                    <article className="panel">
                      <div className="panel__eyebrow">Waiting</div>
                      <div className="metric-value">{monitorQuery.data.waitingCount}</div>
                      <p className="metric-caption">
                        Patients still waiting in {monitorQuery.data.queueId}.
                      </p>
                    </article>

                    <article className="panel">
                      <div className="panel__eyebrow">Average wait</div>
                      <div className="metric-value">
                        {formatMinutes(monitorQuery.data.averageWaitTimeMinutes)}
                      </div>
                      <p className="metric-caption">
                        Persisted average wait for the selected queue.
                      </p>
                    </article>

                    <article className="panel">
                      <div className="panel__eyebrow">Active rooms</div>
                      <div className="metric-value">
                        {monitorQuery.data.activeConsultationRooms}
                      </div>
                      <p className="metric-caption">
                        Rooms currently visible with active attention.
                      </p>
                    </article>
                  </div>

                  <div className="grid grid--two" style={{ marginTop: '20px' }}>
                    <section className="panel">
                      <div className="panel__header">
                        <div>
                          <div className="panel__eyebrow">Status breakdown</div>
                          <h2>Visible states for {monitorQuery.data.queueId}</h2>
                        </div>
                        <StatusBadge tone="info">
                          {monitorQuery.data.statusBreakdown.length} states
                        </StatusBadge>
                      </div>

                      <div className="data-list">
                        {monitorQuery.data.statusBreakdown.map((entry) => (
                          <div className="data-list__row" key={entry.status}>
                            <strong>{entry.status}</strong>
                            <span>{entry.total}</span>
                          </div>
                        ))}
                      </div>

                      <div className="panel__meta">
                        <span>Generated at {formatTimestamp(monitorQuery.data.generatedAt)}</span>
                        <span>
                          Last event:{' '}
                          {realtime.lastEvent
                            ? `${realtime.lastEvent.eventType} at ${formatTimestamp(realtime.lastEvent.occurredAt)}`
                            : 'Waiting for invalidations.'}
                        </span>
                      </div>
                    </section>

                    <section className="panel">
                      <div className="panel__header">
                        <div>
                          <div className="panel__eyebrow">Live entries</div>
                          <h2>Materialized turn list</h2>
                        </div>
                        <StatusBadge tone="info">
                          {monitorQuery.data.entries.length} entries
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
                                <StatusBadge tone="info">{entry.status}</StatusBadge>
                              </div>
                              <div className="monitor-entry__meta">
                                <span>Updated {formatTimestamp(entry.updatedAt)}</span>
                                <span>
                                  Room{' '}
                                  {entry.roomAssigned && entry.roomAssigned.length > 0
                                    ? entry.roomAssigned
                                    : 'Pending'}
                                </span>
                              </div>
                            </article>
                          ))}
                        </div>
                      ) : (
                        <div className="empty-state" style={{ marginTop: '20px' }}>
                          <p>No materialized entries are currently visible for this queue.</p>
                        </div>
                      )}
                    </section>
                  </div>
                </>
              ) : null}
            </>
          ) : (
            <ContractAlert
              title="Monitor boundary"
              items={[
                'The persisted waiting room monitor is currently authorized only for Receptionist and Supervisor.',
                'Doctor workflow keeps command access here, but the projection-backed monitor remains restricted by contract.',
              ]}
            />
          )}
        </section>

        {canCheckIn ? (
          <ActionFormCard
            contractWarnings={[
              'appointmentReference, consultationType and priority are required but ignored by the backend handler.',
              'The endpoint reuses the same underlying command as reception register.',
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
              { name: 'patientId', label: 'Patient ID (NUIP)', placeholder: 'PAT-0045' },
              { name: 'patientName', label: 'Patient name', placeholder: 'Ana Perez' },
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.checkInPatient(values)}
            schema={checkInSchema}
            submitLabel="Check in patient"
            title="Waiting room check-in"
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
              { name: 'queueId', label: 'Queue ID', placeholder: DEFAULT_QUEUE_ID },
              {
                name: 'consultingRoomId',
                label: 'Consulting room ID',
                placeholder: 'ROOM-01',
              },
            ]}
            notes={[
              'Use this step to reserve the next eligible turn for the consulting room.',
              'The patient remains visible as waiting until call-patient and start-consultation are completed.',
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.claimNextPatient(values)}
            schema={claimNextSchema}
            submitLabel="Claim next patient"
            title="Claim next for consultation"
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
              { name: 'queueId', label: 'Queue ID', placeholder: DEFAULT_QUEUE_ID },
              {
                name: 'turnId',
                label: 'Turn ID',
                placeholder: `${DEFAULT_QUEUE_ID}-PAT-0045`,
              },
              {
                name: 'consultingRoomId',
                label: 'Consulting room ID',
                placeholder: 'ROOM-01',
              },
            ]}
            notes={[
              'Use the turn ID from the monitor or from the claim-next response.',
              'Medical will start the consultation explicitly after the patient enters the room.',
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.callPatient(values)}
            schema={callPatientSchema}
            submitLabel="Call patient"
            title="Call claimed patient"
          />
        ) : null}

        <OperationHistory
          entries={journal.entries}
          onClear={journal.clearEntries}
          title="Waiting room journal"
        />
      </div>
    </>
  );
}
