import { MedicalConsole } from '@/features/medical/medical-console';
import { requireServerSession } from '@/lib/auth';

export default async function MedicalPage() {
  const session = await requireServerSession();

  // Type validation: ensure role is a valid string
  const roleValue = (
    typeof session.role === 'string' ? session.role : String(session.role)
  ) as typeof session.role;

  return <MedicalConsole role={roleValue} />;
}
