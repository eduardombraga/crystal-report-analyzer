# Section Properties Detail — Design

## Problem

`SectionModel` currently only carries `Name`, `Kind`, `ObjectCount`, and `IsVisible` (derived from `SectionFormat.EnableSuppress`). Clicking a section node in the tree does nothing — sections render as flat leaf text. Users analyzing a report for migration have no way to see a section's full formatting configuration (suppress logic including conditional formulas, page-break behavior, keep-together, underlay, etc.) without opening the `.rpt` in Crystal Reports Designer.

## Goal

Clicking a section node in the tree shows its full `SectionFormat` configuration: suppress (plus its conditional formula text, if the property is driven by a formula rather than a fixed checkbox), suppress-if-blank, new-page-before, new-page-after, reset-page-number-after, keep-together, and underlay-following-sections.

## Non-goals

- Per-object layout within a section (position/size of each report object) — `ObjectCount` already exists and is not being expanded to an object-by-object breakdown.
- Section-level formulas other than the suppress condition (e.g. font/color formulas) — out of scope unless requested later.

## Changes

### 1. `Core/Models/SectionModel.cs`

Add:
```csharp
public bool SuppressIfBlank { get; set; }
public bool NewPageBefore { get; set; }
public bool NewPageAfter { get; set; }
public bool ResetPageNumberAfter { get; set; }
public bool KeepTogether { get; set; }
public bool UnderlayFollowingSections { get; set; }
public string SuppressFormulaText { get; set; } = string.Empty;
```
`IsVisible` (existing) continues to represent `!EnableSuppress`, unchanged in meaning.

### 2. `Extractors/SectionExtractor.cs`

For each flag, read the corresponding `SectionFormat` property (`EnableSuppressIfBlank`, `EnableNewPageBefore`, `EnableNewPageAfter`, `EnableResetPageNumberAfter`, `EnableKeepTogether`, `EnableUnderlaySection`), each wrapped in its own try/catch mirroring the existing `TryGetVisible` pattern — Crystal Reports can throw if a property isn't supported for a given section type, and one unsupported property shouldn't blank out the others.

For the suppress conditional formula: call `section.SectionFormat.ConditionFormula("EnableSuppress")`, which returns a `FormulaFieldDefinition`-like object (or throws/returns null if no conditional formula is set for that property). If present and non-empty, its `.Text` populates `SuppressFormulaText`; otherwise it stays empty, meaning suppress is governed by the fixed `EnableSuppress` boolean alone.

### 3. UI (`MainWindow.xaml`, `MainWindow.xaml.cs`)

- Section tree nodes (built in `BuildSectionNodes`) gain `Tag = SectionModel` (currently they're plain `Node(...)` leaves with no tag).
- A new `SectionDetailPanel` is added to `MainWindow.xaml`, mirroring the existing `FormulaDetailPanel`/`SubreportDetailPanel` Border pattern (same `Grid.Row="1"`, `Card` style, `Visibility="Collapsed"`). It shows: section name/kind, a list of the boolean properties each rendered as "Label: Sim/Não", and if `SuppressFormulaText` is non-empty, the formula text in a monospace block (reusing the same font/style as `FormulaDetailText`).
- `ReportTreeView_SelectedItemChanged` gains a third branch for `Tag: SectionModel`, calling a new `ShowSectionDetail(SectionModel)`. The three-panel mutual-exclusion pattern already established (`ShowFormulaDetail`/`ShowSubreportDetail`/`HideAllDetailPanels` each explicitly setting all panel visibilities) extends to four panels the same way — each `Show*` method explicitly collapses the other three plus `StatsCard`, and `HideAllDetailPanels` collapses all three detail panels and shows `StatsCard`.

## Data flow (updated)

1. `SectionExtractor.Extract(doc)` now also reads the extra `SectionFormat` flags and the suppress conditional formula per section.
2. UI tree: selecting a section node shows its full formatting configuration in a detail panel, same UX shape as the formula/subreport detail panels.

## Testing

No automated tests in this project (per CLAUDE.md). Manual verification: open a `.rpt` with at least one section that has a conditional suppress formula and one with a fixed new-page-before flag, confirm the tree shows the section node is clickable, and the detail panel shows correct values for both cases.
