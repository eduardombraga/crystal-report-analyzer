# Subreport Database Detail Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clicking a subreport node in the report tree shows its database tables/fields and the name(s) of the C# DTO class(es) generated for it.

**Architecture:** Extend `SubreportModel` with a `Tables` list populated by opening each subreport's own `ReportDocument` (via `SubreportObject.OpenSubreport`) and running the existing `DatabaseExtractor` against it. Extend `CSharpCodeGenerator.GenerateDtos` to emit DTOs for subreport tables too, prefixed by subreport name. Extend the WPF tree/detail-panel UI to show subreport tables and DTO names on selection, mirroring the existing formula detail panel.

**Tech Stack:** C# / .NET Framework 4.8, WPF, Crystal Reports Engine SDK (`CrystalDecisions.CrystalReports.Engine`).

**No automated tests:** this project has none (per `CLAUDE.md`) and the Crystal Reports SDK requires a licensed runtime + real `.rpt` files, so there is no practical unit-test harness. Verification is `dotnet build` after each code task, plus a manual UI checklist in the final task.

---

### Task 1: Add `Tables` to `SubreportModel`

**Files:**
- Modify: `CrystalReportAnalyzer/Core/Models/SubreportModel.cs`

- [ ] **Step 1: Add the `Tables` property**

Replace the full file contents with:

```csharp
namespace CrystalReportAnalyzer.Core.Models;

public class SubreportModel
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsOnDemand { get; set; }
    public List<TableModel> Tables { get; set; } = new();
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build CrystalReportAnalyzer.sln`
Expected: Build succeeds (the new property is unused so far, which is fine).

- [ ] **Step 3: Commit**

```bash
git add CrystalReportAnalyzer/Core/Models/SubreportModel.cs
git commit -m "feat: add Tables property to SubreportModel"
```

---

### Task 2: Populate subreport tables in `SubreportExtractor`

**Files:**
- Modify: `CrystalReportAnalyzer/Extractors/SubreportExtractor.cs`

**Context:** `SubreportObject` (from `CrystalDecisions.CrystalReports.Engine`) has an `OpenSubreport(string name)` method returning a `ReportDocument` for that subreport, whose `.Database.Tables` works exactly like the main report's. `DatabaseExtractor.Extract(ReportDocument doc)` already knows how to turn that into `List<TableModel>` — reuse it directly. Opening a subreport can throw (e.g. broken/unbound subreport link in a malformed `.rpt`), so each open is wrapped in try/catch and simply leaves `Tables` empty on failure rather than failing the whole analysis.

- [ ] **Step 1: Update the extractor to open each subreport and extract its tables**

Replace the full file contents with:

```csharp
using CrystalDecisions.CrystalReports.Engine;
using CrystalReportAnalyzer.Core.Models;

namespace CrystalReportAnalyzer.Extractors;

public static class SubreportExtractor
{
    public static List<SubreportModel> Extract(ReportDocument doc)
    {
        var subreports = new List<SubreportModel>();
        var seen       = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Section section in doc.ReportDefinition.Sections)
        {
            foreach (ReportObject obj in section.ReportObjects)
            {
                if (obj is not SubreportObject sub) continue;
                if (!seen.Add(sub.SubreportName))   continue; // deduplicate

                var model = new SubreportModel
                {
                    Name       = sub.SubreportName,
                    Location   = section.Name,
                    IsOnDemand = sub.EnableOnDemand,
                };

                try
                {
                    var subDoc = sub.OpenSubreport(sub.SubreportName);
                    model.Tables = DatabaseExtractor.Extract(subDoc);
                }
                catch (Exception)
                {
                    // Broken/unbound subreport link — leave Tables empty.
                }

                subreports.Add(model);
            }
        }

        return subreports;
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build CrystalReportAnalyzer.sln`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add CrystalReportAnalyzer/Extractors/SubreportExtractor.cs
git commit -m "feat: extract subreport tables via OpenSubreport"
```

---

### Task 3: Generate DTOs for subreport tables

**Files:**
- Modify: `CrystalReportAnalyzer/Core/Services/CSharpCodeGenerator.cs:13-41` (`GenerateDtos`)

**Context:** `GenerateDtos` currently only iterates `report.Tables`. It must also iterate `report.Subreports[*].Tables`, naming each subreport DTO class `{SubreportIdentifier}{TableIdentifier}Dto` (using the existing private `ToIdentifier` helper) so it can't collide with a main-report DTO of the same table name.

- [ ] **Step 1: Extend `GenerateDtos`**

Replace the `GenerateDtos` method (currently lines 13-41) with:

```csharp
    public static string GenerateDtos(ReportModel report)
    {
        bool hasMainTables = report.Tables.Count > 0;
        bool hasSubTables  = report.Subreports.Any(s => s.Tables.Count > 0);
        if (!hasMainTables && !hasSubTables) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"// DTOs gerados a partir do relatório: {report.Name}");
        sb.AppendLine($"// Gerado em: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("namespace GeneratedFrom" + ToIdentifier(report.Name) + ";");
        sb.AppendLine();

        foreach (var table in report.Tables)
            AppendDtoClass(sb, ToIdentifier(table.Name) + "Dto",
                $"DTO para a tabela/view '{table.Name}'", table.Fields);

        foreach (var sub in report.Subreports)
        {
            string subPrefix = ToIdentifier(sub.Name);
            foreach (var table in sub.Tables)
                AppendDtoClass(sb, subPrefix + ToIdentifier(table.Name) + "Dto",
                    $"DTO para a tabela/view '{table.Name}' do sub-relatório '{sub.Name}'", table.Fields);
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendDtoClass(StringBuilder sb, string className, string summary,
        List<FieldModel> fields)
    {
        sb.AppendLine($"/// <summary>{summary}</summary>");
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");
        foreach (var field in fields)
        {
            string propType = MapToCSharpType(field.ValueType);
            string propName = ToIdentifier(field.Name);
            sb.AppendLine($"    public {propType} {propName} {{ get; set; }}");
        }
        sb.AppendLine("}");
        sb.AppendLine();
    }
```

This requires `using System.Linq;` — check the top of the file and add it if missing.

- [ ] **Step 2: Verify/add the `System.Linq` using directive**

Read `CrystalReportAnalyzer/Core/Services/CSharpCodeGenerator.cs:1-4` — if `using System.Linq;` is not already present, add it alongside the existing `using System.Text;`.

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build CrystalReportAnalyzer.sln`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add CrystalReportAnalyzer/Core/Services/CSharpCodeGenerator.cs
git commit -m "feat: generate DTOs for subreport tables"
```

---

### Task 4: Add subreport detail panel to `MainWindow.xaml`

**Files:**
- Modify: `CrystalReportAnalyzer/MainWindow.xaml`

**Context:** Insert a new `Border x:Name="SubreportDetailPanel"` immediately after the existing `FormulaDetailPanel` (which ends at line 334) and before `StatsCard` (line 337), following the exact same `Grid.Row="1"` / `Style="{StaticResource Card}"` / `Visibility="Collapsed"` pattern so it occupies the same slot and toggles the same way.

- [ ] **Step 1: Insert the new panel**

After the closing `</Border>` of `FormulaDetailPanel` (the line right after line 334, `</Border>`, and before the `<!-- Estatísticas -->` comment on line 336), insert:

```xml
                <!-- Detalhe de Sub-Relatório (visível ao selecionar sub-relatório na árvore) -->
                <Border x:Name="SubreportDetailPanel"
                        Grid.Row="1"
                        Style="{StaticResource Card}"
                        Visibility="Collapsed">
                    <DockPanel>
                        <StackPanel DockPanel.Dock="Top" Margin="0,0,0,10">
                            <TextBlock x:Name="SubreportDetailTitle"
                                       FontSize="15" FontWeight="SemiBold" Foreground="#2C3E50"/>
                            <TextBlock x:Name="SubreportDetailOnDemand"
                                       FontSize="12" Foreground="#95A5A6" Margin="0,4,0,0"/>
                        </StackPanel>

                        <ScrollViewer VerticalScrollBarVisibility="Auto">
                            <StackPanel>
                                <TextBlock Text="Tabelas / DTOs gerados" FontSize="12" FontWeight="SemiBold"
                                           Foreground="#95A5A6" Margin="0,0,0,6"/>
                                <ItemsControl x:Name="SubreportTablesList">
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate>
                                            <Border BorderBrush="#ECF0F1" BorderThickness="0,0,0,1"
                                                    Padding="2,8">
                                                <StackPanel>
                                                    <TextBlock FontSize="13" FontWeight="SemiBold"
                                                               Foreground="#2C3E50"
                                                               Text="{Binding TableName}"/>
                                                    <TextBlock FontSize="11" Foreground="#2980B9"
                                                               Text="{Binding DtoClassName}"/>
                                                    <TextBlock FontSize="11" Foreground="#95A5A6"
                                                               Text="{Binding FieldSummary}"
                                                               TextWrapping="Wrap"/>
                                                </StackPanel>
                                            </Border>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
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
git commit -m "feat: add subreport detail panel to MainWindow.xaml"
```

---

### Task 5: Wire up subreport detail panel in code-behind

**Files:**
- Modify: `CrystalReportAnalyzer/MainWindow.xaml.cs`

**Context:** Currently `BuildReportTree` builds subreport leaf nodes via `BuildLeafNodes` (`MainWindow.xaml.cs:128-129`), which produces plain string leaves with no `Tag`. This must become a dedicated builder (mirroring `BuildFormulaNodes`) that sets `Tag = SubreportModel` on each node. `ReportTreeView_SelectedItemChanged` (`MainWindow.xaml.cs:172-179`) must add a branch for `Tag: SubreportModel`. `HideFormulaDetail`/`ShowFormulaDetail` (`MainWindow.xaml.cs:181-200`) must be joined by parallel `ShowSubreportDetail`/`HideSubreportDetail`, and both hide paths must hide the other panel too so only one detail panel is visible at a time.

A small display row type is needed for `SubreportTablesList.ItemsSource` — add a `private record SubreportTableRow(string TableName, string DtoClassName, string FieldSummary);` near the other private helpers in the file (next to `StatItem`, if present — check for it; otherwise place it near `ShowFormulaDetail`).

- [ ] **Step 1: Check for an existing `StatItem` record to place `SubreportTableRow` near**

Run (ripgrep via Grep tool, not bash) to confirm `StatItem`'s location in `CrystalReportAnalyzer/MainWindow.xaml.cs`, so `SubreportTableRow` is added in the same declarations area.

- [ ] **Step 2: Replace the subreport tree node line and update `BuildReportTree`**

In `BuildReportTree` (`MainWindow.xaml.cs:114-132`), replace:

```csharp
            root.Items.Add(BuildLeafNodes($"Sub-Relatórios ({report.Subreports.Count})",
                report.Subreports.Select(s => s.IsOnDemand ? $"{s.Name}  [on-demand]" : s.Name)));
```

with:

```csharp
            root.Items.Add(BuildSubreportNodes(report));
```

- [ ] **Step 3: Add `BuildSubreportNodes` next to `BuildFormulaNodes`**

Immediately after the `BuildFormulaNodes` method (`MainWindow.xaml.cs:159-170`), add:

```csharp
        private static TreeViewItem BuildSubreportNodes(ReportModel report)
        {
            var parent = Node($"Sub-Relatórios ({report.Subreports.Count})");
            foreach (var sub in report.Subreports)
            {
                string label = sub.IsOnDemand ? $"{sub.Name}  [on-demand]" : sub.Name;
                parent.Items.Add(new TreeViewItem
                {
                    Header = label,
                    IsExpanded = false,
                    Tag = sub,
                });
            }
            return parent;
        }
```

- [ ] **Step 4: Update `ReportTreeView_SelectedItemChanged`**

Replace (`MainWindow.xaml.cs:172-179`):

```csharp
        private void ReportTreeView_SelectedItemChanged(object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem { Tag: FormulaModel formula })
                ShowFormulaDetail(formula);
            else
                HideFormulaDetail();
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
            else
                HideAllDetailPanels();
        }
```

- [ ] **Step 5: Replace `ShowFormulaDetail`/`HideFormulaDetail` with panel-aware versions**

Replace (`MainWindow.xaml.cs:181-200`):

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

            FormulaDetailPanel.Visibility = Visibility.Visible;
            StatsCard.Visibility          = Visibility.Collapsed;
        }

        private void HideFormulaDetail()
        {
            FormulaDetailPanel.Visibility = Visibility.Collapsed;
            StatsCard.Visibility          = Visibility.Visible;
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
            StatsCard.Visibility            = Visibility.Collapsed;
        }

        private void ShowSubreportDetail(SubreportModel subreport)
        {
            SubreportDetailTitle.Text     = subreport.Name;
            SubreportDetailOnDemand.Text  = subreport.IsOnDemand
                ? "Carregamento sob demanda (on-demand)"
                : "Carregado junto com o relatório principal";

            string subPrefix = ToPascalIdentifier(subreport.Name);
            SubreportTablesList.ItemsSource = subreport.Tables.Select(table => new SubreportTableRow(
                TableName:    table.Name,
                DtoClassName: subPrefix + ToPascalIdentifier(table.Name) + "Dto",
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

- [ ] **Step 6: Add the `ToPascalIdentifier` helper and `SubreportTableRow` record**

`CSharpCodeGenerator.ToIdentifier` is private, so the UI needs its own equivalent to build matching DTO names for display. Near `HideAllDetailPanels` (or wherever the file's other small private helpers/records live — check for `StatItem` from Step 1 and place both declarations in the same area), add:

```csharp
        private static string ToPascalIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unknown";

            name = name.TrimStart('@').Trim();
            var sb = new System.Text.StringBuilder();
            bool capitalizeNext = true;

            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(capitalizeNext ? char.ToUpper(c) : c);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }

            string result = sb.ToString();
            if (result.Length == 0) return "Unknown";
            if (char.IsDigit(result[0])) result = "_" + result;
            return result;
        }

        private record SubreportTableRow(string TableName, string DtoClassName, string FieldSummary);
```

- [ ] **Step 7: Check every other caller of `HideFormulaDetail`**

Run (Grep tool) for `HideFormulaDetail` across `CrystalReportAnalyzer/MainWindow.xaml.cs`. `BuildReportTree` (`MainWindow.xaml.cs:116`) calls `HideFormulaDetail();` at the start — replace that call with `HideAllDetailPanels();` since `HideFormulaDetail` no longer exists.

- [ ] **Step 8: Build to verify it compiles**

Run: `dotnet build CrystalReportAnalyzer.sln`
Expected: Build succeeds with no errors. Fix any remaining reference to the removed `HideFormulaDetail` method if the build reports one.

- [ ] **Step 9: Commit**

```bash
git add CrystalReportAnalyzer/MainWindow.xaml.cs
git commit -m "feat: show subreport tables and DTO names on tree selection"
```

---

### Task 6: Manual verification

**Files:** none (manual QA only)

- [ ] **Step 1: Run the app**

Run: `dotnet run --project CrystalReportAnalyzer/CrystalReportAnalyzer.csproj`
(Requires Windows + the Crystal Reports runtime DLLs already in `CrystalReportAnalyzer/libs/`, per `CLAUDE.md`.)

- [ ] **Step 2: Open or drop a `.rpt` file that contains at least one subreport with its own database table**

Expected: the tree shows a "Sub-Relatórios (N)" node; expanding it lists each subreport by name (with `[on-demand]` suffix where applicable).

- [ ] **Step 3: Click a subreport node**

Expected: the stats panel is replaced by the new subreport detail panel showing the subreport name, on-demand status, and a list of its tables — each with a DTO class name (e.g. `{Subreport}{Table}Dto`) and a field summary.

- [ ] **Step 4: Click a formula node, then click empty space / the root node**

Expected: formula detail panel still works as before; clicking away from both formula and subreport nodes restores the stats panel (`HideAllDetailPanels` behavior).

- [ ] **Step 5: Export dependencies / generated code (if the report has subreport tables) and confirm the subreport DTO classes appear**

Use the existing export button; open the generated `.cs` file and confirm classes named `{Subreport}{Table}Dto` are present alongside the main report's DTOs.

- [ ] **Step 6: If a `.rpt` with a subreport is not available, note this limitation to the user**

If no such test file exists, state explicitly that visual/functional verification was not possible and ask the user to confirm manually — do not claim the feature works without having seen it run.

---

## Self-Review Notes

- Spec coverage: `SubreportModel.Tables` (Task 1), `SubreportExtractor` via `OpenSubreport` (Task 2), `CSharpCodeGenerator.GenerateDtos` subreport DTOs with prefixed names (Task 3), UI tree node `Tag = SubreportModel` + detail panel mirroring formula panel + single-panel-visible invariant (Tasks 4–5), manual verification (Task 6) — all spec sections covered.
- No placeholders: every step has full code.
- Type consistency verified: `SubreportModel.Tables` (List&lt;TableModel&gt;) used identically in `SubreportExtractor`, `CSharpCodeGenerator`, and `MainWindow.xaml.cs`; `ToIdentifier` (generator, private) vs `ToPascalIdentifier` (UI, private) are intentionally separate copies since the UI can't call the generator's private method — both implement the same transform so displayed DTO names match generated ones.
