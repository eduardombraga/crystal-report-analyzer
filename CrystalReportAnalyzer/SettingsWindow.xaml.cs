using System.Collections.ObjectModel;
using System.Windows;
using CrystalReportAnalyzer.Core.Configuration;

namespace CrystalReportAnalyzer;

public class ConfigRow
{
    public string Name  { get; set; } = string.Empty;
    public int    Value { get; set; }
}

public partial class SettingsWindow : Window
{
    private readonly ObservableCollection<ConfigRow> _weights    = new();
    private readonly ObservableCollection<ConfigRow> _thresholds = new();

    /// <summary>The saved config — read by MainWindow after ShowDialog() returns true.</summary>
    public ScoringConfig? Result { get; private set; }

    public SettingsWindow(ScoringConfig current)
    {
        InitializeComponent();

        foreach (var kv in current.Weights)
            _weights.Add(new ConfigRow { Name = kv.Key, Value = kv.Value });

        foreach (var kv in current.Thresholds)
            _thresholds.Add(new ConfigRow { Name = kv.Key, Value = kv.Value });

        WeightsGrid.ItemsSource    = _weights;
        ThresholdsGrid.ItemsSource = _thresholds;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Commit any in-progress cell edit
        WeightsGrid.CommitEdit();
        ThresholdsGrid.CommitEdit();

        if (!AllValuesPositive(_weights, "pesos") ||
            !AllValuesPositive(_thresholds, "limites"))
            return;

        var config = new ScoringConfig
        {
            Weights    = _weights.ToDictionary(r => r.Name, r => r.Value),
            Thresholds = _thresholds.ToDictionary(r => r.Name, r => r.Value),
        };

        ScoringConfigService.Save(config);
        Result = config;
        DialogResult = true;
    }

    private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
    {
        var defaults = ScoringConfig.Default();

        _weights.Clear();
        foreach (var kv in defaults.Weights)
            _weights.Add(new ConfigRow { Name = kv.Key, Value = kv.Value });

        _thresholds.Clear();
        foreach (var kv in defaults.Thresholds)
            _thresholds.Add(new ConfigRow { Name = kv.Key, Value = kv.Value });

        ScoringConfigService.Save(defaults);
        Result = defaults;
        DialogResult = true;
    }

    private bool AllValuesPositive(IEnumerable<ConfigRow> rows, string label)
    {
        if (rows.Any(r => r.Value < 0))
        {
            MessageBox.Show($"Todos os {label} devem ser maiores ou iguais a zero.",
                            "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }
}
