---
description: "Use when editing architecture-sensitive code, changing module boundaries, introducing dependencies, or evaluating compliance with the target design and ADRs."
---

# Architecture Instructions

La arquitectura objetivo esta definida en `/docs/project/03-target-architecture` y formalizada por `/docs/project/04-adr`.

## Leer antes de tocar estructura o dependencias

1. `/docs/project/03-target-architecture/01-TARGET-ARCHITECTURE-OVERVIEW.md`
2. `/docs/project/03-target-architecture/02-HEXAGONAL-ARCHITECTURE-RULES.md`
3. `/docs/project/03-target-architecture/03-LAYERING-AND-DEPENDENCY-RULES.md`
4. `/docs/project/03-target-architecture/05-MODULE-BOUNDARIES.md`
5. ADRs aplicables en `/docs/project/04-adr`

## Reglas ejecutables

- El dominio no conoce HTTP, SQL, RabbitMQ, SignalR ni frameworks.
- Direcciones permitidas: `API -> Application`, `Application -> Domain`, `Infrastructure -> ports`, `Projections -> eventos`.
- Direcciones prohibidas: `Domain -> Application`, `Domain -> API`, `Domain -> Infrastructure`, `Projections -> operaciones write-side`.
- Cada modulo expone casos de uso y eventos; ningun modulo accede al storage interno de otro.
- No introducir nuevos patrones, capas o atajos fuera del catalogo aprobado.

## Detener y escalar si

- la tarea requiere una nueva decision arquitectonica no cubierta por ADR
- se necesita un modulo nuevo no definido en los limites canonicos
- la solucion propuesta rompe la cadena de dependencias aprobada
