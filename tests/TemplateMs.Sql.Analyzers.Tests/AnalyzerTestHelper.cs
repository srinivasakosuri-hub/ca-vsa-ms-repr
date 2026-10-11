using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace TemplateMs.Sql.Analyzers.Tests;

/// <summary>
/// Compiles a snippet of C# source code in-process and runs one or more Roslyn analyzers
/// against it, returning every <see cref="Diagnostic"/> that was reported.
/// </summary>
internal static class AnalyzerTestHelper
{
    // Minimal set of references needed to compile a snippet that only uses BCL types.
    private static readonly IReadOnlyList<MetadataReference> DefaultReferences =
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
        MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
    ];

    /// <summary>
    /// Compiles <paramref name="source"/> and returns diagnostics produced by
    /// <paramref name="analyzers"/> (excluding compiler errors unrelated to the analyzers).
    /// </summary>
    /// <param name="source">The C# source code to analyze.</param>
    /// <param name="analyzers">One or more analyzers to run.</param>
    /// <returns>All analyzer diagnostics sorted by <see cref="Location"/>.</returns>
    public static async Task<IReadOnlyList<Diagnostic>> GetDiagnosticsAsync(
        string source,
        params DiagnosticAnalyzer[] analyzers)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: DefaultReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CompilationWithAnalyzers compilationWithAnalyzers = compilation
            .WithAnalyzers(ImmutableArray.Create(analyzers));

        ImmutableArray<Diagnostic> diagnostics =
            await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToList();
    }

    /// <summary>
    /// Convenience overload that runs a single analyzer.
    /// </summary>
    public static Task<IReadOnlyList<Diagnostic>> GetDiagnosticsAsync(
        string source,
        DiagnosticAnalyzer analyzer) =>
        GetDiagnosticsAsync(source, [analyzer]);
}
