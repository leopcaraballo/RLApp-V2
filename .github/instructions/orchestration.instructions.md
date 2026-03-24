---
name: "Delivery Orchestration"
description: "Use when orchestrating a feature, audit, migration, or cross-cutting change through the RLApp docs-first flow: user story, use case, spec, BDD, TDD, implementation, and validation."
excludeAgent: "code-review"
---

# Delivery Orchestration Instructions

Aplica este flujo cuando la tarea cruza varias capas o requiere decidir si primero se actualiza documentacion, prompts, workflows o codigo.

## Secuencia obligatoria

1. Leer `/.ai-entrypoint.md` para resolver la capa correcta.
2. Confirmar que la rama activa cumple `feature/*`; detener si la rama es `main` o `develop`.
3. Resolver `US-XXX`.
4. Resolver `UC-XXX`.
5. Resolver `S-XXX`.
6. Resolver `BDD-XXX` y `TDD-S-XXX`.
7. Validar dominio, contratos, seguridad y arquitectura.
8. Actualizar primero `/docs/project` si hay vacios.
9. Ejecutar policy review antes de editar o ejecutar.
10. Implementar cambios tecnicos solo cuando la cadena anterior sea suficiente.
11. Validar CI, quality gates, Git Flow y riesgos remanentes.

## Salida minima esperada

- IDs y archivos usados en la trazabilidad.
- Artefactos revisados.
- Bloqueos documentales detectados.
- Decision explicita.
- Cambios realizados por fase.
- Validacion final y riesgos.

## Detener si

- falta `US-XXX`, `UC-XXX` o `S-XXX`
- no existe trazabilidad hacia pruebas
- los contratos o transiciones requeridas no estan documentados
- la rama activa no sigue `feature/*`
