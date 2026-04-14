using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Core.Analyzer;

/// <summary>
/// Scoring rules:
///   +1 per table      +2 per group      +2 per formula
///   +3 per subreport  +1 per parameter  +1 per section
///
/// Classification:
///   0-10  = Simple   |  11-20 = Medium
///   21-40 = Complex  |  40+   = VeryComplex
/// </summary>
public static class ComplexityAnalyzer
{
    public static ComplexityResult Analyze(ReportModel report)
    {
        var breakdown = new Dictionary<string, int>
        {
            ["Tables"]     = report.Tables.Count     * 1,
            ["Groups"]     = report.Groups.Count     * 2,
            ["Formulas"]   = report.Formulas.Count   * 2,
            ["Subreports"] = report.Subreports.Count * 3,
            ["Parameters"] = report.Parameters.Count * 1,
            ["Sections"]   = report.Sections.Count   * 1,
        };

        int score = breakdown.Values.Sum();

        return new ComplexityResult
        {
            Score     = score,
            Level     = GetLevel(score),
            Breakdown = breakdown,
        };
    }

    private static ComplexityLevel GetLevel(int score) => score switch
    {
        <= 10 => ComplexityLevel.Simple,
        <= 20 => ComplexityLevel.Medium,
        <= 40 => ComplexityLevel.Complex,
        _     => ComplexityLevel.VeryComplex,
    };
}
