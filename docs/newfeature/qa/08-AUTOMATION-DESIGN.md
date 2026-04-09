# Automation Design

> **Actualizada**: 2026-04-09 — Basada en infraestructura real del repositorio

## Purpose

Documentar la estrategia de automatizacion de pruebas realmente implementada en RLApp-V2, incluyendo herramientas, estructura y patron de ejecucion por capa.

---

## 1. Stack de Testing Real

### Backend (.NET 10)

| Componente | Paquete | Version | Uso |
|---|---|---|---|
| Framework de tests | xunit | 2.9.3 | Definicion y ejecucion de tests unitarios e integracion |
| Runner | xunit.runner.visualstudio | 3.0.2 | Ejecucion en CI y Visual Studio |
| Mocking | NSubstitute | 5.3.0 | Sustitutos de interfaces (puertos) |
| Assertions fluidas | FluentAssertions | 8.2.0 | Assertions legibles en tests |
| Testcontainers | Testcontainers.PostgreSql, Testcontainers.RabbitMq | 4.x | Contenedores efimeros para integracion |
| Coverage | coverlet.collector | 6.0.4 | Cobertura de codigo |

### Frontend (Next.js 16 / TypeScript)

| Componente | Paquete | Version | Uso |
|---|---|---|---|
| Test runner | vitest | 4.1.3 | Ejecucion de tests unitarios |
| DOM testing | @testing-library/react | 16.3.0 | Rendering y queries |
| Matchers | @testing-library/jest-dom | 6.6.3 | Matchers de DOM extendidos |
| E2E smoke | Node.js scripts | — | `role-smoke.mjs` para verificacion de roles |

---

## 2. Estructura de Proyectos de Test

```
apps/backend/tests/
  RLApp.Tests.Unit/
    Domain/
      PatientTrajectoryTests.cs          # Aggregate root: estados, transiciones
      PatientTrajectoryExtendedTests.cs  # Edge cases: duplicados, cronologia, replay
      WaitingQueueExtendedTests.cs       # Queue: prioridad, limites, requeue
      ConsultingRoomExtendedTests.cs     # Rooms: activacion, assign, release
    Application/
      PatientTrajectoryOrchestratorTests.cs  # Orquestador: 7 Track + idempotencia
      PatientTrajectoryProjectionWriterTests.cs  # Proyecciones: materializacion
      CommandHandlerTests.cs             # Handlers de comandos MediatR
    Infrastructure/
      ConsultationSagaTests.cs           # Saga MassTransit: happy path + timeout
      OutboxProcessorTests.cs            # Outbox: dequeue y publicacion
      HealthCheckTests.cs                # Health endpoints de infraestructura
  RLApp.Tests.Integration/
    PatientTrajectoryIntegrationTests.cs # E2E con PostgreSQL + RabbitMQ reales
    WaitingQueueIntegrationTests.cs
    ConsultingRoomIntegrationTests.cs

apps/frontend/tests/
  e2e/
    role-smoke.mjs                       # Smoke test de roles con login real
  (unit tests colocados junto al codigo fuente en src/)
```

---

## 3. Patrones de Test por Capa

### 3.1 Domain Layer (Aggregate Tests)

**Patron**: Arrange-Act-Assert puro, sin dependencias externas.

```csharp
// Ejemplo real: PatientTrajectoryTests.cs
[Fact]
public void RecordStage_ReceptionToCashier_Succeeds()
{
    var trajectory = PatientTrajectory.Start(patientId, queueId, correlationId, now);
    var result = trajectory.RecordStage("CASHIER", now.AddMinutes(5), Guid.NewGuid());
    result.Should().BeTrue();
    trajectory.CurrentStage.Should().Be("CASHIER");
}
```

**Cobertura**: transiciones validas/invalidas, duplicados, cronologia, estados terminales, replay de eventos.

### 3.2 Application Layer (Orchestrator Tests)

**Patron**: Inyeccion de puertos mockeados con NSubstitute.

```csharp
// Ejemplo real: PatientTrajectoryOrchestratorTests.cs
[Fact]
public async Task TrackCheckIn_ExistingActiveTrajectory_ThrowsDomainException()
{
    _repo.FindActiveAsync(patientId, Arg.Any<CancellationToken>())
         .Returns(existingTrajectory);

    await Assert.ThrowsAsync<DomainException>(
        () => _orchestrator.TrackCheckInAsync(patientId, queueId, ct));
}
```

**Cobertura**: cada Track* method verificado con happy path + idempotencia + error esperado.

### 3.3 Integration Tests

**Patron**: Testcontainers (PostgreSQL + RabbitMQ) con WebApplicationFactory.

```csharp
// Patron: PatientTrajectoryIntegrationTests.cs
public class PatientTrajectoryIntegrationTests : IClassFixture<IntegrationFixture>
{
    // Testcontainers levantan PostgreSQL y RabbitMQ reales
    // HTTP calls reales contra la API
    // Assertions contra respuestas HTTP y proyecciones persistidas
}
```

**Cobertura**: endpoints reales, persistencia, proyecciones, RBAC en headers.

### 3.4 Frontend Unit Tests

**Patron**: vitest + @testing-library/react para componentes y hooks.

```typescript
// Ejemplo: authorization.test.ts
describe('Authorization utilities', () => {
  it('should validate role-based access correctly', () => {
    // Tests de guards, role checks, token parsing
  });
});
```

**Cobertura**: autorizacion, display helpers, HTTP client, validacion de entorno.

---

## 4. Ejecucion en CI

### GitHub Actions Pipeline

```yaml
# .github/workflows/ci.yml (real)
- dotnet test apps/backend/RLApp.slnx --configuration Release
  # Ejecuta RLApp.Tests.Unit + RLApp.Tests.Integration
  # Tests de integracion usan Testcontainers (requieren Docker en runner)

- cd apps/frontend && pnpm test --run
  # Ejecuta vitest con todos los tests unitarios
```

### Comandos Locales

```bash
# Backend: todos los tests
dotnet test apps/backend/RLApp.slnx

# Backend: solo unitarios
dotnet test apps/backend/tests/RLApp.Tests.Unit

# Backend: solo integracion (requiere Docker)
dotnet test apps/backend/tests/RLApp.Tests.Integration

# Frontend: todos los tests
cd apps/frontend && pnpm test --run

# Frontend: smoke E2E (requiere stack levantado)
node apps/frontend/tests/e2e/role-smoke.mjs
```

---

## 5. Brechas de Automatizacion Conocidas

| ID | Descripcion | Prioridad | Estado |
|---|---|---|---|
| AUTO-01 | Test de concurrencia optimista (version conflict) | Media | Backlog |
| AUTO-02 | Test de latencia SLA < 1s para proyecciones | Baja | Backlog |
| AUTO-03 | Audit trail assertions en operaciones normales | Media | Backlog |
| AUTO-04 | Performance test con carga (1000+ pacientes) | Baja | Manual via simulation harness |
| AUTO-05 | E2E automatizado de flujo completo paciente | Media | Parcial (role-smoke.mjs) |

---

## 6. Principios de Automatizacion

1. **TDD first**: el test se escribe antes de la implementacion
2. **Aislamiento por capa**: domain tests sin I/O, application tests con mocks, integration con contenedores reales
3. **No mocks de dominio**: el aggregate se prueba en su estado real, nunca mockeado
4. **Testcontainers para integracion**: PostgreSQL y RabbitMQ efimeros, sin fixtures compartidas
5. **Coverage en CI**: coverlet.collector genera reportes automaticamente
6. **Assertions explicitas**: FluentAssertions para legibilidad; assertions de estructura, no de implementacion
