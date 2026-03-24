---
name: backend-task
description: "Use when implementing or modifying backend behavior from a canonical spec, mapped to use cases, domain rules, contracts, and tests."
argument-hint: "<S-XXX> <UC-XXX> [module]"
agent: "rlapp-delivery"
---

Implementa backend solo despues de pasar por `User Story -> Spec -> BDD -> TDD`.

## Inputs requeridos

- Spec `S-XXX`.
- Caso de uso `UC-XXX`.
- Modulo objetivo.
- Historia `US-XXX` si el impacto funcional no es obvio.

## Procedimiento obligatorio

1. Leer `/docs/project/11-specifications/S-XXX-*.md`.
2. Confirmar `UC-XXX` y `US-XXX` con `/docs/project/10-product` y `/docs/project/15-traceability/03-USE-CASES-TO-SPECS.md`.
3. Leer invariantes, reglas de negocio, estados, eventos e idempotencia en `/docs/project/05-domain`.
4. Leer casos de uso, puertos, handlers y errores en `/docs/project/06-application`.
5. Leer contratos API o eventos en `/docs/project/07-interfaces-and-contracts`.
6. Leer persistencia, outbox, proyecciones y replay en `/docs/project/09-data-and-messaging`.
7. Leer TDD, BDD y seguridad antes de cambiar codigo.

## Reglas

- Mantener arquitectura hexagonal estricta.
- No introducir dependencias prohibidas ni accesos directos entre modulos.
- No implementar si la TDD base no existe; generar o actualizar pruebas primero.

## Output esperado

- Traceability.
- Artifacts Reviewed.
- Changes Applied.
- Validation.
- Residual Risks.
