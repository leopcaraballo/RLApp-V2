# Layering And Dependency Rules

## Layers

- Domain
- Application
- Infrastructure
- API
- Projections

## Allowed direction

- API -> Application
- Application -> Domain
- Application -> ports
- Infrastructure -> ports
- Projections consume eventos, no agregados write-side

## Forbidden direction

- Domain -> Application
- Domain -> API
- Domain -> Infrastructure
- Projections -> Domain write operations
