namespace CrystalReportAnalyzer.Core.Models;

public class TableModel
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<FieldModel> Fields { get; set; } = new();
}
