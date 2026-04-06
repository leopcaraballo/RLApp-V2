# Endpoints To Stories

| Endpoint | Type | Stories | Specs |
| --- | --- | --- | --- |
| POST /api/staff/auth/login | Command | US-001 | S-001 |
| POST /api/session/login | Command | US-001, US-021 | S-001, S-013 |
| GET /api/session/me | Query | US-001, US-021 | S-001, S-013 |
| POST /api/staff/users/register | Command | US-002 | S-001 |
| POST /api/staff/users/change-role | Command | US-002 | S-001 |
| POST /api/staff/users/change-status | Command | US-002 | S-001 |
| POST /api/waiting-room/check-in | Command | US-005 | S-003 |
| POST /api/reception/register | Command | US-005 | S-003 |
| POST /api/cashier/call-next | Command | US-007 | S-004 |
| POST /api/cashier/validate-payment | Command | US-013 | S-004 |
| POST /api/cashier/mark-payment-pending | Command | US-014 | S-004 |
| POST /api/cashier/mark-absent | Command | US-008 | S-004 |
| POST /api/cashier/cancel-payment | Command | US-015 | S-004 |
| POST /api/medical/call-next | Command | US-007 | S-005 |
| POST /api/medical/consulting-room/activate | Command | US-003 | S-002 |
| POST /api/medical/consulting-room/deactivate | Command | US-004 | S-002 |
| POST /api/medical/start-consultation | Command | US-009 | S-005 |
| POST /api/medical/finish-consultation | Command | US-010 | S-005 |
| POST /api/medical/mark-absent | Command | US-008 | S-005 |
| POST /api/waiting-room/claim-next | Command | US-007, US-009 | S-005 |
| POST /api/waiting-room/call-patient | Command | US-007, US-008 | S-005 |
| POST /api/waiting-room/complete-attention | Command | US-010 | S-005 |
| GET /api/v1/operations/dashboard | Query | US-011, US-021 | S-007, S-013 |
| GET /api/v1/audit/timeline/{correlationId} | Query | US-012 | S-007 |
| GET /api/v1/waiting-room/{queueId}/monitor | Query | US-006, US-021 | S-003, S-006, S-013 |
| GET /api/v1/waiting-room/{queueId}/queue-state | Query | US-006 | S-003 |
| GET /api/v1/waiting-room/{queueId}/next-turn | Query | US-006, US-007 | S-003, S-006 |
| GET /api/v1/waiting-room/{queueId}/recent-history | Query | US-006, US-010 | S-003, S-006 |
| POST /api/v1/waiting-room/{queueId}/rebuild | Query-like operation | US-016 | S-008 |
| GET /api/patient-trajectories | Query | US-018 | S-011 |
| GET /api/patient-trajectories/{trajectoryId} | Query | US-012, US-018 | S-011 |
| POST /api/patient-trajectories/rebuild | Query-like operation | US-018 | S-011 |
| GET or WS /ws/waiting-room | Realtime | US-006, US-007, US-010 | S-006 |
| GET /api/realtime/operations | Realtime | US-021 | S-013 |

## Contract gap

- No quedan endpoints faltantes para `S-001` y `S-007`.
- `S-011` ya expone endpoints canonicos para discovery operativo, consulta protegida y rebuild controlado de trayectoria.
- Los contratos de identidad, reporting, recepcion, caja y consulta ya incluyen payloads tipados, required/optional y errores canonicos suficientes para traduccion directa a OpenAPI.
- `S-013` agrega el boundary explicito entre sesion web de staff y el hub interno realtime del backend.
