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
          'The backend health endpoints do not currently validate projection lag.',
          'Realtime channel health is not represented in the current readiness contract.',
        ]}
      />

      <HealthPanels />
    </>
  );
}
