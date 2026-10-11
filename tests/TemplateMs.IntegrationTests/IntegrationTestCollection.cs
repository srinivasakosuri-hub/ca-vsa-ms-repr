using Xunit;

namespace TemplateMs.IntegrationTests;

/// <summary>
/// Defines the xUnit test collection that shares a single <see cref="SqlServerFixture"/>
/// across <see cref="DatabaseIntegrationTests"/> and <see cref="EndToEndTests"/>.
///
/// Any test class decorated with <c>[Collection(Name)]</c> receives the same fixture
/// instance, meaning the SQL Server container is started only once for all tests in
/// both classes and torn down once when they are all complete.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<SqlServerFixture>
{
    /// <summary>Collection name constant — must match the argument passed to <c>[Collection(...)]</c>.</summary>
    public const string Name = "SqlServer";
}
