using System.Collections.Generic;

namespace CrystalReportAnalyzer.Core.Models;

public class FormulaModel
{
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public List<string> ReferencedDbFields      { get; set; } = new();
    public List<string> ReferencedParameters    { get; set; } = new();
    public List<string> ReferencedFormulas      { get; set; } = new();
    public List<string> ReferencedRunningTotals { get; set; } = new();
}
