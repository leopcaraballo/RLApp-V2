# Audit And Correlation

- toda accion mutante debe incluir correlationId
- audit debe persistir actor, accion, entidad, timestamp y resultado
- `trajectoryId` identifica la trayectoria longitudinal del paciente dentro de `Waiting Room`
- una consulta de trayectoria puede reunir multiples `correlationId` bajo el mismo `trajectoryId`
- Fase 2 agrega `trajectoryId` sin reemplazar la correlacion actual de sagas; esa migracion sigue reservada para Fase 4
