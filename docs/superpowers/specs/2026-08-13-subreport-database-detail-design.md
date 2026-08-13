# Subreport Database Detail — Design

## Problem

`SubreportModel` currently only carries `Name`, `Location`, and `IsOnDemand`. In the tree view, subreports render as a flat leaf list with no way to inspect what database tables/fields a given subreport pulls from, and no link to the DTO(s) generated for it. Users analyzing a report for migration have no way to answer "which DTO does this subreport map to?" without opening the `.rpt` in Crystal Reports Designer itself.

## Goal

Clicking a subreport node in the tree shows its tables, fields, and the name(s) of the C# DTO class(es) generated for it — mirroring the existing formula detail panel pattern.

## Non-goals

- Subreport-local parameters or formulas (out of scope; only tables/fields/DTO).
- Recursive extraction of subreports nested inside subreports beyond one level, unless real-world `.rpt` files require it.

## Changes

### 1. `Core/Models/SubreportModel.cs`

Add `public List<TableModel> Tables { get; set; } = new();` reusing the existing `TableModel`/`FieldModel` types.

### 2. `Extractors/SubreportExtractor.cs`

For each unique `SubreportObject` found, open the subreport document via `section.OpenSubreport(sub.SubreportName)` (returns a `ReportDocument`) and run the existing `DatabaseExtractor.Extract()` against it to populate `Tables`. Errors opening an individual subreport (e.g. unbound/broken subreport link) are caught per-subreport so one bad subreport doesn't fail the whole analysis; on failure `Tables` stays empty for that subreport.

### 3. `Core/Services/CSharpCodeGenerator.cs`

`GenerateDtos` extends to also emit DTO classes for each subreport's tables, in addition to the main report's tables. Subreport DTO class names are prefixed with the subreport's identifier (e.g. `{SubreportName}{TableName}Dto`) to avoid collisions with main-report DTOs when a subreport uses a same-named table.

### 4. `Core/Services/ReportAnalysisService.cs`

No structural change needed beyond what `SubreportExtractor` and `CSharpCodeGenerator` already produce — `GenerateDtos(report)` is called the same way, just does more work internally.

### 5. UI (`MainWindow.xaml`, `MainWindow.xaml.cs`)

- Subreport tree nodes gain `Tag = SubreportModel` (same pattern as formula nodes) and child table/field sub-nodes reusing the existing table-node rendering.
- `ReportTreeView_SelectedItemChanged` adds a branch for `Tag: SubreportModel`, showing a new detail panel: subreport name, on-demand flag, list of tables with field counts, and the DTO class name(s) generated for it.
- Reuses the existing show/hide panel pattern (`ShowFormulaDetail`/`HideFormulaDetail` → generalize or add parallel `ShowSubreportDetail`/`HideSubreportDetail`).

## Data flow (updated)

1. `SubreportExtractor.Extract(doc)` now also opens each subreport doc and extracts its tables via `DatabaseExtractor`.
2. `CSharpCodeGenerator.GenerateDtos(report)` emits DTOs for main tables + all subreport tables.
3. UI tree: selecting a subreport node shows table/field list + generated DTO name(s) in a detail panel, same UX shape as the formula detail panel.

## Testing

No automated tests in this project (per CLAUDE.md). Manual verification: open a `.rpt` with at least one subreport containing its own table, confirm the tree shows tables under the subreport node, and the detail panel on click shows correct tables/fields and DTO name.
