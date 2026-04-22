using CrystalDecisions.CrystalReports.Engine;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Extractors;

public static class FormulaExtractor
{
    public static List<FormulaModel> Extract(ReportDocument doc)
    {
        var formulas = new List<FormulaModel>();

        foreach (FormulaFieldDefinition formula in doc.DataDefinition.FormulaFields)
        {
            var text = TryGetText(formula);
            var refs = FormulaReferenceParser.Parse(text);
            formulas.Add(new FormulaModel
            {
                Name                    = formula.Name,
                Text                    = text,
                ReturnType              = formula.ValueType.ToString(),
                ReferencedDbFields      = refs.DbFields,
                ReferencedParameters    = refs.Parameters,
                ReferencedFormulas      = refs.OtherFormulas,
                ReferencedRunningTotals = refs.RunningTotals,
            });
        }

        return formulas;
    }

    // formula.Text may throw if the report cannot resolve all dependencies
    private static string TryGetText(FormulaFieldDefinition formula)
    {
        try   { return formula.Text ?? string.Empty; }
        catch { return string.Empty; }
    }
}
