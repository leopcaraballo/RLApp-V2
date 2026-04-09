import { HealthPanels } from '@/components/health/health-panels';
import { ContractAlert } from '@/components/shared/contract-alert';
import { SectionIntro } from '@/components/shared/section-intro';

export default function HealthPage() {
  return (
    <>
      <SectionIntro
        badge="Public backend endpoints proxied through Next"
        description="Health is the only read surface that the backend exposes today without inventing additional business endpoints."
        eyebrow="Backend health"
        title="Service liveness and readiness"
      />

      <ContractAlert
        title="Known health coverage gaps"
        items={[
          'Readiness reflects API, database, broker, projection lag, and realtime channel registration, but it does not yet prove end-to-end staff clients are connected.',
          'Health is still an operational surface only; business dashboards and monitors remain separate read-model work.',
        ]}
      />

      <HealthPanels />
    </>
  );
}
