# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build the solution
dotnet build CrystalReportAnalyzer.sln

# Run the WPF app (Windows only)
dotnet run --project CrystalReportAnalyzer/CrystalReportAnalyzer.csproj
```

There are no automated tests in this project.

## Crystal Reports Dependency

The project targets **net48** (required for Crystal Reports compatibility). The three referenced DLLs are **not on NuGet** — they must come from installing [SAP Crystal Reports runtime for Visual Studio](https://www.sap.com/products/crystal-reports/downloads.html):

- `CrystalDecisions.CrystalReports.Engine.dll`
- `CrystalDecisions.Shared.dll`
- `CrystalDecisions.ReportSource.dll`

Copy those (and all other `CrystalDecisions.*.dll` files from the runtime install) into `CrystalReportAnalyzer/libs/`. The `.csproj` copies every `CrystalDecisions.*.dll` from `libs/` to the output directory so dynamically-loaded assemblies are resolved at runtime.

If you get 32-bit loader errors, uncomment `<PlatformTarget>x86</PlatformTarget>` in the `.csproj`. The Crystal Reports runtime DLL at `C:\Program Files (x86)\SAP BusinessObjects\...` is 32-bit only.

`Polyfills.cs` provides the `IsExternalInit` stub required to use C# 9+ `init`-only setters and records on .NET Framework 4.8.

## Architecture

```
CrystalReportAnalyzer/
├── Core/
│   ├── Models/          POCOs: ReportModel, TableModel, FieldModel, ParameterModel,
│   │                    FormulaModel, GroupModel, SectionModel, SubreportModel,
│   │                    ComplexityResult, DbObjectModel, ReportDependencies
│   ├── Analyzer/        ComplexityAnalyzer — pure scoring, no CR dependency
│   │                    DependencyAnalyzer — classifies tables as Table/View/SP/Function
│   └── Services/        ReportAnalysisService — orchestrates extractors + analyzers
│                        CSharpCodeGenerator  — generates C# migration stubs
├── Extractors/          One static class per concern, each receives a ReportDocument
│   ├── DatabaseExtractor    → Tables + Fields
│   ├── FormulaExtractor     → FormulaFields
│   ├── ParameterExtractor   → ParameterFields
│   ├── GroupExtractor       → Groups
│   ├── SectionExtractor     → Sections (object count & visibility)
│   └── SubreportExtractor   → SubreportObjects (de-duplicated by name)
├── Exporters/
│   ├── JsonExporter     → System.Text.Json serialization with enum-as-string
│   └── CSharpExporter   → writes ReportDependencies.AllGeneratedCode to a .cs file
├── Polyfills.cs         IsExternalInit stub for net48 + C# 9 compatibility
├── MainWindow.xaml      WPF layout (header, drop zone, tree, score card, stats, export)
└── MainWindow.xaml.cs   Code-behind — no MVVM, direct control manipulation
```

### Data flow

1. User opens or drops a `.rpt` file.
2. `ReportAnalysisService.Analyze()` opens a `ReportDocument`, calls all extractors, flattens fields, then runs:
   - `ComplexityAnalyzer.Analyze()` → `ComplexityResult`
   - `DependencyAnalyzer.Classify()` → list of `DbObjectModel` (typed as Table/View/SP/Function by name prefix)
   - `CSharpCodeGenerator` → generates four C# code blocks (DTOs, parameter class, service interface, business rules) stored in `ReportDependencies`
3. The UI builds a `TreeView` from `ReportModel`, fills the score card, stats panel, and dependency/code-generation tab.
4. Optional exports: full `ReportModel` to JSON (`JsonExporter`) or generated C# stubs to a `.cs` file (`CSharpExporter`).

### Complexity scoring

| Item       | Points |
|------------|--------|
| Table      | +1     |
| Parameter  | +1     |
| Section    | +1     |
| Formula    | +2     |
| Group      | +2     |
| Subreport  | +3     |

Classification: 0–10 Simple · 11–20 Medium · 21–40 Complex · 40+ VeryComplex.

### DB object type inference (`DependencyAnalyzer`)

Type is inferred from the table name prefix/suffix (case-insensitive):

| Prefix/Suffix             | Type            |
|---------------------------|-----------------|
| `VW_`, `_VW_`, `_VW`     | View            |
| `SP_`, `USP_`, `PROC_`   | StoredProcedure |
| `FN_`, `UFN_`, `FUNC_`   | Function        |
| *(anything else)*         | Table           |

### C# code generation (`CSharpCodeGenerator`)

Generates four blocks in namespace `GeneratedFrom<ReportName>`:
- **DTOs** — one class per table, properties typed from Crystal Reports `ValueType`
- **Parameter class** — `<Name>Parameters` with nullable optional params
- **Service interface** — `I<Name>Service` with `Get<Name>()` and per-subreport methods
- **Business rules** — `<Name>BusinessRules` static class with one stub method per formula, preserving the original Crystal formula text in `<remarks>`
