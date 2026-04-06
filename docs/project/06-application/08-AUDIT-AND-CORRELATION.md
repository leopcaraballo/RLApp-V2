# Audit And Correlation

- toda accion mutante debe incluir correlationId
- audit debe persistir actor, accion, entidad, timestamp y resultado
- `trajectoryId` identifica la trayectoria longitudinal del paciente dentro de `Waiting Room`
- una consulta de trayectoria puede reunir multiples `correlationId` bajo el mismo `trajectoryId`
- el discovery operacional de trayectorias puede resolver candidatos por `patientId` y `queueId` desde proyecciones persistidas, sin usar replay en hot path
- Fase 2 agrega `trajectoryId` sin reemplazar la correlacion actual de sagas
- Fase 4 migra las sagas longitudinales para correlacionarse primariamente por `trajectoryId`, manteniendo `correlationId` como rastro operativo por comando, evento y auditoria
- un mismo `trajectoryId` puede reunir multiples `correlationId` sin abrir una saga paralela
- toda consulta de discovery debe registrar `correlationId`, filtros operativos y cantidad de coincidencias sin exponer PII adicional fuera del contexto autorizado
