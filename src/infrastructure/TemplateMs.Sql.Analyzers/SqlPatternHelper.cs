using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TemplateMs.Sql.Analyzers;

/// <summary>
/// Shared SQL pattern detection logic used by both <see cref="HardCodedDatabaseNameAnalyzer"/>
/// and <see cref="HardCodedSchemaNameAnalyzer"/>.
/// </summary>
internal static class SqlPatternHelper
{
    // Matches an identifier segment: either [anything] or a bare word (letters, digits, underscore, dollar).
    private const string IdentifierSegment = @"(?:\[[^\]]+\]|[\w$]+)";

    // Matches a dot, optionally surrounded by whitespace.
    private const string Dot = @"\s*\.\s*";

    // 3-part name: db.schema.table  (with optional brackets on each segment)
    // 2-part name: schema.table
    // Captured group 1 = the entire qualified name (used to count dots).
    private static readonly string QualifiedNameCapture =
        $@"({IdentifierSegment}(?:{Dot}{IdentifierSegment}){{1,2}})";

    // SQL keywords that can be followed by a qualified object name.
    private const string SqlKeywords = @"(?:FROM|INTO|UPDATE|JOIN|INNER\s+JOIN|LEFT\s+JOIN|RIGHT\s+JOIN|FULL\s+JOIN|CROSS\s+JOIN)";

    // The full pattern: keyword (whitespace) qualified-name
    private static readonly Regex QualifiedNameRegex = new Regex(
        $@"(?i)\b{SqlKeywords}\s+{QualifiedNameCapture}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Strips bracket characters so we can count dots reliably.
    private static readonly Regex BracketStripper = new Regex(@"[\[\]\s]", RegexOptions.Compiled);

    /// <summary>
    /// Returns all qualified object names found in <paramref name="sql"/>
    /// that contain at least <paramref name="minimumDots"/> dots.
    /// Each result includes the match position and the raw matched text.
    /// </summary>
    public static IEnumerable<(int Index, int Length, string RawName, int DotCount)> FindQualifiedNames(
        string sql, int minimumDots)
    {
        foreach (Match m in QualifiedNameRegex.Matches(sql))
        {
            Group nameGroup = m.Groups[1];
            string raw = nameGroup.Value;
            string stripped = BracketStripper.Replace(raw, string.Empty);
            int dots = CountChar(stripped, '.');

            if (dots >= minimumDots)
            {
                yield return (nameGroup.Index, nameGroup.Length, raw, dots);
            }
        }
    }

    private static int CountChar(string s, char c)
    {
        int count = 0;
        foreach (char ch in s)
        {
            if (ch == c) count++;
        }
        return count;
    }
}
