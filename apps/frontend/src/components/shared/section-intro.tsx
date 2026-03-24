import { StatusBadge } from '@/components/shared/status-badge';

interface SectionIntroProps {
  eyebrow: string;
  title: string;
  description: string;
  badge?: string;
}

export function SectionIntro({ eyebrow, title, description, badge }: SectionIntroProps) {
  return (
    <header className="section-intro">
      <div className="section-intro__eyebrow">{eyebrow}</div>
      <div className="section-intro__row">
        <div>
          <h1>{title}</h1>
          <p>{description}</p>
        </div>
        {badge ? <StatusBadge tone="info">{badge}</StatusBadge> : null}
      </div>
    </header>
  );
}
