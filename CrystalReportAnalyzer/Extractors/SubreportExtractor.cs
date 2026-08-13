using CrystalDecisions.CrystalReports.Engine;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Extractors;

public static class SubreportExtractor
{
    public static List<SubreportModel> Extract(ReportDocument doc)
    {
        var subreports = new List<SubreportModel>();
        var seen       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Section section in doc.ReportDefinition.Sections)
        {
            foreach (ReportObject obj in section.ReportObjects)
            {
                if (obj is not SubreportObject sub) continue;
                if (!seen.Add(sub.SubreportName))   continue; // deduplicate

                var model = new SubreportModel
                {
                    Name       = sub.SubreportName,
                    Location   = section.Name,
                    IsOnDemand = sub.EnableOnDemand,
                };

                try
                {
                    var subDoc = sub.OpenSubreport(sub.SubreportName);
                    model.Tables = DatabaseExtractor.Extract(subDoc);
                }
                catch (Exception)
                {
                    // Broken/unbound subreport link — leave Tables empty.
                }

                subreports.Add(model);
            }
        }

        return subreports;
    }
}
