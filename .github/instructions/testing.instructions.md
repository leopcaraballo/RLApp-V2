---
description: "Use when designing, writing, or reviewing tests, coverage, quality gates, BDD scenarios, TDD plans, or validation evidence."
---

# Testing Instructions

Toda prueba debe nacer desde `/docs/project`, no desde supuestos del codigo.

## Leer antes de probar

1. `/docs/project/12-testing/01-TEST-STRATEGY.md`
2. `/docs/project/12-testing/02-QUALITY-GATES.md`
3. `/docs/project/12-testing/03-COVERAGE-MODEL.md`
4. `/docs/project/12-testing/bdd`
5. `/docs/project/12-testing/tdd`
6. `/docs/project/15-traceability/05-SPECS-TO-TESTS.md`
7. `/docs/project/08-security/08-SECURITY-TEST-REQUIREMENTS.md`

## Reglas ejecutables

- Secuencia obligatoria: `BDD -> TDD -> implementacion de pruebas -> validacion`.
- Cubrir dominio, aplicacion, infraestructura e integracion segun el impacto del cambio.
- Incluir pruebas de seguridad minimas cuando haya autenticacion, autorizacion, display publico, secretos o configuracion.
- La calidad minima exige build limpio, pruebas en verde y trazabilidad completa.
- Cada prueba debe referenciar `S-XXX` y, cuando aplique, `BDD-XXX` o `TDD-S-XXX`.

## Rechazar si

- no existe spec asociada
- el comportamiento validado no esta documentado
- faltan pruebas en capas afectadas por la change set
- la evidencia no cubre invariantes, contratos o reglas de seguridad relevantes
