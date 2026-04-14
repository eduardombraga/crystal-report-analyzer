namespace CrystalReportAnalyzer.Core.Models;

public enum ComplexityLevel
{
    Simple,
    Medium,
    Complex,
    VeryComplex
}

public class ComplexityResult
{
    public int Score { get; set; }
    public ComplexityLevel Level { get; set; }
    public Dictionary<string, int> Breakdown { get; set; } = new();

    public string LevelDescription => Level switch
    {
        ComplexityLevel.Simple     => "Simple",
        ComplexityLevel.Medium     => "Medium",
        ComplexityLevel.Complex    => "Complex",
        ComplexityLevel.VeryComplex => "Very Complex",
        _ => "Unknown"
    };
}
