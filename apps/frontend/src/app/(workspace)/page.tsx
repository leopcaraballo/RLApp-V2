import { DashboardHome } from '@/features/dashboard/dashboard-home';
import { requireServerSession } from '@/lib/auth';

export default async function HomePage() {
  const session = await requireServerSession();

  return (
    <DashboardHome
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
