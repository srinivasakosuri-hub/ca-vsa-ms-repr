using Testcontainers.MsSql;
using Xunit;

namespace TemplateMs.IntegrationTests;

/// <summary>
/// Shared Testcontainers fixture that starts a single SQL Server container once for the
/// entire integration test collection and tears it down when the last test in the
/// collection finishes.
///
/// xUnit's <see cref="ICollectionFixture{TFixture}"/> guarantees:
///   • <see cref="InitializeAsync"/> is called once before the first test in the collection.
///   • <see cref="DisposeAsync"/> is called once after the last test in the collection.
///
/// All tests in the <see cref="IntegrationTestCollection"/> share the same container and
/// schema.  Individual tests remain isolated because every test inserts its own rows
/// identified by unique IDs / GUIDs — no shared mutable state is left between tests.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("IntegrationTest!1")
        .Build();

    /// <summary>
    /// The connection string for the shared SQL Server container.
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <inheritdoc />
    /// <remarks>Starts the container and applies the canonical DDL schema exactly once.</remarks>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await SchemaLoader.ApplyAsync(ConnectionString);
    }

    /// <inheritdoc />
    /// <remarks>Stops and removes the container after all tests in the collection have run.</remarks>
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
