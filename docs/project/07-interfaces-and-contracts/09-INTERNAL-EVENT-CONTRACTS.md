# Internal Event Contracts

## Canonical metadata target

- eventId
- eventName
- aggregateId
- aggregateVersion
- occurredAt
- correlationId
- schemaVersion

## Current executable async artifact

- `apps/backend/docs/api/asyncapi.yaml` formaliza el contrato machine-readable vigente para `EV-011`, `EV-012` y `EV-013`.
- El artefacto refleja el payload serializado hoy por el outbox y no inventa metadata que aun no existe en el runtime.
- `eventId`, `aggregateVersion` y `schemaVersion` siguen siendo metadata objetivo canonica, pero permanecen fuera del payload implementado actualmente.

## Consultation saga payload baseline

- shared payload fields: `eventType`, `aggregateId`, `occurredAt`, `correlationId`, `trajectoryId`
- `PatientCalled` agrega `patientId`, `roomId`
- `PatientAttentionCompleted` agrega `patientId`, `roomId`, `turnId`, `outcome`
- `PatientAbsentAtConsultation` agrega `patientId`, `turnId`, `reason`

## Trajectory extension

- `trajectoryId` es obligatorio para eventos nuevos emitidos por `TrayectoriaPaciente`
- los eventos consumidos o emitidos por sagas longitudinales deben transportar `trajectoryId` y `correlationId` juntos
- `trajectoryId` identifica la trayectoria longitudinal compartida por la saga; `correlationId` identifica la cadena operativa concreta
- los eventos historicos previos a Fase 2 pueden carecer de `trajectoryId`; el replay debe inferirlo sin mutar el evento original

## Runtime persistence expectations

- las sagas longitudinales no pueden depender de `InMemoryRepository` como repositorio por defecto del perfil ejecutable
- el estado persistido debe conservar `trajectoryId`, `correlationId` causal mas reciente y timestamps de transicion suficientes para diagnostico
