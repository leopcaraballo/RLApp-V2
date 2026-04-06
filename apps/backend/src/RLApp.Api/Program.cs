using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RLApp.Application.Observability;
using RLApp.Adapters.Messaging.Observability;
using RLApp.Adapters.Http.Middleware;
using RLApp.Adapters.Http.Security;
using RLApp.Adapters.Persistence.Data;
using RLApp.Infrastructure;
using RLApp.Infrastructure.BackgroundServices;
using RLApp.Infrastructure.Data;
using RLApp.Infrastructure.Observability;
using RLApp.Api.Hubs;
using RLApp.Api.Consumers;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MassTransit")
        .AddSource(PatientTrajectoryTelemetry.ActivitySourceName)
        .AddSource(MessageFlowTelemetry.ActivitySourceName)
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(PatientTrajectoryTelemetry.MeterName)
        .AddMeter(RealtimeChannelTelemetry.MeterName)
        .AddMeter(OutboxProcessorTelemetry.MeterName)
        .AddPrometheusExporter());

// Configure Hexagonal Architecture dependencies with SignalR consumers
builder.Services.AddInfrastructureServices(builder.Configuration, x =>
{
    x.AddConsumer<SignalRNotificationConsumer>();
});

// Configure JWT authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ??
    throw new InvalidOperationException("Jwt:Secret not configured");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "RLApp",
            ValidateAudience = true,
            ValidAudience = "RLApp.API",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AuthenticatedStaff, policy =>
        policy.RequireAuthenticatedUser());
    options.AddPolicy(AuthorizationPolicies.ReceptionOperations, policy =>
        policy.RequireAuthenticatedUser().RequireRole("Receptionist", "Supervisor"));
    options.AddPolicy(AuthorizationPolicies.CashierOperations, policy =>
        policy.RequireAuthenticatedUser().RequireRole("Cashier", "Supervisor"));
    options.AddPolicy(AuthorizationPolicies.DoctorOperations, policy =>
        policy.RequireAuthenticatedUser().RequireRole("Doctor", "Supervisor"));
    options.AddPolicy(AuthorizationPolicies.SupervisorOnly, policy =>
        policy.RequireAuthenticatedUser().RequireRole("Supervisor"));
    options.AddPolicy(AuthorizationPolicies.SupportOnly, policy =>
        policy.RequireAuthenticatedUser().RequireRole("Support"));
    options.AddPolicy(AuthorizationPolicies.SupportOrSupervisor, policy =>
        policy.RequireAuthenticatedUser().RequireRole("Support", "Supervisor"));
});

var app = builder.Build();

var applyMigrationsOnStartup = app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");
if (applyMigrationsOnStartup)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    app.Logger.LogInformation("Applying pending database migrations on startup.");
    dbContext.Database.Migrate();

    // Seed initial development data
    var seeder = scope.ServiceProvider.GetRequiredService<RLApp.Infrastructure.Data.DbSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization(AuthorizationPolicies.AuthenticatedStaff);

// Health Checks & Metrics
var healthResponseWriter = new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            details = report.Entries.Select(e => new { key = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description })
        });
        await context.Response.WriteAsync(result);
    }
};

app.MapHealthChecks("/health", healthResponseWriter);
app.MapHealthChecks("/health/ready", healthResponseWriter);
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = healthResponseWriter.ResponseWriter
});
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
