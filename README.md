# Crystal Report Analyzer

[Leia em Português](README.pt-BR.md)

A WPF tool for analyzing SAP Crystal Reports (`.rpt`) files. Extracts tables, fields, parameters, formulas, groups, sections, and subreports; scores report complexity; classifies database objects; and generates C# migration stubs (DTOs, service interface, parameter class, business rules).

## Features

- Drag-and-drop or open any `.rpt` file
- Complexity scoring with visual classification (Simple / Medium / Complex / Very Complex)
- Dependency analysis: classifies database objects as Table, View, Stored Procedure, or Function
- C# code generation for migrating reports to HTML/Razor
- Export full analysis to JSON or generated stubs to a `.cs` file

## Prerequisites

### 1. SAP Crystal Reports Runtime (required)

Crystal Reports DLLs are **proprietary and non-redistributable**, so they are not included in this repository. You must install them separately:

1. Download **SAP Crystal Reports for Visual Studio** (free for development):
   [https://www.sap.com/products/crystal-reports/downloads.html](https://www.sap.com/products/crystal-reports/downloads.html)
2. Run the installer.
3. After installation, copy the following DLLs from the install directory to `CrystalReportAnalyzer/libs/`:

   | DLL | Typical location |
   |-----|-----------------|
   | `CrystalDecisions.CrystalReports.Engine.dll` | `C:\Program Files (x86)\SAP BusinessObjects\Crystal Reports for .NET Framework 4.0\Common\SAP BusinessObjects Enterprise XI 4.0\win32_x86\` |
   | `CrystalDecisions.Shared.dll` | same |
   | `CrystalDecisions.ReportSource.dll` | same |

   Also copy any other `CrystalDecisions.*.dll` files — they are loaded dynamically at runtime.

> **Note:** Crystal Reports is a 32-bit runtime. If you get loader errors, uncomment `<PlatformTarget>x86</PlatformTarget>` in the `.csproj`.

### 2. .NET SDK

- [.NET SDK 4.8+](https://dotnet.microsoft.com/download) (project targets `net48`)

## Build & Run

```bash
# Build
dotnet build CrystalReportAnalyzer.sln

# Run
dotnet run --project CrystalReportAnalyzer/CrystalReportAnalyzer.csproj
```

## Architecture

The project is organized around a pipeline: **Extractors → Service → Analyzers → UI / Exporters**.

- **Extractors** — one class per concern (`DatabaseExtractor`, `FormulaExtractor`, `ParameterExtractor`, `GroupExtractor`, `SectionExtractor`, `SubreportExtractor`), each receiving a `ReportDocument` and returning typed model objects.
- **ReportAnalysisService** — orchestrates all extractors, then feeds the result to two analyzers:
  - `ComplexityAnalyzer` — scores the report and classifies it as Simple / Medium / Complex / Very Complex.
  - `DependencyAnalyzer` — classifies each database object as Table, View, Stored Procedure, or Function based on naming conventions.
- **CSharpCodeGenerator** — produces four C# code blocks (DTOs, parameter class, service interface, business rules) to assist migration away from Crystal Reports.
- **Exporters** — `JsonExporter` serializes the full model; `CSharpExporter` writes the generated stubs to a `.cs` file.
- **MainWindow** — WPF code-behind (no MVVM) that drives the tree view, score card, dependency panel, and export buttons.

For full details — scoring weights, type inference rules, and data flow — see [CLAUDE.md](CLAUDE.md).

## License

MIT — see [LICENSE](LICENSE).

The Crystal Reports runtime dependency is proprietary software owned by SAP SE and is subject to SAP's license terms. It is not included in this repository.
