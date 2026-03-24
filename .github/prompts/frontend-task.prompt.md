---
name: frontend-task
description: "Use when implementing staff UI or public display flows from canonical product docs, specs, contracts, and tests."
argument-hint: "<S-XXX> <US-XXX> [screen or flow]"
agent: "rlapp-delivery"
---

Implementa frontend siguiendo `User Story -> Spec -> BDD -> TDD -> Implementation -> Validation`.

## Inputs requeridos

- Spec `S-XXX`.
- Historia `US-XXX`.
- Pantalla, journey o flujo objetivo.

## Procedimiento obligatorio

1. Leer la spec en `/docs/project/11-specifications`.
2. Leer la historia en `/docs/project/10-product/user-stories`.
3. Leer journeys y mapa de casos de uso en `/docs/project/10-product`.
4. Leer contratos REST, realtime, errores y display en `/docs/project/07-interfaces-and-contracts`.
5. Leer seguridad del display y autenticacion en `/docs/project/08-security`.
6. Leer BDD y TDD antes de modificar la UI.

## Reglas

- No crear acciones, campos o estados visuales fuera de specs y contratos.
- No mezclar experiencia de staff autenticado con display anonimo.
- No mover reglas de negocio al cliente.

## Output esperado

- Traceability.
- Artifacts Reviewed.
- Changes Applied.
- Validation.
- Residual Risks.
