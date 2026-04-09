import { StatusBadge } from '@/components/shared/status-badge';

interface ContractAlertProps {
  title: string;
  items: string[];
}

export function ContractAlert({ title, items }: ContractAlertProps) {
  if (items.length === 0) {
    return null;
  }

  return (
    <section className="contract-alert">
      <div className="contract-alert__header">
        <StatusBadge tone="warning">Importante</StatusBadge>
        <h3>{title}</h3>
      </div>
      <ul className="contract-alert__list">
        {items.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    </section>
  );
}
