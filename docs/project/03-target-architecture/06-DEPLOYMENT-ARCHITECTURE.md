# Deployment Architecture

## Components

- backend API
- worker
- PostgreSQL
- RabbitMQ
- observability stack
- staff frontend
- public display frontend

## Deployment rule

API y worker pueden desplegarse por separado, compartiendo contratos de eventos y esquema de persistencia versionado.
