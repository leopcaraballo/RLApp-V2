---
name: full-flow
description: "Use when executing the full delivery flow from canonical user story and spec through tests, implementation, and validation."
argument-hint: "<US-XXX> <S-XXX> [scope summary]"
agent: "rlapp-delivery"
---

Orquesta el flujo completo sin crear fuentes paralelas.

## Inputs requeridos

- Historia `US-XXX`.
- Spec `S-XXX`.
- Resumen de alcance o modulos impactados.

## Flujo obligatorio

1. `Git Flow Check`: confirmar que la rama activa sigue `feature/*` y que la integracion sera por Pull Request.
2. `User Story`: leer `/docs/project/10-product/user-stories/US-XXX-*.md`.
3. `Spec`: leer `/docs/project/11-specifications/S-XXX-*.md`.
4. `BDD`: leer el feature aplicable en `/docs/project/12-testing/bdd`.
5. `TDD`: leer el archivo correspondiente en `/docs/project/12-testing/tdd`.
6. `Policy Review`: validar runtime policy, quality gates y riesgos operativos antes de editar o ejecutar.
7. `Implementation`: ejecutar cambios backend, frontend, contratos o datos solo dentro de la arquitectura aprobada.
8. `Validation`: revisar quality gates, seguridad, Git Flow y trazabilidad.

## Reglas

- Detener el flujo si falta alguna pieza de trazabilidad.
- Detener el flujo si la rama activa es `main` o `develop`.
- Si la spec requiere cambios, actualizar primero `/docs/project` y luego el codigo.
- No considerar terminado un cambio sin pruebas y validacion explicita.

## Output esperado

- Plan de ejecucion por fase.
- Artefactos revisados y decision.
- Cambios implementados con trazabilidad.
- Resultado final de validacion y riesgos remanentes.
