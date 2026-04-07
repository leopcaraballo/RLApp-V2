# Operational Flow Test Plan

## Purpose

Definir un plan realista y ejecutable para validar el flujo operativo completo de RLApp sobre pacientes, recepcion, caja, consulta, consultorios, trayectoria, monitor, dashboard, BFF y realtime same-origin.

## Traceability

- Specs: `S-002`, `S-003`, `S-004`, `S-005`, `S-007`, `S-009`, `S-011`, `S-013`
- Use cases: `UC-003`, `UC-004`, `UC-005`, `UC-006`, `UC-007`, `UC-008`, `UC-009`, `UC-010`, `UC-011`, `UC-012`, `UC-013`, `UC-014`, `UC-015`, `UC-018`, `UC-021`
- User stories: `US-003`, `US-004`, `US-005`, `US-006`, `US-007`, `US-008`, `US-009`, `US-010`, `US-011`, `US-012`, `US-013`, `US-014`, `US-018`, `US-021`
- BDD base: `BDD-002`, `BDD-003`, `BDD-004`, `BDD-005`, `BDD-010`, `BDD-012`
- TDD base: `TDD-S-002`, `TDD-S-003`, `TDD-S-004`, `TDD-S-005`, `TDD-S-011`, `TDD-S-013`, `TDD-S-009`
- Seguridad y resiliencia: `SEC-TEST-001`, `SEC-TEST-003`, `RES-TEST-002`, `RES-TEST-004`

## Validation objective

- validar todas las transiciones documentadas de `ST-001` a `ST-012`
- validar todos los cierres de paciente hoy soportados por contrato: finalizado y cancelado por ausencia
- validar que recepcion, caja, doctor, supervisor y support solo puedan ejecutar lo permitido por rol
- validar que monitor, dashboard y trayectoria converjan sobre snapshots persistidos sin leer replay en hot path
- validar que la sesion web y el stream realtime same-origin no expongan `accessToken` al navegador
- validar que la operacion siga consistente bajo volumen, reconexion y cambios de topologia de consultorios

## Entry criteria

- rama de trabajo `feature/*`
- Docker local operativo con PostgreSQL y RabbitMQ reales o equivalentes controlados
- frontend y backend compilando en limpio
- datos de prueba versionados para escenarios nominales, de borde y de error
- usuarios de prueba disponibles por rol o preparados en setup usando contratos de staff

## Exit criteria

- build limpio
- suites unitarias e integracion en verde
- monitor, dashboard y trayectoria sin drift bloqueante en escenarios nominales y de alto volumen
- ningun caso critico de autorizacion, idempotencia, concurrencia o realtime queda sin evidencia
- todos los gaps documentales detectados durante la ejecucion quedan cerrados o escalados explicitamente

## Environments and evidence

### Local quality gates

```bash
dotnet test apps/backend/RLApp.slnx -c Release
npm --prefix apps/frontend run typecheck
npm --prefix apps/frontend run lint
npm --prefix apps/frontend run build
```

### Docker operational validation

```bash
docker compose --profile backend --profile frontend up --build -d
curl -sS -o /tmp/rlapp-backend-ready.json -w '%{http_code}' http://127.0.0.1:5094/health/ready
curl -sS -o /tmp/rlapp-backend-startup.json -w '%{http_code}' http://127.0.0.1:5094/health/startup
curl -sS -o /tmp/rlapp-frontend-health.json -w '%{http_code}' http://127.0.0.1:3000/api/health
docker compose ps -a
```

### High-volume runtime validation

```bash
RLAPP_TOTAL_PATIENTS=100 \
RLAPP_INITIAL_ROOMS=10 \
RLAPP_REMAINING_ROOMS=5 \
RLAPP_DEACTIVATE_AT_PATIENT=50 \
RLAPP_REPORT_PATH=.tmp/rlapp-patient-simulation-random-100.json \
node .tmp/rlapp-patient-simulation.mjs
```

### Evidence to collect

- `dotnet test` output or `.trx`
- frontend `typecheck`, `lint` and `build` output
- `docker compose ps -a`
- backend and frontend health responses
- monitor, dashboard and trajectory snapshots used for assertions
- audit timeline by `correlationId` for critical failures
- simulation report JSON for deterministic and randomized runs

## Execution order

1. static gates: backend unit and integration, frontend typecheck/lint/build
2. role and contract validation: recepcion, caja, consulta, supervisor and support endpoints
3. synchronized reads: monitor, dashboard, queue state, recent history, trajectory and audit timeline
4. security and BFF validation: session login, session summary, realtime same-origin and RBAC failures
5. deterministic end-to-end patient flows with one patient per outcome family
6. multi-patient operational regression with mixed priorities and mixed room availability
7. high-volume randomized run with dynamic room deactivation and post-run drift inspection

## Scenario catalog

### Reception and queue

- `REC-01` nominal check-in (`S-003`, `BDD-003`): una cita valida entra una sola vez, crea queue si aplica y deja el turno en `EnEsperaTaquilla`.
- `REC-02` idempotent replay (`S-003`, `BDD-003`): mismo `X-Idempotency-Key` reemite el resultado y no duplica turno.
- `REC-03` priority ordering (`S-003`): prioridad mas alta sale primero en `next-turn` y monitor.
- `REC-04` fifo within same priority (`S-003`): a igualdad de prioridad gana el menor `check-in time`.
- `REC-05` monitor from projections (`S-003`, `S-013`, `BDD-012`): monitor, queue state y recent history salen de proyecciones persistidas.
- `REC-06` receptionist authorization (`S-003`, `S-009`): `401` sin autenticacion y `403` con rol distinto a recepcion.

### Cashier

- `CASH-01` call next nominal (`S-004`, `BDD-004`): `EnEsperaTaquilla -> EnTaquilla` para el siguiente turno elegible.
- `CASH-02` validate payment nominal (`S-004`, `BDD-004`): `EnTaquilla -> EnEsperaConsulta` y el turno deja de contar como espera de caja.
- `CASH-03` pending then validate (`S-004`): `EnTaquilla -> PagoPendiente -> EnEsperaConsulta` manteniendo trazabilidad e intentos auditables.
- `CASH-04` pending remains operational (`S-004`, `05-domain/11-BUSINESS-RULES.md`): `PagoPendiente` no dispara cancelacion automatica y solo puede avanzar a `EnEsperaConsulta` o `CanceladoPorAusencia` mediante contratos soportados.
- `CASH-05` invalid current turn (`S-004`): no se puede validar pago para un turno distinto al actual en caja.
- `CASH-06` terminal absence rejection (`S-004`): un turno cancelado por ausencia en caja no puede reingresar a caja ni volver a consulta.
- `CASH-07` cashier authorization (`S-004`, `S-009`): `401` sin autenticacion y `403` con rol distinto a caja.
- `CASH-08` cashier absence terminal (`S-004`, `BDD-004`): `EnTaquilla` o `PagoPendiente` terminan en `CanceladoPorAusencia`, el monitor lo materializa como `Absent` y el turno sale del flujo de caja sin consumir nuevos intentos de pago.

### Consulting rooms and doctors

- `ROOM-01` activate inactive room (`S-002`, `BDD-002`): activacion exitosa deja el consultorio elegible para claim.
- `ROOM-02` inactive room cannot claim (`S-002`, `S-005`): un consultorio inactivo no puede reservar ni llamar pacientes.
- `ROOM-03` claim next with active idle room (`S-005`): solo un consultorio activo y libre puede reclamar el siguiente turno elegible.
- `ROOM-04` no concurrent occupancy (`S-002`, `S-005`): un consultorio ocupado no puede recibir un segundo claim.
- `ROOM-05` call patient (`S-005`): `EnEsperaConsulta -> LlamadoConsulta` con consultorio visible en monitor cuando aplique.
- `ROOM-06` start consultation (`S-005`): solo un turno correctamente llamado puede pasar a `EnConsulta`.
- `ROOM-07` finish consultation releases room (`S-002`, `S-005`): `EnConsulta -> Finalizado` y el consultorio queda libre.
- `ROOM-08` mark absent in consultation (`S-005`): `LlamadoConsulta -> CanceladoPorAusencia` y el consultorio vuelve a quedar disponible.
- `ROOM-09` deactivate idle room (`S-002`): desactivacion exitosa solo cuando no existe atencion activa.
- `ROOM-10` reject deactivation of occupied room (`S-002`): desactivar un consultorio ocupado debe fallar y dejar traza auditable.
- `ROOM-11` supervisor-only lifecycle (`S-002`, `S-009`): doctor, caja o recepcion no pueden activar ni desactivar consultorios.

### Patient lifecycle outcomes

- `PAT-01` full nominal patient (`S-003`, `S-004`, `S-005`, `S-011`): check-in, caja validada, consulta finalizada y trayectoria cerrada en `TrayectoriaFinalizada`.
- `PAT-02` patient with payment retry (`S-004`, `S-011`): pago pendiente una o dos veces antes de completar consulta.
- `PAT-03` patient cancelled by cashier absence (`S-004`, `S-011`): una ausencia en caja dispara `CanceladoPorAusencia` y `TrayectoriaCancelada`.
- `PAT-04` patient cancelled by consultation absence (`S-005`, `S-011`): llamado a consulta sin comparecencia termina en `CanceladoPorAusencia` y trayectoria cancelada.
- `PAT-05` no duplicate active trajectory (`S-011`, `BDD-010`): un mismo paciente no puede sostener dos trayectorias activas en la misma `QueueId`.
- `PAT-06` closed trajectory rejects new stages (`S-011`): una trayectoria finalizada o cancelada no admite nuevos hitos mutantes.
- `PAT-07` no unsupported cancellation flow (`S-011`, `S-013`): no se debe validar una cancelacion de paciente fuera del contrato canonico de ausencia operativa.

### Synchronized visibility, trajectory and audit

- `VIS-01` monitor taxonomy (`S-013`, `BDD-012`): `entries[*].status` y `statusBreakdown` usan exactamente `Waiting`, `AtCashier`, `PaymentPending`, `WaitingForConsultation`, `Called`, `InConsultation`, `Completed`, `Absent`.
- `VIS-02` waiting count semantics (`S-013`, `TDD-S-013`): `waitingCount` y `currentWaitingCount` solo suman `Waiting` y `WaitingForConsultation`.
- `VIS-03` active consultation rooms semantics (`S-013`): `activeConsultationRooms` y `activeRooms` solo reflejan entradas visibles en `InConsultation`.
- `VIS-04` dashboard from projections (`S-007`, `S-013`): el dashboard no usa replay en hot path y expone `projectionLagSeconds`.
- `VIS-05` trajectory discovery by patient (`S-011`, `BDD-010`): `GET /api/patient-trajectories` devuelve candidatas activas primero y luego por `openedAt` descendente.
- `VIS-06` trajectory query chronology (`S-011`): la trayectoria expone hitos ordenados cronologicamente y mantiene `trajectoryId` estable.
- `VIS-07` audit timeline chronology (`S-007`): el timeline por `correlationId` sale en orden cronologico ascendente y permite reconstruir el flujo.
- `VIS-08` monitor, dashboard and trajectory convergence (`S-013`): tras un flujo terminal no quedan entradas no terminales en monitor ni drift bloqueante entre monitor y trayectoria.

### Security and BFF

- `SEC-01` backend login for trusted clients (`S-001`, `S-009`): `POST /api/staff/auth/login` devuelve Bearer solo a cliente confiable.
- `SEC-02` session login does not leak token (`S-013`, `SEC-TEST-003`): `POST /api/session/login` entrega cookie `httpOnly` y nunca expone `accessToken`.
- `SEC-03` session summary is sanitized (`S-013`, `SEC-TEST-003`): `GET /api/session/me` devuelve resumen minimo o `null`, nunca secretos.
- `SEC-04` realtime stream same-origin (`S-013`, `BDD-012`, `SEC-TEST-003`): `/api/realtime/operations` opera con cookie de sesion y no requiere Bearer desde el browser.
- `SEC-05` realtime reconnect (`S-013`, `BDD-012`): una desconexion provoca reconexion y refetch de snapshots persistidos.
- `SEC-06` no PII in invalidation events (`S-013`, `TDD-S-013`): payload realtime solo contiene metadata aprobada de invalidacion.
- `SEC-07` 401 and 403 coverage (`S-009`, `08-security/08-SECURITY-TEST-REQUIREMENTS.md`): todos los endpoints protegidos y el stream validan ausencia de sesion y rol invalido.

### Rebuild, resilience and load

- `REL-01` rebuild dry-run (`S-011`, `RES-TEST-002`): el dry-run valida alcance sin materializar efectos.
- `REL-02` rebuild idempotency (`S-011`): mismo alcance y misma llave no dispara un rebuild duplicado.
- `REL-03` replay without side effects (`S-011`, `S-008`): el rebuild no reemite mensajes externos ni modifica eventos legacy.
- `REL-04` projection update after messaging (`S-008`, `S-013`): eventos publicados terminan reflejados en monitor, dashboard y trayectoria.
- `REL-05` deterministic docker smoke (`S-009`): flujo nominal de 1 a 10 pacientes con compose saludable y endpoints `200`.
- `REL-06` mixed-flow volume run (`S-013`, `RES-TEST-004`): corrida de 100 pacientes con mezcla de finalizados y ausencias terminales, 10 consultorios habilitados y 5 deshabilitados a mitad de la prueba.
- `REL-07` post-load cleanliness (`S-013`): despues de la corrida no quedan pacientes no terminales en monitor ni servicios degradados en compose.

## Blocking gaps to resolve before claiming perfect validation

- `BG-01` no existe un contrato canonico separado para una cancelacion voluntaria del paciente fuera de pago o ausencia; cualquier suite debe evitar inventar ese flujo.
- `BG-02` `activeRooms` en dashboard sigue el contrato visible de `InConsultation`, no la cantidad total de consultorios activados; la asercion debe seguir el contrato vigente.

## Recommended execution batches

1. `batch-a`: `REC-*`, `CASH-*`, `ROOM-*` como unitarias e integracion backend.
2. `batch-b`: `VIS-*` y `SEC-*` como integracion backend + frontend BFF.
3. `batch-c`: `PAT-*` y `REL-01` a `REL-04` como docker end-to-end con evidencia de trayectoria y auditoria.
4. `batch-d`: `REL-05` a `REL-07` como smoke y volumen con compose real y harness `.tmp/rlapp-patient-simulation.mjs`.

## Decision rule

No declarar el sistema "perfectamente validado" mientras exista algun caso critico rojo en idempotencia, RBAC, convergencia entre monitor/dashboard/trayectoria, desactivacion de consultorios ocupados o drift documental sobre politicas de ausencia y cancelacion.
