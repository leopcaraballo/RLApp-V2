using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using System.Reflection;

namespace RLApp.Infrastructure.HealthChecks;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly ConnectionFactory _connectionFactory;

    public RabbitMqHealthCheck(IConfiguration configuration)
    {
        var host = configuration["RabbitMQ:Host"] ?? "localhost";
        var port = ushort.TryParse(configuration["RabbitMQ:Port"], out var parsedPort)
            ? parsedPort
            : (ushort)5672;

        _connectionFactory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = configuration["RabbitMQ:Username"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
        };
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => CheckHealthInternalAsync(cancellationToken);

    private async Task<HealthCheckResult> CheckHealthInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var connection = await OpenConnectionAsync(cancellationToken);

            if (!connection.IsOpen)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ connection was created but is not open.");
            }

            return HealthCheckResult.Healthy(
                $"RabbitMQ reachable at {_connectionFactory.HostName}:{_connectionFactory.Port}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Failed to connect to RabbitMQ: {ex.Message}");
        }
    }

    private async Task<IConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var factoryType = _connectionFactory.GetType();

        var syncFactoryMethod = factoryType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method => method.Name == "CreateConnection" && method.GetParameters().Length == 0);

        if (syncFactoryMethod is not null)
        {
            var syncConnection = syncFactoryMethod.Invoke(_connectionFactory, null) as IConnection;
            if (syncConnection is not null)
            {
                return syncConnection;
            }
        }

        var asyncFactoryMethod = factoryType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name == "CreateConnectionAsync")
            .FirstOrDefault(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 0
                    || (parameters.Length == 1 && parameters[0].ParameterType == typeof(CancellationToken));
            });

        if (asyncFactoryMethod is null)
        {
            throw new InvalidOperationException("RabbitMQ client does not expose a supported connection factory method.");
        }

        var arguments = asyncFactoryMethod.GetParameters().Length == 1
            ? new object?[] { cancellationToken }
            : null;

        var invocationResult = asyncFactoryMethod.Invoke(_connectionFactory, arguments)
            ?? throw new InvalidOperationException("RabbitMQ client returned no connection task.");

        if (invocationResult is Task task)
        {
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty?.GetValue(task) is IConnection taskConnection)
            {
                return taskConnection;
            }
        }

        var resultMember = invocationResult.GetType().GetProperty("Result");
        if (resultMember?.GetValue(invocationResult) is IConnection connection)
        {
            return connection;
        }

        throw new InvalidOperationException("RabbitMQ client returned an unsupported connection result.");
    }
}