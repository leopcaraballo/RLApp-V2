# Data Architecture

## Core stores

- PostgreSQL event store
- PostgreSQL read models persistentes
- PostgreSQL audit store o esquema dedicado

## Rules

- write side append-only para eventos
- read side optimizada por consulta
- idempotencia persistida
