---
applyTo: "**/*.{ts,tsx,js,jsx,css,scss}"
description: "Use when editing staff web UI, public display UI, realtime client, frontend tests, or product-facing interaction flows."
---

# Frontend Instructions

**Estado del proyecto:** Fase 0 — Diagnóstico Ejecutivo completado (2026-04-01). Ver: /docs/project/02-as-is-audit/10-FASE-0-DIAGNOSTICO.md

Frontend no define comportamiento; lo materializa desde `/docs/project`.

Lee primero:

1. `/docs/project/11-specifications/S-XXX-*.md`
2. `/docs/project/10-product/02-OPERATIONAL-JOURNEYS.md`
3. `/docs/project/10-product/03-USE-CASE-MAP.md`
4. `/docs/project/10-product/user-stories/US-XXX-*.md`
5. `/docs/project/07-interfaces-and-contracts`
6. `/docs/project/08-security`
7. `/docs/project/12-testing`

## Reglas de implementacion

- Cada vista, componente, accion y estado visual debe mapear a `US-XXX`, `UC-XXX` y `S-XXX`.
- Consumir solo contratos documentados: command endpoints, query endpoints, realtime contracts, error contracts y metadata canonica.
- Mantener separados flujos de staff autenticado y display publico.
- El display publico es anonimo, sanitizado y de solo lectura; nunca expone mutaciones ni datos internos del write-side.
- No inventar campos, filtros, acciones, labels operativos o estados que no existan en specs o contratos.
- La UI de monitoreo, caja, consulta, display y auditoria debe seguir journeys y reglas de visibilidad oficiales.

## Prohibiciones

- No codificar decisiones de producto fuera de `/docs/project/10-product`.
- No consumir endpoints no documentados.
- No reconstruir reglas de negocio en el cliente si pertenecen al dominio o a la aplicacion.
- No mezclar vistas de staff con capacidades anonimas del display.

## Checklist minimo antes de finalizar

- Confirmar historias de usuario y spec asociadas.
- Validar contratos en `/docs/project/07-interfaces-and-contracts`.
- Incluir pruebas ligadas a BDD o TDD aplicable.
- Verificar restricciones de seguridad para autenticacion, autorizacion y display publico.
