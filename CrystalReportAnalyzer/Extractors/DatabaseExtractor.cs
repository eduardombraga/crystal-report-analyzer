using CrystalDecisions.CrystalReports.Engine;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Extractors;

public static class DatabaseExtractor
{
    public static List<TableModel> Extract(ReportDocument doc)
    {
        var tables = new List<TableModel>();

        foreach (Table table in doc.Database.Tables)
        {
            var tableModel = new TableModel
            {
                Name     = table.Name,
                Location = table.Location,
            };

            foreach (DatabaseFieldDefinition field in table.Fields)
            {
                tableModel.Fields.Add(new FieldModel
                {
                    Name      = field.Name,
                    TableName = field.TableName,
                    ValueType = field.ValueType.ToString(),
                });
            }

            tables.Add(tableModel);
        }

        return tables;
    }
}
