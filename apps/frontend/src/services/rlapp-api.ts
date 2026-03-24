import { httpRequest } from '@/services/http-client';
import type {
  ActivateConsultingRoomRequest,
  AuthenticationResult,
  CallNextAtCashierRequest,
  CallPatientRequest,
  CashierMarkAbsentRequest,
  ChangeRoleRequest,
  CheckInRequest,
  ClaimNextPatientRequest,
  ClaimedPatientResult,
  CommandResult,
  DeactivateConsultingRoomRequest,
  FinishConsultationRequest,
  HealthStatusResponse,
  LoginRequest,
  MarkPaymentPendingRequest,
  MedicalMarkAbsentRequest,
  PatientCallResult,
  ReceptionRegisterRequest,
  ValidatePaymentRequest,
} from '@/types/api';
import type { LoginResponseEnvelope, SessionUser } from '@/types/session';

const proxyBaseUrl = '/api/proxy';

function buildPath(path: string, query?: Record<string, string>): string {
  const url = new URL(`${proxyBaseUrl}${path}`, 'http://localhost');

  if (query) {
    for (const [key, value] of Object.entries(query)) {
      url.searchParams.set(key, value);
    }
  }

  return `${url.pathname}${url.search}`;
}

export const rlappApi = {
  login(payload: LoginRequest): Promise<LoginResponseEnvelope> {
    return httpRequest<LoginResponseEnvelope>('/api/session/login', {
      method: 'POST',
      json: payload,
    });
  },

  logout(): Promise<void> {
    return httpRequest<void>('/api/session/logout', {
      method: 'POST',
    });
  },

  getSession(): Promise<SessionUser | null> {
    return httpRequest<SessionUser | null>('/api/session/me');
  },

  getHealth(): Promise<HealthStatusResponse> {
    return httpRequest<HealthStatusResponse>(buildPath('/health'));
  },

  getReadiness(): Promise<HealthStatusResponse> {
    return httpRequest<HealthStatusResponse>(buildPath('/health/ready'));
  },

  getLiveness(): Promise<HealthStatusResponse> {
    return httpRequest<HealthStatusResponse>(buildPath('/health/live'));
  },

  changeRole(payload: ChangeRoleRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/staff/users/change-role'), {
      method: 'POST',
      json: payload,
    });
  },

  registerReceptionArrival(payload: ReceptionRegisterRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/reception/register'), {
      method: 'POST',
      json: payload,
    });
  },

  checkInPatient(payload: CheckInRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/waiting-room/check-in'), {
      method: 'POST',
      json: payload,
    });
  },

  callPatient(payload: CallPatientRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/waiting-room/call-patient'), {
      method: 'POST',
      json: payload,
    });
  },

  claimNextPatient(payload: ClaimNextPatientRequest): Promise<ClaimedPatientResult> {
    return httpRequest<ClaimedPatientResult>(buildPath('/waiting-room/claim-next'), {
      method: 'POST',
      json: payload,
    });
  },

  callNextAtCashier(payload: CallNextAtCashierRequest): Promise<PatientCallResult> {
    return httpRequest<PatientCallResult>(buildPath('/cashier/call-next'), {
      method: 'POST',
      json: payload,
    });
  },

  validatePayment(payload: ValidatePaymentRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/cashier/validate-payment'), {
      method: 'POST',
      json: payload,
    });
  },

  markPaymentPending(payload: MarkPaymentPendingRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/cashier/mark-payment-pending'), {
      method: 'POST',
      json: payload,
    });
  },

  markCashierAbsence(payload: CashierMarkAbsentRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/cashier/mark-absent'), {
      method: 'POST',
      json: payload,
    });
  },

  activateConsultingRoom(payload: ActivateConsultingRoomRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/medical/consulting-room/activate'), {
      method: 'POST',
      json: payload,
    });
  },

  deactivateConsultingRoom(payload: DeactivateConsultingRoomRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/medical/consulting-room/deactivate'), {
      method: 'POST',
      json: payload,
    });
  },

  finishConsultation(payload: FinishConsultationRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/medical/finish-consultation'), {
      method: 'POST',
      json: payload,
    });
  },

  markMedicalAbsence(payload: MedicalMarkAbsentRequest): Promise<CommandResult> {
    return httpRequest<CommandResult>(buildPath('/medical/mark-absent'), {
      method: 'POST',
      json: payload,
    });
  },
};

export type BackendLoginResult = AuthenticationResult;
