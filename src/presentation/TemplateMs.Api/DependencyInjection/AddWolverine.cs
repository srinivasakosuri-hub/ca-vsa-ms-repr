using TemplateMs.Infrastructure.Configuration;
using TemplateMs.Infrastructure.Messaging.Wolverine;
using Wolverine;

namespace TemplateMs.Api.DependencyInjection;

/// <summary>Extension method to configure and register WolverineFX with the host.</summary>
public static class AddWolverine
{
    /// <summary>
    /// Configures WolverineFX using <see cref="WolverineRegistration.Configure"/> and registers
    /// it with the host builder. Call this after <c>AddInfrastructure</c>.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The same <see cref="WebApplicationBuilder"/> for chaining.</returns>
    public static WebApplicationBuilder AddWolverineMessaging(this WebApplicationBuilder builder)
    {
        var commandConnectionString = builder.Configuration
            .GetSection(InfrastructureOptions.SectionName)
            .GetValue<string>("CommandConnectionString") ?? string.Empty;

        var useInMemoryTransport = builder.Configuration
            .GetSection(InfrastructureOptions.SectionName)
            .GetValue<bool>("UseInMemoryTransport");

        builder.Host.UseWolverine(opts =>
            WolverineRegistration.Configure(opts, commandConnectionString, useInMemoryTransport));

        return builder;
    }
}
