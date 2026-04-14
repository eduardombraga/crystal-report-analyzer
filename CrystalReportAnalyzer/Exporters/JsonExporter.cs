using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Exporters;

public static class JsonExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters    = { new JsonStringEnumConverter() },
    };

    public static string Export(ReportModel report) =>
        JsonSerializer.Serialize(report, Options);

    public static void ExportToFile(ReportModel report, string filePath) =>
        File.WriteAllText(filePath, Export(report));
}
