using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TemplateMs.Sql.Analyzers;

/// <summary>
/// TMSSQL002 — Detects hard-coded two-part schema-qualified names (e.g. <c>dbo.Orders</c> or
/// <c>[dbo].[Orders]</c>) inside <see langword="const"/> string fields of the
/// <c>TemplateMs.Sql</c> project.  Schema names must be injected at runtime, not baked into SQL.
/// </summary>
/// <remarks>
/// Three-part names are reported exclusively by <see cref="HardCodedDatabaseNameAnalyzer"/>
/// (TMSSQL001).  This analyzer only fires when the dot count is exactly one (schema.table),
/// avoiding duplicate diagnostics on the same location.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HardCodedSchemaNameAnalyzer : DiagnosticAnalyzer
{
    /// <summary>The diagnostic identifier emitted by this analyzer.</summary>
    public const string DiagnosticId = "TMSSQL002";

    private static readonly LocalizableString Title =
        "Hard-coded schema name detected in SQL constant";

    private static readonly LocalizableString MessageFormat =
        "SQL constant '{0}' contains a hard-coded two-part name '{1}'. " +
        "Schema names must not be embedded in SQL strings; use a configurable token instead.";

    private static readonly LocalizableString Description =
        "Two-part qualified names (schema.table) embed the schema name directly in the SQL " +
        "string, making the code environment-specific. " +
        "Remove the schema prefix and configure the default schema at the connection level, " +
        "or use a configurable placeholder.";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: "SqlHardCoding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/your-org/TemplateMs/blob/main/docs/analyzers/TMSSQL002.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
    {
        var field = (FieldDeclarationSyntax)context.Node;

        // Only examine: const string fields.
        if (!field.Modifiers.Any(SyntaxKind.ConstKeyword)) return;

        foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
        {
            if (variable.Initializer?.Value is not LiteralExpressionSyntax literal) continue;
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                !literal.IsKind(SyntaxKind.Utf8StringLiteralExpression)) continue;

            string sql = literal.Token.ValueText;

            foreach ((int index, int length, string rawName, int dotCount)
                in SqlPatternHelper.FindQualifiedNames(sql, minimumDots: 1))
            {
                // Only report exactly 2-part names (schema.table).
                // 3-part names are the responsibility of TMSSQL001.
                if (dotCount == 1)
                {
                    string fieldName = variable.Identifier.ValueText;
                    Location location = literal.GetLocation();
                    context.ReportDiagnostic(Diagnostic.Create(Rule, location, fieldName, rawName));
                }
            }
        }
    }
}
