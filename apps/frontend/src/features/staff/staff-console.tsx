'use client';

import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { SectionIntro } from '@/components/shared/section-intro';
import { getRoleDisplayName } from '@/lib/display-text';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { rlappApi } from '@/services/rlapp-api';

const roleSchema = z.object({
  staffUserId: z.string().min(1, 'El identificador del usuario es obligatorio.'),
  newRole: z.enum(['Receptionist', 'Cashier', 'Doctor', 'Supervisor', 'Support']),
  reason: z.string().min(1, 'La justificacion es obligatoria.'),
});

export function StaffConsole() {
  const journal = useOperationJournal('staff');

  return (
    <>
      <SectionIntro
        badge={`Solo ${getRoleDisplayName('Supervisor')}`}
        description="Esta vista permite cambiar roles internos cuando ya conoces el identificador exacto del usuario."
        eyebrow="Administracion de personal"
        title="Cambiar rol interno"
      />

      <div className="grid grid--two">
        <ActionFormCard
          contractWarnings={[
            'El backend exige reason en el contrato, pero el handler actual no lo usa.',
          ]}
          defaultValues={{
            staffUserId: 'staff-cashier-01',
            newRole: 'Supervisor' as const,
            reason: 'Cobertura de turno',
          }}
          description="POST /api/staff/users/change-role"
          fields={[
            { name: 'staffUserId', label: 'ID del usuario', placeholder: 'staff-cashier-01' },
            {
              name: 'newRole',
              kind: 'select',
              label: 'Nuevo rol',
              options: [
                { label: 'Recepcion', value: 'Receptionist' },
                { label: 'Caja', value: 'Cashier' },
                { label: 'Medico', value: 'Doctor' },
                { label: 'Supervisor', value: 'Supervisor' },
                { label: 'Soporte', value: 'Support' },
              ],
            },
            {
              name: 'reason',
              kind: 'textarea',
              label: 'Justificacion',
              placeholder: 'Cobertura de turno',
            },
          ]}
          onSettled={(entry) =>
            journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
          }
          onSubmit={(values) => rlappApi.changeRole(values)}
          schema={roleSchema}
          submitLabel="Cambiar rol"
          title="Actualizar rol del usuario"
        />

        <OperationHistory
          entries={journal.entries}
          onClear={journal.clearEntries}
          title="Bitacora de personal"
        />
      </div>
    </>
  );
}
