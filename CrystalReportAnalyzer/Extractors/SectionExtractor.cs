using CrystalDecisions.CrystalReports.Engine;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Extractors;

public static class SectionExtractor
{
    public static List<SectionModel> Extract(ReportDocument doc)
    {
        var sections = new List<SectionModel>();

        foreach (Section section in doc.ReportDefinition.Sections)
        {
            sections.Add(new SectionModel
            {
                Name        = section.Name,
                Kind        = section.Kind.ToString(),
                ObjectCount = section.ReportObjects.Count,
                IsVisible   = TryGetVisible(section),
            });
        }

        return sections;
    }

    private static bool TryGetVisible(Section section)
    {
        try   { return !section.SectionFormat.EnableSuppress; }
        catch { return true; }
    }
}
