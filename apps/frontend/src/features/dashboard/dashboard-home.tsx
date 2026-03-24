import { HealthPanels } from '@/components/health/health-panels';
import { ContractAlert } from '@/components/shared/contract-alert';
import { SectionIntro } from '@/components/shared/section-intro';
import { StatusBadge } from '@/components/shared/status-badge';
import type { SessionUser } from '@/types/session';

const criticalFindings = [
  'There are no real business GET endpoints, so this frontend cannot render list/detail CRUD screens without inventing contracts.',
  'The backend runtime cannot publish Swagger today because startup crashes on a MassTransit license requirement.',
  'Several request fields are required by DTOs but ignored by handlers, which can mislead frontend and QA automation.',
];

const availableModules = [
  {
    name: 'Reception',
    detail: 'Operational alias for patient arrival registration.',
    status: 'Command-only',
  },
  {
    name: 'Waiting Room',
    detail: 'Mixed responsibilities: check-in, claim-next and call-patient.',
    status: 'Contract inconsistency',
  },
  {
    name: 'Cashier',
    detail: 'Payment validation and absence handling.',
    status: 'Fields partially ignored',
  },
  {
    name: 'Medical',
    detail: 'Consulting room lifecycle plus consultation completion.',
    status: 'Command-only',
  },
];

export function DashboardHome({ session }: { session: SessionUser }) {
  return (
    <>
      <SectionIntro
        badge={session.role}
        description="This console is intentionally shaped as an operational cockpit because the backend exposes commands, not CRUD read models."
        eyebrow="Backend-aligned overview"
        title={`Welcome, ${session.username}`}
      />

      <ContractAlert title="Critical backend findings surfaced in the UI" items={criticalFindings} />

      <section className="grid grid--two">
        {availableModules.map((module) => (
          <article className="panel" key={module.name}>
            <div className="panel__header">
              <div>
                <div className="panel__eyebrow">{module.name}</div>
                <h2>{module.detail}</h2>
              </div>
              <StatusBadge tone="info">{module.status}</StatusBadge>
            </div>
          </article>
        ))}
      </section>

      <HealthPanels />
    </>
  );
}
