import { StatusBadge } from '@/components/shared/status-badge';
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
          <div className="panel__eyebrow">Audit-friendly trace</div>
          <h2>{title}</h2>
        </div>
        <button className="ghost-button" onClick={onClear} type="button">
          Clear
        </button>
      </div>

      {entries.length === 0 ? (
        <div className="empty-state">
          <p>No operation journal entries yet. Successful and failed actions will appear here.</p>
        </div>
      ) : (
        <div className="history-list">
          {entries.map((entry) => (
            <article className="history-item" key={`${entry.timestamp}-${entry.title}`}>
              <div className="history-item__header">
                <h3>{entry.title}</h3>
                <StatusBadge tone={entry.status === 'success' ? 'success' : 'danger'}>
                  {entry.status}
                </StatusBadge>
              </div>
              <p>{entry.message}</p>
              <div className="history-item__meta">
                <span>{new Date(entry.timestamp).toLocaleString()}</span>
                {entry.correlationId ? <span>Correlation: {entry.correlationId}</span> : null}
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
