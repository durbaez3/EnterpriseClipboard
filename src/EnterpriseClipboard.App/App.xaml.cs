using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EnterpriseClipboard.App.ViewModels;
using EnterpriseClipboard.Application.Interfaces;
using EnterpriseClipboard.Application.Services;
using EnterpriseClipboard.Infrastructure.Services;
using EnterpriseClipboard.Persistence.Context;
using EnterpriseClipboard.Persistence.Repositories;
using EnterpriseClipboard.WindowsIntegration.Services;

namespace EnterpriseClipboard.App;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private QuickPopup? _quickPopup;
    private IHotkeyService? _hotkeyService;
    private IClipboardCaptureService? _captureService;
    private HwndSource? _hwndSource;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Load appsettings.json
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        var configuration = configBuilder.Build();

        // 2. Configure Dependency Injection
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        // Configure DBContext with SQLite
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dbFolder = Path.Combine(localAppData, "EnterpriseClipboard");
        if (!Directory.Exists(dbFolder))
        {
            Directory.CreateDirectory(dbFolder);
        }
        string dbPath = Path.Combine(dbFolder, "history.db");

        services.AddDbContext<ClipboardDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Application Services
        services.AddSingleton<IEncryptionService, DpapiEncryptionService>();
        services.AddSingleton<IImageStorageService, ImageStorageService>();
        services.AddSingleton<IClipboardCaptureService, ClipboardCaptureService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        
        // Repositories
        services.AddScoped<IClipboardRepository, ClipboardRepository>();
        services.AddScoped<ISensitiveDataRuleRepository, SensitiveDataRuleRepository>();
        services.AddScoped<IApplicationExclusionRepository, ApplicationExclusionRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IHotkeyConfigurationRepository, HotkeyConfigurationRepository>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        // Windows Integration Services
        services.AddSingleton<IClipboardListener, ClipboardListener>();
        services.AddSingleton<IClipboardReader, ClipboardReader>();
        services.AddSingleton<IActiveWindowService, ActiveWindowService>();
        services.AddSingleton<IPasteService, PasteService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<QuickPopupViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();
        services.AddSingleton<QuickPopup>();
        services.AddTransient<SettingsWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // 3. Setup Tray Icon FIRST so app appears instantly in taskbar
        SetupTrayIcon();

        // 4. Initialize Database in background for faster startup
        await Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var dbInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            await dbInitializer.InitializeAsync();
        });

        // 5. Get windows and services from DI
        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _quickPopup = _serviceProvider.GetRequiredService<QuickPopup>();
        _hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
        _captureService = _serviceProvider.GetRequiredService<IClipboardCaptureService>();

        MainWindow = _mainWindow;

        // 6. Set up Window handle hook for Native Windows messages
        var interopHelper = new WindowInteropHelper(_mainWindow);
        interopHelper.EnsureHandle(); // Ensure window handle is created
        
        IntPtr hwnd = interopHelper.Handle;
        _hwndSource = HwndSource.FromHwnd(hwnd);
        _hwndSource.AddHook(WndProcHook);

        // 7. Start listener and hotkeys
        _captureService.StartListening(hwnd);

        _hotkeyService.SetWindowHandle(hwnd);

        // Load hotkeys dynamically from the SQLite database
        using (var scope = _serviceProvider.CreateScope())
        {
            var hotkeyRepo = scope.ServiceProvider.GetRequiredService<IHotkeyConfigurationRepository>();
            var hotkeys = await hotkeyRepo.GetAllEnabledAsync();
            
            bool hasQp = false;
            bool hasMw = false;
            
            foreach (var hk in hotkeys)
            {
                if (hk.Action == "OpenQuickPopup")
                {
                    _hotkeyService.RegisterHotkey(1, hk.Modifiers, hk.Key, ShowQuickPopup);
                    hasQp = true;
                }
                else if (hk.Action == "OpenMainWindow")
                {
                    _hotkeyService.RegisterHotkey(2, hk.Modifiers, hk.Key, ShowMainWindow);
                    hasMw = true;
                }
            }

            // Fallback to defaults if not seeded
            // Default: Ctrl + Backtick for QuickPopup, Ctrl+Shift+H for MainWindow
            if (!hasQp) _hotkeyService.RegisterHotkey(1, 2, 192, ShowQuickPopup);
            if (!hasMw) _hotkeyService.RegisterHotkey(2, 6, 0x48, ShowMainWindow);
        }

        var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainViewModel.OnOpenSettings = ShowSettingsWindow;

        // Optional: show window if configured (default is minimized to tray)
        string showOnStart = configuration["General:OpenOnStartup"] ?? "False";
        if (bool.TryParse(showOnStart, out bool show) && show)
        {
            _mainWindow.Show();
        }

        // Check for updates in background (non-blocking)
        _ = Task.Run(async () => await CheckForUpdatesInBackgroundAsync());
    }

    private async Task CheckForUpdatesInBackgroundAsync()
    {
        try
        {
            await Task.Delay(5000); // Wait 5s after startup before checking
            if (_serviceProvider == null) return;

            var updateService = _serviceProvider.GetRequiredService<IUpdateService>();
            var (available, version, url) = await updateService.CheckForUpdatesAsync();

            if (available && !string.IsNullOrEmpty(url))
            {
                Dispatcher.Invoke(() =>
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Nueva versión disponible: v{version}\n\n¿Deseas descargar e instalar la actualización ahora?",
                        "Actualización Disponible",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Information);

                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        _ = ApplyUpdateAsync(updateService, url);
                    }
                });
            }
        }
        catch { /* Silently ignore update check errors */ }
    }

    private async Task ApplyUpdateAsync(IUpdateService updateService, string url)
    {
        try
        {
            await updateService.DownloadAndApplyUpdateAsync(url, percent =>
            {
                _notifyIcon!.Text = $"Descargando actualización... {percent}%";
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
                System.Windows.MessageBox.Show($"Error al actualizar: {ex.Message}", "Error de Actualización",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error));
        }
    }

    private void SetupTrayIcon()
    {
        // Load app icon from embedded resources
        System.Drawing.Icon trayIcon = System.Drawing.SystemIcons.Application;
        try
        {
            var streamInfo = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/app.ico"));
            if (streamInfo != null)
            {
                trayIcon = new System.Drawing.Icon(streamInfo.Stream);
            }
        }
        catch
        {
            // Fallback to default if resource not found
        }

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = trayIcon,
            Visible = true,
            Text = "Enterprise Clipboard Manager"
        };

        // Context Menu
        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Abrir Historial", null, (s, e) => ShowMainWindow());
        contextMenu.Items.Add("Historial Rápido (Ctrl + `)", null, (s, e) => ShowQuickPopup());
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        
        var pauseItem = new System.Windows.Forms.ToolStripMenuItem("Pausar Captura");
        pauseItem.Click += (s, e) =>
        {
            if (pauseItem.Text == "Pausar Captura")
            {
                _captureService?.StopListening();
                pauseItem.Text = "Reanudar Captura";
            }
            else
            {
                if (_mainWindow != null)
                {
                    var hwnd = new WindowInteropHelper(_mainWindow).Handle;
                    _captureService?.StartListening(hwnd);
                }
                pauseItem.Text = "Pausar Captura";
            }
        };
        contextMenu.Items.Add(pauseItem);

        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Configuración", null, (s, e) => ShowSettingsWindow());
        contextMenu.Items.Add("Buscar Actualizaciones", null, async (s, e) => await CheckForUpdatesInBackgroundAsync());
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Salir", null, (s, e) => ShutdownApp());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _ = _mainWindow.Dispatcher.InvokeAsync(async () =>
            {
                if (_mainWindow.DataContext is MainWindowViewModel vm)
                {
                    await vm.LoadItemsAsync();
                }
            });
        }
    }

    private void ShowQuickPopup()
    {
        if (_quickPopup != null)
        {
            _quickPopup.ShowAtCursor();
        }
    }

    private void ShowSettingsWindow()
    {
        if (_serviceProvider != null)
        {
            var settingsWin = _serviceProvider.GetRequiredService<SettingsWindow>();
            settingsWin.Owner = _mainWindow;
            settingsWin.ShowSettings();
        }
    }

    private void ShutdownApp()
    {
        _notifyIcon?.Dispose();
        _hwndSource?.RemoveHook(WndProcHook);
        _captureService?.StopListening();
        _hotkeyService?.UnregisterAll();
        _mainWindow?.ForceClose();
        
        _serviceProvider?.Dispose();
        
        System.Windows.Application.Current.Shutdown();
    }

    private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // 1. Process clipboard updates
        if (_serviceProvider != null)
        {
            var listener = _serviceProvider.GetService<IClipboardListener>() as ClipboardListener;
            listener?.HandleMessage(msg, wParam, lParam);
        }

        // 2. Process global hotkeys
        _hotkeyService?.HandleMessage(msg, wParam);

        return IntPtr.Zero;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}
