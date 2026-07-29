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

namespace EnterpriseClipboard.App.ViewModels;

public class QuickPopupViewModel : BaseViewModel
{
    private readonly IClipboardRepository _repository;
    private readonly IPasteService _pasteService;
    private readonly IAppSettingRepository _settingsRepository;

    private string _searchText = string.Empty;
    private ClipboardItem? _selectedItem;
    private ObservableCollection<ClipboardItem> _items = new();
    private CancellationTokenSource? _searchCts;

    public QuickPopupViewModel(
        IClipboardRepository repository,
        IPasteService pasteService,
        IAppSettingRepository settingsRepository)
    {
        _repository = repository;
        _pasteService = pasteService;
        _settingsRepository = settingsRepository;

        PasteCommand = new RelayCommand<ClipboardItem>(async (item) => await PasteItemAsync(item));
        ToggleFavoriteCommand = new RelayCommand<ClipboardItem>(async (item) => await ToggleFavoriteAsync(item));
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

    public ICommand PasteCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }

    public async Task LoadItemsAsync()
    {
        try
        {
            var dbItems = await _repository.GetPagedAsync(0, 15, SearchText); // Load top 15 most recent
            Items.Clear();
            foreach (var item in dbItems)
            {
                Items.Add(item);
            }

            if (Items.Count > 0)
            {
                SelectedItem = Items[0];
            }
        }
        catch (Exception)
        {
            // Fail silently or log to Serilog
        }
    }

    private void TriggerDebouncedSearch()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        var token = _searchCts.Token;
        Task.Delay(150, token).ContinueWith(async t =>
        {
            if (t.IsCanceled) return;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadItemsAsync();
            });
        }, token);
    }

    public async Task PasteItemAsync(ClipboardItem? item)
    {
        if (item == null) return;

        // Hide window so it releases window focus back to destination application
        var popup = System.Windows.Application.Current.Windows[0]; // Or find active quick popup window
        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            if (window.GetType().Name == "QuickPopup")
            {
                window.Hide();
            }
        }

        bool autoPaste = true;
        var autoPasteSetting = await _settingsRepository.GetValueAsync("General:AutoPaste");
        if (bool.TryParse(autoPasteSetting, out bool val))
        {
            autoPaste = val;
        }

        await _pasteService.PasteAsync(item, autoPaste);

        // Update counts
        item.UseCount++;
        item.LastUsedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(item);
    }

    private async Task ToggleFavoriteAsync(ClipboardItem item)
    {
        if (item == null) return;
        item.IsFavorite = !item.IsFavorite;
        item.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(item);
        await LoadItemsAsync();
    }
}
