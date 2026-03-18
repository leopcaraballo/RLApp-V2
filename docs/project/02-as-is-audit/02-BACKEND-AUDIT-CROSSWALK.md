# Backend Audit Crosswalk

## Purpose

Cruzar la auditoria tecnica vigente del backend con la documentacion Enterprise objetivo.

## Confirmed baseline

- backend .NET 10
- arquitectura hexagonal con mejoras pendientes
- Event Sourcing y CQRS
- PostgreSQL como event store y soporte operativo
- RabbitMQ para publicacion asincrona
- Outbox Pattern
- read models en memoria con deuda de persistencia

## Main implications for TO-BE

- el dominio de caja no puede omitirse sin una decision formal de alcance
- estados y eventos reales deben catalogarse completos
- seguridad actual basada en header no puede documentarse como si ya fuera JWT/RBAC robusto
