using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using CrystalReportAnalyzer.Core.Configuration;
using CrystalReportAnalyzer.Core.Models;
using System.Windows.Controls.Primitives;
using CrystalReportAnalyzer.Core.Services;
using CrystalReportAnalyzer.Core.Services.RazorGeneration;
using CrystalReportAnalyzer.Exporters;
using System.IO;
using WinForms = System.Windows.Forms;

namespace CrystalReportAnalyzer
{
    public partial class MainWindow : Window
    {
        private readonly ReportAnalysisService _service = new();
        private ReportModel? _currentReport;
        private ScoringConfig _config = ScoringConfigService.Load();

        // ── Complexity level colors ──────────────────────────────────────
        private static readonly Dictionary<ComplexityLevel, (string Hex, string Range)> LevelMeta = new()
        {
            [ComplexityLevel.Simple]      = ("#27AE60", "0 – 10"),
            [ComplexityLevel.Medium]      = ("#F39C12", "11 – 20"),
            [ComplexityLevel.Complex]     = ("#E74C3C", "21 – 40"),
            [ComplexityLevel.VeryComplex] = ("#8E44AD", "40+"),
        };

        public MainWindow() => InitializeComponent();

        // ── File opening ─────────────────────────────────────────────────

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title  = "Selecionar Relatório Crystal Reports",
                Filter = "Crystal Reports (*.rpt)|*.rpt",
            };

            if (dialog.ShowDialog() == true)
                LoadReport(dialog.FileName);
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) &&
                        ((string[])e.Data.GetData(DataFormats.FileDrop))
                            .Any(f => f.EndsWith(".rpt", StringComparison.OrdinalIgnoreCase))
                        ? DragDropEffects.Copy
                        : DragDropEffects.None;

            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var rpt = ((string[])e.Data.GetData(DataFormats.FileDrop))
                          .FirstOrDefault(f => f.EndsWith(".rpt", StringComparison.OrdinalIgnoreCase));

            if (rpt != null) LoadReport(rpt);
        }

        // ── Settings ─────────────────────────────────────────────────────

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var window = new SettingsWindow(_config) { Owner = this };
            if (window.ShowDialog() == true && window.Result is not null)
            {
                _config = window.Result;
                if (_currentReport is not null)
                    LoadReport(_currentReport.FilePath);
            }
        }

        // ── Core: load & analyse ─────────────────────────────────────────

        private void LoadReport(string filePath)
        {
            try
            {
                SetStatus($"Analisando: {Path.GetFileName(filePath)} …");
                FilePathText.Text = filePath;
                Cursor = Cursors.Wait;

                _currentReport = _service.Analyze(filePath, _config);

                BuildReportTree(_currentReport);
                DisplayComplexity(_currentReport.Complexity!);
                DisplayStats(_currentReport);
                DisplayDependencies(_currentReport);
                ExportButton.IsEnabled = true;
                GenerateRazorButton.IsEnabled = true;

                SetStatus($"Análise concluída: {_currentReport.Name}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao analisar o relatório:\n\n{ex.Message}",
                                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Erro ao analisar o relatório.");
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        // ── Tree building ────────────────────────────────────────────────

        private void BuildReportTree(ReportModel report)
        {
            ReportTreeView.Items.Clear();

            var root = Node($"Relatório: {report.Name}", expanded: true);

            root.Items.Add(BuildTableNodes(report));
            root.Items.Add(BuildLeafNodes($"Fórmulas ({report.Formulas.Count})",
                report.Formulas.Select(f => $"{f.Name}  [{f.ReturnType}]")));
            root.Items.Add(BuildLeafNodes($"Parâmetros ({report.Parameters.Count})",
                report.Parameters.Select(p => $"? {p.Name}  [{p.ValueType}]")));
            root.Items.Add(BuildLeafNodes($"Grupos ({report.Groups.Count})",
                report.Groups.Select(g => $"» {g.ConditionField}")));
            root.Items.Add(BuildSectionNodes(report));
            root.Items.Add(BuildLeafNodes($"Sub-Relatórios ({report.Subreports.Count})",
                report.Subreports.Select(s => s.IsOnDemand ? $"{s.Name}  [on-demand]" : s.Name)));

            ReportTreeView.Items.Add(root);
        }

        private static TreeViewItem BuildTableNodes(ReportModel report)
        {
            var parent = Node($"Tabelas ({report.Tables.Count})");
            foreach (var table in report.Tables)
            {
                var tableNode = Node($"{table.Name}  ({table.Fields.Count} campos)");
                foreach (var field in table.Fields)
                    tableNode.Items.Add(Node($"  {field.Name}  [{field.ValueType}]", expanded: false));
                parent.Items.Add(tableNode);
            }
            return parent;
        }

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

        private static TreeViewItem BuildLeafNodes(string header, IEnumerable<string> items)
        {
            var parent = Node(header);
            foreach (var item in items)
                parent.Items.Add(Node(item, expanded: false));
            return parent;
        }

        private static TreeViewItem Node(string header, bool expanded = true) =>
            new() { Header = header, IsExpanded = expanded };

        // ── Complexity display ───────────────────────────────────────────

        private void DisplayComplexity(ComplexityResult result)
        {
            var (hex, range) = LevelMeta.TryGetValue(result.Level, out var meta)
                               ? meta
                               : ("#95A5A6", "");

            var color = (Color)ColorConverter.ConvertFromString(hex);
            var brush  = new SolidColorBrush(color);

            ScoreText.Text        = result.Score.ToString();
            ScoreText.Foreground  = brush;
            LevelText.Text        = result.LevelDescription;
            LevelBadge.Background = brush;
            ScoreRange.Text       = $"Faixa: {range}";

            RebuildBreakdown(result.Breakdown);
        }

        private void RebuildBreakdown(Dictionary<string, int> breakdown)
        {
            BreakdownGrid.Children.Clear();
            BreakdownGrid.RowDefinitions.Clear();

            var items = breakdown.ToList();
            for (int i = 0; i < items.Count; i += 2)
            {
                BreakdownGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                int row = BreakdownGrid.RowDefinitions.Count - 1;
                AddBreakdownCell(BreakdownGrid, items[i].Key, items[i].Value, row, col: 0);
                if (i + 1 < items.Count)
                    AddBreakdownCell(BreakdownGrid, items[i + 1].Key, items[i + 1].Value, row, col: 4);
            }
        }

        private static void AddBreakdownCell(Grid grid, string key, int value, int row, int col)
        {
            var label = new TextBlock
            {
                Text              = key + ":",
                FontSize          = 12,
                Foreground        = new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 2, 0, 2),
            };
            Grid.SetRow(label, row);
            Grid.SetColumn(label, col);
            grid.Children.Add(label);

            var val = new TextBlock
            {
                Text                = value.ToString(),
                FontSize            = 12,
                FontWeight          = FontWeights.Bold,
                Foreground          = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment   = VerticalAlignment.Center,
            };
            Grid.SetRow(val, row);
            Grid.SetColumn(val, col + 2);
            grid.Children.Add(val);
        }

        // ── Statistics panel ─────────────────────────────────────────────

        private void DisplayStats(ReportModel r)
        {
            StatsPanel.ItemsSource = new StatItem[]
            {
                new("Relatório",       r.Name),
                new("Tabelas",         r.Tables.Count.ToString()),
                new("Campos (total)",  r.Fields.Count.ToString()),
                new("Fórmulas",        r.Formulas.Count.ToString()),
                new("Parâmetros",      r.Parameters.Count.ToString()),
                new("Grupos",          r.Groups.Count.ToString()),
                new("Seções",          r.Sections.Count.ToString()),
                new("Sub-Relatórios",  r.Subreports.Count.ToString()),
                new("Score",           r.Complexity!.Score.ToString()),
                new("Classificação",   r.Complexity.LevelDescription),
            };
        }

        // ── Razor generation ─────────────────────────────────────────────

        private void GenerateRazor_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReport is null) return;

            using var dialog = new WinForms.FolderBrowserDialog
            {
                Description         = "Selecione a pasta de destino para os arquivos .cshtml",
                ShowNewFolderButton = true,
            };

            if (dialog.ShowDialog() != WinForms.DialogResult.OK) return;

            try
            {
                var config = new RazorGenerationConfig
                {
                    OutputDirectory = dialog.SelectedPath,
                };

                var service = new RazorGenerationService();
                var result  = service.Generate(_currentReport, config);

                SetStatus($"Gerado: {result.GeneratedFiles.Count} arquivo(s) em {dialog.SelectedPath}");
                System.Windows.MessageBox.Show(
                    $"{result.GeneratedFiles.Count} arquivo(s) gerado(s) com sucesso!\n\n" +
                    string.Join("\n", result.GeneratedFiles.Select(Path.GetFileName)),
                    "Geração .cshtml",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao gerar .cshtml:\n{ex.Message}", "Erro",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Export ───────────────────────────────────────────────────────

        private void ExportJson_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReport is null) return;

            var dialog = new SaveFileDialog
            {
                Title    = "Exportar para JSON",
                Filter   = "JSON (*.json)|*.json",
                FileName = _currentReport.Name + "_analysis.json",
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                JsonExporter.ExportToFile(_currentReport, dialog.FileName);
                SetStatus($"Exportado: {dialog.FileName}");
                MessageBox.Show("Arquivo exportado com sucesso!", "Exportação",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar:\n{ex.Message}", "Erro",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Dependencies tab ─────────────────────────────────────────

        private void DisplayDependencies(ReportModel report)
        {
            DependenciesListView.ItemsSource = report.Dependencies?.DbObjects;
        }

        private void SetStatus(string message) => StatusText.Text = message;
    }

    // Simple DTO for the statistics ItemsControl binding
    public record StatItem(string Label, string Value);
}
