using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CrystalReportAnalyzer.Extractors;

public static class FormulaReferenceParser
{
    private static readonly Regex DbFieldRx  = new(@"\{([A-Za-z_][A-Za-z0-9_ ]*\.[A-Za-z_][A-Za-z0-9_ ]*)\}", RegexOptions.Compiled);
    private static readonly Regex ParamRx    = new(@"\{\?([^}]+)\}", RegexOptions.Compiled);
    private static readonly Regex FormulaRx  = new(@"\{@([^}]+)\}", RegexOptions.Compiled);
    private static readonly Regex RunTotalRx = new(@"\{#([^}]+)\}", RegexOptions.Compiled);

    public static FormulaReferences Parse(string text)
    {
        if (string.IsNullOrEmpty(text)) return new FormulaReferences();
        return new FormulaReferences
        {
            DbFields      = Matches(DbFieldRx, text),
            Parameters    = Matches(ParamRx, text),
            OtherFormulas = Matches(FormulaRx, text),
            RunningTotals = Matches(RunTotalRx, text),
        };
    }

    private static List<string> Matches(Regex rx, string text) =>
        rx.Matches(text).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
}

public class FormulaReferences
{
    public List<string> DbFields      { get; init; } = new();
    public List<string> Parameters    { get; init; } = new();
    public List<string> OtherFormulas { get; init; } = new();
    public List<string> RunningTotals { get; init; } = new();
}
