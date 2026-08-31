using System.Text;
using CrystalReportAnalyzer.Core.Models;
using CrystalReportAnalyzer.Core.Services.RazorGeneration.Helpers;

namespace CrystalReportAnalyzer.Core.Services.RazorGeneration.Templates;

public static class GridViewGenerator
{
    public static string Generate(ReportModel report, RazorGenerationConfig config)
    {
        var fields = report.Tables.Count > 0
                     ? report.Tables[0].Fields
                     : report.Fields;

        if (fields.Count == 0)
            return "<!-- Nenhum campo encontrado para gerar a grid -->";

        string viewModelName = RazorTypeMapper.ToPascalCase(report.Name) + "ViewModel";
        var sb = new StringBuilder();

        sb.AppendLine($"@model IEnumerable<{viewModelName}>");
        sb.AppendLine();
        sb.AppendLine("<div class=\"ui-data-table\">");
        sb.AppendLine("    <table>");

        // ── thead ──
        sb.AppendLine("        <thead>");
        sb.AppendLine("            <tr>");
        foreach (var field in fields)
        {
            string align = RazorTypeMapper.Alignment(field.ValueType);
            string cls   = string.IsNullOrEmpty(align) ? "" : $" class=\"{align}\"";
            string label = HumanizeFieldName(field.Name);
            sb.AppendLine($"                <th{cls}>{label}</th>");
        }
        sb.AppendLine("            </tr>");
        sb.AppendLine("        </thead>");

        // ── tbody ──
        sb.AppendLine("        <tbody>");
        sb.AppendLine("            @if (Model != null && Model.Any())");
        sb.AppendLine("            {");
        sb.AppendLine("                foreach (var item in Model)");
        sb.AppendLine("                {");
        sb.AppendLine("                    <tr>");

        foreach (var field in fields)
        {
            string propName = RazorTypeMapper.ToPascalCase(field.Name);
            string align    = RazorTypeMapper.Alignment(field.ValueType);
            string cls      = string.IsNullOrEmpty(align) ? "" : $" class=\"{align}\"";
            string format   = RazorTypeMapper.DisplayFormat(field.ValueType);

            string display;
            if (field.ValueType == "BooleanType")
                display = $"@(item.{propName}{format})";
            else if (!string.IsNullOrEmpty(format))
                display = $"@item.{propName}{format}";
            else
                display = $"@item.{propName}";

            sb.AppendLine($"                        <td{cls}>{display}</td>");
        }

        sb.AppendLine("                    </tr>");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine($"                <tr><td colspan=\"{fields.Count}\" class=\"center\">Nenhum registro encontrado...</td></tr>");
        sb.AppendLine("            }");
        sb.AppendLine("        </tbody>");
        sb.AppendLine("    </table>");
        sb.AppendLine("</div>");

        return sb.ToString().TrimEnd();
    }

    /// <summary>Turns "CUSTOMER_NAME" or "CustomerName" into "Customer Name".</summary>
    private static string HumanizeFieldName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // Replace underscores and dots with spaces
        name = name.Replace('_', ' ').Replace('.', ' ').Trim();

        // Insert space before uppercase letters in PascalCase
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && char.IsLower(name[i - 1]))
                sb.Append(' ');
            sb.Append(name[i]);
        }

        // Title case: first letter of each word capitalized
        string result = sb.ToString();
        if (result.Length > 0)
            result = char.ToUpper(result[0]) + result.Substring(1).ToLower();

        return result;
    }
}
