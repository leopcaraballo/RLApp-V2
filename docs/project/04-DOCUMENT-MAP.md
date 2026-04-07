# Document Map

## Purpose

Proveer una vista maestra de la estructura documental y de la dependencia entre secciones.

## Sections

- 01-foundation: reglas fundacionales
- 02-as-is-audit: evidencia objetiva del sistema actual
  - 02-as-is-audit/10-FASE-0-DIAGNOSTICO.md: Fase 0 — Diagnóstico Ejecutivo (2026-04-01)
  - 02-as-is-audit/13-FASE-1-KICKOFF.md: Fase 1 — inicio de estabilizacion de propagacion y observabilidad
  - 02-as-is-audit/14-FASE-1-CIERRE.md: Fase 1 — cierre tecnico, evidencia y decision sobre LISTEN/NOTIFY
  - 02-as-is-audit/15-FASE-2-KICKOFF.md: Fase 2 — inicio documental del agregado TrayectoriaPaciente y replay controlado
  - 02-as-is-audit/16-FASE-2-CIERRE.md: Fase 2 — cierre tecnico, evidencia Docker-first y consola operativa de trayectoria
  - 02-as-is-audit/17-FASE-3-KICKOFF.md: Fase 3 — inicio documental de concurrencia optimista en EventStore
  - 02-as-is-audit/18-FASE-3-CIERRE.md: Fase 3 — cierre tecnico de concurrencia optimista, evidencia backend y validacion Docker-first
  - 02-as-is-audit/19-FASE-4-KICKOFF.md: Fase 4 — inicio documental de correlacion de sagas y state machines por `trajectoryId`
  - 02-as-is-audit/20-FASE-4-CIERRE.md: Fase 4 — cierre tecnico de correlacion longitudinal de sagas y persistencia durable
  - 02-as-is-audit/21-FASE-5-KICKOFF.md: Fase 5 — inicio documental de discovery operacional de trayectorias desde proyecciones persistidas
  - 02-as-is-audit/22-FASE-6-KICKOFF.md: Fase 6 — inicio documental de visibilidad operacional sincronizada y realtime mediado por BFF
  - 02-as-is-audit/23-FASE-6-AUDITORIA-CODIGO-REAL-TRAYECTORIAS.md: Fase 6 — auditoria basada en codigo real del estado actual de trayectorias, CQRS, proyecciones y realtime
- 03-target-architecture: arquitectura objetivo del proyecto nuevo
- 04-adr: decisiones formales
- 05-domain: modelo de dominio, estados y eventos
- 06-application: casos de uso y puertos
- 07-interfaces-and-contracts: APIs y contratos internos
- 08-security: baseline de seguridad
- 09-data-and-messaging: persistencia, eventos y mensajeria
- 10-product: actores, journeys e historias
- 11-specifications: requisitos funcionales y tecnicos
- 12-testing: estrategia y artefactos de prueba
  - 12-testing/06-OPERATIONAL-FLOW-TEST-PLAN.md: plan integrado de pruebas para pacientes, staff, consultorios, trayectoria, monitor, dashboard y realtime
- 13-operations: operacion y observabilidad
- 14-diagrams: soporte visual
- 15-traceability: matrices formales
- 16-generation-pack: reglas para generacion con IA

## Dependency flow

foundation -> as-is-audit -> target-architecture -> adr -> domain -> application -> contracts -> security/data -> product/specs -> testing -> operations -> traceability -> generation-pack
