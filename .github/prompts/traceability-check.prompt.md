---
name: traceability-check
description: "Use when you need an exact RLApp traceability map before implementation, QA, or remediation."
argument-hint: "<capability, feature, or requested change>"
agent: "traceability-auditor"
---

Resuelve la trazabilidad exacta antes de cualquier cambio.

## Inputs requeridos

- Capacidad, feature o cambio solicitado.

## Procedimiento obligatorio

1. Identificar `US-XXX` aplicable.
2. Resolver `UC-XXX` asociado.
3. Resolver `S-XXX`, `BDD-XXX` y `TDD-S-XXX`.
4. Identificar archivos canonicos de dominio, contratos, seguridad y arquitectura.
5. Reportar cualquier enlace nominal, faltante o ambiguo.

## Output esperado

- Cadena exacta de trazabilidad.
- Artefactos revisados.
- Archivos canonicos aplicables.
- Bloqueos documentales antes de implementar.
