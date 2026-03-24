'use client';

import { z } from 'zod';
import { ActionFormCard } from '@/components/operations/action-form-card';
import { OperationHistory } from '@/components/operations/operation-history';
import { SectionIntro } from '@/components/shared/section-intro';
import { useOperationJournal } from '@/hooks/use-operation-journal';
import { rlappApi } from '@/services/rlapp-api';

const callNextSchema = z.object({
  queueId: z.string().default('MAIN-QUEUE-001'),
  cashierStationId: z.string().default('CASH-01'),
});

const validatePaymentSchema = z.object({
  turnId: z.string().default('AUTO-TURN'),
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1),
  paymentReference: z.string().default('AUTO-PAY-' + new Date().getTime()),
  validatedAmount: z.number().positive(),
});

const pendingPaymentSchema = z.object({
  turnId: z.string().default('AUTO-TURN'),
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1),
  reason: z.string().min(1),
  attemptNumber: z.number().int().default(1),
});

const cashierAbsentSchema = z.object({
  turnId: z.string().default('AUTO-TURN'),
  queueId: z.string().default('MAIN-QUEUE-001'),
  patientId: z.string().min(1),
  reason: z.string().min(1),
});

export function CashierConsole() {
  const journal = useOperationJournal('cashier');

  return (
    <>
      <SectionIntro
        badge="Cashier · Supervisor"
        description="Cashier flows are command-driven and return sparse payloads. This console keeps the contract visible so QA can test against the real backend behavior."
        eyebrow="Cashier operations"
        title="Payment and absence handling"
      />

      <div className="grid grid--two">
        <ActionFormCard
          contractWarnings={[
            'cashierStationId is required by the DTO but ignored by the backend handler.',
          ]}
          defaultValues={{
            queueId: 'MAIN-QUEUE-001',
            cashierStationId: 'CASH-01',
          }}
          description="POST /api/cashier/call-next"
          fields={[]}
          onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
          onSubmit={(values) => rlappApi.callNextAtCashier(values)}
          schema={callNextSchema}
          submitLabel="Call next patient"
          title="Cashier call-next"
        />

        <ActionFormCard
          contractWarnings={[
            'turnId and paymentReference are required but ignored by the backend handler.',
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
            { name: 'patientId', label: 'Patient ID (NUIP)', placeholder: 'PAT-0045' },
            {
              name: 'validatedAmount',
              kind: 'number',
              label: 'Validated amount',
              step: '0.01',
              min: 0,
            },
          ]}
          onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
          onSubmit={(values) => rlappApi.validatePayment(values)}
          schema={validatePaymentSchema}
          submitLabel="Validate payment"
          title="Validate payment"
        />

        <ActionFormCard
          contractWarnings={[
            'turnId, reason and attemptNumber are required but ignored by the backend handler.',
          ]}
          defaultValues={{
            turnId: 'TURN-00045',
            queueId: 'Q-2026-03-19-MAIN',
            patientId: 'PAT-0045',
            reason: 'Dataphone offline',
            attemptNumber: 1,
          }}
          description="POST /api/cashier/mark-payment-pending"
          fields={[
            { name: 'turnId', label: 'Turn ID', placeholder: 'TURN-00045' },
            { name: 'queueId', label: 'Queue ID', placeholder: 'Q-2026-03-19-MAIN' },
            { name: 'patientId', label: 'Patient ID', placeholder: 'PAT-0045' },
            { name: 'reason', kind: 'textarea', label: 'Reason', placeholder: 'Why is payment pending?' },
            { name: 'attemptNumber', kind: 'number', label: 'Attempt number', min: 1, step: '1' },
          ]}
          onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
          onSubmit={(values) => rlappApi.markPaymentPending(values)}
          schema={pendingPaymentSchema}
          submitLabel="Mark pending"
          title="Mark payment pending"
        />

        <ActionFormCard
          contractWarnings={[
            'turnId and reason are required but ignored by the backend handler.',
            'The backend internally injects ROOM-CASHIER as the location identifier.',
          ]}
          defaultValues={{
            turnId: 'TURN-00045',
            queueId: 'Q-2026-03-19-MAIN',
            patientId: 'PAT-0045',
            reason: 'No response at cashier',
          }}
          description="POST /api/cashier/mark-absent"
          fields={[
            { name: 'turnId', label: 'Turn ID', placeholder: 'TURN-00045' },
            { name: 'queueId', label: 'Queue ID', placeholder: 'Q-2026-03-19-MAIN' },
            { name: 'patientId', label: 'Patient ID', placeholder: 'PAT-0045' },
            { name: 'reason', kind: 'textarea', label: 'Reason', placeholder: 'No response at cashier' },
          ]}
          onSettled={(entry) => journal.pushEntry({ ...entry, timestamp: new Date().toISOString() })}
          onSubmit={(values) => rlappApi.markCashierAbsence(values)}
          schema={cashierAbsentSchema}
          submitLabel="Mark absent"
          title="Cashier absence"
        />

        <OperationHistory entries={journal.entries} onClear={journal.clearEntries} title="Cashier journal" />
      </div>
    </>
  );
}
