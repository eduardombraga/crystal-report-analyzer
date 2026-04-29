namespace CrystalReportAnalyzer.Core.Models;

public class ReportModel
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public List<TableModel> Tables { get; set; } = new();
    public List<FieldModel> Fields { get; set; } = new();
    public List<ParameterModel> Parameters { get; set; } = new();
    public List<FormulaModel> Formulas { get; set; } = new();
    public List<GroupModel> Groups { get; set; } = new();
    public List<SectionModel> Sections { get; set; } = new();
    public List<SubreportModel> Subreports { get; set; } = new();
    // Page setup
    public string PaperOrientation { get; set; } = string.Empty;
    public string PaperSize { get; set; } = string.Empty;
    public double MarginTopMm { get; set; }
    public double MarginBottomMm { get; set; }
    public double MarginLeftMm { get; set; }
    public double MarginRightMm { get; set; }

    // Summary info
    public string ReportTitle { get; set; } = string.Empty;
    public string ReportAuthor { get; set; } = string.Empty;
    public string ReportSubject { get; set; } = string.Empty;
    public string ReportComments { get; set; } = string.Empty;

    // Fonts
    public List<string> FontsUsed { get; set; } = new();

    public ComplexityResult? Complexity { get; set; }
    public ReportDependencies? Dependencies { get; set; }
}
