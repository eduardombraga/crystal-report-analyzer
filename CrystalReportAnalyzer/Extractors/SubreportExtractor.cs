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

                subreports.Add(new SubreportModel
                {
                    Name       = sub.SubreportName,
                    Location   = section.Name,
                    IsOnDemand = sub.EnableOnDemand,
                });
            }
        }

        return subreports;
    }
}
