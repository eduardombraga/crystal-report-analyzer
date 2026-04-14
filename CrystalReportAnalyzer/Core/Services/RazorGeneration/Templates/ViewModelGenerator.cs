using System.Text;
using CrystalReportAnalyzer.Core.Models;
using CrystalReportAnalyzer.Core.Services.RazorGeneration.Helpers;

namespace CrystalReportAnalyzer.Core.Services.RazorGeneration.Templates;

public static class ViewModelGenerator
{
    /// <summary>Generates FilterViewModel + ReportViewModel + main ViewModel (DTO).</summary>
    public static string Generate(ReportModel report, RazorGenerationConfig config)
    {
        string pascal = RazorTypeMapper.ToPascalCase(report.Name);
        string ns     = config.BaseNamespace;

        var sb = new StringBuilder();
        sb.AppendLine($"// ViewModels gerados a partir do relatório: {report.Name}");
        sb.AppendLine($"// Gerado em: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns}.ViewModels;");
        sb.AppendLine();

        // ── Main ViewModel (DTO) ──
        GenerateMainViewModel(sb, report, pascal);

        // ── FilterViewModel ──
        if (report.Parameters.Count > 0)
            GenerateFilterViewModel(sb, report, pascal);

        // ── ReportViewModel ──
        GenerateReportViewModel(sb, report, pascal);

        return sb.ToString().TrimEnd();
    }

    private static void GenerateMainViewModel(StringBuilder sb, ReportModel report, string pascal)
    {
        var fields = report.Tables.Count > 0
                     ? report.Tables[0].Fields
                     : report.Fields;

        sb.AppendLine($"public class {pascal}ViewModel");
        sb.AppendLine("{");
        foreach (var field in fields)
        {
            string propType = MapToCSharpType(field.ValueType);
            string propName = RazorTypeMapper.ToPascalCase(field.Name);
            sb.AppendLine($"    public {propType} {propName} {{ get; set; }}");
        }
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateFilterViewModel(StringBuilder sb, ReportModel report, string pascal)
    {
        sb.AppendLine($"public class {pascal}FilterViewModel");
        sb.AppendLine("{");
        foreach (var param in report.Parameters)
        {
            string propType = MapToCSharpType(param.ValueType);
            string propName = RazorTypeMapper.ToPascalCase(param.Name);
            string nullable = param.IsRequired ? "" : "?";
            string label    = !string.IsNullOrEmpty(param.PromptText)
                              ? param.PromptText
                              : propName;

            sb.AppendLine($"    [Display(Name = \"{EscapeString(label)}\")]");
            if (param.IsRequired)
                sb.AppendLine($"    [Required(ErrorMessage = \"{EscapeString(label)} é obrigatório.\")]");
            sb.AppendLine($"    public {propType}{nullable} {propName} {{ get; set; }}");
            sb.AppendLine();
        }
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateReportViewModel(StringBuilder sb, ReportModel report, string pascal)
    {
        sb.AppendLine($"public class {pascal}ReportViewModel");
        sb.AppendLine("{");
        sb.AppendLine($"    public IEnumerable<{pascal}ViewModel> Items {{ get; set; }} = Enumerable.Empty<{pascal}ViewModel>();");
        if (report.Parameters.Count > 0)
            sb.AppendLine($"    public {pascal}FilterViewModel? Filtro {{ get; set; }}");
        sb.AppendLine("}");
    }

    private static string MapToCSharpType(string crType) => crType switch
    {
        "StringType"   => "string",
        "NumberType"   => "decimal",
        "CurrencyType" => "decimal",
        "DateType"     => "DateTime",
        "DateTimeType" => "DateTime",
        "TimeType"     => "TimeSpan",
        "BooleanType"  => "bool",
        "Int16sType"   => "short",
        "Int32sType"   => "int",
        "Int64sType"   => "long",
        "ByteType"     => "byte",
        "BlobType"     => "byte[]",
        _              => "object",
    };

    private static string EscapeString(string text) =>
        text.Replace("\"", "\\\"");
}
