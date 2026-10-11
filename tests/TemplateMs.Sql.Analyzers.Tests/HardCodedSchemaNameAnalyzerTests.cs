namespace TemplateMs.Sql.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="HardCodedSchemaNameAnalyzer"/> (TMSSQL002).
/// Each test follows the Arrange / Act / Assert pattern.
/// </summary>
public sealed class HardCodedSchemaNameAnalyzerTests
{
    private readonly HardCodedSchemaNameAnalyzer _sut = new();

    // -------------------------------------------------------------------------
    // Positive cases — diagnostic MUST fire
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Diagnostic_WhenConstStringContainsPlain2PartName()
    {
        // Arrange
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
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_WhenConstStringContainsBracketed2PartName()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT Id FROM [dbo].[Orders] WHERE Id = @Id;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_WhenConstStringContainsMixedBracketing2PartName()
    {
        // Arrange — [dbo].Orders mixes styles
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Insert = "INSERT INTO [dbo].Orders (Name) VALUES (@Name);";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_WhenConstStringContains2PartNameInUpdateStatement()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Update = "UPDATE sales.Orders SET IsActive = 0 WHERE Id = @Id;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_WhenConstStringContains2PartNameInJoin()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = @"
                    SELECT o.Id
                    FROM   Orders o
                    LEFT JOIN audit.AuditLog al ON al.OrderId = o.Id;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_WhenRawStringLiteralContains2PartName()
    {
        // Arrange — triple-quoted raw string literal (C# 11+)
        const string source = """"
            namespace TestNs;
            public static class Sql
            {
                public const string Query = """
                    SELECT Id FROM archive.Orders WHERE Id = @Id;
                    """;
            }
            """";

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Diagnostic_ContainsHardCodedNameInMessage()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT * FROM sales.Products;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Should().ContainSingle()
            .Which.GetMessage().Should().Contain("sales.Products");
    }

    [Fact]
    public async Task Diagnostic_HasErrorSeverity()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT * FROM dbo.Orders;";
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
        // Arrange — no schema prefix at all
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
        diagnostics.Where(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnostic_WhenConstStringContains3PartName()
    {
        // Arrange — 3-part names are reported by TMSSQL001, not TMSSQL002
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT Id FROM MyDb.dbo.Orders WHERE Id = @Id;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Where(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnostic_WhenNonConstFieldContains2PartName()
    {
        // Arrange — only const fields are analyzed
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public static readonly string Query = "SELECT * FROM dbo.Orders;";
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Where(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnostic_WhenLocalVariableContains2PartName()
    {
        // Arrange — local string variables are out of scope for this analyzer
        const string source = """
            namespace TestNs;
            public class Service
            {
                public void Run()
                {
                    string sql = "SELECT * FROM dbo.Orders;";
                }
            }
            """;

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, _sut);

        // Assert
        diagnostics.Where(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnostic_WhenSqlContainsAliasedDotAccess()
    {
        // Arrange — "o.Id" is a column alias, not a schema-qualified name after a keyword
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
        diagnostics.Where(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId)
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
        diagnostics.Where(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId)
                   .Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Cross-analyzer separation — both analyzers on the same source
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BothAnalyzers_3PartNameRaisesOnlyTMSSQL001_NotTMSSQL002()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT Id FROM MyDb.dbo.Orders WHERE Id = @Id;";
            }
            """;

        var dbAnalyzer = new HardCodedDatabaseNameAnalyzer();
        var schemaAnalyzer = new HardCodedSchemaNameAnalyzer();

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, dbAnalyzer, schemaAnalyzer);

        // Assert — only TMSSQL001 fires; TMSSQL002 must stay silent for 3-part names
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId);
        diagnostics.Should().NotContain(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task BothAnalyzers_2PartNameRaisesOnlyTMSSQL002_NotTMSSQL001()
    {
        // Arrange
        const string source = """
            namespace TestNs;
            public static class Sql
            {
                public const string Query = "SELECT Id FROM dbo.Orders WHERE Id = @Id;";
            }
            """;

        var dbAnalyzer = new HardCodedDatabaseNameAnalyzer();
        var schemaAnalyzer = new HardCodedSchemaNameAnalyzer();

        // Act
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, dbAnalyzer, schemaAnalyzer);

        // Assert — only TMSSQL002 fires; TMSSQL001 must stay silent for 2-part names
        diagnostics.Should().ContainSingle(d => d.Id == HardCodedSchemaNameAnalyzer.DiagnosticId);
        diagnostics.Should().NotContain(d => d.Id == HardCodedDatabaseNameAnalyzer.DiagnosticId);
    }
}
