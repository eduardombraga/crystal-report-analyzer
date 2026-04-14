using CrystalReportAnalyzer.Core.Configuration;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Core.Analyzer;

public static class ComplexityAnalyzer
{
    public static ComplexityResult Analyze(ReportModel report, ScoringConfig config)
    {
        int W(string key, int @default) =>
            config.Weights.TryGetValue(key, out int v) ? v : @default;

        var breakdown = new Dictionary<string, int>
        {
            ["Tables"]     = report.Tables.Count     * W("Tables",     1),
            ["Groups"]     = report.Groups.Count     * W("Groups",     2),
            ["Formulas"]   = report.Formulas.Count   * W("Formulas",   2),
            ["Subreports"] = report.Subreports.Count * W("Subreports", 3),
            ["Parameters"] = report.Parameters.Count * W("Parameters", 1),
            ["Sections"]   = report.Sections.Count   * W("Sections",   1),
        };

        int score = breakdown.Values.Sum();

        return new ComplexityResult
        {
            Score     = score,
            Level     = GetLevel(score, config),
            Breakdown = breakdown,
        };
    }

    private static ComplexityLevel GetLevel(int score, ScoringConfig config)
    {
        int T(string key, int @default) =>
            config.Thresholds.TryGetValue(key, out int v) ? v : @default;

        if (score <= T("Simple",  10)) return ComplexityLevel.Simple;
        if (score <= T("Medium",  20)) return ComplexityLevel.Medium;
        if (score <= T("Complex", 40)) return ComplexityLevel.Complex;
        return ComplexityLevel.VeryComplex;
    }
}
