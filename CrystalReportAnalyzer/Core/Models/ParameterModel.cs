namespace CrystalReportAnalyzer.Core.Models;

public class ParameterModel
{
    public string Name { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}
