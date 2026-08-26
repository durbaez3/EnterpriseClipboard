using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using EnterpriseClipboard.App.ViewModels;
using EnterpriseClipboard.Domain.Entities;

namespace EnterpriseClipboard.App;

public partial class QuickPopup : Window
{
    private readonly QuickPopupViewModel _viewModel;

    public QuickPopup(QuickPopupViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public async void ShowAtCursor()
    {
        // 1. Center on active monitor (where cursor is located) with DPI-awareness
        var point = System.Windows.Forms.Control.MousePosition;
        var screen = System.Windows.Forms.Screen.FromPoint(point);
        var area = screen.WorkingArea;

        double dpiX = 1.0;
        double dpiY = 1.0;
        var presentationSource = PresentationSource.FromVisual(this);
        if (presentationSource?.CompositionTarget != null)
        {
            dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
            dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
        }

        // Center calculation converting pixels to logical WPF units
        Left = (area.Left + (area.Width - (Width * dpiX)) / 2) / dpiX;
        Top = (area.Top + (area.Height - (Height * dpiY)) / 2) / dpiY;

        // 2. Reset search and load latest items so popup always shows fresh results
        _viewModel.SearchText = string.Empty;
        _ = _viewModel.LoadItemsAsync();

        // 3. Show and focus
        Show();
        Activate();
        
        // Use dispatcher to ensure UI elements are drawn before focusing
        Dispatcher.BeginInvoke(new Action(() =>
        {
            SearchBox.Focus();
            Keyboard.Focus(SearchBox);
        }), DispatcherPriority.Input);
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Reset search so next open shows all items fresh
        _viewModel.SearchText = string.Empty;
        Hide();
    }

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        SearchBox.SelectAll();
    }

    private async void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            // Move selection down
            if (ClipsList.SelectedIndex < ClipsList.Items.Count - 1)
            {
                ClipsList.SelectedIndex++;
                ClipsList.ScrollIntoView(ClipsList.SelectedItem);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            // Move selection up
            if (ClipsList.SelectedIndex > 0)
            {
                ClipsList.SelectedIndex--;
                ClipsList.ScrollIntoView(ClipsList.SelectedItem);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            // Paste selected item
            if (ClipsList.SelectedItem != null)
            {
                await _viewModel.PasteItemAsync(_viewModel.SelectedItem);
            }
            e.Handled = true;
        }
    }

    private async void ClipsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ClipsList.SelectedItem is ClipboardItem item)
        {
            await _viewModel.PasteItemAsync(item);
        }
    }
}
