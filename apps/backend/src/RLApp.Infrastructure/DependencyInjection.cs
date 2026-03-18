using Microsoft.Extensions.DependencyInjection;
using RLApp.Application.Commands;

namespace RLApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // MediatR registration picking up Application Assembly
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(Command).Assembly);
        });

        // Other generic infrastructure needs would go here, e.g., Scoped Repositories, Event Publishers.
        // E.g., services.AddScoped<IEventStore, EventStoreRepository>();

        return services;
    }
}
