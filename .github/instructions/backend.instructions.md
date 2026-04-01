---
applyTo: "**/*.{cs,csproj,sln}"
description: "Use when editing backend, API, worker, application, domain, infrastructure, messaging, projections, or persistence code."
---

# Backend Instructions

**Estado del proyecto:** Fase 0 — Diagnóstico Ejecutivo completado (2026-04-01). Ver: /docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md

Usa `/docs/project` como fuente de verdad. Antes de escribir backend, lee como minimo:

1. `/docs/project/11-specifications/S-XXX-*.md`
2. `/docs/project/05-domain/06-INVARIANTS.md`
3. `/docs/project/05-domain/11-BUSINESS-RULES.md`
4. `/docs/project/06-application/01-USE-CASE-CATALOG.md`
5. `/docs/project/06-application/02-APPLICATION-SERVICES.md`
6. `/docs/project/07-interfaces-and-contracts`
7. `/docs/project/09-data-and-messaging`
8. `/docs/project/08-security`

## Reglas de implementacion

- Backend objetivo: API .NET 10 + background worker + PostgreSQL + RabbitMQ.
- Separar endpoints de comando y endpoints de consulta segun `/docs/project/07-interfaces-and-contracts/01-API-STYLE-GUIDE.md`.
- Los servicios de aplicacion coordinan puertos, cargan agregados, invocan dominio y persisten resultados. No alojan reglas nucleares de negocio.
- El dominio implementa invariantes, reglas y transiciones; no conoce HTTP, SQL, RabbitMQ, SignalR ni frameworks.
- La persistencia de write-side, event store, outbox y proyecciones debe respetar `/docs/project/09-data-and-messaging`.
- La pantalla publica y cualquier read model visible nunca se construyen desde el write model en linea.
- Los envelopes de error, headers y metadata deben seguir `/docs/project/07-interfaces-and-contracts`.

## Prohibiciones

- No escribir endpoints, comandos, queries ni eventos que no aparezcan en la spec o en contratos canonicos.
- No saltar capas ni resolver dependencias con accesos directos entre API, Application, Domain, Infrastructure o Projections.
- No acceder al storage interno de otro modulo.
- No introducir reglas de negocio nuevas fuera de `/docs/project/05-domain` y `/docs/project/11-specifications`.

## Checklist minimo antes de finalizar

- Citar `US-XXX`, `UC-XXX` y `S-XXX` aplicables.
- Confirmar modulo objetivo con `/docs/project/03-target-architecture/05-MODULE-BOUNDARIES.md`.
- Confirmar estados, eventos e idempotencia si el cambio altera flujos transaccionales.
- Escribir primero o actualizar pruebas ligadas a `/docs/project/12-testing/tdd` y `/docs/project/12-testing/bdd`.
