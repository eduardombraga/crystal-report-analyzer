namespace CrystalReportAnalyzer.Core.Models;

public class SectionModel
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public int ObjectCount { get; set; }
    public bool IsVisible { get; set; } = true;
}
