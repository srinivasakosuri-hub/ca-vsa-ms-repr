using System.Data;

namespace TemplateMs.Application.Common.Interfaces;

/// <summary>
/// Factory for creating connections to the query (read) database.
/// Use this in read repositories that perform SELECT-only operations.
/// </summary>
public interface IQueryDbConnectionFactory
{
    /// <summary>Creates and opens a new <see cref="IDbConnection"/> to the query database.</summary>
    IDbConnection CreateConnection();
}
