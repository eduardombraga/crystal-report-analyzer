namespace CrystalReportAnalyzer.Core.Models;

public class ReportDependencies
{
    public List<DbObjectModel> DbObjects { get; set; } = new();
    public string GeneratedDtos { get; set; } = string.Empty;
    public string GeneratedServiceInterface { get; set; } = string.Empty;
    public string GeneratedParameterClass { get; set; } = string.Empty;
    public string GeneratedBusinessRules { get; set; } = string.Empty;

    /// <summary>All generated code blocks concatenated for display/export.</summary>
    public string AllGeneratedCode =>
        string.Join(Environment.NewLine + Environment.NewLine,
            new[] { GeneratedParameterClass, GeneratedDtos, GeneratedServiceInterface, GeneratedBusinessRules }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
}
