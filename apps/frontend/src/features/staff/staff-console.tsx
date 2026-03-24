'use client';

import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { SectionIntro } from '@/components/shared/section-intro';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { rlappApi } from '@/services/rlapp-api';

const roleSchema = z.object({
  staffUserId: z.string().min(1),
  newRole: z.enum(['Receptionist', 'Cashier', 'Doctor', 'Supervisor', 'Support']),
  reason: z.string().min(1),
});

export function StaffConsole() {
  const journal = useOperationJournal('staff');

  return (
    <>
      <SectionIntro
        badge="Supervisor only"
        description="Administrative role changes are exposed as a single backend command. There are no staff listing endpoints yet, so this screen operates on explicit staff IDs."
        eyebrow="Staff administration"
        title="Change internal role"
      />

      <div className="grid grid--two">
        <ActionFormCard
          contractWarnings={['The backend requires reason, but the current handler ignores it.']}
          defaultValues={{
            staffUserId: 'staff-cashier-01',
            newRole: 'Supervisor' as const,
            reason: 'Shift coverage',
          }}
          description="POST /api/staff/users/change-role"
          fields={[
            { name: 'staffUserId', label: 'Staff user ID', placeholder: 'staff-cashier-01' },
            {
              name: 'newRole',
              kind: 'select',
              label: 'New role',
              options: [
                { label: 'Receptionist', value: 'Receptionist' },
                { label: 'Cashier', value: 'Cashier' },
                { label: 'Doctor', value: 'Doctor' },
                { label: 'Supervisor', value: 'Supervisor' },
                { label: 'Support', value: 'Support' },
              ],
            },
            { name: 'reason', kind: 'textarea', label: 'Reason', placeholder: 'Shift coverage' },
          ]}
          onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
          onSubmit={(values) => rlappApi.changeRole(values)}
          schema={roleSchema}
          submitLabel="Change role"
          title="Change staff role"
        />

        <OperationHistory entries={journal.entries} onClear={journal.clearEntries} title="Staff journal" />
      </div>
    </>
  );
}
