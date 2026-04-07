'use client';

import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { SectionIntro } from '@/components/shared/section-intro';
import { getRoleDisplayName } from '@/lib/display-text';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { rlappApi } from '@/services/rlapp-api';

const receptionSchema = z.object({
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1, 'El documento o NUIP del paciente es obligatorio.'),
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
        badge={`${getRoleDisplayName('Receptionist')} · ${getRoleDisplayName('Supervisor')}`}
        description="Usa esta vista para registrar la llegada del paciente. La respuesta confirma la accion, pero no devuelve una foto completa de la cola ni del turno."
        eyebrow="Recepcion"
        title="Registrar llegada del paciente"
      />

      <div className="grid grid--two">
        <ActionFormCard
          contractWarnings={[
            'appointmentReference, priority y notes se aceptan, pero hoy el backend no los usa.',
            'La respuesta solo confirma la accion; no incluye queueId ni turnId materializados.',
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
            {
              name: 'patientId',
              label: 'Documento o NUIP',
              placeholder: 'Escribe el NUIP aqui...',
            },
            {
              name: 'patientName',
              label: 'Nombre del paciente',
              description: 'Opcional.',
              placeholder: 'Ana Perez',
            },
          ]}
          onSettled={(entry) =>
            journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
          }
          onSubmit={(values) => rlappApi.registerReceptionArrival(values)}
          schema={receptionSchema}
          submitLabel="Registrar llegada"
          title="Ingreso por recepcion"
        />

        <OperationHistory
          entries={journal.entries}
          onClear={journal.clearEntries}
          title="Bitacora de recepcion"
        />
      </div>
    </>
  );
}
