using System.Data;

namespace TemplateMs.Application.Common.Interfaces;

/// <summary>
/// Factory for creating connections to the command (write) database.
/// Use this in repositories and unit of work that perform INSERT/UPDATE/DELETE operations.
/// </summary>
public interface ICommandDbConnectionFactory
{
    /// <summary>Creates and opens a new <see cref="IDbConnection"/> to the command database.</summary>
    IDbConnection CreateConnection();
}
