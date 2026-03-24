'use client';

import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { SectionIntro } from '@/components/shared/section-intro';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { rlappApi } from '@/services/rlapp-api';
import type { StaffRole } from '@/types/api';

const checkInSchema = z.object({
  queueId: z.string().default('MAIN-QUEUE-001'),
  appointmentReference: z.string().default('AUTO-APT-' + new Date().getTime()),
  patientId: z.string().min(1),
  patientName: z.string().optional(),
  consultationType: z.string().default('GeneralMedicine'),
  priority: z.string().default('Standard'),
});

const claimNextSchema = z.object({
  queueId: z.string().default('MAIN-QUEUE-001'),
  roomId: z.string().default('ROOM-01'),
});

const callPatientSchema = z.object({
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1),
  roomId: z.string().default('ROOM-01'),
});

export function WaitingRoomConsole({ role }: { role: StaffRole }) {
  const journal = useOperationJournal('waiting-room');
  const canCheckIn = role === 'Receptionist' || role === 'Supervisor';
  const canConsult = role === 'Doctor' || role === 'Supervisor';

  return (
    <>
      <SectionIntro
        badge={role}
        description="This area mixes reception and doctor workflows because that is how the backend is currently split across endpoints."
        eyebrow="Waiting room"
        title="Queue and consultation entry flow"
      />

      <div className="grid grid--two">
        {canCheckIn ? (
          <ActionFormCard
            contractWarnings={[
              'appointmentReference, consultationType and priority are required but ignored by the backend handler.',
              'The endpoint reuses the same underlying command as reception register.',
            ]}
            defaultValues={{
              queueId: 'MAIN-QUEUE-001',
              appointmentReference: 'AUTO-APT-' + new Date().getTime(),
              patientId: '',
              patientName: '',
              consultationType: 'GeneralMedicine',
              priority: 'Standard',
            }}
            description="POST /api/waiting-room/check-in"
            fields={[
              { name: 'patientId', label: 'Patient ID (NUIP)', placeholder: 'PAT-0045' },
              { name: 'patientName', label: 'Patient name', placeholder: 'Ana Perez' },
            ]}
            onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
            onSubmit={(values) => rlappApi.checkInPatient(values)}
            schema={checkInSchema}
            submitLabel="Check in patient"
            title="Waiting room check-in"
          />
        ) : null}

        {canConsult ? (
          <ActionFormCard
            contractWarnings={[
              'queueId is passed through query string here, unlike most other commands.',
              'The backend returns only patientId, roomId and claimedAt.',
            ]}
            defaultValues={{
              queueId: 'MAIN-QUEUE-001',
              roomId: 'ROOM-01',
            }}
            description="POST /api/waiting-room/claim-next?queueId=..."
            fields={[]}
            onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
            onSubmit={(values) => rlappApi.claimNextPatient(values)}
            schema={claimNextSchema}
            submitLabel="Claim next patient"
            title="Claim next for consultation"
          />
        ) : null}

        {canConsult ? (
          <ActionFormCard
            contractWarnings={[
              'queueId is provided via query string, which breaks consistency with the rest of the API.',
              'The endpoint operates on patientId, not turnId.',
            ]}
            defaultValues={{
              queueId: 'Q-2026-03-19-MAIN',
              patientId: 'PAT-0045',
              roomId: 'ROOM-01',
            }}
            description="POST /api/waiting-room/call-patient?queueId=..."
            fields={[
              { name: 'queueId', label: 'Queue ID', placeholder: 'Q-2026-03-19-MAIN' },
              { name: 'patientId', label: 'Patient ID', placeholder: 'PAT-0045' },
              { name: 'roomId', label: 'Room ID', placeholder: 'ROOM-01' },
            ]}
            onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
            onSubmit={(values) => rlappApi.callPatient(values)}
            schema={callPatientSchema}
            submitLabel="Call patient"
            title="Call specific patient"
          />
        ) : null}

        <OperationHistory
          entries={journal.entries}
          onClear={journal.clearEntries}
          title="Waiting room journal"
        />
      </div>
    </>
  );
}
