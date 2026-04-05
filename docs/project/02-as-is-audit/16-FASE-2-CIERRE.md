# Fase 2 — Cierre

**Fecha:** 2026-04-05
**Rama de trabajo:** feature/fase2-trayectoria-paciente-kickoff
**Estado:** completada

## Objetivo cumplido

Fase 2 cierra la implementacion operativa de `TrayectoriaPaciente` como agregado longitudinal con `TrajectoryId` determinista, proyeccion persistida, query protegida, rebuild controlado y consola frontend utilizable en el perfil oficial de Docker Compose.

## Trazabilidad

- US: `US-012`, `US-016`, `US-018`
- UC: `UC-016`, `UC-018`
- S: `S-008`, `S-009`, `S-011`
- BDD/TDD: `BDD-008`, `BDD-010`, `TDD-S-008`, `TDD-S-009`, `TDD-S-011`, `SEC-TEST-001`, `SEC-TEST-003`, `RES-TEST-001`, `RES-TEST-002`, `RES-TEST-003`

## Decision tecnica

- `TrayectoriaPaciente` se mantiene dentro del bounded context `Waiting Room`, sin abrir un modulo adicional fuera de la arquitectura objetivo aprobada.
- La API ejecutable se normaliza al prefijo operativo existente `api/...` y reutiliza `X-Idempotency-Key` para conservar consistencia con el resto de controladores y con el proxy frontend.
- El frontend incorpora una consola especifica de trayectoria en lugar de inventar pantallas CRUD: consulta por `trajectoryId` conocido y rebuild `dryRun/materialized` solo para `Support`.
- Docker Compose pasa a ser evidencia obligatoria del slice: el stack local levanta `db`, `backend` y `frontend` saludables y permite ejecutar el flujo protegido de soporte desde la UI real.

## Entregables cerrados

1. Agregado `TrayectoriaPaciente` implementado con replay desde eventos historicos y proyeccion persistida.
2. Contrato OpenAPI auditado actualizado y tipos frontend regenerados.
3. Consola frontend `/trajectory` alineada al contrato real y al modelo de autorizacion `Support`/`Supervisor`.
4. Seed local de usuarios `Supervisor` y `Support` para hacer usable el slice al levantar Docker.
5. Evidencia automatizada y smoke Docker del flujo protegido de rebuild a traves del proxy de sesion del frontend.

## Validacion

- `dotnet test RLApp.slnx --filter 'FullyQualifiedName~PatientTrajectory|FullyQualifiedName~LocalOutbox'`: `9/9` pruebas en verde.
- `npm run typecheck`: correcto.
- `npm run build`: correcto.
- `docker compose --profile backend --profile frontend up --build -d`: `db`, `backend` y `frontend` saludables.
- Smoke end-to-end por frontend:
  - `POST /api/session/login` con `support` -> `200`
  - `GET /trajectory` con cookie de sesion -> `200`
  - `POST /api/proxy/patient-trajectories/rebuild` con `dryRun=true` -> `200`

## Riesgos remanentes

- La consulta sigue requiriendo `trajectoryId` conocido; no existe todavia busqueda por `patientId` o `queueId`.
- La migracion de correlacion de sagas a `TrajectoryId` permanece fuera de Fase 2 y sigue reservada para slices posteriores.
- `S-009` todavia necesita automatizacion completa de evidencia de observabilidad y thresholds end-to-end, aunque el runtime Docker ya quedo validado.

## Siguiente corte propuesto

- Abrir el siguiente slice sobre descubrimiento de trayectorias y observabilidad end-to-end antes de mover la correlacion de sagas.
- Mantener como baseline el arranque Docker con usuarios seeded y smoke del proxy frontend para cualquier cambio posterior del slice.
