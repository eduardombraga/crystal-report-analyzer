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
                Name                       = section.Name,
                Kind                       = section.Kind.ToString(),
                ObjectCount                = section.ReportObjects.Count,
                IsVisible                  = TryGetBool(section, s => !s.SectionFormat.EnableSuppress, defaultValue: true),
                SuppressIfBlank            = TryGetBool(section, s => s.SectionFormat.EnableSuppressIfBlank, defaultValue: false),
                NewPageBefore              = TryGetBool(section, s => s.SectionFormat.EnableNewPageBefore, defaultValue: false),
                NewPageAfter               = TryGetBool(section, s => s.SectionFormat.EnableNewPageAfter, defaultValue: false),
                ResetPageNumberAfter       = TryGetBool(section, s => s.SectionFormat.EnableResetPageNumberAfter, defaultValue: false),
                KeepTogether                = TryGetBool(section, s => s.SectionFormat.EnableKeepTogether, defaultValue: false),
                UnderlayFollowingSections  = TryGetBool(section, s => s.SectionFormat.EnableUnderlaySection, defaultValue: false),
                SuppressFormulaText        = string.Empty,
            });
        }

        return sections;
    }

    private static bool TryGetBool(Section section, Func<Section, bool> getter, bool defaultValue)
    {
        try   { return getter(section); }
        catch { return defaultValue; }
    }
}
