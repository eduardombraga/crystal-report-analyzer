using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Core.Analyzer;

public static class DependencyAnalyzer
{
    public static List<DbObjectModel> Classify(List<TableModel> tables)
    {
        var result = new List<DbObjectModel>();
        foreach (var table in tables)
        {
            result.Add(new DbObjectModel
            {
                Name       = table.Name,
                Location   = table.Location,
                Type       = InferType(table.Name),
                FieldCount = table.Fields.Count,
            });
        }
        return result;
    }

    private static DbObjectType InferType(string name)
    {
        if (string.IsNullOrEmpty(name)) return DbObjectType.Unknown;

        string upper = name.ToUpperInvariant();

        if (upper.StartsWith("VW_") || upper.StartsWith("VW") && upper.Length > 2
            || upper.Contains("_VW_") || upper.EndsWith("_VW"))
            return DbObjectType.View;

        if (upper.StartsWith("SP_") || upper.StartsWith("USP_") || upper.StartsWith("PROC_"))
            return DbObjectType.StoredProcedure;

        if (upper.StartsWith("FN_") || upper.StartsWith("UFN_") || upper.StartsWith("FUNC_"))
            return DbObjectType.Function;

        return DbObjectType.Table;
    }
}
