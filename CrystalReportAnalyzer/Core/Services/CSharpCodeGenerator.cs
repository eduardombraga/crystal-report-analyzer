using System.Text;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Core.Services;

/// <summary>
/// Generates C# code stubs from a ReportModel to assist migration to HTML/Razor.
/// </summary>
public static class CSharpCodeGenerator
{
    // ── Public entry points ──────────────────────────────────────────

    public static string GenerateDtos(ReportModel report)
    {
        if (report.Tables.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"// DTOs gerados a partir do relatório: {report.Name}");
        sb.AppendLine($"// Gerado em: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("namespace GeneratedFrom" + ToIdentifier(report.Name) + ";");
        sb.AppendLine();

        foreach (var table in report.Tables)
        {
            string className = ToIdentifier(table.Name) + "Dto";
            sb.AppendLine($"/// <summary>DTO para a tabela/view '{table.Name}'</summary>");
            sb.AppendLine($"public class {className}");
            sb.AppendLine("{");
            foreach (var field in table.Fields)
            {
                string propType = MapToCSharpType(field.ValueType);
                string propName = ToIdentifier(field.Name);
                sb.AppendLine($"    public {propType} {propName} {{ get; set; }}");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public static string GenerateParameterClass(ReportModel report)
    {
        if (report.Parameters.Count == 0) return string.Empty;

        string className = ToIdentifier(report.Name) + "Parameters";
        var sb = new StringBuilder();
        sb.AppendLine($"// Classe de parâmetros para o relatório: {report.Name}");
        sb.AppendLine();
        sb.AppendLine("namespace GeneratedFrom" + ToIdentifier(report.Name) + ";");
        sb.AppendLine();
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");

        foreach (var param in report.Parameters)
        {
            string propType = MapToCSharpType(param.ValueType);
            string propName = ToIdentifier(param.Name);

            if (!string.IsNullOrEmpty(param.PromptText))
                sb.AppendLine($"    /// <summary>{EscapeXml(param.PromptText)}</summary>");

            string nullable = param.IsRequired ? "" : "?";
            sb.AppendLine($"    public {propType}{nullable} {propName} {{ get; set; }}");
        }

        sb.AppendLine("}");
        return sb.ToString().TrimEnd();
    }

    public static string GenerateServiceInterface(ReportModel report)
    {
        string ifaceName   = "I" + ToIdentifier(report.Name) + "Service";
        string paramsClass = ToIdentifier(report.Name) + "Parameters";
        string mainDto     = report.Tables.Count > 0
                             ? ToIdentifier(report.Tables[0].Name) + "Dto"
                             : "object";

        var sb = new StringBuilder();
        sb.AppendLine($"// Interface de serviço para o relatório: {report.Name}");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("namespace GeneratedFrom" + ToIdentifier(report.Name) + ";");
        sb.AppendLine();
        sb.AppendLine($"public interface {ifaceName}");
        sb.AppendLine("{");

        string paramArg = report.Parameters.Count > 0 ? $"{paramsClass} parameters" : string.Empty;
        sb.AppendLine($"    /// <summary>Retorna os dados do relatório {report.Name}.</summary>");
        sb.AppendLine($"    IEnumerable<{mainDto}> Get{ToIdentifier(report.Name)}({paramArg});");

        if (report.Subreports.Count > 0)
        {
            sb.AppendLine();
            foreach (var sub in report.Subreports)
            {
                string subName = ToIdentifier(sub.Name);
                sb.AppendLine($"    /// <summary>Dados do sub-relatório '{sub.Name}'</summary>");
                sb.AppendLine($"    IEnumerable<object> Get{subName}({paramArg});");
            }
        }

        sb.AppendLine("}");
        return sb.ToString().TrimEnd();
    }

    public static string GenerateBusinessRules(ReportModel report)
    {
        if (report.Formulas.Count == 0) return string.Empty;

        string className = ToIdentifier(report.Name) + "BusinessRules";
        var sb = new StringBuilder();
        sb.AppendLine($"// Regras de negócio extraídas das fórmulas do relatório: {report.Name}");
        sb.AppendLine("// TODO: Reimplementar cada método com a lógica equivalente em C#");
        sb.AppendLine();
        sb.AppendLine("namespace GeneratedFrom" + ToIdentifier(report.Name) + ";");
        sb.AppendLine();
        sb.AppendLine($"public static class {className}");
        sb.AppendLine("{");

        foreach (var formula in report.Formulas)
        {
            string methodName   = ToIdentifier(formula.Name.TrimStart('@'));
            string returnType   = MapToCSharpType(formula.ReturnType);

            sb.AppendLine($"    /// <summary>Fórmula Crystal Reports: {EscapeXml(formula.Name)}</summary>");

            if (!string.IsNullOrWhiteSpace(formula.Text))
            {
                sb.AppendLine("    /// <remarks>");
                sb.AppendLine("    /// <code>");
                foreach (var line in formula.Text.Split('\n'))
                    sb.AppendLine("    /// " + EscapeXml(line.TrimEnd('\r')));
                sb.AppendLine("    /// </code>");
                sb.AppendLine("    /// </remarks>");
            }

            sb.AppendLine($"    public static {returnType} {methodName}(/* adicionar parâmetros */)");
            sb.AppendLine("    {");
            sb.AppendLine("        throw new System.NotImplementedException();");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Remove last blank line before closing brace
        string result = sb.ToString().TrimEnd();
        return result + Environment.NewLine + "}";
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>Maps Crystal Reports ValueType string to a C# type name.</summary>
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

    /// <summary>Converts an arbitrary name into a valid PascalCase C# identifier.</summary>
    private static string ToIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";

        name = name.TrimStart('@').Trim();
        var sb = new StringBuilder();
        bool capitalizeNext = true;

        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(capitalizeNext ? char.ToUpper(c) : c);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        string result = sb.ToString();
        if (result.Length == 0) return "Unknown";
        if (char.IsDigit(result[0])) result = "_" + result;
        return result;
    }

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
