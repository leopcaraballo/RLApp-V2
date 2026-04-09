# RLApp Backend

**Estado del proyecto:** Fase 2 — TrayectoriaPaciente implementada y validada en Docker local (2026-04-05). Ver: ../../docs/project/02-as-is-audit/16-FASE-2-CIERRE.md

## Architecture

This backend implements **Hexagonal Architecture** (Ports & Adapters) as defined in [ADR-001](../../docs/project/04-adr/ADR-001-hexagonal-architecture.md).

## Layer Structure

```text
apps/backend/
├── src/
│   ├── RLApp.Domain/              # Core domain logic (pure, no dependencies)
│   │   ├── Aggregates/            # Aggregate roots (StaffUser, WaitingQueue)
│   │   ├── Events/                # Domain events (immutable)
│   │   ├── ValueObjects/          # Domain value objects
│   │   ├── Entities/              # Domain entities
│   │   ├── Specifications/        # Business rule specifications
│   │   └── Common/                # Base classes (DomainEntity, DomainEvent)
│   │
│   ├── RLApp.Ports/               # Port interfaces (inbound/outbound contracts)
│   │   ├── Inbound/               # Repositories, publishers
│   │   └── Outbound/              # Event store, projections, audit
│   │
│   ├── RLApp.Application/         # Application services (use cases)
│   │   ├── UseCases/              # Command/query handlers
│   │   ├── Handlers/              # Event handlers
│   │   ├── DTOs/                  # Data transfer objects
│   │   ├── Services/              # Application-level services
│   │   └── Common/                # Utilities, validators
│   │
│   ├── RLApp.Adapters.Http/       # REST API adapter
│   │   ├── Controllers/           # HTTP endpoints
│   │   ├── Requests/              # Request DTOs
│   │   ├── Responses/             # Response DTOs
│   │   └── Middleware/            # HTTP middleware
│   │
│   ├── RLApp.Adapters.Persistence/ # Data persistence adapter
│   │   ├── Repositories/          # Repository implementations
│   │   ├── DbContext/             # EF Core contexts
│   │   └── Migrations/            # Database migrations
│   │
│   ├── RLApp.Adapters.Messaging/  # Message broker adapter
│   │   ├── Publishers/            # Event publishers
│   │   ├── Subscribers/           # Event subscribers
│   │   └── Events/                # Message contracts
│   │
│   ├── RLApp.Infrastructure/      # Infrastructure composition
│   │   ├── DependencyInjection/   # IoC registration
│   │   └── Configuration/         # Infrastructure config
│   │
│   └── RLApp.Api/                 # ASP.NET Core entry point
│       ├── Program.cs             # Composition root
│       └── appsettings.json       # Configuration
│
└── tests/
    ├── RLApp.Tests.Unit/          # Unit tests (xUnit)
    └── RLApp.Tests.Integration/   # Integration tests (xUnit)
```

## Dependency Rules

- **Domain** → No outbound dependencies (pure domain logic)
- **Application** → Depends on: Domain, Ports
- **Adapters** → Depend on: Ports, Domain
- **Infrastructure** → Depends on: Adapters, Application
- **Api** → Depends on: Infrastructure, Application, Ports, Domain

## Events

Domain events follow [EVENT-CATALOG.md](../../docs/project/05-domain/08-EVENT-CATALOG.md):

- EV-001 to EV-007: Queue events
- EV-008 to EV-014: Consultation events

## Specifications

See architecture specs:

- [S-001](../../docs/project/11-specifications/S-001-staff-identity-and-access.md) - Staff Identity And Access
- [S-002](../../docs/project/11-specifications/S-002-consulting-room-lifecycle.md) - Consulting Room Lifecycle
- [S-003 to S-010](../../docs/project/11-specifications/) - Other specifications

## Building

```bash
dotnet build RLApp.slnx
```

## Database Migrations

```bash
dotnet ef migrations add <MigrationName> --project src/RLApp.Adapters.Persistence/RLApp.Adapters.Persistence.csproj --context AppDbContext --output-dir Data/Migrations
dotnet ef database update --project src/RLApp.Adapters.Persistence/RLApp.Adapters.Persistence.csproj --context AppDbContext
```

## Testing

```bash
dotnet test RLApp.slnx
```

## API Documentation

- OpenAPI source of truth: [docs/api/openapi.yaml](./docs/api/openapi.yaml)
- Critical API audit and frontend/QA guide: [docs/api/API-AUDIT-AND-GUIDE.md](./docs/api/API-AUDIT-AND-GUIDE.md)

## Docker local

Perfil operativo validado:

```bash
docker compose --profile backend --profile frontend up --build
```

Perfil de mensajeria ejecutable:

- Docker Compose levanta `db`, `rabbitmq`, `backend` y `frontend`.
- El backend usa `MassTransit + RabbitMQ` por defecto en Docker local.
- RabbitMQ expone AMQP en `5672` y management UI en `15672`.

Usuarios seeded para el perfil local:

- `superadmin` (`Supervisor`)
- `support` (`Support`)

Passwords del seed local:

- configurar `RLAPP_SEED_SUPERVISOR_PASSWORD` para `superadmin`
- configurar `RLAPP_SEED_SUPPORT_PASSWORD` para `support`
- si no se configuran, el runtime usa defaults locales pensados para smoke y QA

Slice Fase 2 disponible en runtime:

- `GET /api/patient-trajectories/{trajectoryId}`
- `POST /api/patient-trajectories/rebuild`

## Running

```bash
dotnet run --project src/RLApp.Api/RLApp.Api.csproj
```
