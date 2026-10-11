using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Infrastructure.Configuration;

namespace TemplateMs.Infrastructure.Database;

/// <summary>
/// Creates and opens SQL Server connections for the query (read) database.
/// Implements <see cref="IQueryDbConnectionFactory"/>.
/// </summary>
public sealed class QueryDbConnectionFactory : IQueryDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>Initializes a new <see cref="QueryDbConnectionFactory"/>.</summary>
    /// <param name="options">Infrastructure configuration options.</param>
    public QueryDbConnectionFactory(IOptions<InfrastructureOptions> options) =>
        _connectionString = options.Value.QueryConnectionString;

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
