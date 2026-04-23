using System.Text;
using CrystalReportAnalyzer.Core.Models;
using CrystalReportAnalyzer.Core.Services.RazorGeneration.Helpers;

namespace CrystalReportAnalyzer.Core.Services.RazorGeneration.Templates;

public static class JavaScriptGenerator
{
    public static string Generate(ReportModel report, RazorGenerationConfig config)
    {
        string camel = RazorTypeMapper.ToCamelCase(report.Name);

        var sb = new StringBuilder();

        sb.AppendLine($"var {camel} = (function () {{");
        sb.AppendLine("    var _urls = {};");
        sb.AppendLine();

        // init
        sb.AppendLine("    function init(config) {");
        sb.AppendLine("        _urls = config.urls || {};");
        sb.AppendLine("    }");
        sb.AppendLine();

        // buscar
        sb.AppendLine("    function buscar() {");
        sb.AppendLine("        var form = document.getElementById('formPesquisa');");
        sb.AppendLine("        $.get(_urls.buscar, $(form).serialize(), function (html) {");
        sb.AppendLine("            $('#divGrid').html(html).show();");
        sb.AppendLine("        });");
        sb.AppendLine("    }");
        sb.AppendLine();

        // imprimir
        sb.AppendLine("    function imprimir() {");
        sb.AppendLine("        var form = document.getElementById('formPesquisa');");
        sb.AppendLine("        window.open(_urls.report + '?' + $(form).serialize());");
        sb.AppendLine("    }");
        sb.AppendLine();

        // return public API
        sb.AppendLine("    return {");
        sb.AppendLine("        init: init,");
        sb.AppendLine("        buscar: buscar,");
        sb.AppendLine("        imprimir: imprimir");
        sb.AppendLine("    };");
        sb.AppendLine("})();");

        return sb.ToString().TrimEnd();
    }
}
