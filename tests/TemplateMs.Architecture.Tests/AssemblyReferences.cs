using System.Reflection;
using TemplateMs.Api.Endpoints.Orders;
using TemplateMs.Application.Features.Order.Commands;
using TemplateMs.Domain.Entities;
using TemplateMs.Infrastructure.Repositories;
using TemplateMs.Sql.Features.Order.Queries;

namespace TemplateMs.Architecture.Tests;

/// <summary>
/// Typed anchors for each production assembly.
/// Using a concrete type from each assembly guarantees the assembly is loaded into the
/// test's AppDomain and that the reference is never trimmed by the compiler.
/// </summary>
internal static class AssemblyReferences
{
    /// <summary>TemplateMs.Domain — the innermost ring; no dependencies on other production layers.</summary>
    public static readonly Assembly Domain = typeof(Order).Assembly;

    /// <summary>TemplateMs.Application — use-case ring; may depend only on Domain.</summary>
    public static readonly Assembly Application = typeof(CreateOrderCommand).Assembly;

    /// <summary>TemplateMs.Infrastructure — outer ring; implements Application interfaces.</summary>
    public static readonly Assembly Infrastructure = typeof(OrderWriteRepository).Assembly;

    /// <summary>TemplateMs.Api — presentation ring; depends on Application (commands/queries/DTOs) only.</summary>
    public static readonly Assembly Api = typeof(CreateOrderEndpoint).Assembly;

    /// <summary>TemplateMs.Sql — raw SQL string constants; no business logic, no layer dependencies.</summary>
    public static readonly Assembly Sql = typeof(OrderQuerySql).Assembly;

    /// <summary>All five production assemblies — used for blanket guard-rail tests.</summary>
    public static readonly Assembly[] All = [Domain, Application, Infrastructure, Api, Sql];
}
