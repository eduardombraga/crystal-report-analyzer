namespace CrystalReportAnalyzer.Core.Configuration;

public class ScoringConfig
{
    public Dictionary<string, int> Weights    { get; set; } = new();
    public Dictionary<string, int> Thresholds { get; set; } = new();

    public static ScoringConfig Default() => new()
    {
        Weights = new()
        {
            ["Tables"]     = 1,
            ["Parameters"] = 1,
            ["Sections"]   = 1,
            ["Formulas"]   = 2,
            ["Groups"]     = 2,
            ["Subreports"] = 3,
        },
        Thresholds = new()
        {
            ["Simple"]  = 10,
            ["Medium"]  = 20,
            ["Complex"] = 40,
        },
    };
}
