import { PatientTrajectoryConsole } from '@/features/trajectory/patient-trajectory-console';
import { requireServerSession } from '@/lib/auth';

export default async function PatientTrajectoryPage() {
  const session = await requireServerSession();

  return (
    <PatientTrajectoryConsole
      session={{
        staffId: session.staffId,
        username: session.username,
        email: session.email,
        role: session.role,
        authenticatedAt: session.authenticatedAt,
        expiresAt: session.expiresAt,
      }}
    />
  );
}
