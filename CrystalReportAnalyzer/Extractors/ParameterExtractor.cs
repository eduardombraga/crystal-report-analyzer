using CrystalDecisions.CrystalReports.Engine;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Extractors;

public static class ParameterExtractor
{
    public static List<ParameterModel> Extract(ReportDocument doc)
    {
        var parameters = new List<ParameterModel>();

        foreach (ParameterFieldDefinition param in doc.DataDefinition.ParameterFields)
        {
            parameters.Add(new ParameterModel
            {
                Name       = param.Name,
                PromptText = param.PromptText,
                ValueType  = GetValueType(param),
                // Required when no default value is defined
                IsRequired = param.DefaultValues.Count == 0,
            });
        }

        return parameters;
    }

    // ParameterValueType was renamed/moved in some CR runtime versions;
    // fall back through known alternatives to avoid compile/runtime errors.
    private static string GetValueType(ParameterFieldDefinition param)
    {
        try
        {
            // CR for VS 2013+
            var prop = param.GetType().GetProperty("ParameterValueType")
                    ?? param.GetType().GetProperty("ParameterType")
                    ?? param.GetType().GetProperty("DiscreteOrRangeKind");
            if (prop != null)
                return prop.GetValue(param)?.ToString() ?? string.Empty;
        }
        catch { /* ignore */ }

        return string.Empty;
    }
}
