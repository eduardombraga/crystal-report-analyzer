using CrystalDecisions.CrystalReports.Engine;

namespace CrystalReportAnalyzer.Extractors;

public static class FontExtractor
{
    public static List<string> Extract(ReportDocument doc)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Section section in doc.ReportDefinition.Sections)
            foreach (ReportObject obj in section.ReportObjects)
                TryAdd(obj, seen);

        return seen.OrderBy(f => f).ToList();
    }

    private static void TryAdd(ReportObject obj, HashSet<string> seen)
    {
        try
        {
            System.Drawing.Font? font = obj switch
            {
                TextObject  t => t.Font,
                FieldObject f => f.Font,
                _             => null,
            };

            if (font is null) return;

            var desc = $"{font.Name} {font.Size}pt";
            if (font.Bold)   desc += " Bold";
            if (font.Italic) desc += " Italic";
            seen.Add(desc);
        }
        catch { }
    }
}
