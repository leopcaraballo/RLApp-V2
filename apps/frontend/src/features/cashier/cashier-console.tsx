'use client';

import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { SectionIntro } from '@/components/shared/section-intro';
import { getRoleDisplayName } from '@/lib/display-text';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { rlappApi } from '@/services/rlapp-api';

const callNextSchema = z.object({
  queueId: z.string().default('MAIN-QUEUE-001'),
  cashierStationId: z.string().default('CASH-01'),
});

const validatePaymentSchema = z.object({
  turnId: z.string().default('AUTO-TURN'),
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1, 'El paciente es obligatorio.'),
  paymentReference: z.string().default('AUTO-PAY-' + new Date().getTime()),
  validatedAmount: z.number().positive('El valor debe ser mayor que cero.'),
});

const pendingPaymentSchema = z.object({
  turnId: z.string().default('AUTO-TURN'),
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1, 'El paciente es obligatorio.'),
  reason: z.string().min(1, 'La razon es obligatoria.'),
  attemptNumber: z.number().int().default(1),
});

const cashierAbsentSchema = z.object({
  turnId: z.string().default('AUTO-TURN'),
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1, 'El paciente es obligatorio.'),
  reason: z.string().min(1, 'Debes indicar la razon de la ausencia.'),
});

export function CashierConsole() {
  const journal = useOperationJournal('cashier');

  return (
    <>
      <SectionIntro
        badge={`${getRoleDisplayName('Cashier')} · ${getRoleDisplayName('Supervisor')}`}
        description="Desde esta vista puedes llamar el siguiente turno, validar pagos, dejar pagos pendientes o registrar ausencia en ventanilla."
        eyebrow="Caja"
        title="Pagos y ausencias en caja"
      />

      <div className="grid grid--two">
        <ActionFormCard
          contractWarnings={[
            'cashierStationId aparece en el contrato, pero hoy el backend no lo usa.',
          ]}
          defaultValues={{
            queueId: 'MAIN-QUEUE-001',
            cashierStationId: 'CASH-01',
          }}
          description="POST /api/cashier/call-next"
          fields={[]}
          onSettled={(entry) =>
            journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
          }
          onSubmit={(values) => rlappApi.callNextAtCashier(values)}
          schema={callNextSchema}
          submitLabel="Llamar siguiente paciente"
          title="Llamar siguiente turno"
        />

        <ActionFormCard
          contractWarnings={[
            'turnId y paymentReference son parte del contrato, pero hoy el backend no los usa.',
          ]}
          defaultValues={{
            turnId: 'AUTO-TURN',
            queueId: 'MAIN-QUEUE-001',
            patientId: '',
            paymentReference: 'AUTO-PAY-' + new Date().getTime(),
            validatedAmount: 100.0,
          }}
          description="POST /api/cashier/validate-payment"
          fields={[
            { name: 'patientId', label: 'Documento o NUIP', placeholder: 'PAT-0045' },
            {
              name: 'validatedAmount',
              kind: 'number',
              label: 'Valor pagado',
              step: '0.01',
              min: 0,
            },
          ]}
          onSettled={(entry) =>
            journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
          }
          onSubmit={(values) => rlappApi.validatePayment(values)}
          schema={validatePaymentSchema}
          submitLabel="Validar pago"
          title="Registrar pago"
        />

        <ActionFormCard
          contractWarnings={[
            'turnId, reason y attemptNumber existen en el contrato, pero hoy el backend no los usa.',
          ]}
          defaultValues={{
            turnId: 'TURN-00045',
            queueId: 'Q-2026-03-19-MAIN',
            patientId: 'PAT-0045',
            reason: 'Datáfono fuera de servicio',
            attemptNumber: 1,
          }}
          description="POST /api/cashier/mark-payment-pending"
          fields={[
            { name: 'turnId', label: 'Turno', placeholder: 'TURN-00045' },
            { name: 'queueId', label: 'Cola', placeholder: 'Q-2026-03-19-MAIN' },
            { name: 'patientId', label: 'Paciente', placeholder: 'PAT-0045' },
            {
              name: 'reason',
              kind: 'textarea',
              label: 'Motivo',
              placeholder: 'Explica por que el pago queda pendiente',
            },
            { name: 'attemptNumber', kind: 'number', label: 'Intento', min: 1, step: '1' },
          ]}
          onSettled={(entry) =>
            journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
          }
          onSubmit={(values) => rlappApi.markPaymentPending(values)}
          schema={pendingPaymentSchema}
          submitLabel="Marcar pago pendiente"
          title="Dejar pago pendiente"
        />

        <ActionFormCard
          contractWarnings={[
            'turnId y reason hacen parte del contrato, pero hoy el backend no los usa de forma completa.',
            'El backend agrega internamente ROOM-CASHIER como identificador de ubicacion.',
          ]}
          defaultValues={{
            turnId: 'TURN-00045',
            queueId: 'Q-2026-03-19-MAIN',
            patientId: 'PAT-0045',
            reason: 'No hubo respuesta en caja',
          }}
          description="POST /api/cashier/mark-absent"
          fields={[
            { name: 'turnId', label: 'Turno', placeholder: 'TURN-00045' },
            { name: 'queueId', label: 'Cola', placeholder: 'Q-2026-03-19-MAIN' },
            { name: 'patientId', label: 'Paciente', placeholder: 'PAT-0045' },
            {
              name: 'reason',
              kind: 'textarea',
              label: 'Motivo',
              placeholder: 'No hubo respuesta en caja',
            },
          ]}
          onSettled={(entry) =>
            journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })
          }
          onSubmit={(values) => rlappApi.markCashierAbsence(values)}
          schema={cashierAbsentSchema}
          submitLabel="Registrar ausencia"
          title="Ausencia en caja"
        />

        <OperationHistory
          entries={journal.entries}
          onClear={journal.clearEntries}
          title="Bitacora de caja"
        />
      </div>
    </>
  );
}
