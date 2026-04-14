namespace CrystalReportAnalyzer.Core.Services.RazorGeneration;

public class RazorGenerationConfig
{
    /// <summary>Base namespace for generated C# files (Controller, ViewModel).</summary>
    public string BaseNamespace { get; set; } = "Generated";

    /// <summary>Layout path used in Index.cshtml.</summary>
    public string LayoutPath { get; set; } = "~/Views/Shared/_LayoutNew.cshtml";

    /// <summary>Layout path used in Report.cshtml (print version).</summary>
    public string ReportLayoutPath { get; set; } = "Reports/_Layout";

    /// <summary>Output directory where files are written.</summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>Generate partial views for subreports.</summary>
    public bool IncludeSubreports { get; set; } = true;
}
