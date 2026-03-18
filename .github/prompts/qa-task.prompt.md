---
name: qa-task
description: "Use when validating a feature against canonical specs, BDD, TDD, security, resilience, and traceability requirements."
argument-hint: "<S-XXX> <US-XXX> [changed modules]"
agent: "policy-reviewer"
---

Ejecuta QA documental y tecnico sin salir de `/docs/project`.

## Inputs requeridos

- Spec `S-XXX`.
- Historia `US-XXX`.
- Modulos o componentes impactados.

## Procedimiento obligatorio

1. Leer la spec y la historia asociada.
2. Leer `/docs/project/12-testing/01-TEST-STRATEGY.md`, `/docs/project/12-testing/02-QUALITY-GATES.md` y `/docs/project/12-testing/03-COVERAGE-MODEL.md`.
3. Leer BDD, TDD, seguridad y resiliencia aplicables.
4. Validar la cadena de trazabilidad completa con `/docs/project/15-traceability`.

## Reglas

- Rechazar si falta `S-XXX`, `US-XXX`, pruebas asociadas o contratos documentados.
- Rechazar si la implementacion rompe arquitectura, seguridad o reglas de dominio.
- Reportar riesgos de calidad antes que resumenes cosmeticos.

## Output esperado

- Traceability
- Artifacts Reviewed
- Blocking Gaps
- Decision
- Validation
- Residual Risks

## Nota de reporting

- Dentro de `Validation`, reportar hallazgos por severidad antes de cualquier resumen cosmetico.
