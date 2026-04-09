# Synchronized Trajectory QA Test Cases

## Purpose

Catalogar los casos QA del slice de trayectoria sincronizada reutilizando IDs y familias canonicas ya aprobadas, y mapear su cobertura actual sobre las tres implementaciones de automatizacion.

## Traceability

- Specs: `S-011`, `S-013`, `S-009`
- BDD base: `BDD-010`, `BDD-012`
- TDD base: `TDD-S-011`, `TDD-S-013`, `TDD-S-009`
- Seguridad y resiliencia: `SEC-TEST-001`, `SEC-TEST-003`, `RES-TEST-002`, `RES-TEST-004`

## Coverage overview

| Anchor | Objetivo | API Screenplay | Front POM | Front Screenplay | Estado actual |
| --- | --- | --- | --- | --- | --- |
| `PAT-01` | cerrar una trayectoria nominal como `TrayectoriaFinalizada` | Automated | Not applicable | Not applicable | Automated |
| `VIS-05` | descubrir trayectorias por `patientId` desde proyeccion persistida | Automated | Planned positive path | Planned positive path | Partial |
| `VIS-05` negativo | retornar vacio cuando no existen candidatas | Automated | Automated | Automated | Automated |
| `VIS-06` | consultar detalle por `trajectoryId` con hitos cronologicos | Automated | Planned | Planned | Automated |
| `REL-01` | ejecutar rebuild en dry-run sin side effects | Automated | Not applicable | Not applicable | Automated |
| `SEC-07` | validar `401` y `403` sobre contratos y consola protegida | Planned | Planned | Planned | Planned |
| `VIS-08` | convergencia entre flujo terminal, trayectoria y vista sincronizada | Planned | Planned | Planned | Planned |
| `S-011` + `S-013` | acceso autorizado a la consola protegida de trayectoria | Not applicable | Automated | Automated | Automated |

## Case details

### Case `PAT-01` - Full nominal patient closes trajectory

- Canonical anchors: `PAT-01`, `S-011`, `BDD-010`, `TDD-S-011`
- Preconditions: existe una cola operativa valida, actor autenticado como `Supervisor`, y flujo de recepcion, caja y consulta disponible.
- Steps:
  1. registrar la llegada del paciente
  2. completar llamado y validacion de caja
  3. activar consultorio, llamar paciente, iniciar consulta y finalizarla
  4. consultar trajectory discovery y detalle
- Expected result: al menos una trayectoria candidata para el paciente y detalle final en `TrayectoriaFinalizada`.
- Coverage: automatizado en `AUTO_API_SCREENPLAY`; no aplica para las dos implementaciones UI porque el cierre nominal se valida por API.

### Case `VIS-05` - Trajectory discovery by patient

- Canonical anchors: `VIS-05`, `S-011`, `BDD-010`, `TDD-S-011`
- Preconditions: existe al menos una trayectoria materializada para el `patientId` consultado.
- Steps:
  1. consultar `GET /api/patient-trajectories` con `patientId`
  2. opcionalmente acotar por `queueId`
  3. revisar orden de candidatas y datos visibles
- Expected result: discovery desde proyeccion persistida, trayectorias activas primero, luego `openedAt` descendente.
- Coverage: automatizado por API; el camino positivo en UI queda planificado para POM y Screenplay.

### Case `VIS-05` negativo - Discovery returns empty

- Canonical anchors: `VIS-05`, `S-011`
- Preconditions: `patientId` inexistente o sin trayectoria materializada.
- Steps:
  1. buscar discovery por un `patientId` inexistente
  2. observar respuesta o mensaje visual
- Expected result: `total = 0`, lista vacia o mensaje de sin resultados, sin error de contrato.
- Coverage: automatizado en API Screenplay, Front POM y Front Screenplay.

### Case `VIS-06` - Direct trajectory query chronology

- Canonical anchors: `VIS-06`, `S-011`, `TDD-S-011`
- Preconditions: ya existe una candidata valida y se conoce el `trajectoryId`.
- Steps:
  1. descubrir la trayectoria del paciente
  2. consultar `GET /api/patient-trajectories/{trajectoryId}`
  3. inspeccionar hitos y metadata fuente
- Expected result: `trajectoryId` estable, hitos cronologicos y cada etapa con `occurredAt` y `sourceEvent`.
- Coverage: automatizado por API; queda planificado para UI como extension natural del discovery positivo.

### Case `REL-01` - Rebuild dry-run

- Canonical anchors: `REL-01`, `S-011`, `RES-TEST-002`
- Preconditions: actor autenticado como `Support`, historial del paciente disponible, `X-Correlation-Id` controlado por el cliente.
- Steps:
  1. solicitar `POST /api/patient-trajectories/rebuild` con `dryRun = true`
  2. inspeccionar payload de resultado
- Expected result: respuesta `200`, `dryRun = true`, `jobId` presente y reporte de eventos o trayectorias procesadas sin efectos laterales visibles.
- Coverage: automatizado en API Screenplay; no aplica para las implementaciones UI del slice actual.

### Case `SEC-07` - Unauthorized and forbidden trajectory access

- Canonical anchors: `SEC-07`, `S-009`, `SEC-TEST-001`, `SEC-TEST-003`
- Preconditions: cliente sin sesion o con rol insuficiente.
- Steps:
  1. intentar consultar discovery, detalle o rebuild sin autenticacion
  2. intentar abrir la consola protegida con rol no autorizado
  3. intentar rebuild con `Supervisor` en lugar de `Support`
- Expected result: `401` sin sesion y `403` con rol invalido segun contrato, sin bypass por headers legacy ni exposicion de token en browser.
- Coverage: planificado para las tres implementaciones.

### Case `VIS-08` - Terminal flow convergence

- Canonical anchors: `VIS-08`, `S-013`, `BDD-012`, `TDD-S-013`, `RES-TEST-004`
- Preconditions: flujo terminal ya ejecutado, snapshots persistidos disponibles y canal realtime habilitado.
- Steps:
  1. ejecutar un flujo terminal completo
  2. esperar invalidacion o refetch de la superficie sincronizada
  3. comparar snapshot de trayectoria con la vista de staff
- Expected result: no queda drift bloqueante entre trayectoria y la vista sincronizada tras el cierre del flujo.
- Coverage: planificado para las tres implementaciones.

### Case `S-011` + `S-013` - Authorized trajectory console access

- Canonical anchors: `S-011`, `S-013`, `US-018`, `US-021`
- Preconditions: sesion valida de `Supervisor`.
- Steps:
  1. autenticarse como `Supervisor`
  2. abrir la pagina protegida `/trajectory`
  3. verificar disponibilidad de la seccion de consulta y del estado visual base
- Expected result: la consola carga, mantiene labels funcionales de trayectoria y no requiere exponer `accessToken` al browser.
- Coverage: automatizado en Front POM y Front Screenplay; no aplica a API Screenplay.
