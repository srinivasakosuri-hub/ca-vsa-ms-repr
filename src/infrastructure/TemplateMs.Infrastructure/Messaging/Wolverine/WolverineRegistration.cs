using TemplateMs.Application.Behaviors;
using TemplateMs.Application.Features.Order.Commands;
using Wolverine;
using Wolverine.SqlServer;

namespace TemplateMs.Infrastructure.Messaging.Wolverine;

/// <summary>
/// Configures WolverineFX options for the TemplateMs solution.
/// Registers handler discovery, middleware policies, SQL Server durability, and message routing.
/// </summary>
public static class WolverineRegistration
{
    /// <summary>Applies all WolverineFX configuration to the provided <see cref="WolverineOptions"/>.</summary>
    /// <param name="opts">The Wolverine options instance to configure.</param>
    /// <param name="commandConnectionString">
    /// The SQL Server connection string used for Wolverine's durable inbox/outbox schema.
    /// </param>
    /// <param name="useInMemoryTransport">
    /// When <see langword="true"/>, skips SQL Server persistence (use in integration tests to
    /// avoid cleanup SQL against an already-disposed Testcontainers instance).
    /// </param>
    public static void Configure(WolverineOptions opts, string commandConnectionString, bool useInMemoryTransport = false)
    {
        // Auto-discover handlers in the Application assembly.
        opts.Discovery.IncludeAssembly(typeof(CreateOrderCommandHandler).Assembly);

        // Enable SQL Server-backed durable messaging (inbox/outbox) via WolverineFx.SqlServer.
        // Skipped when useInMemoryTransport is true (e.g. integration tests) to avoid
        // WolverineRuntime.StopAsync attempting SQL cleanup after container teardown.
        if (!useInMemoryTransport)
        {
            opts.PersistMessagesWithSqlServer(commandConnectionString);
        }

        // Apply logging middleware globally to all message handlers.
        // This logger is crap. It is giving all error while logging. 
        opts.Policies.AddMiddleware<LoggingBehavior>();

        // NOTE: Wolverine retry policies are configured per-handler via [Retry] attributes
        // or by extending this method with opts.Handlers.OnException<T>().RetryTimes(n)
        // once the correct WolverineFx version API is confirmed.
    }
}
