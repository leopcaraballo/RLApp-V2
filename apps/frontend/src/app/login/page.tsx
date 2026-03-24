import { redirect } from 'next/navigation';
import { getServerSession } from '@/lib/auth';
import { LoginForm } from '@/features/auth/login-form';

export default async function LoginPage() {
  const session = await getServerSession();

  if (session) {
    redirect('/');
  }

  return (
    <div className="auth-shell">
      <LoginForm />
    </div>
  );
}
