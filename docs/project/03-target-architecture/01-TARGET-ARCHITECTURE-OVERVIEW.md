# Target Architecture Overview

## Purpose

Definir la forma final del proyecto nuevo y limpio.

## Target modules

- Staff Identity and Access
- Waiting Room Core
- Cashier Flow
- Consultation Flow
- Public Display Projection
- Reporting and Audit

## Ownership note

`Waiting Room Core` conserva la verdad write-side de `WaitingQueue` y `TrayectoriaPaciente`. Los flujos de recepcion, caja y consulta operan mediante casos de uso y eventos del core, sin crear storage paralelo para la trayectoria longitudinal.

## Runtime components

- API .NET 10
- background worker para outbox y proyecciones
- PostgreSQL
- RabbitMQ
- cliente web de staff
- cliente publico de display
