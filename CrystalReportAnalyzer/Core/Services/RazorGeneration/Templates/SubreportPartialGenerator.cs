using System.Text;
using CrystalReportAnalyzer.Core.Models;
using CrystalReportAnalyzer.Core.Services.RazorGeneration.Helpers;

namespace CrystalReportAnalyzer.Core.Services.RazorGeneration.Templates;

public static class SubreportPartialGenerator
{
    /// <summary>Generates a placeholder partial view for each subreport.</summary>
    public static List<(string FileName, string Content)> Generate(ReportModel report, RazorGenerationConfig config)
    {
        var results = new List<(string, string)>();

        foreach (var sub in report.Subreports)
        {
            string subPascal = RazorTypeMapper.ToPascalCase(sub.Name);
            string fileName  = $"_Sub{subPascal}.cshtml";

            var sb = new StringBuilder();
            sb.AppendLine($"@* Sub-relatório: {sub.Name} *@");
            sb.AppendLine($"@* Localização original: {sub.Location} *@");
            if (sub.IsOnDemand)
                sb.AppendLine("@* Tipo: on-demand (carregamento sob demanda) *@");
            sb.AppendLine();
            sb.AppendLine("@* TODO: Definir @model e implementar o conteúdo deste sub-relatório *@");
            sb.AppendLine();

            sb.AppendLine("<div class=\"ui-card\" style=\"margin: 8px 0;\">");
            sb.AppendLine($"    <div class=\"ui-card-title\">{sub.Name}</div>");
            sb.AppendLine("    <div class=\"ui-card-content\">");
            sb.AppendLine($"        <p style=\"color: #999; font-style: italic;\">Sub-relatório '{sub.Name}' — implementação pendente.</p>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</div>");

            results.Add((fileName, sb.ToString().TrimEnd()));
        }

        return results;
    }
}
