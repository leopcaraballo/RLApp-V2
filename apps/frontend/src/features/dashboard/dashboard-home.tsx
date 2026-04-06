import { HealthPanels } from '@/components/health/health-panels';
import { ContractAlert } from '@/components/shared/contract-alert';
import { SectionIntro } from '@/components/shared/section-intro';
import { StatusBadge } from '@/components/shared/status-badge';
import type { SessionUser } from '@/types/session';

const criticalFindings = [
  'Business GET coverage is still narrow: the audited operational reads are patient trajectory discovery and patient trajectory detail.',
  'Patient trajectory diagnostics now support discovery by patientId with optional queueId narrowing for Support and Supervisor.',
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
  {
    name: 'Trajectory',
    detail: 'Protected read model plus controlled rebuild for support and supervisors.',
    status: 'Read model available',
  },
];

export function DashboardHome({ session }: { session: SessionUser }) {
  return (
    <>
      <SectionIntro
        badge={session.role}
        description="This console stays backend-aligned: command-first for daily operations, with a single audited longitudinal read model for patient trajectories."
        eyebrow="Backend-aligned overview"
        title={`Welcome, ${session.username}`}
      />

      <ContractAlert
        title="Critical backend findings surfaced in the UI"
        items={criticalFindings}
      />

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
