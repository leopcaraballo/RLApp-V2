# Integration Architecture

## Async backbone

- outbox transaccional
- RabbitMQ para distribucion de eventos
- consumidores de proyeccion y soporte operativo

## Contract rule

Todo evento publicado debe tener version, productor, schema y estrategia de evolucion.
