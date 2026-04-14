using System.IO;
using CrystalDecisions.CrystalReports.Engine;
using CrystalReportAnalyzer.Core.Analyzer;
using CrystalReportAnalyzer.Core.Configuration;
using CrystalReportAnalyzer.Core.Models;
using CrystalReportAnalyzer.Extractors;

namespace CrystalReportAnalyzer.Core.Services;

public class ReportAnalysisService
{
    public ReportModel Analyze(string filePath, ScoringConfig config)
    {
        using var doc = new ReportDocument();
        doc.Load(filePath);

        var report = new ReportModel
        {
            Name      = Path.GetFileNameWithoutExtension(filePath),
            FilePath  = filePath,
            Tables     = DatabaseExtractor.Extract(doc),
            Parameters = ParameterExtractor.Extract(doc),
            Formulas   = FormulaExtractor.Extract(doc),
            Groups     = GroupExtractor.Extract(doc),
            Sections   = SectionExtractor.Extract(doc),
            Subreports = SubreportExtractor.Extract(doc),
        };

        // Flatten all fields from all tables for top-level access
        report.Fields = report.Tables.SelectMany(t => t.Fields).ToList();

        report.Complexity = ComplexityAnalyzer.Analyze(report, config);

        report.Dependencies = new ReportDependencies
        {
            DbObjects                 = DependencyAnalyzer.Classify(report.Tables),
            GeneratedDtos             = CSharpCodeGenerator.GenerateDtos(report),
            GeneratedServiceInterface = CSharpCodeGenerator.GenerateServiceInterface(report),
            GeneratedParameterClass   = CSharpCodeGenerator.GenerateParameterClass(report),
            GeneratedBusinessRules    = CSharpCodeGenerator.GenerateBusinessRules(report),
        };

        return report;
    }
}
