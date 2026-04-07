import { StatusBadge } from '@/components/shared/status-badge';
import { formatDisplayDateTime, getJournalStatusDisplayName } from '@/lib/display-text';
import type { OperationJournalEntry } from '@/hooks/use-operation-journal';

interface OperationHistoryProps {
  title: string;
  entries: OperationJournalEntry[];
  onClear: () => void;
}

export function OperationHistory({ title, entries, onClear }: OperationHistoryProps) {
  return (
    <section className="panel">
      <div className="panel__header">
        <div>
          <div className="panel__eyebrow">Actividad reciente</div>
          <h2>{title}</h2>
        </div>
        <button className="ghost-button" onClick={onClear} type="button">
          Limpiar
        </button>
      </div>

      {entries.length === 0 ? (
        <div className="empty-state">
          <p>
            Aun no hay movimientos registrados. Aqui veras acciones correctas y errores recientes.
          </p>
        </div>
      ) : (
        <div className="history-list">
          {entries.map((entry) => (
            <article className="history-item" key={`${entry.timestamp}-${entry.title}`}>
              <div className="history-item__header">
                <h3>{entry.title}</h3>
                <StatusBadge tone={entry.status === 'success' ? 'success' : 'danger'}>
                  {getJournalStatusDisplayName(entry.status)}
                </StatusBadge>
              </div>
              <p>{entry.message}</p>
              <div className="history-item__meta">
                <span>{formatDisplayDateTime(entry.timestamp)}</span>
                {entry.correlationId ? <span>Correlacion: {entry.correlationId}</span> : null}
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
