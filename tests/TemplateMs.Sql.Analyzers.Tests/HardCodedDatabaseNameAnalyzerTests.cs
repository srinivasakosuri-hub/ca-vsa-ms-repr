namespace TemplateMs.Sql.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="HardCodedDatabaseNameAnalyzer"/> (TMSSQL001).
/// Each test follows the Arrange / Act / Assert pattern.
/// </summary>
public sealed class HardCodedDatabaseNameAnalyzerTests
{
    private readonly HardCodedDatabaseNameAnalyzer _sut = new();

    // -------------------------------------------------------------------------
    // Positive cases — diagnostic MUST fire
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Diagnostic_WhenConstStringContainsPlain3PartName()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT Id FROM MyDatabase.dbo.Orders WHERE Id = @Id;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_WhenConstStringContainsBracketed3PartName()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT Id FROM [MyDatabase].[dbo].[Orders] WHERE Id = @Id;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_WhenConstStringContainsMixedBracketing3PartName()
    {
        // Arrange — [MyDb].dbo.[Orders] mixes styles
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Insert = "INSERT INTO [MyDb].dbo.[Orders] (Name) VALUES (@Name);";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_WhenConstStringContains3PartNameInInnerJoin()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = @"
                    SELECT o.Id
                    FROM   Orders o
                    INNER JOIN ReportingDb.dbo.AuditLog al ON al.OrderId = o.Id;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_WhenRawStringLiteralContains3PartName()
    {
        // Arrange — triple-quoted raw string literal (C# 11+)
        const string source = """"
            namespace TestNs;
            public static class Sql
            {
                public const string Delete = """
                    DELETE FROM ArchiveDb.archive.Orders WHERE Id = @Id;
                    """;
            }
            """";

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_ContainsHardCodedNameInMessage()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT * FROM CatalogDb.sales.Products;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.GetMessage().Should().Contain("CatalogDb.sales.Products");
    }

    [Fact]
    public async Task Diagnostic_HasErrorSeverity()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT * FROM MyDb.dbo.Orders;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.Severity.Should().Be(DiagnosticSeverity.Error);
    }

    // -------------------------------------------------------------------------
    // Negative cases — diagnostic must NOT fire
    // -------------------------------------------------------------------------

    [Fact]
    public async Task NoDiagnostic_WhenConstStringContainsOnlyTableName()
    {
        // Arrange — no schema or database prefix
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT Id FROM Orders WHERE Id = @Id;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Where(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnostic_WhenConstStringContainsOnly2PartName()
    {
        // Arrange — 2-part names are reported by TMSSQL002, not TMSSQL001
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT Id FROM dbo.Orders WHERE Id = @Id;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Where(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnostic_WhenNonConstFieldContains3PartName()
    {
        // Arrange — only const fields are analyzed
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public static readonly string Query = "SELECT * FROM MyDb.dbo.Orders;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Where(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnostic_WhenLocalVariableContains3PartName()
    {
        // Arrange — local string variables are out of scope for this analyzer
        const string source = """
            namespace TestNs;
            public class Service
            {
                public void Run()
                {
                    string sql = "SELECT * FROM MyDb.dbo.Orders;";
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Where(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnostic_WhenSqlContainsAliasedDotAccess()
    {
        // Arrange — "o.Id" is a column alias, not a qualified name
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT o.Id, o.Name FROM Orders o WHERE o.Id = @Id;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Where(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnostic_WhenStringIsEmpty()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Where(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }
}
