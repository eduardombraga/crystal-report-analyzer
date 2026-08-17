# Section Properties Detail Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clicking a section node in the report tree shows its full `SectionFormat` configuration: suppress (+ conditional formula text if driven by a formula), suppress-if-blank, new-page-before, new-page-after, reset-page-number-after, keep-together, and underlay-following-sections.

**Architecture:** Extend `SectionModel` with the extra boolean flags plus a `SuppressFormulaText` string, read from `Section.SectionFormat` in `SectionExtractor` (each property wrapped in its own try/catch, mirroring the existing `TryGetVisible` pattern). Extend the WPF UI with a fourth mutually-exclusive detail panel (`SectionDetailPanel`), following the exact same pattern already established by `FormulaDetailPanel` and `SubreportDetailPanel`.

**Tech Stack:** C# / .NET Framework 4.8, WPF, Crystal Reports Engine SDK (`CrystalDecisions.CrystalReports.Engine`).

**No automated tests:** this project has none (per `CLAUDE.md`) and the Crystal Reports SDK requires a licensed runtime + real `.rpt` files. Verification is `dotnet build` after each code task, plus a manual UI checklist in the final task.

---

### Task 1: Add section-format properties to `SectionModel`

**Files:**
- Modify: `CrystalReportAnalyzer/Core/Models/SectionModel.cs`

- [ ] **Step 1: Add the new properties**

Replace the full file contents with:

```csharp
namespace CrystalReportAnalyzer.Core.Models;

public class SectionModel
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public int ObjectCount { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool SuppressIfBlank { get; set; }
    public bool NewPageBefore { get; set; }
    public bool NewPageAfter { get; set; }
    public bool ResetPageNumberAfter { get; set; }
    public bool KeepTogether { get; set; }
    public bool UnderlayFollowingSections { get; set; }
    public string SuppressFormulaText { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build CrystalReportAnalyzer.sln`
Expected: Build succeeds (the new properties are unused so far, which is fine).

- [ ] **Step 3: Commit**

```bash
git add CrystalReportAnalyzer/Core/Models/SectionModel.cs
git commit -m "feat: add SectionFormat properties to SectionModel"
```

---

### Task 2: Populate the new properties in `SectionExtractor`

**Files:**
- Modify: `CrystalReportAnalyzer/Extractors/SectionExtractor.cs`

**Context:** `Section.SectionFormat` (from `CrystalDecisions.CrystalReports.Engine`) exposes `EnableSuppress`, `EnableSuppressIfBlank`, `EnableNewPageBefore`, `EnableNewPageAfter`, `EnableResetPageNumberAfter`, `EnableKeepTogether`, `EnableUnderlaySection` as booleans, and `ConditionFormula(string propertyName)` returns an `IFormulaFieldDefinition`-like object whose `.Text` holds the conditional formula source (or the call throws / returns an object with empty `.Text` if no conditional formula is set for that property — wrap in try/catch same as the booleans). Each property read is wrapped in its own try/catch (mirroring the existing `TryGetVisible` pattern) since Crystal Reports can throw for properties unsupported by a given section type, and one unsupported property must not blank out the others.

- [ ] **Step 1: Update the extractor**

Replace the full file contents with:

```csharp
using CrystalDecisions.CrystalReports.Engine;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Extractors;

public static class SectionExtractor
{
    public static List<SectionModel> Extract(ReportDocument doc)
    {
        var sections = new List<SectionModel>();

        foreach (Section section in doc.ReportDefinition.Sections)
        {
            sections.Add(new SectionModel
            {
                Name                       = section.Name,
                Kind                       = section.Kind.ToString(),
                ObjectCount                = section.ReportObjects.Count,
                IsVisible                  = TryGetBool(section, s => !s.SectionFormat.EnableSuppress, defaultValue: true),
                SuppressIfBlank            = TryGetBool(section, s => s.SectionFormat.EnableSuppressIfBlank, defaultValue: false),
                NewPageBefore              = TryGetBool(section, s => s.SectionFormat.EnableNewPageBefore, defaultValue: false),
                NewPageAfter               = TryGetBool(section, s => s.SectionFormat.EnableNewPageAfter, defaultValue: false),
                ResetPageNumberAfter       = TryGetBool(section, s => s.SectionFormat.EnableResetPageNumberAfter, defaultValue: false),
                KeepTogether               = TryGetBool(section, s => s.SectionFormat.EnableKeepTogether, defaultValue: false),
                UnderlayFollowingSections  = TryGetBool(section, s => s.SectionFormat.EnableUnderlaySection, defaultValue: false),
                SuppressFormulaText        = TryGetSuppressFormulaText(section),
            });
        }

        return sections;
    }

    private static bool TryGetBool(Section section, Func<Section, bool> getter, bool defaultValue)
    {
        try   { return getter(section); }
        catch { return defaultValue; }
    }

    private static string TryGetSuppressFormulaText(Section section)
    {
        try
        {
            var formula = section.SectionFormat.ConditionFormula("EnableSuppress");
            return formula?.Text ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build CrystalReportAnalyzer.sln`
Expected: Build succeeds. If `ConditionFormula` or any `Enable*` member name doesn't match the actual SDK signature (compiler error naming the missing member), stop and report BLOCKED/NEEDS_CONTEXT rather than guessing an alternative member name — the exact SDK surface must be confirmed by the compiler error, not assumed.

- [ ] **Step 3: Commit**

```bash
git add CrystalReportAnalyzer/Extractors/SectionExtractor.cs
git commit -m "feat: extract section format properties including suppress formula"
```

---

### Task 3: Add `SectionDetailPanel` to `MainWindow.xaml`

**Files:**
- Modify: `CrystalReportAnalyzer/MainWindow.xaml`

**Context:** Insert a new `Border x:Name="SectionDetailPanel"` immediately after the existing `SubreportDetailPanel` Border's closing `</Border>` and before the `<!-- Estatísticas -->` comment / `StatsCard` Border, following the exact same `Grid.Row="1"` / `Style="{StaticResource Card}"` / `Visibility="Collapsed"` pattern.

- [ ] **Step 1: Insert the new panel**

Find the `SubreportDetailPanel` Border's closing `</Border>` (immediately followed by a blank line and then `<!-- Estatísticas -->` / `<Border x:Name="StatsCard"`). Insert this block right after that closing `</Border>` and before the `<!-- Estatísticas -->` comment:

```xml
                <!-- Detalhe de Seção (visível ao selecionar seção na árvore) -->
                <Border x:Name="SectionDetailPanel"
                        Grid.Row="1"
                        Style="{StaticResource Card}"
                        Visibility="Collapsed">
                    <DockPanel>
                        <StackPanel DockPanel.Dock="Top" Margin="0,0,0,10">
                            <TextBlock x:Name="SectionDetailTitle"
                                       FontSize="15" FontWeight="SemiBold" Foreground="#2C3E50"/>
                            <TextBlock x:Name="SectionDetailKind"
                                       FontSize="12" Foreground="#95A5A6" Margin="0,4,0,0"/>
                        </StackPanel>

                        <ScrollViewer VerticalScrollBarVisibility="Auto">
                            <StackPanel>
                                <TextBlock Text="Propriedades" FontSize="12" FontWeight="SemiBold"
                                           Foreground="#95A5A6" Margin="0,0,0,6"/>
                                <ItemsControl x:Name="SectionPropertiesList">
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate>
                                            <Grid Margin="0,0,0,4">
                                                <Grid.ColumnDefinitions>
                                                    <ColumnDefinition Width="220"/>
                                                    <ColumnDefinition Width="*"/>
                                                </Grid.ColumnDefinitions>
                                                <TextBlock Grid.Column="0" FontSize="12"
                                                           Foreground="#2C3E50"
                                                           Text="{Binding Label}"/>
                                                <TextBlock Grid.Column="1" FontSize="12"
                                                           FontWeight="SemiBold" Foreground="#2980B9"
                                                           Text="{Binding Value}"/>
                                            </Grid>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>

                                <StackPanel x:Name="SectionSuppressFormulaPanel" Margin="0,12,0,0"
                                            Visibility="Collapsed">
                                    <TextBlock Text="Fórmula de Suprimir" FontSize="12" FontWeight="SemiBold"
                                               Foreground="#95A5A6" Margin="0,0,0,6"/>
                                    <TextBlock x:Name="SectionSuppressFormulaText"
                                               FontFamily="Consolas, Courier New"
                                               FontSize="12"
                                               Foreground="#2C3E50"
                                               TextWrapping="Wrap"
                                               xml:space="preserve"/>
                                </StackPanel>
                            </StackPanel>
                        </ScrollViewer>
                    </DockPanel>
                </Border>

```

- [ ] **Step 2: Build to verify XAML compiles**

Run: `dotnet build CrystalReportAnalyzer.sln`
Expected: Build succeeds (no code-behind references yet, so no missing-member errors).

- [ ] **Step 3: Commit**

```bash
git add CrystalReportAnalyzer/MainWindow.xaml
git commit -m "feat: add section detail panel to MainWindow.xaml"
```

---

### Task 4: Wire up section detail panel in code-behind

**Files:**
- Modify: `CrystalReportAnalyzer/MainWindow.xaml.cs`

**Context:** `BuildSectionNodes` (`MainWindow.xaml.cs:146-156`) currently builds plain `Node(...)` leaves with no `Tag`. It must set `Tag = sec` on each node, matching the pattern already used in `BuildSubreportNodes`. `ReportTreeView_SelectedItemChanged` (`MainWindow.xaml.cs:187-196`) must add a branch for `Tag: SectionModel`. The three `Show*`/`HideAllDetailPanels` methods (`MainWindow.xaml.cs:198-238`) must each account for the new fourth panel: every `Show*` method must explicitly collapse `SectionDetailPanel` (in addition to what it already collapses), a new `ShowSectionDetail` method must explicitly collapse the other three panels plus `StatsCard`, and `HideAllDetailPanels` must also collapse `SectionDetailPanel`.

A small display row type `SectionPropertyRow(string Label, string Value)` is needed for `SectionPropertiesList.ItemsSource`, declared next to the existing `SubreportTableRow` record (`MainWindow.xaml.cs:388`).

- [ ] **Step 1: Update `BuildSectionNodes` to set `Tag`**

Replace (`MainWindow.xaml.cs:146-156`):

```csharp
        private static TreeViewItem BuildSectionNodes(ReportModel report)
        {
            var parent = Node($"Seções ({report.Sections.Count})");
            foreach (var sec in report.Sections)
            {
                string hidden = sec.IsVisible ? "" : "  [oculta]";
                parent.Items.Add(Node($"[{sec.Kind}] {sec.Name}  {sec.ObjectCount} obj{hidden}",
                                      expanded: false));
            }
            return parent;
        }
```

with:

```csharp
        private static TreeViewItem BuildSectionNodes(ReportModel report)
        {
            var parent = Node($"Seções ({report.Sections.Count})");
            foreach (var sec in report.Sections)
            {
                string hidden = sec.IsVisible ? "" : "  [oculta]";
                parent.Items.Add(new TreeViewItem
                {
                    Header = $"[{sec.Kind}] {sec.Name}  {sec.ObjectCount} obj{hidden}",
                    IsExpanded = false,
                    Tag = sec,
                });
            }
            return parent;
        }
```

- [ ] **Step 2: Update `ReportTreeView_SelectedItemChanged`**

Replace (`MainWindow.xaml.cs:187-196`):

```csharp
        private void ReportTreeView_SelectedItemChanged(object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem { Tag: FormulaModel formula })
                ShowFormulaDetail(formula);
            else if (e.NewValue is TreeViewItem { Tag: SubreportModel subreport })
                ShowSubreportDetail(subreport);
            else
                HideAllDetailPanels();
        }
```

with:

```csharp
        private void ReportTreeView_SelectedItemChanged(object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem { Tag: FormulaModel formula })
                ShowFormulaDetail(formula);
            else if (e.NewValue is TreeViewItem { Tag: SubreportModel subreport })
                ShowSubreportDetail(subreport);
            else if (e.NewValue is TreeViewItem { Tag: SectionModel section })
                ShowSectionDetail(section);
            else
                HideAllDetailPanels();
        }
```

- [ ] **Step 3: Update `ShowFormulaDetail`, `ShowSubreportDetail`, add `ShowSectionDetail`, update `HideAllDetailPanels`**

Replace (`MainWindow.xaml.cs:198-238`):

```csharp
        private void ShowFormulaDetail(FormulaModel formula)
        {
            FormulaDetailTitle.Text      = formula.Name;
            FormulaDetailReturnType.Text = formula.ReturnType;
            FormulaDetailText.Text       = formula.Text.Replace("\r", "");

            var combined = formula.ReferencedFormulas.Concat(formula.ReferencedRunningTotals).ToList();
            FormulaDbFieldsList.ItemsSource   = formula.ReferencedDbFields;
            FormulaParametersList.ItemsSource = formula.ReferencedParameters;
            FormulaReferencesList.ItemsSource = combined;

            FormulaDetailPanel.Visibility   = Visibility.Visible;
            SubreportDetailPanel.Visibility = Visibility.Collapsed;
            StatsCard.Visibility            = Visibility.Collapsed;
        }

        private void ShowSubreportDetail(SubreportModel subreport)
        {
            SubreportDetailTitle.Text     = subreport.Name;
            SubreportDetailOnDemand.Text  = subreport.IsOnDemand
                ? "Carregamento sob demanda (on-demand)"
                : "Carregado junto com o relatório principal";

            string subPrefix = CSharpCodeGenerator.ToIdentifier(subreport.Name);
            SubreportTablesList.ItemsSource = subreport.Tables.Select(table => new SubreportTableRow(
                TableName:    table.Name,
                DtoClassName: subPrefix + CSharpCodeGenerator.ToIdentifier(table.Name) + "Dto",
                FieldSummary: string.Join(", ", table.Fields.Select(f => $"{f.Name} ({f.ValueType})"))
            )).ToList();

            SubreportDetailPanel.Visibility = Visibility.Visible;
            FormulaDetailPanel.Visibility   = Visibility.Collapsed;
            StatsCard.Visibility            = Visibility.Collapsed;
        }

        private void HideAllDetailPanels()
        {
            FormulaDetailPanel.Visibility   = Visibility.Collapsed;
            SubreportDetailPanel.Visibility = Visibility.Collapsed;
            StatsCard.Visibility            = Visibility.Visible;
        }
```

with:

```csharp
        private void ShowFormulaDetail(FormulaModel formula)
        {
            FormulaDetailTitle.Text      = formula.Name;
            FormulaDetailReturnType.Text = formula.ReturnType;
            FormulaDetailText.Text       = formula.Text.Replace("\r", "");

            var combined = formula.ReferencedFormulas.Concat(formula.ReferencedRunningTotals).ToList();
            FormulaDbFieldsList.ItemsSource   = formula.ReferencedDbFields;
            FormulaParametersList.ItemsSource = formula.ReferencedParameters;
            FormulaReferencesList.ItemsSource = combined;

            FormulaDetailPanel.Visibility   = Visibility.Visible;
            SubreportDetailPanel.Visibility = Visibility.Collapsed;
            SectionDetailPanel.Visibility   = Visibility.Collapsed;
            StatsCard.Visibility            = Visibility.Collapsed;
        }

        private void ShowSubreportDetail(SubreportModel subreport)
        {
            SubreportDetailTitle.Text     = subreport.Name;
            SubreportDetailOnDemand.Text  = subreport.IsOnDemand
                ? "Carregamento sob demanda (on-demand)"
                : "Carregado junto com o relatório principal";

            string subPrefix = CSharpCodeGenerator.ToIdentifier(subreport.Name);
            SubreportTablesList.ItemsSource = subreport.Tables.Select(table => new SubreportTableRow(
                TableName:    table.Name,
                DtoClassName: subPrefix + CSharpCodeGenerator.ToIdentifier(table.Name) + "Dto",
                FieldSummary: string.Join(", ", table.Fields.Select(f => $"{f.Name} ({f.ValueType})"))
            )).ToList();

            SubreportDetailPanel.Visibility = Visibility.Visible;
            FormulaDetailPanel.Visibility   = Visibility.Collapsed;
            SectionDetailPanel.Visibility   = Visibility.Collapsed;
            StatsCard.Visibility            = Visibility.Collapsed;
        }

        private void ShowSectionDetail(SectionModel section)
        {
            SectionDetailTitle.Text = section.Name;
            SectionDetailKind.Text  = $"Tipo: {section.Kind}";

            SectionPropertiesList.ItemsSource = new List<SectionPropertyRow>
            {
                new("Suprimir",                    YesNo(!section.IsVisible)),
                new("Suprimir se em branco",        YesNo(section.SuppressIfBlank)),
                new("Nova página antes",            YesNo(section.NewPageBefore)),
                new("Nova página depois",           YesNo(section.NewPageAfter)),
                new("Reiniciar número de página",   YesNo(section.ResetPageNumberAfter)),
                new("Manter juntos",                YesNo(section.KeepTogether)),
                new("Sublinhar seções seguintes",   YesNo(section.UnderlayFollowingSections)),
            };

            bool hasSuppressFormula = !string.IsNullOrWhiteSpace(section.SuppressFormulaText);
            SectionSuppressFormulaPanel.Visibility = hasSuppressFormula ? Visibility.Visible : Visibility.Collapsed;
            SectionSuppressFormulaText.Text        = hasSuppressFormula
                ? section.SuppressFormulaText.Replace("\r", "")
                : string.Empty;

            SectionDetailPanel.Visibility    = Visibility.Visible;
            FormulaDetailPanel.Visibility    = Visibility.Collapsed;
            SubreportDetailPanel.Visibility  = Visibility.Collapsed;
            StatsCard.Visibility             = Visibility.Collapsed;
        }

        private static string YesNo(bool value) => value ? "Sim" : "Não";

        private void HideAllDetailPanels()
        {
            FormulaDetailPanel.Visibility   = Visibility.Collapsed;
            SubreportDetailPanel.Visibility = Visibility.Collapsed;
            SectionDetailPanel.Visibility   = Visibility.Collapsed;
            StatsCard.Visibility            = Visibility.Visible;
        }
```

- [ ] **Step 4: Add the `SectionPropertyRow` record**

Next to the existing `private record SubreportTableRow(string TableName, string DtoClassName, string FieldSummary);` (`MainWindow.xaml.cs:388`), add:

```csharp
        private record SectionPropertyRow(string Label, string Value);
```

- [ ] **Step 5: Build to verify it compiles**

Run: `dotnet build CrystalReportAnalyzer.sln`
Expected: Build succeeds with no errors. If any XAML-named element (`SectionDetailPanel`, `SectionDetailTitle`, `SectionDetailKind`, `SectionPropertiesList`, `SectionSuppressFormulaPanel`, `SectionSuppressFormulaText`) is reported missing, confirm Task 3's XAML was committed correctly before proceeding.

- [ ] **Step 6: Commit**

```bash
git add CrystalReportAnalyzer/MainWindow.xaml.cs
git commit -m "feat: show section format properties on tree selection"
```

---

### Task 5: Manual verification

**Files:** none (manual QA only)

- [ ] **Step 1: Run the app**

Run: `dotnet run --project CrystalReportAnalyzer/CrystalReportAnalyzer.csproj`
(Requires Windows + the Crystal Reports runtime DLLs already in `CrystalReportAnalyzer/libs/`, per `CLAUDE.md`.)

- [ ] **Step 2: Open a `.rpt` file with at least one section that has a conditional suppress formula, and one with a fixed new-page-before flag set**

Expected: the tree shows a "Seções (N)" node; expanding it lists each section by kind/name/object count/visibility, as before.

- [ ] **Step 3: Click a section node**

Expected: the stats panel is replaced by the new section detail panel showing the section name/kind and a property list (Suprimir, Suprimir se em branco, Nova página antes/depois, Reiniciar número de página, Manter juntos, Sublinhar seções seguintes) each as Sim/Não.

- [ ] **Step 4: Click a section known to have a conditional suppress formula**

Expected: a "Fórmula de Suprimir" block appears below the property list showing the formula text. For a section without a conditional suppress formula, this block should not appear.

- [ ] **Step 5: Click a formula node, then a subreport node, then a section node, then empty space**

Expected: only one detail panel is visible at a time in each case; clicking away from all three restores the stats panel (`HideAllDetailPanels` behavior, now covering four panels total).

- [ ] **Step 6: If no `.rpt` with a conditional suppress formula is available, note this limitation to the user**

If no such test file exists, state explicitly that the conditional-formula path was not visually verified and ask the user to confirm manually — do not claim the feature works without having seen it run.

---

## Self-Review Notes

- Spec coverage: `SectionModel` new fields (Task 1), `SectionExtractor` reading `SectionFormat` flags + `ConditionFormula` (Task 2), XAML panel mirroring existing pattern (Task 3), tree `Tag` + four-way mutual-exclusion panel wiring + `SectionPropertyRow` (Task 4), manual verification (Task 5) — all spec sections covered.
- No placeholders: every step has full code.
- Type consistency verified: `SectionModel`'s new properties are used identically in `SectionExtractor` and `MainWindow.xaml.cs` (`ShowSectionDetail`); `SectionPropertyRow(Label, Value)` matches the XAML bindings `{Binding Label}`/`{Binding Value}` in Task 3's `SectionPropertiesList` template.
- Existing mutual-exclusion invariant (each `Show*` method explicitly sets all sibling panels' visibility) is preserved and extended to four panels consistently across `ShowFormulaDetail`, `ShowSubreportDetail`, `ShowSectionDetail`, and `HideAllDetailPanels`.
