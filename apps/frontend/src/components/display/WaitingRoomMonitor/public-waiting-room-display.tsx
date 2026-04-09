'use client';

import { useEffect, useState } from 'react';
import { StatusBadge } from '@/components/shared/status-badge';
import { formatDisplayClockTime, formatDisplayDateTime } from '@/lib/display-text';
import { getRealtimeLabel, getRealtimeTone } from '@/lib/realtime-status';
import {
  usePublicWaitingRoomDisplay,
  usePublicWaitingRoomRealtime,
} from '@/hooks/use-public-waiting-room';
import { ApiError } from '@/services/http-client';
import type {
  PublicWaitingRoomCall,
  PublicWaitingRoomCallStatus,
  PublicWaitingRoomTurn,
} from '@/types/public-display';

type DisplayTone = 'neutral' | 'success' | 'warning' | 'danger' | 'info';

interface DisplaySignalSlot {
  slotLabel: string;
  turnNumber: string;
  destination: string;
  statusLabel: string;
  tone: DisplayTone;
  className: string;
}

function readErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'No fue posible abrir el monitor publico de sala de espera.';
}

function pushVisibleTurn(turnNumbers: string[], seen: Set<string>, turnNumber?: string | null) {
  const normalizedTurnNumber = turnNumber?.trim();

  if (!normalizedTurnNumber || seen.has(normalizedTurnNumber)) {
    return;
  }

  seen.add(normalizedTurnNumber);
  turnNumbers.push(normalizedTurnNumber);
}

function collectVisibleTurnNumbers({
  activeCalls,
  currentTurn,
  upcomingTurns,
}: {
  activeCalls: PublicWaitingRoomCall[];
  currentTurn: PublicWaitingRoomTurn | null | undefined;
  upcomingTurns: PublicWaitingRoomTurn[];
}) {
  const visibleTurnNumbers: string[] = [];
  const seen = new Set<string>();

  for (const activeCall of activeCalls) {
    pushVisibleTurn(visibleTurnNumbers, seen, activeCall.turnNumber);
  }

  pushVisibleTurn(visibleTurnNumbers, seen, currentTurn?.turnNumber);

  for (const turn of upcomingTurns) {
    pushVisibleTurn(visibleTurnNumbers, seen, turn.turnNumber);
  }

  return visibleTurnNumbers;
}

function describeCallStatus(status: PublicWaitingRoomCallStatus): {
  label: string;
  tone: DisplayTone;
  className: string;
} {
  switch (status) {
    case 'Called':
      return {
        label: 'Dirigirse ahora',
        tone: 'warning',
        className: 'public-monitor__lane-card--called',
      };
    case 'AtCashier':
      return {
        label: 'Atencion en caja',
        tone: 'info',
        className: 'public-monitor__lane-card--cashier',
      };
    case 'InConsultation':
      return {
        label: 'En atencion',
        tone: 'success',
        className: 'public-monitor__lane-card--consultation',
      };
  }
}

function buildSignalSlots(activeCalls: PublicWaitingRoomCall[]): DisplaySignalSlot[] {
  return activeCalls.slice(1, 6).map((activeCall, index) => {
    const status = describeCallStatus(activeCall.status);

    return {
      slotLabel: `Destino ${index + 2}`,
      turnNumber: activeCall.turnNumber,
      destination: activeCall.destination,
      statusLabel: status.label,
      tone: status.tone,
      className: status.className,
    };
  });
}

function getFocusTone({
  activeCallCount,
  visibleTurnCount,
  isPending,
}: {
  activeCallCount: number;
  visibleTurnCount: number;
  isPending: boolean;
}) {
  if (activeCallCount > 0) {
    return 'warning' as const;
  }

  if (visibleTurnCount > 0) {
    return 'info' as const;
  }

  if (isPending) {
    return 'warning' as const;
  }

  return 'neutral' as const;
}

function getFocusLabel({
  activeCallCount,
  visibleTurnCount,
  isPending,
}: {
  activeCallCount: number;
  visibleTurnCount: number;
  isPending: boolean;
}) {
  if (activeCallCount >= 2) {
    return `${activeCallCount} activos`;
  }

  if (activeCallCount === 1) {
    return '1 activo';
  }

  if (visibleTurnCount >= 3) {
    return '3+ visibles';
  }

  if (visibleTurnCount > 0) {
    return `${visibleTurnCount} visibles`;
  }

  if (isPending) {
    return 'Sincronizando';
  }

  return 'En espera';
}

function buildLiveRegionMessage(
  activeCalls: PublicWaitingRoomCall[],
  visibleTurnNumbers: string[]
) {
  if (activeCalls.length > 0) {
    return `Destinos activos ${activeCalls
      .slice(0, 3)
      .map((activeCall) => `${activeCall.turnNumber} en ${activeCall.destination}`)
      .join(', ')}.`;
  }

  if (visibleTurnNumbers.length > 0) {
    return `Turnos visibles ${visibleTurnNumbers.slice(0, 3).join(', ')}.`;
  }

  return 'No hay turnos visibles en este momento.';
}

export function PublicWaitingRoomDisplay({ initialQueueId }: { initialQueueId: string }) {
  const queueId = initialQueueId.trim();
  const [now, setNow] = useState(() => new Date());
  const displayQuery = usePublicWaitingRoomDisplay(queueId, true);
  const realtime = usePublicWaitingRoomRealtime({ queueId, enabled: true });

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setNow(new Date());
    }, 1_000);

    return () => {
      window.clearInterval(intervalId);
    };
  }, []);

  const snapshot = displayQuery.data;
  const activeCalls = snapshot?.activeCalls ?? [];
  const primaryCall = activeCalls[0] ?? null;
  const upcomingTurns = snapshot?.upcomingTurns ?? [];
  const visibleTurnNumbers = collectVisibleTurnNumbers({
    activeCalls,
    currentTurn: snapshot?.currentTurn,
    upcomingTurns,
  });
  const signalSlots = buildSignalSlots(activeCalls);
  const activeTurnNumbers = new Set(
    activeCalls
      .map((activeCall) => activeCall.turnNumber.trim())
      .filter((turnNumber) => turnNumber.length > 0)
  );
  const ribbonTurns = upcomingTurns
    .map((turn) => turn.turnNumber.trim())
    .filter((turnNumber) => turnNumber.length > 0 && !activeTurnNumbers.has(turnNumber))
    .slice(0, 12);
  const visibleTurnCount = visibleTurnNumbers.length;
  const currentTurn =
    primaryCall?.turnNumber ??
    snapshot?.currentTurn?.turnNumber ??
    (displayQuery.isPending ? '...' : '--');
  const currentDestination = primaryCall?.destination ?? 'Sin destino activo';
  const primaryCallDescriptor = primaryCall ? describeCallStatus(primaryCall.status) : null;
  const generatedAtLabel = snapshot
    ? formatDisplayDateTime(snapshot.generatedAt)
    : displayQuery.isPending
      ? 'Sincronizando'
      : 'Pendiente';
  const focusTone = getFocusTone({
    activeCallCount: activeCalls.length,
    visibleTurnCount,
    isPending: displayQuery.isPending,
  });
  const focusLabel = getFocusLabel({
    activeCallCount: activeCalls.length,
    visibleTurnCount,
    isPending: displayQuery.isPending,
  });
  const liveRegionMessage = buildLiveRegionMessage(activeCalls, visibleTurnNumbers);

  return (
    <div className="public-monitor">
      <header className="brand-card public-monitor__hero">
        <div className="public-monitor__hero-copy">
          <div className="public-monitor__hero-topline">
            <div className="brand-card__eyebrow">Gestion dinamica de espera</div>
            <StatusBadge tone={getRealtimeTone(realtime.connectionState)}>
              {getRealtimeLabel(realtime.connectionState)}
            </StatusBadge>
          </div>

          <div className="public-monitor__hero-headline">
            <h1>Turnos clinicos</h1>
            <p className="public-monitor__subtitle">
              Destinos simultaneos y cola visible en tiempo real.
            </p>
          </div>

          <div className="public-monitor__hero-meta">
            <article className="public-monitor__meta-card">
              <span className="public-monitor__meta-label">Cola</span>
              <strong>{queueId}</strong>
            </article>

            <article className="public-monitor__meta-card">
              <span className="public-monitor__meta-label">Destinos activos</span>
              <strong>{activeCalls.length}</strong>
            </article>

            <article className="public-monitor__meta-card">
              <span className="public-monitor__meta-label">Ultima actualizacion</span>
              <strong>{generatedAtLabel}</strong>
            </article>
          </div>
        </div>

        <div className="public-monitor__clock-card">
          <div className="brand-card__eyebrow">Hora</div>
          <div
            className="public-monitor__clock"
            aria-label={`Hora actual ${formatDisplayClockTime(now)}`}
          >
            {formatDisplayClockTime(now)}
          </div>
          <p className="public-monitor__clock-caption">Pantalla coordinada</p>
        </div>
      </header>

      <main className="public-monitor__layout">
        <section
          className={`panel public-monitor__signal-wall${primaryCall ? ' public-monitor__signal-wall--active' : ''}`}
          aria-labelledby="public-monitor-current-turn"
        >
          <div className="panel__header public-monitor__panel-header">
            <div>
              <div className="panel__eyebrow">Comunicacion dinamica</div>
              <h2 className="public-monitor__section-title" id="public-monitor-current-turn">
                Destinos simultaneos
              </h2>
            </div>
            <StatusBadge tone={focusTone}>{focusLabel}</StatusBadge>
          </div>

          <div className="public-monitor__signal-main">
            <article
              className={`public-monitor__signal-card${primaryCall ? ' public-monitor__signal-card--active' : ''}${primaryCallDescriptor ? ` ${primaryCallDescriptor.className}` : ''}`}
            >
              <div className="public-monitor__signal-card-header">
                <span className="public-monitor__announcement-label">Destino principal</span>
                {primaryCallDescriptor ? (
                  <StatusBadge tone={primaryCallDescriptor.tone}>
                    {primaryCallDescriptor.label}
                  </StatusBadge>
                ) : null}
              </div>
              <strong className="public-monitor__signal-value">{currentDestination}</strong>
              <div className="public-monitor__signal-turn">{currentTurn}</div>
              <span className="public-monitor__signal-meta">
                {primaryCall
                  ? 'Visible en la proyeccion sincronizada'
                  : 'Esperando proximo destino visible'}
              </span>
            </article>

            {signalSlots.length > 0 ? (
              <div className="public-monitor__lane-grid">
                {signalSlots.map((slot) => (
                  <article
                    className={`public-monitor__lane-card ${slot.className}`}
                    key={slot.slotLabel}
                  >
                    <div className="public-monitor__lane-card-header">
                      <span className="public-monitor__lane-label">{slot.slotLabel}</span>
                      <StatusBadge tone={slot.tone}>{slot.statusLabel}</StatusBadge>
                    </div>
                    <div className="public-monitor__lane-turn">{slot.turnNumber}</div>
                    <span className="public-monitor__lane-note">{slot.destination}</span>
                  </article>
                ))}
              </div>
            ) : (
              <div className="empty-state public-monitor__empty">
                <p>No hay otros destinos activos en este momento.</p>
              </div>
            )}
          </div>

          <p className="public-monitor__sr-only" aria-live="polite" aria-atomic="true">
            {liveRegionMessage}
          </p>
        </section>

        <section className="panel public-monitor__ribbon">
          <div className="panel__header public-monitor__panel-header">
            <div>
              <div className="panel__eyebrow">Cola visible</div>
              <h2 className="public-monitor__section-title">Siguientes turnos</h2>
            </div>
            <StatusBadge tone={ribbonTurns.length > 0 ? 'info' : 'neutral'}>
              {ribbonTurns.length > 0 ? `${ribbonTurns.length} en espera` : 'En espera'}
            </StatusBadge>
          </div>

          {ribbonTurns.length > 0 ? (
            <div className="public-monitor__ribbon-list">
              {ribbonTurns.map((turnNumber, index) => (
                <article
                  className={`public-monitor__ribbon-chip${index === 0 ? ' public-monitor__ribbon-chip--active' : ''}`}
                  key={`${turnNumber}-${index}`}
                >
                  <span>{index === 0 ? 'Siguiente' : `Cola ${index + 1}`}</span>
                  <strong>{turnNumber}</strong>
                </article>
              ))}
            </div>
          ) : (
            <div className="empty-state public-monitor__empty">
              <p>No hay turnos visibles en este momento.</p>
            </div>
          )}
        </section>
      </main>

      {displayQuery.isError && !snapshot ? (
        <section className="panel public-monitor__notice" role="alert">
          <div className="panel__eyebrow">Monitor publico</div>
          <p>{readErrorMessage(displayQuery.error)}</p>
        </section>
      ) : null}

      <footer className="panel public-monitor__footer">
        <div>
          <strong>Display anonimo</strong>
        </div>
        <p className="public-monitor__footer-meta">
          {snapshot
            ? `Actualizado ${formatDisplayDateTime(snapshot.generatedAt)}`
            : 'Esperando sincronizacion.'}
        </p>
      </footer>
    </div>
  );
}
