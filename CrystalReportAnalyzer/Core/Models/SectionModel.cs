namespace CrystalReportAnalyzer.Core.Models;

public class SectionModel
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public int ObjectCount { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool SuppressIfBlank { get; set; }
    public bool NewPageBefore { get; set; }
    public bool NewPageAfter { get; set; }
    public bool ResetPageNumberAfter { get; set; }
    public bool KeepTogether { get; set; }
    public bool UnderlayFollowingSections { get; set; }
    public string SuppressFormulaText { get; set; } = string.Empty;

    /// <summary>
    /// Objects placed in this section, in reading order (sorted by Top, then Left).
    /// </summary>
    public List<ReportObjectModel> Objects { get; set; } = new();
}
