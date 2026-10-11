using TemplateMs.Infrastructure.Messaging.Amq;

namespace TemplateMs.Api.DependencyInjection;

/// <summary>Extension method to register message bus (ActiveMQ) configuration with DI.</summary>
public static class AddMessageBus
{
    /// <summary>Adds ActiveMQ configuration options. The publisher itself is registered in <c>AddInfrastructure</c>.</summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMessageBusServices(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<ActiveMqConfiguration>(
            configuration.GetSection(ActiveMqConfiguration.SectionName));

        return services;
    }
}
