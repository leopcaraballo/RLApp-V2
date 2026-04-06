import type { components } from '@/generated/backend-api';

export type StaffRole = components['schemas']['StaffRole'];
export type LoginRequest = components['schemas']['LoginRequest'];
export type ChangeRoleRequest = components['schemas']['ChangeRoleRequest'];
export type ReceptionRegisterRequest = components['schemas']['ReceptionRegisterRequest'];
export type CheckInRequest = components['schemas']['CheckInRequest'];
export type CallPatientRequest = components['schemas']['CallPatientRequest'];
export type ClaimNextPatientRequest = components['schemas']['ClaimNextPatientRequest'];
export type CallNextAtCashierRequest = components['schemas']['CallNextAtCashierRequest'];
export type ValidatePaymentRequest = components['schemas']['ValidatePaymentRequest'];
export type MarkPaymentPendingRequest = components['schemas']['MarkPaymentPendingRequest'];
export type CashierMarkAbsentRequest = components['schemas']['CashierMarkAbsentRequest'];
export type ActivateConsultingRoomRequest = components['schemas']['ActivateConsultingRoomRequest'];
export type DeactivateConsultingRoomRequest =
  components['schemas']['DeactivateConsultingRoomRequest'];
export type FinishConsultationRequest = components['schemas']['FinishConsultationRequest'];
export type MedicalMarkAbsentRequest = components['schemas']['MedicalMarkAbsentRequest'];
export type AuthenticationResult = components['schemas']['AuthenticationResult'];
export type CommandResult = components['schemas']['CommandResult'];
export type PatientCallResult = components['schemas']['PatientCallResult'];
export type ClaimedPatientResult = components['schemas']['ClaimedPatientResult'];
export type InlineCommandError = components['schemas']['InlineCommandError'];
export type ValidationProblemDetails = components['schemas']['ValidationProblemDetails'];
export type ProblemDetails = components['schemas']['ProblemDetails'];
export type HealthDetail = components['schemas']['HealthDetail'];
export type HealthStatusResponse = components['schemas']['HealthStatusResponse'];
export type PatientTrajectoryDiscoveryEntry =
  components['schemas']['PatientTrajectoryDiscoveryEntry'];
export type PatientTrajectoryDiscoveryResponse =
  components['schemas']['PatientTrajectoryDiscoveryResponse'];
export type PatientTrajectoryResponse = components['schemas']['PatientTrajectoryResponse'];
export type PatientTrajectoryStageEntry = components['schemas']['TrajectoryStageEntry'];
export type RebuildPatientTrajectoriesRequest =
  components['schemas']['RebuildPatientTrajectoriesRequest'];
export type RebuildPatientTrajectoriesResult =
  components['schemas']['RebuildPatientTrajectoriesResult'];
export type TrajectoryOperationError = components['schemas']['TrajectoryOperationError'];

export interface OperationalStatusCount {
  status: string;
  total: number;
}

export interface DashboardQueueSnapshot {
  queueId: string;
  totalPending: number;
  averageWaitTimeMinutes: number;
  lastUpdatedAt: string;
}

export interface OperationsDashboardSnapshot {
  generatedAt: string;
  currentWaitingCount: number;
  averageWaitTimeMinutes: number;
  totalPatientsToday: number;
  totalCompleted: number;
  activeRooms: number;
  projectionLagSeconds: number;
  queueSnapshots: DashboardQueueSnapshot[];
  statusBreakdown: OperationalStatusCount[];
}

export interface WaitingRoomMonitorEntry {
  turnId: string;
  patientId: string;
  patientName: string;
  ticketNumber: string;
  status: string;
  roomAssigned?: string | null;
  checkedInAt: string;
  updatedAt: string;
}

export interface WaitingRoomMonitorSnapshot {
  queueId: string;
  generatedAt: string;
  waitingCount: number;
  averageWaitTimeMinutes: number;
  activeConsultationRooms: number;
  statusBreakdown: OperationalStatusCount[];
  entries: WaitingRoomMonitorEntry[];
}

export type OperationalRealtimeScope = 'queue' | 'dashboard' | 'trajectory';

export interface OperationalRealtimeEvent {
  version: string;
  eventType: string;
  scope: OperationalRealtimeScope;
  queueId?: string;
  trajectoryId?: string;
  correlationId?: string;
  occurredAt: string;
}

export type ApiEnvelopeError =
  | InlineCommandError
  | ValidationProblemDetails
  | ProblemDetails
  | TrajectoryOperationError
  | { error: string; correlationId?: string }
  | { message: string };
