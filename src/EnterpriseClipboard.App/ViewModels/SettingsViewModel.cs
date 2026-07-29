using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EnterpriseClipboard.App.Helpers;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.Domain.Entities;
using MessageBox = System.Windows.MessageBox;

namespace EnterpriseClipboard.App.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly IAppSettingRepository _settingsRepository;
    private readonly IHotkeyConfigurationRepository _hotkeyRepository;

    private int _retentionDays;
    private int _selectedQpModifier;
    private int _selectedQpKey;
    private int _selectedMwModifier;
    private int _selectedMwKey;

    public SettingsViewModel(
        IAppSettingRepository settingsRepository,
        IHotkeyConfigurationRepository hotkeyRepository)
    {
        _settingsRepository = settingsRepository;
        _hotkeyRepository = hotkeyRepository;

        // Populate lists for bindings
        ModifiersList = new ObservableCollection<HotkeyModifierOption>
        {
            new("Ctrl + Shift", 6),
            new("Ctrl + Alt", 3),
            new("Alt + Shift", 5),
            new("Ctrl + Windows", 10),
            new("Ctrl Only", 2),
            new("Alt Only", 1)
        };

        KeysList = new ObservableCollection<HotkeyKeyOption>
        {
            new("V", 0x56),
            new("H", 0x48),
            new("Backtick ( ` )", 192),
            new("Espacio", 0x20),
            new("Tab", 9),
            new("Insert", 45),
            new("Delete", 46),
            new("F1", 112),
            new("F2", 113),
            new("F3", 114),
            new("F4", 115),
            new("F5", 116),
            new("F6", 117),
            new("F7", 118),
            new("F8", 119),
            new("F9", 120),
            new("F10", 121),
            new("F11", 122),
            new("F12", 123),
            new("C", 0x43),
            new("X", 0x58),
            new("A", 0x41),
            new("Z", 0x5A)
        };

        SaveCommand = new RelayCommand(async () => await SaveSettingsAsync());
    }

    public int RetentionDays
    {
        get => _retentionDays;
        set => SetProperty(ref _retentionDays, value);
    }

    public int SelectedQpModifier
    {
        get => _selectedQpModifier;
        set => SetProperty(ref _selectedQpModifier, value);
    }

    public int SelectedQpKey
    {
        get => _selectedQpKey;
        set => SetProperty(ref _selectedQpKey, value);
    }

    public int SelectedMwModifier
    {
        get => _selectedMwModifier;
        set => SetProperty(ref _selectedMwModifier, value);
    }

    public int SelectedMwKey
    {
        get => _selectedMwKey;
        set => SetProperty(ref _selectedMwKey, value);
    }

    public ObservableCollection<HotkeyModifierOption> ModifiersList { get; }
    public ObservableCollection<HotkeyKeyOption> KeysList { get; }

    public ICommand SaveCommand { get; }

    public async Task LoadSettingsAsync()
    {
        // 1. Load retention days
        var retentionVal = await _settingsRepository.GetValueAsync("History:RetentionDays");
        if (int.TryParse(retentionVal, out int days))
        {
            RetentionDays = days;
        }
        else
        {
            RetentionDays = 30; // fallback
        }

        // 2. Load hotkeys
        var hotkeys = await _hotkeyRepository.GetAllAsync();
        var qpHotkey = hotkeys.FirstOrDefault(h => h.Action == "OpenQuickPopup");
        if (qpHotkey != null)
        {
            SelectedQpModifier = qpHotkey.Modifiers;
            SelectedQpKey = qpHotkey.Key;
        }
        else
        {
            SelectedQpModifier = 6;
            SelectedQpKey = 0x56;
        }

        var mwHotkey = hotkeys.FirstOrDefault(h => h.Action == "OpenMainWindow");
        if (mwHotkey != null)
        {
            SelectedMwModifier = mwHotkey.Modifiers;
            SelectedMwKey = mwHotkey.Key;
        }
        else
        {
            SelectedMwModifier = 6;
            SelectedMwKey = 0x48;
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            // 1. Save retention days setting
            await _settingsRepository.SetValueAsync("History:RetentionDays", RetentionDays.ToString(), "Integer");

            // 2. Save hotkey registrations
            var hotkeys = await _hotkeyRepository.GetAllAsync();
            
            var qpHotkey = hotkeys.FirstOrDefault(h => h.Action == "OpenQuickPopup");
            if (qpHotkey != null)
            {
                qpHotkey.Modifiers = SelectedQpModifier;
                qpHotkey.Key = SelectedQpKey;
                await _hotkeyRepository.UpdateAsync(qpHotkey);
            }
            else
            {
                await _hotkeyRepository.AddAsync(new HotkeyConfiguration { Action = "OpenQuickPopup", Modifiers = SelectedQpModifier, Key = SelectedQpKey });
            }

            var mwHotkey = hotkeys.FirstOrDefault(h => h.Action == "OpenMainWindow");
            if (mwHotkey != null)
            {
                mwHotkey.Modifiers = SelectedMwModifier;
                mwHotkey.Key = SelectedMwKey;
                await _hotkeyRepository.UpdateAsync(mwHotkey);
            }
            else
            {
                await _hotkeyRepository.AddAsync(new HotkeyConfiguration { Action = "OpenMainWindow", Modifiers = SelectedMwModifier, Key = SelectedMwKey });
            }

            MessageBox.Show("Configuraciones guardadas. Por favor reinicie la aplicación para registrar los nuevos atajos.", "Configuración", MessageBoxButton.OK, MessageBoxImage.Information);

            // Find active window and close it
            foreach (Window win in System.Windows.Application.Current.Windows)
            {
                if (win.GetType().Name == "SettingsWindow")
                {
                    win.Close();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error guardando configuraciones: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public class HotkeyModifierOption
{
    public string DisplayName { get; }
    public int Value { get; }

    public HotkeyModifierOption(string displayName, int value)
    {
        DisplayName = displayName;
        Value = value;
    }
}

public class HotkeyKeyOption
{
    public string DisplayName { get; }
    public int Value { get; }

    public HotkeyKeyOption(string displayName, int value)
    {
        DisplayName = displayName;
        Value = value;
    }
}
