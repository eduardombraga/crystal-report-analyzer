using CrystalDecisions.CrystalReports.Engine;
using CrystalReportAnalyzer.Core.Models;
using RasArea = CrystalDecisions.ReportAppServer.ReportDefModel.Area;
using RasSection = CrystalDecisions.ReportAppServer.ReportDefModel.Section;
using RasConditionFormulas = CrystalDecisions.ReportAppServer.ReportDefModel.ISCRSectionAreaFormatConditionFormulas;
using CrSectionAreaFormatConditionFormulaTypeEnum = CrystalDecisions.ReportAppServer.ReportDefModel.CrSectionAreaFormatConditionFormulaTypeEnum;

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
                SuppressFormulaText        = TryGetSuppressFormulaText(section, doc),
            });
        }

        return sections;
    }

    private static bool TryGetBool(Section section, Func<Section, bool> getter, bool defaultValue)
    {
        try   { return getter(section); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Retrieves the "Suppress (No Drill-Down)" conditional formula text for a section, if any.
    /// The Engine object model (CrystalDecisions.CrystalReports.Engine.SectionFormat) does not
    /// expose conditional formula text directly, so this drops down to the Report Application
    /// Server (RAS) object model via ReportDocument.ReportClientDocument, correlating the Engine
    /// Section to the equivalent RAS ISCRSection by name.
    /// Defensive: returns string.Empty on any failure (no RAS document, no matching section,
    /// no suppress condition formula set, etc.) rather than throwing.
    /// </summary>
    private static string TryGetSuppressFormulaText(Section section, ReportDocument doc)
    {
        try
        {
            var rcd = doc.ReportClientDocument;
            var reportDef = rcd.ReportDefController.ReportDefinition;

            foreach (RasArea area in reportDef.Areas)
            {
                foreach (RasSection rasSection in area.Sections)
                {
                    if (rasSection.Name != section.Name)
                        continue;

                    if (rasSection.Format?.ConditionFormulas is not RasConditionFormulas conditionFormulas)
                        return string.Empty;

                    var suppressFormula = conditionFormulas[
                        CrSectionAreaFormatConditionFormulaTypeEnum.crSectionAreaConditionFormulaTypeEnableSuppress];

                    return suppressFormula?.Text ?? string.Empty;
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
