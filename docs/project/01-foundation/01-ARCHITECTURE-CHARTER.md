# Architecture Charter

## Purpose

Congelar el contrato arquitectonico del proyecto nuevo para que la generacion con IA no introduzca interpretaciones ambiguas.

## Approved product scope

- flujo de recepcion, caja y consulta para sala de espera medica
- pantalla publica para estado de espera y llamado actual
- usuarios internos autenticados: Receptionist, Cashier, Doctor, Supervisor y Support

## Out of scope

- paciente autenticado
- canales de notificacion externos
- multi-tenancy
- agenda abierta y reservas self-service

## Non-negotiable technical constraints

- backend .NET 10
- arquitectura hexagonal estricta
- principios SOLID estrictos
- clean code como criterio de aceptacion
- Event Sourcing y CQRS
- PostgreSQL como store principal
- RabbitMQ para mensajeria asincrona
- Outbox Pattern para entrega confiable

## Architectural promise

- dominio puro sin dependencias de infraestructura
- casos de uso en capa de aplicacion
- adaptadores separados por entrada y salida
- composicion de dependencias confinada al composition root
