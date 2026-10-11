using Dapper;

namespace TemplateMs.Infrastructure.Database;

/// <summary>
/// Registers custom Dapper type mappings and column-name conventions.
/// Call <see cref="Configure"/> once at application startup before any Dapper queries execute.
/// </summary>
public static class DapperTypeMappings
{
    private static bool _configured;
    private static readonly object Lock = new();

    /// <summary>Configures all Dapper type handlers and mapping conventions. Thread-safe and idempotent.</summary>
    public static void Configure()
    {
        lock (Lock)
        {
            if (_configured) return;

            DefaultTypeMap.MatchNamesWithUnderscores = true;

            _configured = true;
        }
    }
}
