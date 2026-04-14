using System.IO;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Exporters;

public static class CSharpExporter
{
    public static void ExportToFile(ReportDependencies deps, string filePath) =>
        File.WriteAllText(filePath, deps.AllGeneratedCode, System.Text.Encoding.UTF8);
}
