using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Infrastructure.Configuration;

namespace TemplateMs.Infrastructure.Database;

/// <summary>
/// Creates and opens SQL Server connections for the command (write) database.
/// Implements <see cref="ICommandDbConnectionFactory"/>.
/// </summary>
public sealed class CommandDbConnectionFactory : ICommandDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>Initializes a new <see cref="CommandDbConnectionFactory"/>.</summary>
    /// <param name="options">Infrastructure configuration options.</param>
    public CommandDbConnectionFactory(IOptions<InfrastructureOptions> options) =>
        _connectionString = options.Value.CommandConnectionString;

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
