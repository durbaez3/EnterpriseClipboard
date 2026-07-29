using System.Windows;
using EnterpriseClipboard.App.ViewModels;

namespace EnterpriseClipboard.App;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public async void ShowSettings()
    {
        await _viewModel.LoadSettingsAsync();
        ShowDialog(); // Show as modal dialog
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
