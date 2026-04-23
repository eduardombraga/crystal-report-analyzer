using System.IO;
using CrystalReportAnalyzer.Core.Models;
using CrystalReportAnalyzer.Core.Services.RazorGeneration.Helpers;
using CrystalReportAnalyzer.Core.Services.RazorGeneration.Templates;

namespace CrystalReportAnalyzer.Core.Services.RazorGeneration;

public class RazorGenerationResult
{
    public List<string> GeneratedFiles { get; set; } = new();
}

public class RazorGenerationService
{
    public RazorGenerationResult Generate(ReportModel report, RazorGenerationConfig config)
    {
        var result = new RazorGenerationResult();
        string reportName = RazorTypeMapper.ToPascalCase(report.Name);

        Directory.CreateDirectory(config.OutputDirectory);

        // Index.cshtml — filter form
        string indexContent = IndexViewGenerator.Generate(report, config);
        string indexPath    = Path.Combine(config.OutputDirectory, $"{reportName}_Index.cshtml");
        File.WriteAllText(indexPath, indexContent, System.Text.Encoding.UTF8);
        result.GeneratedFiles.Add(indexPath);

        // _Grid.cshtml — data table
        string gridContent = GridViewGenerator.Generate(report, config);
        string gridPath    = Path.Combine(config.OutputDirectory, $"{reportName}_Grid.cshtml");
        File.WriteAllText(gridPath, gridContent, System.Text.Encoding.UTF8);
        result.GeneratedFiles.Add(gridPath);

        // Report.cshtml — print version with grouping
        string reportContent = ReportViewGenerator.Generate(report, config);
        string reportPath    = Path.Combine(config.OutputDirectory, $"{reportName}_Report.cshtml");
        File.WriteAllText(reportPath, reportContent, System.Text.Encoding.UTF8);
        result.GeneratedFiles.Add(reportPath);

        // Controller
        string controllerContent = ControllerGenerator.Generate(report, config);
        string controllerPath    = Path.Combine(config.OutputDirectory, $"{reportName}Controller.cs");
        File.WriteAllText(controllerPath, controllerContent, System.Text.Encoding.UTF8);
        result.GeneratedFiles.Add(controllerPath);

        // ViewModels
        string viewModelContent = ViewModelGenerator.Generate(report, config);
        string viewModelPath    = Path.Combine(config.OutputDirectory, $"{reportName}ViewModel.cs");
        File.WriteAllText(viewModelPath, viewModelContent, System.Text.Encoding.UTF8);
        result.GeneratedFiles.Add(viewModelPath);

        // JavaScript
        string jsContent = JavaScriptGenerator.Generate(report, config);
        string camelName = RazorTypeMapper.ToCamelCase(report.Name);
        string jsPath    = Path.Combine(config.OutputDirectory, $"{camelName}.js");
        File.WriteAllText(jsPath, jsContent, System.Text.Encoding.UTF8);
        result.GeneratedFiles.Add(jsPath);

        // Subreport partials
        if (config.IncludeSubreports && report.Subreports.Count > 0)
        {
            var partials = SubreportPartialGenerator.Generate(report, config);
            foreach (var (fileName, content) in partials)
            {
                string partialPath = Path.Combine(config.OutputDirectory, fileName);
                File.WriteAllText(partialPath, content, System.Text.Encoding.UTF8);
                result.GeneratedFiles.Add(partialPath);
            }
        }

        return result;
    }
}
