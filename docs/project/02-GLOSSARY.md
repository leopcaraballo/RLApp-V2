# Glossary

## Core terms

- Waiting Room: bounded context principal que gobierna el flujo operativo de espera, caja y consulta.
- Queue: cola operativa identificada por queueId.
- Turn: unidad operativa de atencion para un paciente dentro de una queue.
- Consulting Room: consultorio o sala de atencion medica identificada por stationId o consultingRoomId.
- Public Display: cliente read-only que muestra informacion sanitizada del estado de espera.
- Staff User: usuario interno autenticado con rol operativo.
- Projection: modelo de lectura derivado de eventos.
- Event Store: almacenamiento append-only de eventos de dominio.
- Outbox: mecanismo transaccional para publicacion confiable de eventos.

## Roles

- Receptionist
- Cashier
- Doctor
- Supervisor
- Support

## Forbidden ambiguities

- no usar appointment como sinonimo de turn
- no usar notification para referirse a la pantalla publica sin aclararlo
- no usar waiting room y queue como sinonimos absolutos
- no usar patient user o patient account porque no existen en el alcance objetivo
