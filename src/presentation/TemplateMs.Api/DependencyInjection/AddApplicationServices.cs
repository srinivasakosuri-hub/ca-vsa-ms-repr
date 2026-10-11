using FluentValidation;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Application.Features.Order.Commands;
using TemplateMs.Infrastructure.Services;

namespace TemplateMs.Api.DependencyInjection;

/// <summary>Extension method to register all Application-layer services with the DI container.</summary>
public static class AddApplicationServices
{
    /// <summary>Adds application services (validators, date-time provider) to <paramref name="services"/>.</summary>
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // FluentValidation validators — scanned from Application assembly
        services.AddValidatorsFromAssembly(typeof(CreateOrderCommandValidator).Assembly);

        // Date-time provider
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
