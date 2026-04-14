using System.Text;
using CrystalReportAnalyzer.Core.Models;
using CrystalReportAnalyzer.Core.Services.RazorGeneration.Helpers;

namespace CrystalReportAnalyzer.Core.Services.RazorGeneration.Templates;

public static class ControllerGenerator
{
    public static string Generate(ReportModel report, RazorGenerationConfig config)
    {
        string pascal = RazorTypeMapper.ToPascalCase(report.Name);
        string camel  = RazorTypeMapper.ToCamelCase(report.Name);
        string kebab  = ToKebabCase(report.Name);
        string ns     = config.BaseNamespace;

        var sb = new StringBuilder();

        sb.AppendLine($"using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine($"using {ns}.ViewModels;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns}.Controllers;");
        sb.AppendLine();
        sb.AppendLine($"[Route(\"{kebab}\")]");
        sb.AppendLine($"public class {pascal}Controller : BaseController");
        sb.AppendLine("{");

        // Repository field + constructor
        sb.AppendLine($"    private readonly I{pascal}Repository _{camel}Repository;");
        sb.AppendLine();
        sb.AppendLine($"    public {pascal}Controller(I{pascal}Repository {camel}Repository)");
        sb.AppendLine("    {");
        sb.AppendLine($"        _{camel}Repository = {camel}Repository;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Index
        sb.AppendLine("    [HttpGet]");
        sb.AppendLine("    public IActionResult Index() => View();");
        sb.AppendLine();

        // Buscar
        string filterParam = report.Parameters.Count > 0
            ? $"{pascal}FilterViewModel filtro"
            : "";
        string filterArg = report.Parameters.Count > 0 ? "filtro" : "";

        sb.AppendLine("    [HttpGet(\"buscar\")]");
        sb.AppendLine($"    public async Task<IActionResult> Buscar({filterParam})");
        sb.AppendLine("    {");
        sb.AppendLine($"        var dados = await _{camel}Repository.BuscarAsync({filterArg});");
        sb.AppendLine("        return View(\"_Grid\", dados);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Report
        sb.AppendLine("    [HttpGet(\"report\")]");
        sb.AppendLine($"    public async Task<IActionResult> Report({filterParam})");
        sb.AppendLine("    {");
        sb.AppendLine($"        var dados = await _{camel}Repository.BuscarAsync({filterArg});");
        sb.Append(    $"        return View(new {pascal}ReportViewModel {{ Items = dados");
        if (report.Parameters.Count > 0)
            sb.Append($", Filtro = filtro");
        sb.AppendLine(" });");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString().TrimEnd();
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return "unknown";

        var sb = new StringBuilder();
        foreach (char c in name)
        {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append('-');
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLower(c));
        }
        return sb.ToString();
    }
}
