# Solution Context

## Business context

El sistema orquesta la atencion de pacientes que ya tienen una cita registrada para el dia. La operacion incluye recepcion, flujo de caja cuando aplique, atencion en consultorio y exposicion del estado en una pantalla publica.

## Actors

- Receptionist: registra llegada y consulta estado de cola
- Cashier: procesa pago, pago pendiente, ausencia y cancelacion por pago
- Doctor: llama, atiende y finaliza consulta
- Supervisor: activa consultorios, monitorea dashboard y auditoria
- Support: diagnostica incidentes y reconstruye proyecciones
- Public Display: cliente read-only anonimo

## External dependencies

- PostgreSQL
- RabbitMQ
- entorno de observabilidad

## Platform identity

- El frontend autenticado de staff se presenta visualmente como `RLApp Clinical Orchestrator`.
- El texto secundario permitido para chrome informativo, pie de pagina o secciones de contexto es `Orquestador de Trayectorias Clínicas Sincronizadas`.
- Esta identidad visual aplica solo a copy visible para el usuario; no renombra contratos, rutas, headers, cookies, IDs de prueba ni identificadores tecnicos internos.
