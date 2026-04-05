# Project Docs Enterprise

## Purpose

Este paquete documental define la base Enterprise para generar con IA un proyecto nuevo, limpio y consistente para la operacion de sala de espera medica.

## Scope

- recepcion, caja y consulta medica
- staff interno autenticado
- pantalla publica sanitizada para llamados y estado de espera
- backend .NET 10 con arquitectura hexagonal estricta
- Event Sourcing, CQRS, PostgreSQL, RabbitMQ y Outbox Pattern

## Out of scope

- autogestion del paciente
- correo, SMS o push
- multi-tenancy
- agenda medica completa y booking self-service

## Reading order

1. 01-foundation
2. 02-as-is-audit
3. 03-target-architecture
4. 04-adr
5. 05-domain
6. 06-application
7. 07-interfaces-and-contracts
8. 08-security
9. 09-data-and-messaging
10. 10-product
11. 11-specifications
12. 12-testing
13. 13-operations
14. 14-diagrams
15. 15-traceability
16. 16-generation-pack

## Non-negotiable rules

- no mezclar AS-IS con TO-BE sin separacion explicita
- no documentar capacidades que no esten aprobadas en el alcance
- no romper la cadena ADR -> Design -> Use Case -> Spec -> State/Event -> Test
- no introducir decisiones de arquitectura fuera del catalogo de ADRs

## Estado del proyecto (resumen)

- Fase 0 — Diagnóstico Ejecutivo completado (2026-04-01). Ver: docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md
- Fase 1 — Completada (2026-04-01). Ver: docs/project/02-as-is-audit/14-FASE-1-CIERRE.md
- Fase 2 — Completada (2026-04-05). Ver: docs/project/02-as-is-audit/16-FASE-2-CIERRE.md
- Fase 3 — Completada (2026-04-05). Ver: docs/project/02-as-is-audit/18-FASE-3-CIERRE.md
