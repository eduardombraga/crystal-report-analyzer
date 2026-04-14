using CrystalDecisions.CrystalReports.Engine;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Extractors;

public static class GroupExtractor
{
    public static List<GroupModel> Extract(ReportDocument doc)
    {
        var groups = new List<GroupModel>();

        foreach (Group group in doc.DataDefinition.Groups)
        {
            var field   = group.ConditionField;
            var dbField = field as DatabaseFieldDefinition;
            string qualified = dbField != null && !string.IsNullOrEmpty(dbField.TableName)
                               ? $"{dbField.TableName}.{dbField.Name}"
                               : field.Name;

            groups.Add(new GroupModel
            {
                Name           = field.Name,
                ConditionField = qualified,
                SortOrder      = string.Empty,
            });
        }

        return groups;
    }
}
