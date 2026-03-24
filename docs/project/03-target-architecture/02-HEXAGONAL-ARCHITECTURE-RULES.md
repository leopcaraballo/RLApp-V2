# Hexagonal Architecture Rules

## Core rule

El dominio no conoce HTTP, SQL, RabbitMQ, SignalR ni frameworks.

## Inbound adapters

- HTTP endpoints
- realtime commands si existieran
- background triggers

## Outbound adapters

- event store
- outbox publisher
- projection store
- audit store
- realtime broadcaster

## Forbidden dependencies

- API -> Domain internals por atajo
- Domain -> Infrastructure
- Application -> implementaciones concretas
