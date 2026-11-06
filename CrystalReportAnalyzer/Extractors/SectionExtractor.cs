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
        // Pass 1: read everything from the Engine object model (doc.ReportDefinition.Sections)
        // first, without touching RAS. Accessing doc.ReportClientDocument (needed for the suppress
        // conditional formula) invalidates previously-obtained Engine Section COM references, so any
        // Engine property access on a Section must happen strictly before RAS is touched at all.
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
                KeepTogether               = TryGetBool(section, s => s.SectionFormat.EnableKeepTogether, defaultValue: false),
                UnderlayFollowingSections  = TryGetBool(section, s => s.SectionFormat.EnableUnderlaySection, defaultValue: false),
                Objects                    = ExtractObjects(section),
            });
        }

        // Pass 2: now that no Engine Section references are needed anymore, touch RAS exactly once
        // to build a name -> suppress-formula-text lookup, then apply it to the already-built models.
        var suppressFormulasByName = TryGetSuppressFormulasByName(doc);
        foreach (var section in sections)
        {
            if (suppressFormulasByName.TryGetValue(section.Name, out var formulaText))
                section.SuppressFormulaText = formulaText;
        }

        return sections;
    }

    /// <summary>
    /// Reads every drawable object of a section, sorted into reading order (top, then left).
    /// Engine-only — must stay inside pass 1, before RAS is touched.
    /// </summary>
    private static List<ReportObjectModel> ExtractObjects(Section section)
    {
        var objects = new List<ReportObjectModel>();

        foreach (ReportObject reportObject in section.ReportObjects)
        {
            var model = new ReportObjectModel
            {
                Name     = reportObject.Name,
                Kind     = reportObject.Kind.ToString(),
                Top      = reportObject.Top,
                Left     = reportObject.Left,
                Width    = reportObject.Width,
                Height   = reportObject.Height,
                Suppress = TryGet(() => reportObject.ObjectFormat.EnableSuppress, defaultValue: false),
            };

            // Kind-specific extras. Each report object type exposes a different subset of
            // properties, and a malformed/unsupported object can throw on access, so every
            // read is individually guarded.
            switch (reportObject)
            {
                case TextObject textObject:
                    model.Text    = TryGet(() => textObject.Text, defaultValue: string.Empty);
                    model.CanGrow = TryGet(() => textObject.ObjectFormat.EnableCanGrow, defaultValue: false);
                    break;

                case FieldObject fieldObject:
                    // FormulaName is Crystal's own notation: "{Table.Column}" or "{@Formula}",
                    // which is exactly what links an object back to the exported formulas.
                    model.DataSource = TryGet(() => fieldObject.DataSource?.FormulaName ?? string.Empty,
                                              defaultValue: string.Empty);
                    model.CanGrow    = TryGet(() => fieldObject.ObjectFormat.EnableCanGrow, defaultValue: false);
                    break;

                case SubreportObject subreportObject:
                    model.SubreportName = TryGet(() => subreportObject.SubreportName, defaultValue: string.Empty);
                    break;
            }

            objects.Add(model);
        }

        return objects
            .OrderBy(o => o.Top)
            .ThenBy(o => o.Left)
            .ToList();
    }

    private static T TryGet<T>(Func<T> getter, T defaultValue)
    {
        try   { return getter(); }
        catch { return defaultValue; }
    }

    private static bool TryGetBool(Section section, Func<Section, bool> getter, bool defaultValue)
    {
        try   { return getter(section); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Retrieves the "Suppress (No Drill-Down)" conditional formula text for every section that has
    /// one, keyed by section name. The Engine object model (CrystalDecisions.CrystalReports.Engine.
    /// SectionFormat) does not expose conditional formula text directly, so this drops down to the
    /// Report Application Server (RAS) object model via ReportDocument.ReportClientDocument.
    /// IMPORTANT: touching ReportClientDocument invalidates previously-obtained Engine Section COM
    /// references, so this traversal must be self-contained — it must not receive or touch any
    /// Engine Section object, only plain strings/RAS types — and must run only once, after all
    /// Engine-side section data has already been read.
    /// Defensive: returns an empty dictionary on any failure (no RAS document, unsupported report,
    /// etc.) rather than throwing; a section simply absent from the result has no conditional
    /// suppress formula (or a lookup for it failed), which is indistinguishable by design.
    /// </summary>
    private static Dictionary<string, string> TryGetSuppressFormulasByName(ReportDocument doc)
    {
        var result = new Dictionary<string, string>();

        try
        {
            var rcd = doc.ReportClientDocument;
            var reportDef = rcd.ReportDefController.ReportDefinition;

            foreach (RasArea area in reportDef.Areas)
            {
                foreach (RasSection rasSection in area.Sections)
                {
                    if (rasSection.Format?.ConditionFormulas is not RasConditionFormulas conditionFormulas)
                        continue;

                    var suppressFormula = conditionFormulas[
                        CrSectionAreaFormatConditionFormulaTypeEnum.crSectionAreaConditionFormulaTypeEnableSuppress];

                    string? formulaText = suppressFormula?.Text;
                    if (!string.IsNullOrEmpty(formulaText))
                        result[rasSection.Name] = formulaText!;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SectionExtractor: failed to extract section suppress formulas: {ex.Message}");
        }

        return result;
    }
}
