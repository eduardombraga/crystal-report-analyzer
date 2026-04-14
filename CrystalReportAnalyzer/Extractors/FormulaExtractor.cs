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
            formulas.Add(new FormulaModel
            {
                Name       = formula.Name,
                Text       = TryGetText(formula),
                ReturnType = formula.ValueType.ToString(),
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
