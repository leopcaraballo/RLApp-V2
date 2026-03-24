'use client';

import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { SectionIntro } from '@/components/shared/section-intro';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { rlappApi } from '@/services/rlapp-api';

const receptionSchema = z.object({
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1, 'Patient ID (NUIP) is required.'),
  patientName: z.string().optional(),
  appointmentReference: z.string().default('AUTO-APT-' + new Date().getTime()),
  priority: z.string().default('Standard'),
  notes: z.string().optional(),
});

export function ReceptionConsole() {
  const journal = useOperationJournal('reception');

  return (
    <>
      <SectionIntro
        badge="Receptionist · Supervisor"
        description="Reception currently exposes a single command alias for check-in. The backend returns a generic command result without queue or turn snapshots."
        eyebrow="Reception operations"
        title="Register patient arrival"
      />

      <div className="grid grid--two">
        <ActionFormCard
          contractWarnings={[
            'appointmentReference, priority and notes are accepted but ignored by the backend handler.',
            'The response does not return queueId or turnId; it only acknowledges the command.',
          ]}
          defaultValues={{
            queueId: 'MAIN-QUEUE-001',
            patientId: '',
            patientName: '',
            appointmentReference: 'AUTO-APT-' + new Date().getTime(),
            priority: 'Standard',
            notes: '',
          }}
          description="POST /api/reception/register"
          fields={[
            { name: 'patientId', label: 'Patient ID (NUIP)', placeholder: 'Enter NUIP here...' },
            {
              name: 'patientName',
              label: 'Patient name',
              description: 'Optional.',
              placeholder: 'Ana Perez',
            },
          ]}
          onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
          onSubmit={(values) => rlappApi.registerReceptionArrival(values)}
          schema={receptionSchema}
          submitLabel="Register arrival"
          title="Reception register"
        />

        <OperationHistory entries={journal.entries} onClear={journal.clearEntries} title="Reception journal" />
      </div>
    </>
  );
}
