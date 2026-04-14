using System.IO;
using System.Text.Json;

namespace CrystalReportAnalyzer.Core.Configuration;

public static class ScoringConfigService
{
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public static string ConfigFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CrystalReportAnalyzer",
        "scoring-config.json");

    public static ScoringConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
                return ScoringConfig.Default();

            string json = File.ReadAllText(ConfigFilePath);
            var config  = JsonSerializer.Deserialize<ScoringConfig>(json, _json);

            return IsValid(config) ? config! : ScoringConfig.Default();
        }
        catch
        {
            return ScoringConfig.Default();
        }
    }

    public static void Save(ScoringConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);
        File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config, _json));
    }

    private static bool IsValid(ScoringConfig? config) =>
        config?.Weights    is { Count: > 0 } &&
        config?.Thresholds is { Count: > 0 };
}
