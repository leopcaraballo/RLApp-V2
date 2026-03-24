import type { StaffRole } from '@/types/api';

export interface SessionUser {
  staffId: string;
  username: string;
  email: string;
  role: StaffRole;
  authenticatedAt: string;
  expiresAt: string;
}

export interface SessionPayload extends SessionUser {
  accessToken: string;
}

export interface LoginResponseEnvelope {
  session: SessionUser;
  warnings: string[];
}

