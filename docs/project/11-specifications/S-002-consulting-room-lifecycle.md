# S-002 Consulting Room Lifecycle

## Purpose

Definir activacion, desactivacion, disponibilidad y liberacion de consultorios como prerequisito del flujo de consulta y del display asociado.

## Traceability

- User stories: `US-003`, `US-004`
- Use cases: `UC-003`, `UC-004`
- Tests: `BDD-002`, `TDD-S-002`

## Scope

- activar consultorio antes de operacion diaria
- desactivar consultorio solo cuando no exista atencion activa
- exponer disponibilidad para claim y llamado de consulta

## Required behavior

- Un consultorio debe activarse antes de recibir un paciente.
- Un consultorio ocupado no puede recibir otro claim.
- La desactivacion solo es valida cuando no exista atencion activa asociada.
- La disponibilidad del consultorio impacta la elegibilidad del siguiente paciente para consulta.
- El estado de disponibilidad debe reflejarse en monitor y display cuando aplique.

## Contracts

- Commands: `POST /api/medical/consulting-room/activate`, `POST /api/medical/consulting-room/deactivate`
- Operacion protegida por autenticacion y autorizacion de supervisor.

## State and event impact

- No genera transicion directa del catalogo de estados de turnos `ST-001` a `ST-009`.
- Eventos canonicos: `EV-008 ConsultingRoomActivated`, `EV-009 ConsultingRoomDeactivated`.
- La ocupacion y liberacion de consultorio se materializan durante `S-005`.

## Validation criteria

- Activar un consultorio inactivo lo deja elegible para consulta.
- Desactivar un consultorio ocupado debe rechazarse.
- Toda activacion o desactivacion debe dejar trazabilidad operativa.
