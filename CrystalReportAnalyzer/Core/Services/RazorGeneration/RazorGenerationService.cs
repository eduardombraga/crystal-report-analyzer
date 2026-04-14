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

        return result;
    }
}
