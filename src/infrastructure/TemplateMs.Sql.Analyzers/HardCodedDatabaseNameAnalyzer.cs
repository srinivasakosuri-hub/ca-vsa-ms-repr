using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TemplateMs.Sql.Analyzers;

/// <summary>
/// TMSSQL001 — Detects hard-coded three-part database names (e.g. <c>MyDb.dbo.Orders</c> or
/// <c>[MyDb].[dbo].[Orders]</c>) inside <see langword="const"/> string fields of the
/// <c>TemplateMs.Sql</c> project.  Database names must be injected at runtime, not baked into SQL.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HardCodedDatabaseNameAnalyzer : DiagnosticAnalyzer
{
    /// <summary>The diagnostic identifier emitted by this analyzer.</summary>
    public const string DiagnosticId = "TMSSQL001";

    private static readonly LocalizableString Title =
        "Hard-coded database name detected in SQL constant";

    private static readonly LocalizableString MessageFormat =
        "SQL constant '{0}' contains a hard-coded three-part name '{1}'. " +
        "Database names must not be embedded in SQL strings; use a configurable token instead.";

    private static readonly LocalizableString Description =
        "Three-part qualified names (database.schema.table) embed the database name directly " +
        "in the SQL string, making the code environment-specific. " +
        "Remove the database prefix and configure the target database at the connection level.";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: "SqlHardCoding",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/your-org/TemplateMs/blob/main/docs/analyzers/TMSSQL001.md");

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

        // Only examine: public/internal const string fields.
        if (!field.Modifiers.Any(SyntaxKind.ConstKeyword)) return;

        foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
        {
            if (variable.Initializer?.Value is not LiteralExpressionSyntax literal) continue;
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                !literal.IsKind(SyntaxKind.Utf8StringLiteralExpression)) continue;

            string sql = literal.Token.ValueText;

            foreach ((int index, int length, string rawName, int dotCount)
                in SqlPatternHelper.FindQualifiedNames(sql, minimumDots: 2))
            {
                // 3-part name: exactly 2 dots  (db.schema.table)
                if (dotCount == 2)
                {
                    string fieldName = variable.Identifier.ValueText;
                    Location location = GetLocationInsideLiteral(literal, index, length);
                    context.ReportDiagnostic(Diagnostic.Create(Rule, location, fieldName, rawName));
                }
            }
        }
    }

    /// <summary>
    /// Tries to map a character offset inside the string value back to a source location.
    /// Falls back to the literal token location when precise mapping is not possible.
    /// </summary>
    private static Location GetLocationInsideLiteral(
        LiteralExpressionSyntax literal, int valueIndex, int valueLength)
    {
        // For raw strings the ValueText does not include the delimiters, but computing the
        // exact offset inside multi-line raw literals is non-trivial; report the whole literal.
        return literal.GetLocation();
    }
}
