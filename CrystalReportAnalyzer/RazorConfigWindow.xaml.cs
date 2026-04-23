using System.Windows;
using CrystalReportAnalyzer.Core.Services.RazorGeneration;

namespace CrystalReportAnalyzer;

public partial class RazorConfigWindow : Window
{
    public RazorGenerationConfig? Result { get; private set; }
    public bool IncludeSubreports => IncludeSubreportsCheck.IsChecked == true;

    public RazorConfigWindow(RazorGenerationConfig defaults)
    {
        InitializeComponent();
        NamespaceBox.Text         = defaults.BaseNamespace;
        LayoutPathBox.Text        = defaults.LayoutPath;
        ReportLayoutPathBox.Text  = defaults.ReportLayoutPath;
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NamespaceBox.Text))
        {
            MessageBox.Show("O namespace não pode ficar vazio.", "Validação",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new RazorGenerationConfig
        {
            BaseNamespace    = NamespaceBox.Text.Trim(),
            LayoutPath       = LayoutPathBox.Text.Trim(),
            ReportLayoutPath = ReportLayoutPathBox.Text.Trim(),
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
