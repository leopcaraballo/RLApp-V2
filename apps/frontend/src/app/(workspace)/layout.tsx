import { AppShell } from '@/components/layout/app-shell';
import { requireServerSession } from '@/lib/auth';

export default async function WorkspaceLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await requireServerSession();

  return (
    <AppShell
      session={{
        staffId: session.staffId,
        username: session.username,
        email: session.email,
        role: session.role,
        authenticatedAt: session.authenticatedAt,
        expiresAt: session.expiresAt,
      }}
    >
      {children}
    </AppShell>
  );
}
