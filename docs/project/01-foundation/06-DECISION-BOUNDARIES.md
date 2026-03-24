# Decision Boundaries

## Closed decisions

- .NET 10 en backend
- PostgreSQL como store principal
- RabbitMQ para mensajeria asincrona
- Event Sourcing + CQRS
- pantalla publica como unico canal visible para pacientes
- sin usuarios de paciente

## Open decisions

- si SignalR sera el canal realtime definitivo o si habra abstraccion para cambiarlo
- si las proyecciones persistentes conviviran con cache en memoria
- si el frontend de staff y el display publico comparten codebase o se separan

## Rule for open decisions

Ninguna decision abierta puede materializarse en codigo sin ADR aprobado.
