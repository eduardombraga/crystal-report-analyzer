using System.Text;
using CrystalReportAnalyzer.Core.Models;
using CrystalReportAnalyzer.Core.Services.RazorGeneration.Helpers;

namespace CrystalReportAnalyzer.Core.Services.RazorGeneration.Templates;

public static class ReportViewGenerator
{
    public static string Generate(ReportModel report, RazorGenerationConfig config)
    {
        string pascal = RazorTypeMapper.ToPascalCase(report.Name);
        string ns     = config.BaseNamespace;

        var fields = report.Tables.Count > 0
                     ? report.Tables[0].Fields
                     : report.Fields;

        var sb = new StringBuilder();

        // ── Header ──
        sb.AppendLine($"@using {ns}.ViewModels");
        sb.AppendLine($"@model {pascal}ReportViewModel");
        sb.AppendLine($"@{{ Layout = \"{config.ReportLayoutPath}\"; }}");
        sb.AppendLine();

        // ── Inline styles ──
        sb.AppendLine("<style>");
        sb.AppendLine($"    .relatorio-{RazorTypeMapper.ToCamelCase(report.Name)} {{ border-spacing: 0; width: 100%; }}");
        sb.AppendLine($"    .relatorio-{RazorTypeMapper.ToCamelCase(report.Name)} th,");
        sb.AppendLine($"    .relatorio-{RazorTypeMapper.ToCamelCase(report.Name)} td {{ padding: 4px 8px; border: 1px solid #ddd; font-size: 12px; }}");
        sb.AppendLine($"    .relatorio-{RazorTypeMapper.ToCamelCase(report.Name)} thead th {{ background-color: #f5f5f5; font-weight: bold; }}");
        sb.AppendLine("    .group-header {{ background-color: #e8e8e8; font-weight: bold; }}");
        sb.AppendLine("    .right {{ text-align: right; }}");
        sb.AppendLine("    .center {{ text-align: center; }}");
        sb.AppendLine("</style>");
        sb.AppendLine();

        if (report.Groups.Count > 0)
            GenerateGroupedReport(sb, report, fields, pascal);
        else
            GenerateSimpleReport(sb, report, fields, pascal);

        // Subreport references
        if (config.IncludeSubreports && report.Subreports.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("@* ── Sub-relatórios ── *@");
            foreach (var sub in report.Subreports)
            {
                string subPascal = RazorTypeMapper.ToPascalCase(sub.Name);
                sb.AppendLine($"@await Html.PartialAsync(\"_Sub{subPascal}\", Model)");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static void GenerateSimpleReport(StringBuilder sb, ReportModel report, List<FieldModel> fields, string pascal)
    {
        string cssClass = $"relatorio-{RazorTypeMapper.ToCamelCase(report.Name)}";

        sb.AppendLine($"<h2>{report.Name}</h2>");
        sb.AppendLine();

        // Filters summary
        AppendFilterSummary(sb, report);

        sb.AppendLine($"<table class=\"{cssClass}\">");

        // Header
        sb.AppendLine("    <thead>");
        sb.AppendLine("        <tr>");
        foreach (var field in fields)
        {
            string align = RazorTypeMapper.Alignment(field.ValueType);
            string cls   = string.IsNullOrEmpty(align) ? "" : $" class=\"{align}\"";
            sb.AppendLine($"            <th{cls}>{HumanizeFieldName(field.Name)}</th>");
        }
        sb.AppendLine("        </tr>");
        sb.AppendLine("    </thead>");

        // Body
        sb.AppendLine("    <tbody>");
        sb.AppendLine("        @foreach (var item in Model.Items)");
        sb.AppendLine("        {");
        sb.AppendLine("            <tr>");
        foreach (var field in fields)
        {
            string propName = RazorTypeMapper.ToPascalCase(field.Name);
            string align    = RazorTypeMapper.Alignment(field.ValueType);
            string cls      = string.IsNullOrEmpty(align) ? "" : $" class=\"{align}\"";
            string format   = RazorTypeMapper.DisplayFormat(field.ValueType);
            string display  = FormatDisplay(propName, field.ValueType, format);

            sb.AppendLine($"                <td{cls}>{display}</td>");
        }
        sb.AppendLine("            </tr>");
        sb.AppendLine("        }");
        sb.AppendLine("    </tbody>");
        sb.AppendLine("</table>");
    }

    private static void GenerateGroupedReport(StringBuilder sb, ReportModel report, List<FieldModel> fields, string pascal)
    {
        string cssClass = $"relatorio-{RazorTypeMapper.ToCamelCase(report.Name)}";

        sb.AppendLine($"<h2>{report.Name}</h2>");
        sb.AppendLine();

        // Filters summary
        AppendFilterSummary(sb, report);

        // Determine group field(s)
        var groups = report.Groups;
        string firstGroupField = RazorTypeMapper.ToPascalCase(
            groups[0].ConditionField.Contains('.')
                ? groups[0].ConditionField.Split('.').Last()
                : groups[0].ConditionField);

        sb.AppendLine("@{");
        sb.AppendLine($"    var grouped = Model.Items.GroupBy(x => x.{firstGroupField});");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("@foreach (var group in grouped)");
        sb.AppendLine("{");

        // Page break between groups (not on first)
        sb.AppendLine("    @if (group != grouped.First())");
        sb.AppendLine("    {");
        sb.AppendLine("        <div style=\"page-break-before: always;\"></div>");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Group header
        sb.AppendLine($"    <h3>@group.Key</h3>");
        sb.AppendLine();

        sb.AppendLine($"    <table class=\"{cssClass}\">");
        sb.AppendLine("        <thead>");
        sb.AppendLine("            <tr>");
        foreach (var field in fields)
        {
            string align = RazorTypeMapper.Alignment(field.ValueType);
            string cls   = string.IsNullOrEmpty(align) ? "" : $" class=\"{align}\"";
            sb.AppendLine($"                <th{cls}>{HumanizeFieldName(field.Name)}</th>");
        }
        sb.AppendLine("            </tr>");
        sb.AppendLine("        </thead>");

        sb.AppendLine("        <tbody>");
        sb.AppendLine("            @foreach (var item in group)");
        sb.AppendLine("            {");
        sb.AppendLine("                <tr>");
        foreach (var field in fields)
        {
            string propName = RazorTypeMapper.ToPascalCase(field.Name);
            string align    = RazorTypeMapper.Alignment(field.ValueType);
            string cls      = string.IsNullOrEmpty(align) ? "" : $" class=\"{align}\"";
            string format   = RazorTypeMapper.DisplayFormat(field.ValueType);
            string display  = FormatDisplay(propName, field.ValueType, format);

            sb.AppendLine($"                    <td{cls}>{display}</td>");
        }
        sb.AppendLine("                </tr>");
        sb.AppendLine("            }");
        sb.AppendLine("        </tbody>");
        sb.AppendLine("    </table>");

        // Group footer with count
        sb.AppendLine("    <p style=\"font-size: 11px; color: #888;\">Total do grupo: @group.Count() registro(s)</p>");

        sb.AppendLine("}");
        sb.AppendLine();

        // Grand total
        sb.AppendLine("<p><strong>Total geral: @Model.Items.Count() registro(s)</strong></p>");
    }

    private static void AppendFilterSummary(StringBuilder sb, ReportModel report)
    {
        if (report.Parameters.Count == 0) return;

        sb.AppendLine("@if (Model.Filtro != null)");
        sb.AppendLine("{");
        sb.AppendLine("    <p style=\"font-size: 11px; color: #666;\">");
        sb.AppendLine("        Filtros:");

        foreach (var param in report.Parameters)
        {
            string propName = RazorTypeMapper.ToPascalCase(param.Name);
            string label    = !string.IsNullOrEmpty(param.PromptText)
                              ? param.PromptText
                              : propName;
            sb.AppendLine($"        <strong>{label}:</strong> @Model.Filtro.{propName}");
        }

        sb.AppendLine("    </p>");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static string FormatDisplay(string propName, string valueType, string format)
    {
        if (valueType == "BooleanType")
            return $"@(item.{propName}{format})";
        if (!string.IsNullOrEmpty(format))
            return $"@item.{propName}{format}";
        return $"@item.{propName}";
    }

    private static string HumanizeFieldName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Replace('_', ' ').Replace('.', ' ').Trim();
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && char.IsLower(name[i - 1]))
                sb.Append(' ');
            sb.Append(name[i]);
        }
        string result = sb.ToString();
        if (result.Length > 0)
            result = char.ToUpper(result[0]) + result.Substring(1).ToLower();
        return result;
    }
}
