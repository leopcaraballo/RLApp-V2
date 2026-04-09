# Synchronized Trajectory QA Test Plan

## Purpose

Definir la planeacion QA del slice de trayectoria sincronizada como una descomposicion controlada de `12-testing/06-OPERATIONAL-FLOW-TEST-PLAN.md`, sin crear una fuente de verdad paralela por repositorio de automatizacion.

## Traceability

- User stories: `US-012`, `US-018`, `US-021`
- Use cases: `UC-018`, `UC-021`
- Specs: `S-011`, `S-013`, `S-009`
- BDD base: `BDD-010`, `BDD-012`
- TDD base: `TDD-S-011`, `TDD-S-013`, `TDD-S-009`
- Seguridad y resiliencia: `SEC-TEST-001`, `SEC-TEST-003`, `RES-TEST-002`, `RES-TEST-004`
- Catalogo operacional reutilizado: `PAT-01`, `VIS-05`, `VIS-06`, `VIS-08`, `REL-01`, `SEC-07`

## Scope

- discovery, query directa y rebuild controlado de trayectoria desde contratos protegidos
- consola protegida de trayectoria para staff autenticado
- validacion de lectura desde proyecciones persistidas y no desde replay en hot path
- validacion de sesion segura para frontend, same-origin realtime y no exposicion de `accessToken`
- planeacion de cobertura para `AUTO_API_SCREENPLAY`, `AUTO_FRONT_POM_FACTORY` y `AUTO_FRONT_SCREENPLAY`

## Out of scope

- display publico anonimo y sus snapshots sanitizados
- creacion de una taxonomia nueva de estados o cierres no aprobados por `S-011` y `S-013`
- numeracion nueva de casos fuera de `PAT`, `VIS`, `REL`, `SEC`, `BDD` y `TDD`
- convertir los repositorios de automatizacion en fuente primaria de reglas QA

## Entry criteria

- rama activa `feature/*`
- cadena documental cerrada para `US-018 -> UC-018 -> S-011 -> BDD-010 -> TDD-S-011`
- cadena documental cerrada para `US-021 -> UC-021 -> S-013 -> BDD-012 -> TDD-S-013`
- quality gates base del slice en verde segun `12-testing/02-QUALITY-GATES.md`
- entorno local o Docker operativo con PostgreSQL y RabbitMQ reales o equivalentes controlados
- usuarios de prueba disponibles para `Supervisor` y `Support`
- proyectos de automatizacion compilando y con dependencias resueltas

## Exit criteria

- suites automatizadas del slice en verde en las tres implementaciones
- evidencia de ejecucion disponible para Gradle y Serenity donde aplique
- ningun caso critico queda rojo en RBAC, cronologia de hitos, dry-run de rebuild o sesion segura en frontend
- la cobertura documentada distingue claramente entre automatizado, planificado y no aplicable
- cualquier gap residual queda registrado sin inventar comportamiento fuera de `docs/project`

## Environments and tools

| Implementacion | Tipo | Objetivo principal | Comando base | Evidencia principal |
| --- | --- | --- | --- | --- |
| `AUTO_API_SCREENPLAY` | API Screenplay | contratos protegidos de trajectory discovery, query y rebuild | `./gradlew clean test --no-daemon` | `build/test-results/test`, `target/site/serenity` |
| `AUTO_FRONT_POM_FACTORY` | UI POM | acceso autorizado a consola y mensajes de vacio | `./gradlew clean test --no-daemon` | `build/test-results/test`, `target/site/serenity` |
| `AUTO_FRONT_SCREENPLAY` | UI Screenplay | acceso autorizado, disponibilidad visual y vacio de resultados | `./gradlew clean test --no-daemon` | `build/test-results/test`, `target/site/serenity` |

## Coverage lanes

### Lane A - Protected trajectory contracts

- validar `PAT-01`, `VIS-05`, `VIS-06` y `REL-01` en el contrato HTTP protegido
- asegurar que `Supervisor` consulte discovery y detalle segun contrato vigente
- asegurar que `Support` ejecute rebuild controlado segun `SupportOnly`

### Lane B - Protected trajectory console

- validar que `Supervisor` pueda abrir la consola protegida sin romper `S-011` ni `S-013`
- validar el estado vacio de busqueda cuando no existen trayectorias candidatas
- mantener la cobertura capability-first: la UI automatiza reglas del dominio y del contrato, no copy arbitraria del repositorio de automatizacion

### Lane C - Security and synchronized visibility follow-up

- registrar como planificados los negativos explicitos de `401` y `403` sobre contratos y superficies protegidas
- registrar como planificada la convergencia `VIS-08` entre flujo terminal, trayectoria, vista staff y realtime
- registrar como planificada la verificacion explicita de no exposicion de `accessToken` en la superficie de trayectoria

## Evidence to collect

- salida de Gradle por suite
- `build/test-results/test/*.xml` para conteos y fallos
- `target/site/serenity/index.html` para trazabilidad de escenarios automatizados
- rutas de features, step definitions, runners y tareas que materializan cada caso
- decision explicita por caso: `automated`, `planned`, `not-applicable`

## Delivery notes

- `08-SYNCHRONIZED-TRAJECTORY-QA-TEST-CASES.md` contiene el catalogo compartido de casos para este slice
- `12-testing/qa/README.md` concentra los anexos por implementacion y evita duplicar reglas en tres planes independientes
- cualquier extension de cobertura debe nacer desde `S-011`, `S-013`, `BDD-010`, `BDD-012`, `TDD-S-011`, `TDD-S-013` y el plan operacional integrado
