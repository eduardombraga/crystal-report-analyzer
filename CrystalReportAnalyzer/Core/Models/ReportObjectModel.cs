namespace CrystalReportAnalyzer.Core.Models;

/// <summary>
/// A single drawable object inside a section (field, text, line, box, subreport, ...).
/// Positions are in twips, as reported by Crystal Reports (1440 twips = 1 inch).
/// </summary>
public class ReportObjectModel
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Crystal's <c>ReportObjectKind</c> as text: FieldObject, TextObject, LineObject, ...</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Literal text of a text object; empty for every other kind.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>For field objects: <c>{Table.Column}</c> or <c>{@Formula}</c>; empty otherwise.</summary>
    public string DataSource { get; set; } = string.Empty;

    public int Top { get; set; }
    public int Left { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Object-level suppression, independent of the section's own suppress flag.</summary>
    public bool Suppress { get; set; }

    /// <summary>True when the object grows vertically to fit its content (fields and text objects).</summary>
    public bool CanGrow { get; set; }

    /// <summary>Name of the referenced subreport, for subreport objects; empty otherwise.</summary>
    public string SubreportName { get; set; } = string.Empty;
}
