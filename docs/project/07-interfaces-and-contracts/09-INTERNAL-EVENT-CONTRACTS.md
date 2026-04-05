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
- los eventos historicos previos a Fase 2 pueden carecer de `trajectoryId`; el replay debe inferirlo sin mutar el evento original
