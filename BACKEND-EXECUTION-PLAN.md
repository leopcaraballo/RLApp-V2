# RLApp-V2 Backend: Detailed Execution Plan

**Branch**: `feature/backend-phase-4`
**Phase**: 4a-4d (Security → Data Consistency → Resilience → Testing)
**Estimated Duration**: 110-125 hours (~3 sprints)

---

## PHASE 4A: SECURITY & CORE FIXES (72-96 hours)

### BLOCKER #1: Authentication & Authorization (6 hours)

**Files to Modify**:

1. `src/RLApp.Infrastructure/DependencyInjection.cs`
2. `src/RLApp.Api/Program.cs`
3. `src/RLApp.Application/Handlers/AuthenticateStaffHandler.cs`
4. `src/RLApp.Adapters.Http/Middleware/AuthenticationMiddleware.cs` (CREATE)
5. `src/RLApp.Domain/ValueObjects/StaffRole.cs` (already exists)

**Step 1.1: Add JWT Configuration (1.5h)**

```csharp
// In DependencyInjection.cs, add:
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = configuration["Jwt:Key"];
        var jwtIssuer = configuration["Jwt:Issuer"] ?? "RLApp";
        var jwtAudience = configuration["Jwt:Audience"] ?? "RLApp";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ReceptionOnly", policy => policy.RequireRole("Reception"));
    options.AddPolicy("CashierOnly", policy => policy.RequireRole("Cashier"));
    options.AddPolicy("DoctorOnly", policy => policy.RequireRole("Doctor"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Admin", "Reception", "Cashier", "Doctor"));
});

// Add IUserContextService for retrieving current user
services.AddScoped<IUserContextService, UserContextService>();
```

**Step 1.2: Add Middleware in Program.cs (0.5h)**

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

**Step 1.3: Implement Password Hashing (2h)**

- Create new class: `src/RLApp.Domain/Services/PasswordHasher.cs`
- Implement BCrypt hashing with salt
- Update `AuthenticateStaffHandler` to use it
- Remove TODO on line 32

**Step 1.4: Implement JWT Token Generation (1.5h)**

```csharp
// In AuthenticateStaffHandler.Handle():
var token = GenerateJwtToken(staffUser);
return CommandResult<AuthenticationResultDto>.Ok(
    new AuthenticationResultDto {
        Token = token,
        StaffId = staffUser.Id,
        Role = staffUser.Role.ToString()
    }
    ...);
```

**Acceptance Criteria**:

- [ ] AuthenticateStaffCommand returns JWT token
- [ ] Expired tokens are rejected (401)
- [ ] Invalid tokens are rejected (401)
- [ ] Missing Authorization header returns 401
- [ ] [Authorize] attribute enforces auth
- [ ] [Authorize(Roles="Admin")] enforces roles

---

### BLOCKER #2: Audit Trail Implementation (3 hours)

**Files to Create/Modify**:

1. `src/RLApp.Adapters.Persistence/Data/Models/AuditLogRecord.cs` (CREATE)
2. `src/RLApp.Ports/Outbound/IAuditStore.cs` (exists, needs implementation)
3. `src/RLApp.Adapters.Persistence/Repositories/AuditStoreRepository.cs` (CREATE)
4. `src/RLApp.Infrastructure/DependencyInjection.cs` (add IAuditStore registration)
5. `src/RLApp.Application/Services/AuditService.cs` (CREATE)

**Step 2.1: Create Audit Log Model (0.5h)**

```csharp
public class AuditLogRecord
{
    public string Id { get; set; }
    public string Action { get; set; }           // "Authenticate", "ChangeRole", "CreateQueue"
    public string ActorId { get; set; }          // Who performed the action
    public string EntityType { get; set; }       // "StaffUser", "WaitingQueue"
    public string EntityId { get; set; }         // What entity was affected
    public string Description { get; set; }
    public string CorrelationId { get; set; }
    public Dictionary<string, object> Before { get; set; }  // JSON snapshot
    public Dictionary<string, object> After { get; set; }
    public DateTime OccurredAt { get; set; }
    public int ResultCode { get; set; }          // 200, 400, 401, 403
}
```

**Step 2.2: Implement IAuditStore (1h)**

- Create `AuditStoreRepository.cs` implementing `IAuditStore`
- Add DbSet to AppDbContext
- Implement `LogAsync()` method

**Step 2.3: Integrate with Handlers (1h)**

- Create `AuditService` to intercept commands
- Log all sensitive operations:
  - Staff authentication (success/failure)
  - Role changes
  - Queue operations
  - Payment validation
- Add to DependencyInjection

**Acceptance Criteria**:

- [ ] Every authentication attempt logged (success/failure)
- [ ] Every role change logged with before/after
- [ ] Logs are immutable (no delete operations)
- [ ] CorrelationId passed through entire chain
- [ ] Operations dashboard includes audit summary

---

### BLOCKER #3: Complete Missing Handlers (3 hours)

**Step 3.1: Create ConsultingRoom Aggregate (1.5h)**

**File**: `src/RLApp.Domain/Aggregates/ConsultingRoom.cs` (CREATE)

```csharp
public class ConsultingRoom : DomainEntity
{
    public string RoomName { get; private set; }
    public bool IsActive { get; private set; }
    public string CurrentPatientId { get; private set; }
    public DateTime ActivatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

    public static ConsultingRoom Create(string id, string roomName, string correlationId)
    {
        var room = new ConsultingRoom { Id = id, RoomName = roomName, IsActive = true };
        room.RaiseDomainEvent(new ConsultingRoomActivated(id, roomName, correlationId));
        return room;
    }

    public void Activate(string correlationId)
    {
        if (IsActive) throw new DomainException("Room already active");
        IsActive = true;
        ActivatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ConsultingRoomActivated(Id, RoomName, correlationId));
    }

    public void Deactivate(string correlationId)
    {
        if (!IsActive) throw new DomainException("Room already inactive");
        IsActive = false;
        DeactivatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ConsultingRoomDeactivated(Id, correlationId));
    }

    public void AssignPatient(string patientId, string correlationId)
    {
        if (!IsActive) throw new DomainException("Cannot assign to inactive room");
        CurrentPatientId = patientId;
        RaiseDomainEvent(new PatientAssignedToRoom(Id, patientId, correlationId));
    }

    public void ReleasePatient(string correlationId)
    {
        CurrentPatientId = null;
        RaiseDomainEvent(new PatientReleasedFromRoom(Id, correlationId));
    }
}
```

**Step 3.2: Complete Handler Method Bodies (1.5h)**

**Files**:

- `src/RLApp.Application/Handlers/ConsultationFlowHandlers.cs` - Complete `FinishConsultationHandler`
- `src/RLApp.Application/Handlers/AdditionalHandlers.cs` - Fix all TODOs
- `src/RLApp.Application/Handlers/ConsultingRoomAndCashierHandlers.cs` - Use ConsultingRoom aggregate

Replace TODO with actual implementation:

```csharp
// BEFORE (line 28):
// TODO: In a real scenario, there would be a ConsultingRoom aggregate

// AFTER:
var room = await _consultingRoomRepository.GetByIdAsync(command.RoomId);
if (room == null)
    room = ConsultingRoom.Create(command.RoomId, command.RoomName, command.CorrelationId);

room.Activate(command.CorrelationId);
await _consultingRoomRepository.UpdateAsync(room);
var events = room.GetUnraisedEvents();
await _eventPublisher.PublishBatchAsync(events);
```

**Acceptance Criteria**:

- [ ] ConsultingRoom aggregate compiles
- [ ] All handlers have complete method bodies
- [ ] No TODO comments remain in handlers
- [ ] All handlers follow MediatR pattern
- [ ] Tests can verify handler execution

---

## PHASE 4B: DATA CONSISTENCY & REALTIME (72-96 hours)

### BLOCKER #4: Projection Reconstruction (8 hours)

**Files to Create/Modify**:

1. `src/RLApp.Adapters.Persistence/Repositories/ProjectionStoreRepository.cs` (CREATE)
2. `src/RLApp.Application/Services/ProjectionService.cs` (CREATE)
3. `src/RLApp.Application/Handlers/AdditionalHandlers.cs` (modify RebuildProjectionsCommand)
4. `src/RLApp.Adapters.Persistence/Data/Models/ReadModels.cs` (already exists, verify completeness)

**Step 4.1: Implement IProjectionStore (2h)**

**File**: `src/RLApp.Adapters.Persistence/Repositories/ProjectionStoreRepository.cs`

```csharp
public class ProjectionStoreRepository : IProjectionStore
{
    private readonly AppDbContext _context;

    public async Task<WaitingRoomMonitorView> GetQueueMonitorAsync(string queueId)
    {
        return await _context.WaitingRoomMonitors
            .FirstOrDefaultAsync(x => x.QueueId == queueId);
    }

    public async Task UpdateQueueMonitorAsync(WaitingRoomMonitorView view)
    {
        var existing = await _context.WaitingRoomMonitors
            .FirstOrDefaultAsync(x => x.QueueId == view.QueueId);

        if (existing == null)
            _context.WaitingRoomMonitors.Add(view);
        else
            _context.Entry(existing).CurrentValues.SetValues(view);

        await _context.SaveChangesAsync();
    }

    // Similar for other views: QueueState, NextTurn, RecentHistory, OperationsDashboard
}
```

**Step 4.2: Create Event Consumers for Projections (3h)**

**File**: `src/RLApp.Adapters.Messaging/Consumers/` (new directory)

Create consumers for each event type:

- `PatientCheckedInConsumer.cs` → Updates WaitingRoomMonitorView + QueueStateView
- `PatientCalledAtCashierConsumer.cs` → Updates NextTurnView
- `PatientPaymentValidatedConsumer.cs` → Updates OperationsDashboardView
- `ConsultationEventConsumer.cs` → Updates all relevant views

Example:

```csharp
public class PatientCheckedInConsumer : IConsumer<PatientCheckedIn>
{
    private readonly IProjectionStore _projectionStore;

    public async Task Consume(ConsumeContext<PatientCheckedIn> context)
    {
        var evt = context.Message;
        var view = await _projectionStore.GetQueueMonitorAsync(evt.AggregateId);

        if (view == null)
            view = new WaitingRoomMonitorView { QueueId = evt.AggregateId };

        view.TotalPatients++;
        view.LastUpdatedAt = DateTime.UtcNow;

        await _projectionStore.UpdateQueueMonitorAsync(view);
    }
}
```

**Step 4.3: Implement Projection Rebuild (2h)**

Complete `RebuildProjectionsCommand` handler:

```csharp
public class RebuildProjectionsHandler
{
    private readonly IEventStore _eventStore;
    private readonly IProjectionStore _projectionStore;

    public async Task<CommandResult> Handle(RebuildProjectionsCommand command)
    {
        try
        {
            // Get all events from the beginning
            var allEvents = await _eventStore.GetAllEventsAsync();

            // Clear existing projections
            await _projectionStore.ClearAllAsync();

            // Replay all events to rebuild projections
            foreach (var @event in allEvents.OrderBy(x => x.CreatedAt))
            {
                await ReplayEventToProjections(@event);
            }

            return CommandResult.Ok(command.CorrelationId, "Projections rebuilt successfully");
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Rebuild failed: {ex.Message}", command.CorrelationId);
        }
    }

    private async Task ReplayEventToProjections(DomainEvent evt)
    {
        // Route each event type to appropriate projection handlers
        // ...
    }
}
```

**Step 4.4: Register Consumers in MassTransit (1h)**

In `DependencyInjection.cs`:

```csharp
services.AddMassTransit(x =>
{
    x.AddConsumer<PatientCheckedInConsumer>();
    x.AddConsumer<PatientCalledAtCashierConsumer>();
    x.AddConsumer<PatientPaymentValidatedConsumer>();
    x.AddConsumer<ConsultationEventConsumer>();
    // ... register all consumers

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => { /* ... */ });
        cfg.ConfigureEndpoints(context);
    });
});
```

**Acceptance Criteria**:

- [ ] IProjectionStore fully implemented
- [ ] Event consumers created for all event types
- [ ] Consumers registered in MassTransit
- [ ] RebuildProjectionsCommand works end-to-end
- [ ] Projections auto-update when events occur
- [ ] Dashboard queries return non-empty results after events

---

### BLOCKER #5: WebSocket/SignalR for Realtime (5 hours)

**Files to Create**:

1. `src/RLApp.Adapters.Http/Hubs/QueueUpdatesHub.cs` (CREATE)
2. `src/RLApp.Infrastructure/DependencyInjection.cs` (add SignalR)
3. `src/RLApp.Api/Program.cs` (map hub)
4. `src/RLApp.Adapters.Http/Middleware/EventToWebSocketMiddleware.cs` (CREATE)

**Step 5.1: Add SignalR Hub (1.5h)**

**File**: `src/RLApp.Adapters.Http/Hubs/QueueUpdatesHub.cs`

```csharp
public class QueueUpdatesHub : Hub
{
    private readonly ILogger<QueueUpdatesHub> _logger;

    public QueueUpdatesHub(ILogger<QueueUpdatesHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public async Task SubscribeToQueue(string queueId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"queue-{queueId}");
        _logger.LogInformation($"Client {Context.ConnectionId} subscribed to queue {queueId}");
    }
}
```

**Step 5.2: Trigger WebSocket Updates from Events (2h)**

Create event-to-websocket bridge:

```csharp
public class ProjectionUpdateService
{
    private readonly IHubContext<QueueUpdatesHub> _hubContext;

    public async Task NotifyQueueUpdated(string queueId, WaitingRoomMonitorView view)
    {
        await _hubContext.Clients
            .Group($"queue-{queueId}")
            .SendAsync("QueueUpdated", new
            {
                QueueId = queueId,
                TotalPatients = view.TotalPatients,
                Patients = view.Patients,
                LastUpdatedAt = view.LastUpdatedAt
            });
    }
}
```

Register in consumers:

```csharp
public class PatientCheckedInConsumer : IConsumer<PatientCheckedIn>
{
    private readonly IProjectionStore _projectionStore;
    private readonly ProjectionUpdateService _updateService;

    public async Task Consume(ConsumeContext<PatientCheckedIn> context)
    {
        // ... update projection
        var view = await _projectionStore.GetQueueMonitorAsync(evt.AggregateId);
        await _updateService.NotifyQueueUpdated(evt.AggregateId, view);
    }
}
```

**Step 5.3: Register SignalR in DependencyInjection (1h)**

```csharp
services.AddSignalR();
services.AddScoped<ProjectionUpdateService>();

// In Program.cs:
app.MapHub<QueueUpdatesHub>("/hubs/queue-updates");
```

**Step 5.4: Create Frontend Integration Example (0.5h)**

Document WebSocket client usage in API docs

**Acceptance Criteria**:

- [ ] Clients can connect to WebSocket hub
- [ ] Clients can subscribe to queue groups
- [ ] Queue updates broadcast to subscribed clients within 100ms
- [ ] Stale connections are cleaned up
- [ ] Handles reconnection gracefully

---

### BLOCKER #6: Event Consumer Registration (4 hours)

**Already partially covered in Step 4.4**

Additional consumer patterns needed:

1. Saga consumers for multi-step workflows
2. View model updaters
3. External system notifiers (payment, email, etc.)

**Files to Create**:

- `src/RLApp.Adapters.Messaging/Consumers/PaymentProcessingConsumer.cs`
- `src/RLApp.Adapters.Messaging/Consumers/CashierCallConsumer.cs`
- `src/RLApp.Adapters.Messaging/Sagas/ConsultationWorkflowSaga.cs`

---

## PHASE 4C: RESILIENCE & OBSERVABILITY (48-60 hours)

### BLOCKER #7: Resilience Patterns (8 hours)

**Add Polly for Retry + Circuit Breaker**

```csharp
// In DependencyInjection.cs
services.AddHttpClient("ExternalServices")
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddTransientHttpErrorPolicy(policy =>
    {
        return policy
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, attempt, context) =>
                {
                    _logger.LogWarning($"Retry attempt {attempt} after {timespan.TotalSeconds}s");
                })
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, timespan) =>
                {
                    _logger.LogError($"Circuit breaker opened for {timespan.TotalSeconds}s");
                });
    });
```

---

### BLOCKER #8: Observability (6 hours)

**Add OpenTelemetry + Prometheus**

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddConsoleExporter();
    })
    .WithMetrics(builder =>
    {
        builder
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter();
    });
```

---

## PHASE 4D: TEST COVERAGE (80-100 hours)

### TDD Tests Required

Each specification needs:

1. **Unit Tests** (Domain layer)
   - Aggregate creation
   - Business rule validation
   - Event raising

2. **Application Tests** (Handlers)
   - Command success path
   - Command failure scenarios
   - Side effects (events, persistence)

3. **Integration Tests** (End-to-end)
   - HTTP request → Domain → Event
   - Multiple handlers in sequence
   - Persistence verification

### Test Structure Example

**File**: `tests/RLApp.Tests.Unit/Domain/StaffUserTests.cs`

```csharp
public class StaffUserTests
{
    [Fact]
    public void Create_ValidInput_ReturnsStaffUser()
    {
        var user = StaffUser.Create("1", "john", new Email("j@example.com"), "hash", StaffRole.Doctor);
        Assert.NotNull(user);
        Assert.Equal("john", user.Username);
    }

    [Fact]
    public void ChangeRole_ToNewRole_RaisesEvent()
    {
        var user = StaffUser.Create("1", "john", ... , StaffRole.Doctor);
        user.ChangeRole(StaffRole.Admin);

        var events = user.GetUnraisedEvents();
        Assert.Single(events);
        Assert.IsType<StaffRoleChanged>(events.First());
    }
}
```

---

## Summary of Changes

### New Files (~20)

- ConsultingRoom.cs (aggregate)
- ProjectionStoreRepository.cs
- AuditStoreRepository.cs
- AuditLogRecord.cs (model)
- PasswordHasher.cs
- UserContextService.cs
- ~15 Event consumers

### Modified Files (~10)

- DependencyInjection.cs
- Program.cs
- AuthenticateStaffHandler.cs
- ConsultationFlowHandlers.cs
- AppDbContext.cs
- Various handler classes

### Lines of Code Added

- Domain: +500 LOC (new aggregate + events)
- Application: +1000 LOC (new consumers, services)
- Infrastructure: +300 LOC (auth, DI)
- Tests: +2000 LOC (test suite)

**Total**: ~3,800 additional LOC

---

## Validation Checkpoints

### After Phase 4A

```
✓ dotnet build succeeds
✓ JWT token generation works
✓ [Authorize] attribute enforces auth
✓ Password hashing with salt works
✓ IAuditStore logs all operations
```

### After Phase 4B

```
✓ Projections update when events occur
✓ WebSocket clients receive queue updates
✓ RebuildProjectionsCommand rebuilds all views
✓ Dashboard queries return populated results
```

### After Phase 4C

```
✓ HTTP client retries failed requests
✓ Circuit breaker opens after N failures
✓ OpenTelemetry exports traces
✓ Prometheus metrics exposed
```

### After Phase 4D

```
✓ dotnet test runs all suites
✓ Coverage >80%
✓ All BDD scenarios pass
✓ All TDD tests pass
```

---

## Integration Sequence

1. **Week 1**: Phase 4a (Security), Phase 4b (Projections)
2. **Week 2**: Phase 4b completion (WebSocket), Phase 4c (Resilience)
3. **Week 3**: Phase 4c completion, Phase 4d (Testing)

**Target Production-Ready**: End of Week 3
