using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EnterpriseClipboard.App.Helpers;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.Domain.Entities;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace EnterpriseClipboard.App.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    private readonly IClipboardRepository _repository;
    private readonly IClipboardCaptureService _captureService;
    private readonly IPasteService _pasteService;
    private readonly IAppSettingRepository _settingsRepository;

    private string _searchText = string.Empty;
    private bool _isPaused = false;
    private ClipboardItem? _selectedItem;
    private ObservableCollection<ClipboardItem> _items = new();
    private CancellationTokenSource? _searchCts;

    public MainWindowViewModel(
        IClipboardRepository repository,
        IClipboardCaptureService captureService,
        IPasteService pasteService,
        IAppSettingRepository settingsRepository)
    {
        _repository = repository;
        _captureService = captureService;
        _pasteService = pasteService;
        _settingsRepository = settingsRepository;

        // Command bindings
        SearchCommand = new RelayCommand(ExecuteSearch);
        DeleteCommand = new RelayCommand<ClipboardItem>(async (item) => await DeleteItemAsync(item));
        ToggleFavoriteCommand = new RelayCommand<ClipboardItem>(async (item) => await ToggleFavoriteAsync(item));
        TogglePinnedCommand = new RelayCommand<ClipboardItem>(async (item) => await TogglePinnedAsync(item));
        PasteCommand = new RelayCommand<ClipboardItem>(async (item) => await PasteItemAsync(item));
        ClearHistoryCommand = new RelayCommand(async () => await ClearHistoryAsync());
        TogglePauseCommand = new RelayCommand(TogglePause);
        OpenSettingsCommand = new RelayCommand(() => OnOpenSettings?.Invoke());

        // Listen for new clip captures to refresh the list
        _captureService.ClipboardItemAdded += (s, e) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                await LoadItemsAsync();
            });
        };

        // Initial load
        _ = LoadItemsAsync();
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                TriggerDebouncedSearch();
            }
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        set => SetProperty(ref _isPaused, value);
    }

    public ClipboardItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public ObservableCollection<ClipboardItem> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public ICommand SearchCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand TogglePinnedCommand { get; }
    public ICommand PasteCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand TogglePauseCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public Action? OnOpenSettings { get; set; }

    public async Task LoadItemsAsync()
    {
        try
        {
            var dbItems = await _repository.GetPagedAsync(0, 100, SearchText);
            Items.Clear();
            foreach (var item in dbItems)
            {
                Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error cargando elementos: {ex.Message}", "Error de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TriggerDebouncedSearch()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        var token = _searchCts.Token;
        Task.Delay(300, token).ContinueWith(async t =>
        {
            if (t.IsCanceled) return;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadItemsAsync();
            });
        }, token);
    }

    private void ExecuteSearch()
    {
        _ = LoadItemsAsync();
    }

    private async Task DeleteItemAsync(ClipboardItem item)
    {
        if (item == null) return;
        await _repository.DeleteAsync(item.Id);
        Items.Remove(item);
    }

    private async Task ToggleFavoriteAsync(ClipboardItem item)
    {
        if (item == null) return;
        item.IsFavorite = !item.IsFavorite;
        item.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(item);
        await LoadItemsAsync();
    }

    private async Task TogglePinnedAsync(ClipboardItem item)
    {
        if (item == null) return;
        item.IsPinned = !item.IsPinned;
        item.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(item);
        await LoadItemsAsync();
    }

    private async Task PasteItemAsync(ClipboardItem item)
    {
        if (item == null) return;

        // Hide main window before pasting so it returns focus to previous window
        var mainWin = System.Windows.Application.Current.MainWindow;
        if (mainWin != null)
        {
            mainWin.Hide();
        }

        bool autoPaste = true;
        var autoPasteSetting = await _settingsRepository.GetValueAsync("General:AutoPaste");
        if (bool.TryParse(autoPasteSetting, out bool val))
        {
            autoPaste = val;
        }

        await _pasteService.PasteAsync(item, autoPaste);

        // Update statistics
        item.UseCount++;
        item.LastUsedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(item);
    }

    private async Task ClearHistoryAsync()
    {
        var result = MessageBox.Show("¿Está seguro de que desea limpiar todo el historial de clips no favoritos/fijados?", "Confirmar Limpieza", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            // Set max items to 0 and retention to 0 to trigger immediate purge of unprotected items
            await _repository.PurgeOldItemsAsync(0, 0);
            await LoadItemsAsync();
        }
    }

    private void TogglePause()
    {
        if (IsPaused)
        {
            _captureService.StartListening(new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle);
            IsPaused = false;
        }
        else
        {
            _captureService.StopListening();
            IsPaused = true;
        }
    }
}
