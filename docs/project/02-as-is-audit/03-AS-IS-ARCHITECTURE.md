# AS-IS Architecture

## Runtime shape

- API principal
- worker para outbox
- RabbitMQ
- PostgreSQL
- proyecciones en memoria
- canal realtime operativo

## Layers observed

- API
- Application
- Domain
- Infrastructure
- Projections

## Main debt

- Program.cs con demasiada composicion y coordinacion
- proyecciones volatiles
- seguridad actual basada en JWT/RBAC (antes: headers transicionales)
 	- Documentación actualizada para reflejar el mecanismo vigente de autenticación y autorización.
