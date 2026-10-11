using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Interfaces;
using TemplateMs.Infrastructure.Configuration;
using TemplateMs.Infrastructure.Database;
using TemplateMs.Infrastructure.Messaging.Amq;
using TemplateMs.Infrastructure.Messaging.Inbox;
using TemplateMs.Infrastructure.Messaging.Outbox;
using TemplateMs.Infrastructure.Messaging.Wolverine;
using TemplateMs.Infrastructure.Repositories;
using TemplateMs.Infrastructure.Services;
using TemplateMs.Infrastructure.UnitOfWork;

namespace TemplateMs.Api.DependencyInjection;

/// <summary>Extension method to register all Infrastructure-layer services with the DI container.</summary>
public static class AddInfrastructureServices
{
    /// <summary>Adds infrastructure services (repositories, UoW, messaging, background services) to <paramref name="services"/>.</summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="configuration">Application configuration, used to bind <see cref="InfrastructureOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options
        services.Configure<InfrastructureOptions>(configuration.GetSection(InfrastructureOptions.SectionName));
        services.Configure<ActiveMqConfiguration>(configuration.GetSection(ActiveMqConfiguration.SectionName));

        // Connection factories
        services.AddSingleton<ICommandDbConnectionFactory, CommandDbConnectionFactory>();
        services.AddSingleton<IQueryDbConnectionFactory, QueryDbConnectionFactory>();

        // Unit of work (scoped — one per request/message)
        // Registered directly against the interface (no factory) so Wolverine's codegen
        // can resolve it via constructor injection without service location.
        services.AddScoped<IUnitOfWork, DapperUnitOfWork>();

        // Domain event dispatcher
        services.AddScoped<IDomainEventDispatcher, WolverineEventForwarders>();

        // HTTP context — required by CurrentUserProvider
        services.AddHttpContextAccessor();

        // Current user (JWT sub claim → Guid, falls back to Guid.NewGuid() for background operations)
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

        // Repositories
        services.AddScoped<IOrderWriteRepository, OrderWriteRepository>();
        services.AddScoped<IOrderReadRepository, OrderReadRepository>();
        services.AddScoped<IOrderItemWriteRepository, OrderItemWriteRepository>();
        services.AddScoped<IOrderItemAttributeWriteRepository, OrderItemAttributeWriteRepository>();

        // Messaging
        services.AddSingleton<IMessageBusPublisher, ActiveMqPublisher>();
        services.AddScoped<OutboxStorage>();

        // Background services
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<InboxProcessor>();
        services.AddHostedService<ActiveMqConsumer>();

        // Dapper type mappings (idempotent)
        DapperTypeMappings.Configure();

        return services;
    }
}
