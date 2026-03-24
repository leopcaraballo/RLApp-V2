# Pattern Catalog

## Mandatory patterns

- Hexagonal Architecture
- CQRS
- Event Sourcing
- Outbox Pattern
- Projection Pattern
- Repository per output port
- Factory Pattern para creacion controlada de agregados y value objects
- Strategy o Policy Pattern para reglas variables
- Composition Root Pattern

## Conditional patterns

- Specification Pattern para reglas complejas de elegibilidad
- State Pattern solo si mejora claridad frente a un catalogo declarativo de transiciones

## Anti-patterns prohibited by default

- generic repository transversal
- service god objects
- dominio anemico con reglas en application o API
- DTOs con comportamiento de negocio
- acoplar SignalR, RabbitMQ o SQL dentro del dominio
