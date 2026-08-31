using System.Text;
using CrystalReportAnalyzer.Core.Models;
using CrystalReportAnalyzer.Core.Services.RazorGeneration.Helpers;

namespace CrystalReportAnalyzer.Core.Services.RazorGeneration.Templates;

public static class IndexViewGenerator
{
    public static string Generate(ReportModel report, RazorGenerationConfig config)
    {
        string reportCamel  = RazorTypeMapper.ToCamelCase(report.Name);
        string reportPascal = RazorTypeMapper.ToPascalCase(report.Name);

        var sb = new StringBuilder();

        // ── Layout ──
        sb.AppendLine($"@{{ Layout = \"{config.LayoutPath}\"; }}");
        sb.AppendLine();

        // ── Main container ──
        sb.AppendLine("<div id=\"mainContainer\" style=\"max-width: 1280px; margin: 0 auto;\">");

        // ── Filter card ──
        sb.AppendLine("    <div class=\"ui-card\" style=\"margin: 16px;\">");
        sb.AppendLine($"        <div class=\"ui-card-title\">{report.Name} - Filtros</div>");
        sb.AppendLine("        <div class=\"ui-card-content\">");
        sb.AppendLine($"            <form id=\"formPesquisa\" onsubmit=\"{reportCamel}.buscar(); return false;\">");

        // ── Parameter inputs ──
        if (report.Parameters.Count > 0)
        {
            sb.AppendLine("                <div class=\"ui-flex-container break-on-s600\">");

            foreach (var param in report.Parameters)
            {
                string fieldId   = RazorTypeMapper.ToCamelCase(param.Name);
                string label     = !string.IsNullOrEmpty(param.PromptText)
                                   ? param.PromptText
                                   : HumanizeParamName(param.Name);
                string inputType = RazorTypeMapper.InputType(param.ValueType);
                string? dataType = RazorTypeMapper.DataType(param.ValueType);

                string sizeClass = GetSizeClass(param.ValueType);
                string required  = param.IsRequired ? " required" : "";
                string dataAttr  = dataType != null ? $" data-type=\"{dataType}\"" : "";

                sb.AppendLine($"                    <div class=\"ui-input-container {sizeClass}\">");
                sb.AppendLine($"                        <label>{label}</label>");

                if (param.ValueType == "BooleanType")
                {
                    sb.AppendLine($"                        <div class=\"ui-option-container\">");
                    sb.AppendLine($"                            <label class=\"ui-option\"><input type=\"checkbox\" name=\"{fieldId}\"> Sim</label>");
                    sb.AppendLine($"                        </div>");
                }
                else
                {
                    sb.AppendLine($"                        <input type=\"{inputType}\" name=\"{fieldId}\" class=\"ui-control\"{dataAttr}{required} />");
                }

                sb.AppendLine("                    </div>");
            }

            sb.AppendLine("                </div>");
        }
        else
        {
            sb.AppendLine("                <!-- Relatório sem parâmetros de filtro -->");
        }

        // ── Buttons ──
        sb.AppendLine("                <div class=\"ui-button-container align-right\">");
        sb.AppendLine("                    <button type=\"reset\" class=\"ui-button raised\">Limpar</button>");
        sb.AppendLine("                    <button type=\"submit\" class=\"ui-button raised primary\">Pesquisar</button>");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </form>");
        sb.AppendLine("        </div>");

        // ── Grid container ──
        sb.AppendLine("        <div id=\"divGrid\" style=\"display: none;\"></div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        sb.AppendLine();

        // ── Scripts ──
        sb.AppendLine($"<script src=\"~/pages/{reportCamel}/{reportCamel}.js?d=@DateTime.Now.Ticks\"></script>");
        sb.AppendLine("<script>");
        sb.AppendLine($"    {reportCamel}.init({{");
        sb.AppendLine("        urls: {");
        sb.AppendLine("            buscar: '@Url.Action(\"Buscar\")'");
        sb.AppendLine("        }");
        sb.AppendLine("    });");
        sb.AppendLine("</script>");

        return sb.ToString().TrimEnd();
    }

    private static string GetSizeClass(string crType) => crType switch
    {
        "DateType"     => "size-3",
        "DateTimeType" => "size-3",
        "NumberType"   => "size-3",
        "CurrencyType" => "size-3",
        "Int16sType"   => "size-2",
        "Int32sType"   => "size-2",
        "Int64sType"   => "size-2",
        "BooleanType"  => "size-2",
        _              => "size-4",
    };

    private static string HumanizeParamName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.TrimStart('?', '@', '{', '}').Replace('_', ' ').Trim();
        if (name.Length > 0)
            name = char.ToUpper(name[0]) + name.Substring(1);
        return name;
    }
}
