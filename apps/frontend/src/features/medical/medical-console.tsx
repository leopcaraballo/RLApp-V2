'use client';

import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { SectionIntro } from '@/components/shared/section-intro';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { rlappApi } from '@/services/rlapp-api';
import type { StaffRole } from '@/types/api';

const activateRoomSchema = z.object({
  roomId: z.string().min(1),
  roomName: z.string().min(1),
});

const deactivateRoomSchema = z.object({
  roomId: z.string().min(1),
});

const finishConsultationSchema = z.object({
  turnId: z.string().default('AUTO-TURN'),
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1),
  consultingRoomId: z.string().default('ROOM-01'),
  outcome: z.string().default('completed'),
});

const medicalAbsentSchema = z.object({
  turnId: z.string().default('AUTO-TURN'),
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1),
  consultingRoomId: z.string().default('ROOM-01'),
  reason: z.string().min(1),
});

export function MedicalConsole({ role }: { role: StaffRole }) {
  const journal = useOperationJournal('medical');
  const canManageRooms = role === 'Supervisor';
  const canHandleConsultations = role === 'Doctor' || role === 'Supervisor';

  return (
    <>
      <SectionIntro
        badge={role}
        description="Medical flows are split between room lifecycle and consultation completion. The UI keeps those two responsibilities visible because the backend policies differ."
        eyebrow="Medical operations"
        title="Consulting rooms and consultation flow"
      />

      <div className="grid grid--two">
        {canManageRooms ? (
          <ActionFormCard
            defaultValues={{ roomId: 'ROOM-01', roomName: 'Consultorio 1' }}
            description="POST /api/medical/consulting-room/activate"
            fields={[
              { name: 'roomId', label: 'Room ID', placeholder: 'ROOM-01' },
              { name: 'roomName', label: 'Room name', placeholder: 'Consultorio 1' },
            ]}
            onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
            onSubmit={(values) => rlappApi.activateConsultingRoom(values)}
            schema={activateRoomSchema}
            submitLabel="Activate room"
            title="Activate consulting room"
          />
        ) : null}

        {canManageRooms ? (
          <ActionFormCard
            contractWarnings={[
              'When the room does not exist, the backend returns 400 instead of 404.',
            ]}
            defaultValues={{ roomId: 'ROOM-01' }}
            description="POST /api/medical/consulting-room/deactivate"
            fields={[{ name: 'roomId', label: 'Room ID', placeholder: 'ROOM-01' }]}
            onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
            onSubmit={(values) => rlappApi.deactivateConsultingRoom(values)}
            schema={deactivateRoomSchema}
            submitLabel="Deactivate room"
            title="Deactivate consulting room"
          />
        ) : null}

        {canHandleConsultations ? (
          <ActionFormCard
            contractWarnings={[
              'turnId and outcome are required by the DTO but ignored by the backend handler.',
            ]}
          defaultValues={{
            turnId: 'AUTO-TURN',
            queueId: 'MAIN-QUEUE-001',
            patientId: '',
            consultingRoomId: 'ROOM-01',
            outcome: 'completed',
          }}
          description="POST /api/medical/finish-consultation"
          fields={[
            { name: 'patientId', label: 'Patient ID (NUIP)', placeholder: 'PAT-0052' },
            {
              name: 'outcome',
              kind: 'select',
              label: 'Outcome',
              options: [
                { label: 'Completed', value: 'completed' },
                { label: 'Follow-up', value: 'follow-up' },
              ],
            },
          ]}
            onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
            onSubmit={(values) => rlappApi.finishConsultation(values)}
            schema={finishConsultationSchema}
            submitLabel="Finish consultation"
            title="Finish consultation"
          />
        ) : null}

        {canHandleConsultations ? (
          <ActionFormCard
            contractWarnings={[
              'turnId and reason are required by the DTO but ignored by the backend handler.',
            ]}
          defaultValues={{
            turnId: 'AUTO-TURN',
            queueId: 'MAIN-QUEUE-001',
            patientId: '',
            consultingRoomId: 'ROOM-01',
            reason: 'Patient did not enter room',
          }}
          description="POST /api/medical/mark-absent"
          fields={[
            { name: 'patientId', label: 'Patient ID (NUIP)', placeholder: 'PAT-0052' },
            { name: 'reason', kind: 'textarea', label: 'Reason', placeholder: 'Patient did not enter room' },
          ]}
            onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
            onSubmit={(values) => rlappApi.markMedicalAbsence(values)}
            schema={medicalAbsentSchema}
            submitLabel="Mark absent"
            title="Medical absence"
          />
        ) : null}

        <OperationHistory entries={journal.entries} onClear={journal.clearEntries} title="Medical journal" />
      </div>
    </>
  );
}
