namespace CrystalReportAnalyzer.Core.Services.RazorGeneration.Helpers;

/// <summary>
/// Maps Crystal Reports ValueType strings to HTML input attributes,
/// CSS alignment classes, and Razor display format expressions.
/// </summary>
public static class RazorTypeMapper
{
    public static string InputType(string crType) => crType switch
    {
        "DateType"     => "text",
        "DateTimeType" => "text",
        "TimeType"     => "text",
        "BooleanType"  => "checkbox",
        _              => "text",
    };

    /// <summary>Returns a data-type attribute value for the momentum UI framework, or null if none.</summary>
    public static string? DataType(string crType) => crType switch
    {
        "DateType"     => "data",
        "DateTimeType" => "data",
        "NumberType"   => "number",
        "CurrencyType" => "number",
        "Int16sType"   => "number",
        "Int32sType"   => "number",
        "Int64sType"   => "number",
        _              => null,
    };

    /// <summary>CSS alignment class for table headers/cells.</summary>
    public static string Alignment(string crType) => crType switch
    {
        "NumberType"   => "right",
        "CurrencyType" => "right",
        "Int16sType"   => "right",
        "Int32sType"   => "right",
        "Int64sType"   => "right",
        "DateType"     => "center",
        "DateTimeType" => "center",
        "TimeType"     => "center",
        "BooleanType"  => "center",
        _              => "",
    };

    /// <summary>Razor display expression suffix — e.g. .ToString("dd/MM/yyyy")</summary>
    public static string DisplayFormat(string crType) => crType switch
    {
        "NumberType"   => ".ToString(\"N2\")",
        "CurrencyType" => ".ToString(\"C2\")",
        "DateType"     => ".ToString(\"dd/MM/yyyy\")",
        "DateTimeType" => ".ToString(\"dd/MM/yyyy HH:mm\")",
        "TimeType"     => ".ToString(\"HH:mm\")",
        "BooleanType"  => " ? \"Sim\" : \"Não\"",
        _              => "",
    };

    /// <summary>Converts a field/table name to PascalCase C# identifier.</summary>
    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";

        name = name.TrimStart('@').Trim();
        var sb = new System.Text.StringBuilder();
        bool capitalizeNext = true;

        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(capitalizeNext ? char.ToUpper(c) : c);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        string result = sb.ToString();
        if (result.Length == 0) return "Unknown";
        if (char.IsDigit(result[0])) result = "_" + result;
        return result;
    }

    /// <summary>Converts a name to camelCase (for JS variables).</summary>
    public static string ToCamelCase(string name)
    {
        string pascal = ToPascalCase(name);
        if (pascal.Length <= 1) return pascal.ToLower();
        return char.ToLower(pascal[0]) + pascal.Substring(1);
    }
}
