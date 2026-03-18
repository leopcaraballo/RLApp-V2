---
name: generate-spec
description: "Use when drafting or updating a canonical spec in /docs/project from a user story, use case, or approved business requirement."
argument-hint: "<US-XXX or requirement summary>"
agent: "rlapp-delivery"
---

Genera o actualiza una spec canonica usando exclusivamente `/docs/project`.

## Inputs requeridos

- Historia de usuario `US-XXX` o requerimiento con contexto suficiente.
- Caso de uso `UC-XXX` si ya existe.
- Spec `S-XXX` si se va a actualizar una existente.

## Procedimiento obligatorio

1. Leer `/docs/project/00-README.md`, `/docs/project/03-TRACEABILITY-MODEL.md` y `/docs/project/04-DOCUMENT-MAP.md`.
2. Resolver `US-XXX` en `/docs/project/10-product/user-stories`.
3. Resolver `UC-XXX` con `/docs/project/10-product/03-USE-CASE-MAP.md` y `/docs/project/06-application/01-USE-CASE-CATALOG.md`.
4. Si existe `S-XXX`, leerla en `/docs/project/11-specifications`; si no existe, proponer una nueva spec y las actualizaciones de trazabilidad necesarias.
5. Leer dominio, contratos, seguridad y testing aplicables antes de redactar el delta.
6. Relacionar la spec con BDD y TDD en `/docs/project/12-testing` y `/docs/project/15-traceability/05-SPECS-TO-TESTS.md`.

## Reglas

- No crear `.github/specs` ni fuentes paralelas.
- No inventar IDs; si un `S-XXX`, `US-XXX` o `UC-XXX` falta, reportarlo y proponer la actualizacion documental requerida.
- Toda spec debe mantener el flujo `User Story -> Spec -> BDD -> TDD -> Implementation -> Validation`.

## Output esperado

- Borrador o delta de la spec canonica en `/docs/project/11-specifications`.
- Lista exacta de archivos de trazabilidad y testing que deben actualizarse.
- Resumen de alcance, invariantes, contratos y criterios de validacion ligados a IDs reales.
