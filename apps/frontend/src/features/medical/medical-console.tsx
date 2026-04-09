'use client';

import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { SectionIntro } from '@/components/shared/section-intro';
import { getRoleDisplayName } from '@/lib/display-text';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { rlappApi } from '@/services/rlapp-api';
import type { StaffRole } from '@/types/api';

const activateRoomSchema = z.object({
  roomId: z.string().min(1, 'El consultorio es obligatorio.'),
  roomName: z.string().min(1, 'El nombre del consultorio es obligatorio.'),
});

const deactivateRoomSchema = z.object({
  roomId: z.string().min(1, 'El consultorio es obligatorio.'),
});

const medicalCallNextSchema = z.object({
  queueId: z.string().default('MAIN-QUEUE-001'),
  consultingRoomId: z.string().default('ROOM-01'),
});

const startConsultationSchema = z.object({
  turnId: z.string().default('MAIN-QUEUE-001-PAT-0045'),
  consultingRoomId: z.string().default('ROOM-01'),
});

const finishConsultationSchema = z.object({
  turnId: z.string().default('MAIN-QUEUE-001-PAT-0045'),
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1, 'El paciente es obligatorio.'),
  consultingRoomId: z.string().default('ROOM-01'),
  outcome: z.string().default('completed'),
});

const medicalAbsentSchema = z.object({
  turnId: z.string().default('MAIN-QUEUE-001-PAT-0045'),
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1, 'El paciente es obligatorio.'),
  consultingRoomId: z.string().default('ROOM-01'),
  reason: z.string().min(1, 'Debes indicar la razon de la ausencia.'),
});

export function MedicalConsole({ role }: { role: StaffRole }) {
  const journal = useOperationJournal('medical');
  const canManageRooms = role === 'Supervisor';
  const canHandleConsultations = role === 'Doctor' || role === 'Supervisor';

  return (
    <>
      <SectionIntro
        badge={getRoleDisplayName(role)}
        description="Desde aqui puedes operar consultorios, iniciar la atencion y cerrar consultas o registrar ausencias clinicas."
        eyebrow="Atencion medica"
        title="Consultorios y flujo de consulta"
      />

      <div className="grid grid--two">
        {canManageRooms ? (
          <ActionFormCard
            defaultValues={{ roomId: 'ROOM-01', roomName: 'Consultorio 1' }}
            description="POST /api/medical/consulting-room/activate"
            fields={[
              { name: 'roomId', label: 'Consultorio', placeholder: 'ROOM-01' },
              { name: 'roomName', label: 'Nombre visible', placeholder: 'Consultorio 1' },
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.activateConsultingRoom(values)}
            schema={activateRoomSchema}
            submitLabel="Activar consultorio"
            title="Activar consultorio"
          />
        ) : null}

        {canManageRooms ? (
          <ActionFormCard
            contractWarnings={['Si el consultorio no existe, el backend responde 400 y no 404.']}
            defaultValues={{ roomId: 'ROOM-01' }}
            description="POST /api/medical/consulting-room/deactivate"
            fields={[{ name: 'roomId', label: 'Consultorio', placeholder: 'ROOM-01' }]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.deactivateConsultingRoom(values)}
            schema={deactivateRoomSchema}
            submitLabel="Desactivar consultorio"
            title="Desactivar consultorio"
          />
        ) : null}

        {canHandleConsultations ? (
          <ActionFormCard
            defaultValues={{
              queueId: 'MAIN-QUEUE-001',
              consultingRoomId: 'ROOM-01',
            }}
            description="POST /api/medical/call-next"
            fields={[
              { name: 'queueId', label: 'Cola', placeholder: 'MAIN-QUEUE-001' },
              {
                name: 'consultingRoomId',
                label: 'Consultorio',
                placeholder: 'ROOM-01',
              },
            ]}
            notes={[
              'Este atajo reserva y llama al siguiente turno elegible en un solo paso.',
              'Usa iniciar atencion solo cuando el paciente ya haya entrado al consultorio.',
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.medicalCallNext(values)}
            schema={medicalCallNextSchema}
            submitLabel="Llamar siguiente"
            title="Atajo para llamar siguiente"
          />
        ) : null}

        {canHandleConsultations ? (
          <ActionFormCard
            defaultValues={{
              turnId: 'MAIN-QUEUE-001-PAT-0045',
              consultingRoomId: 'ROOM-01',
            }}
            description="POST /api/medical/start-consultation"
            fields={[
              {
                name: 'turnId',
                label: 'Turno',
                placeholder: 'MAIN-QUEUE-001-PAT-0045',
              },
              {
                name: 'consultingRoomId',
                label: 'Consultorio',
                placeholder: 'ROOM-01',
              },
            ]}
            notes={[
              'Inicia este paso solo cuando el paciente ya fue llamado y esta entrando al consultorio.',
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.startConsultation(values)}
            schema={startConsultationSchema}
            submitLabel="Iniciar atencion"
            title="Comenzar consulta activa"
          />
        ) : null}

        {canHandleConsultations ? (
          <ActionFormCard
            defaultValues={{
              turnId: 'MAIN-QUEUE-001-PAT-0045',
              queueId: 'MAIN-QUEUE-001',
              patientId: '',
              consultingRoomId: 'ROOM-01',
              outcome: 'completed',
            }}
            description="POST /api/waiting-room/complete-attention"
            fields={[
              { name: 'turnId', label: 'Turno', placeholder: 'MAIN-QUEUE-001-PAT-0045' },
              { name: 'queueId', label: 'Cola', placeholder: 'MAIN-QUEUE-001' },
              { name: 'patientId', label: 'Documento o NUIP', placeholder: 'PAT-0052' },
              {
                name: 'consultingRoomId',
                label: 'Consultorio',
                placeholder: 'ROOM-01',
              },
              {
                name: 'outcome',
                kind: 'select',
                label: 'Resultado',
                options: [
                  { label: 'Completada', value: 'completed' },
                  { label: 'Seguimiento', value: 'follow-up' },
                ],
              },
            ]}
            notes={[
              'Completar atencion cierra la consulta y libera el consultorio para el siguiente paciente.',
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.finishConsultation(values)}
            schema={finishConsultationSchema}
            submitLabel="Completar atencion"
            title="Cerrar consulta"
          />
        ) : null}

        {canHandleConsultations ? (
          <ActionFormCard
            defaultValues={{
              turnId: 'MAIN-QUEUE-001-PAT-0045',
              queueId: 'MAIN-QUEUE-001',
              patientId: '',
              consultingRoomId: 'ROOM-01',
              reason: 'El paciente no ingreso al consultorio',
            }}
            description="POST /api/medical/mark-absent"
            fields={[
              { name: 'turnId', label: 'Turno', placeholder: 'MAIN-QUEUE-001-PAT-0045' },
              { name: 'queueId', label: 'Cola', placeholder: 'MAIN-QUEUE-001' },
              { name: 'patientId', label: 'Documento o NUIP', placeholder: 'PAT-0052' },
              {
                name: 'consultingRoomId',
                label: 'Consultorio',
                placeholder: 'ROOM-01',
              },
              {
                name: 'reason',
                kind: 'textarea',
                label: 'Motivo',
                placeholder: 'El paciente no ingreso al consultorio',
              },
            ]}
            onSettled={(entry) =>
              journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
            }
            onSubmit={(values) => rlappApi.markMedicalAbsence(values)}
            schema={medicalAbsentSchema}
            submitLabel="Registrar ausencia"
            title="Ausencia en consulta"
          />
        ) : null}

        <OperationHistory
          entries={journal.entries}
          onClear={journal.clearEntries}
          title="Bitacora medica"
        />
      </div>
    </>
  );
}
