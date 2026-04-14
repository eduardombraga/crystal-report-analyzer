namespace CrystalReportAnalyzer.Core.Models;

public enum DbObjectType
{
    Table,
    View,
    StoredProcedure,
    Function,
    Unknown
}

public class DbObjectModel
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DbObjectType Type { get; set; } = DbObjectType.Table;
    public int FieldCount { get; set; }

    public string TypeLabel => Type switch
    {
        DbObjectType.Table           => "Table",
        DbObjectType.View            => "View",
        DbObjectType.StoredProcedure => "Stored Procedure",
        DbObjectType.Function        => "Function",
        _                            => "Unknown",
    };
}
