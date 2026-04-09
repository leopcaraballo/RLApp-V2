# Bug Report Examples

## BR-001 - Race condition on trajectory transition

### BR-001 Summary

Dos transiciones validas y concurrentes sobre la misma trayectoria producen resultados inconsistentes entre write-side y monitor visible.

### BR-001 Classification

- Severity: Critical
- Priority: P1
- Area: concurrency / aggregate consistency
- Traceability: `HU-02`, `RN-11`, `RN-21`, `RN-22`, `TC-TRJ-21`, `TC-TRJ-22`

### BR-001 Preconditions

- trayectoria activa en espera de caja o consulta
- dos actores con permisos validos
- misma version inicial del aggregate

### BR-001 Steps to reproduce

1. disparar dos comandos de transicion casi simultaneos sobre la misma trayectoria
2. permitir que ambos lleguen al write-side con la misma version esperada
3. consultar trayectoria, monitor y dashboard

### BR-001 Expected result

- una sola transicion se persiste
- la segunda recibe conflicto controlado o se correlaciona al estado ya consolidado
- monitor y trayectoria muestran el mismo resultado final

### BR-001 Actual result

- la trayectoria refleja un solo estado final valido
- el monitor visible muestra temporalmente un destino inconsistente respecto al historial consolidado

### BR-001 Suspected root cause

- control optimista aplicado en el aggregate pero no reforzado suficientemente en la actualizacion proyectada o en la secuencia de invalidacion visible

### BR-001 Evidence to capture

- `trajectoryId`, `patientId`, `queueId`, `correlationId`
- version esperada y version persistida
- snapshots de monitor y trayectoria antes y despues del conflicto

## BR-002 - Duplicate event generates repeated projection stage

### BR-002 Summary

Un redelivery del mismo evento produce duplicacion visible del hito proyectado aunque el event store permanece correcto.

### BR-002 Classification

- Severity: High
- Priority: P1
- Area: projector idempotency
- Traceability: `HU-04`, `RN-14`, `RN-16`, `RN-19`, `TC-TRJ-14`, `TC-TRJ-16`, `TC-TRJ-19`

### BR-002 Preconditions

- evento historico ya procesado por el projector
- redelivery o reintento del mismo mensaje en el bus

### BR-002 Steps to reproduce

1. reprocesar el mismo evento de trayectoria en el consumidor
2. consultar la proyeccion de trayectoria
3. comparar contra el event store y el historial esperado

### BR-002 Expected result

- el projector reconoce duplicado y no agrega un nuevo hito visible

### BR-002 Actual result

- la proyeccion muestra un stage repetido con igual `sourceEvent` y `occurredAt`
- el event store sigue sin duplicados, lo cual confirma drift en read-side

### BR-002 Suspected root cause

- ausencia de deduplicacion por id de evento o combinacion `trajectoryId + sequence + sourceEvent`

### BR-002 Evidence to capture

- payload exacto del evento duplicado
- clave de deduplicacion usada por el projector
- consulta de `GET /api/patient-trajectories/{trajectoryId}` antes y despues del redelivery

## BR-003 - Read model desynchronization after messaging lag

### BR-003 Summary

Tras un retraso temporal del bus u outbox, el detalle de trayectoria converge correctamente pero dashboard y monitor quedan desfasados mas alla del umbral operativo.

### BR-003 Classification

- Severity: High
- Priority: P1
- Area: eventual consistency / operational visibility
- Traceability: `HU-03`, `RN-24`, `RN-25`, `RN-26`, `TC-TRJ-24`, `TC-TRJ-25`, `TC-TRJ-26`

### BR-003 Preconditions

- carga concurrente moderada o alta
- retraso artificial del outbox o del consumidor de proyecciones

### BR-003 Steps to reproduce

1. ejecutar flujo nominal de varios pacientes
2. introducir lag de publicacion o consumo
3. consultar trayectoria y dashboard
4. medir convergencia

### BR-003 Expected result

- read models convergen dentro del umbral acordado
- la UI no presenta drift bloqueante al usuario operativo

### BR-003 Actual result

- la trayectoria ya esta cerrada correctamente
- el dashboard sigue reflejando al paciente como activo durante una ventana superior al umbral esperado

### BR-003 Suspected root cause

- presion acumulada en outbox processor o projector sin estrategia suficiente de priorizacion y observabilidad de lag

### BR-003 Evidence to capture

- timestamps exactos del evento write-side y del snapshot visible
- `projectionLagSeconds` y logs de outbox
- captura del stream realtime y del refetch posterior
