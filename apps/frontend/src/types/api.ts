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
export type ActivateConsultingRoomRequest =
  components['schemas']['ActivateConsultingRoomRequest'];
export type DeactivateConsultingRoomRequest =
  components['schemas']['DeactivateConsultingRoomRequest'];
export type FinishConsultationRequest = components['schemas']['FinishConsultationRequest'];
export type MedicalMarkAbsentRequest = components['schemas']['MedicalMarkAbsentRequest'];
export type AuthenticationResult = components['schemas']['AuthenticationResult'];
export type CommandResult = components['schemas']['CommandResult'];
export type PatientCallResult = components['schemas']['PatientCallResult'];
export type ClaimedPatientResult = components['schemas']['ClaimedPatientResult'];
export type RegisterPatientResult = components['schemas']['RegisterPatientResult'];
export type InlineCommandError = components['schemas']['InlineCommandError'];
export type ValidationProblemDetails = components['schemas']['ValidationProblemDetails'];
export type ProblemDetails = components['schemas']['ProblemDetails'];
export type HealthDetail = components['schemas']['HealthDetail'];
export type HealthStatusResponse = components['schemas']['HealthStatusResponse'];

export type ApiEnvelopeError =
  | InlineCommandError
  | ValidationProblemDetails
  | ProblemDetails
  | { message: string };

