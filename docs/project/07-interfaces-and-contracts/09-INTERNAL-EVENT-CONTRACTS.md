# Internal Event Contracts

- eventId
- eventName
- aggregateId
- aggregateVersion
- occurredAt
- correlationId
- schemaVersion

## Trajectory extension

- `trajectoryId` es obligatorio para eventos nuevos emitidos por `TrayectoriaPaciente`
- los eventos consumidos o emitidos por sagas longitudinales deben transportar `trajectoryId` y `correlationId` juntos
- `trajectoryId` identifica la trayectoria longitudinal compartida por la saga; `correlationId` identifica la cadena operativa concreta
- los eventos historicos previos a Fase 2 pueden carecer de `trajectoryId`; el replay debe inferirlo sin mutar el evento original
