# Fase 5 — Kickoff

**Fecha:** 2026-04-06
**Rama de trabajo:** feature/trajectory-discovery-ops
**Origen:** develop sincronizado con el cierre documental de Fase 4

## Objetivo

Iniciar el siguiente slice para descubrir trayectorias materializadas cuando `trajectoryId` no esta disponible, reutilizando `US-018` / `UC-018` / `S-011` y `S-009` como soporte de observabilidad del lookup operacional.

## Decision de arranque

- No se abre un nuevo `UC` para este corte; el discovery operacional se trata como extension natural de `UC-018` y `S-011`.
- El slice se limita a consultas sobre proyecciones persistidas; no introduce nuevos writes, nuevos eventos de negocio ni replay en hot path.
- `Supervisor` y `Support` mantienen el mismo boundary de autorizacion ya aprobado para trayectoria.
- `patientId` se vuelve el filtro minimo de discovery y `queueId` actua como acotador opcional para reducir ambiguedad operativa.

## Trazabilidad

- US: `US-018`
- UC: `UC-018`
- S: `S-011`, `S-009`
- BDD/TDD: `BDD-010`, `TDD-S-011`, `TDD-S-009`

## Alcance de Fase 5

- agregar query canonica de discovery para listar trayectorias candidatas por `patientId` y `queueId` opcional
- mantener la consulta existente por `trajectoryId` y el rebuild controlado sin romper contratos vigentes
- exponer el discovery en la consola frontend `/trajectory` sin inventar CRUD ni atajos fuera del contrato
- registrar logs estructurados del lookup con `correlationId`, filtros operativos y cantidad de coincidencias

## Entregables esperados

1. Cadena documental actualizada de `US-018`, `S-011`, `BDD-010` y `TDD-S-011` para discovery operacional.
2. Contrato OpenAPI actualizado para `GET /api/patient-trajectories` con filtros y respuesta de candidatas.
3. Implementacion backend de consulta desde `v_patient_trajectory` y pruebas de integracion/autorizacion.
4. Consola frontend de trayectoria actualizada para descubrir candidatos y luego consultar la proyeccion completa.

## Restricciones

- Mantener Git Flow: trabajo solo en `feature/*`.
- Mantener docs-first: no tocar codigo sin cerrar `US/UC/S/BDD/TDD` del slice.
- No leer del event store en la consulta de discovery.
- No ampliar el alcance hacia dashboards o una iniciativa transversal completa de observabilidad en este corte.
- No exponer PII adicional fuera de `patientId`, `queueId`, `trajectoryId` y metadatos ya autorizados para `Support` y `Supervisor`.

## Riesgos remanentes

- Un `patientId` con historico amplio puede devolver multiples candidatas y exigir criterio operativo adicional del usuario.
- La medicion automatizada end-to-end de observabilidad sigue siendo un trabajo posterior a este slice funcional.
- El shell local mantiene contaminacion intermitente en comandos git interactivos, por lo que la operacion de rama y push debe vigilarse de forma no interactiva cuando sea posible.
