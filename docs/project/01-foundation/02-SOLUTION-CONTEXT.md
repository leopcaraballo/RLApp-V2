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
