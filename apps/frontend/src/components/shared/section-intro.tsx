import { StatusBadge } from '@/components/shared/status-badge';

interface SectionIntroProps {
  eyebrow: string;
  title: string;
  description?: string;
  badge?: string;
}

export function SectionIntro({ eyebrow, title, description, badge }: SectionIntroProps) {
  return (
    <header className="section-intro">
      <div className="section-intro__row">
        <div className="section-intro__copy">
          <div className="section-intro__eyebrow">{eyebrow}</div>
          <h1>{title}</h1>
          {description ? <p className="section-intro__description">{description}</p> : null}
        </div>
        {badge ? <StatusBadge tone="info">{badge}</StatusBadge> : null}
      </div>
    </header>
  );
}
