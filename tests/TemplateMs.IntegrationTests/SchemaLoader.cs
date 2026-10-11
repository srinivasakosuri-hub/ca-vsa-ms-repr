using Dapper;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace TemplateMs.IntegrationTests;

/// <summary>
/// Applies the canonical DDL scripts from the TemplateMs.Database SSDT project
/// against a target SQL Server connection.
/// Scripts are split on GO batch separators before execution, matching how
/// SQL Server Management Studio and sqlcmd process .sql files.
/// </summary>
internal static class SchemaLoader
{
    // Executed in FK dependency order: Orders → OrderItems → OrderItemAttributes → OutboxMessages
    private static readonly string[] ScriptFiles =
    [
        Path.Combine("DatabaseScripts", "Orders.sql"),
        Path.Combine("DatabaseScripts", "OrderItems.sql"),
        Path.Combine("DatabaseScripts", "OrderItemAttributes.sql"),
        Path.Combine("DatabaseScripts", "OutboxMessages.sql"),
    ];

    public static async Task ApplyAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);

        foreach (var relativePath in ScriptFiles)
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            var sql = await File.ReadAllTextAsync(fullPath);

            // ADO.NET cannot handle GO batch separators — split and execute each batch individually.
            var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    await connection.ExecuteAsync(trimmed);
            }
        }
    }
}
