# S-001 Staff Identity And Access

## Purpose

Definir autenticacion de staff, autorizacion por rol y capacidad, gestion de roles y auditoria de accesos para toda operacion interna.

## Traceability

- User stories: `US-001`, `US-002`
- Use cases: `UC-001`, `UC-002`
- Tests: `BDD-001`, `TDD-S-001`, `SEC-TEST-001`, `SEC-TEST-003`

## Scope

- inicio de sesion de usuarios internos
- aplicacion de RBAC sobre operaciones protegidas
- alta, baja y cambio de rol con trazabilidad
- auditoria de autenticacion y cambios sensibles de acceso

## Required behavior

- Todo actor interno debe autenticarse antes de ejecutar operaciones protegidas.
- La pantalla de acceso de staff debe presentar la marca visible `RLApp Clinical Orchestrator`; el texto secundario `Orquestador de Trayectorias Clínicas Sincronizadas` solo puede aparecer como copy complementario y no cambia contratos ni identificadores tecnicos.
- La autorizacion se resuelve por rol y capacidad, nunca por convenciones del cliente.
- Un acceso con credenciales invalidas debe rechazarse.
- Un acceso autenticado con rol insuficiente debe devolverse como `403`.
- Todo cambio de rol debe persistir auditoria con actor, accion, entidad, `correlationId`, timestamp y resultado.
- El sistema TO-BE no puede depender de `X-User-Role` como mecanismo de autorizacion.

## Contract dependencies

- Header obligatorio para operaciones protegidas: `Authorization`.
- Metadata obligatoria para operaciones sensibles: `X-Correlation-Id`.
- Contratos canonicos: `/docs/project/07-interfaces-and-contracts/11-STAFF-IDENTITY-CONTRACTS.md`.
- Commands canonicos: `POST /api/staff/auth/login`, `POST /api/staff/users/register`, `POST /api/staff/users/change-role`, `POST /api/staff/users/change-status`.

## State and event impact

- Esta spec no usa el catalogo operativo `ST-001` a `ST-009`.
- Su salida se refleja en seguridad, auditoria y control de acceso, no en transiciones del flujo de turnos.

## Validation criteria

- `401` ante ausencia de token o credenciales invalidas.
- `403` ante rol insuficiente.
- Auditoria inmutable para cambios de rol y accesos relevantes.
- Ninguna operacion protegida puede ejecutarse sin autenticacion de staff.
