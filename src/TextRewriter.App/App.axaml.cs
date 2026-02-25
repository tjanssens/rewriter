using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TextRewriter.App.ViewModels;
using TextRewriter.App.Views;
using TextRewriter.Core.Interfaces;
using TextRewriter.Core.Models;
using TextRewriter.Services;
using TextRewriter.Services.Platform;

namespace TextRewriter.App;

public partial class App : Application
{
    private IServiceProvider _services = null!;
    private IHotkeyService _hotkeyService = null!;
    private RewriteOrchestrator _orchestrator = null!;
    private TrayIcon? _trayIcon;
    private NativeMenu? _profilesMenu;
    private AppSettings _settings = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Build DI container
            _services = ConfigureServices();

            // Load settings
            var settingsService = _services.GetRequiredService<ISettingsService>();
            _settings = await settingsService.LoadAsync();

            // Create tray icon
            SetupTrayIcon();

            // Start hotkey listener
            _hotkeyService = _services.GetRequiredService<IHotkeyService>();
            _orchestrator = _services.GetRequiredService<RewriteOrchestrator>();

            _hotkeyService.HotkeyTriggered += OnHotkeyTriggered;
            _orchestrator.BusyStateChanged += OnBusyStateChanged;
            _orchestrator.StatusMessage += OnStatusMessage;

            try
            {
                await _hotkeyService.StartAsync(_settings.Hotkey);
            }
            catch (Exception ex)
            {
                var logger = _services.GetRequiredService<ILogger<App>>();
                logger.LogError(ex, "Failed to start hotkey listener. On macOS, enable Accessibility permissions.");
            }

            desktop.ShutdownRequested += async (_, _) =>
            {
                await _hotkeyService.DisposeAsync();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon()
    {
        _profilesMenu = new NativeMenu();
        RebuildProfileMenu();

        var mainMenu = new NativeMenu();

        var profilesItem = new NativeMenuItem("Profielen")
        {
            Menu = _profilesMenu
        };
        mainMenu.Items.Add(profilesItem);

        mainMenu.Items.Add(new NativeMenuItemSeparator());

        var shortcutItem = new NativeMenuItem($"Sneltoets: {_settings.Hotkey.DisplayName}")
        {
            IsEnabled = false
        };
        mainMenu.Items.Add(shortcutItem);

        mainMenu.Items.Add(new NativeMenuItemSeparator());

        var settingsItem = new NativeMenuItem("Instellingen...");
        settingsItem.Click += OnSettingsClicked;
        mainMenu.Items.Add(settingsItem);

        mainMenu.Items.Add(new NativeMenuItemSeparator());

        var quitItem = new NativeMenuItem("Afsluiten");
        quitItem.Click += OnQuitClicked;
        mainMenu.Items.Add(quitItem);

        _trayIcon = new TrayIcon
        {
            ToolTipText = "Text Rewriter",
            Menu = mainMenu
        };

        // Try to load icon
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://TextRewriter.App/Assets/icon.png"));
            _trayIcon.Icon = new WindowIcon(new Bitmap(stream));
        }
        catch
        {
            // Icon not found, tray will show default
        }

        var icons = new TrayIcons { _trayIcon };
        SetValue(TrayIcon.IconsProperty, icons);
    }

    private void RebuildProfileMenu()
    {
        if (_profilesMenu is null) return;
        _profilesMenu.Items.Clear();

        foreach (var profile in _settings.Profiles)
        {
            var item = new NativeMenuItem(profile.Name)
            {
                ToggleType = NativeMenuItemToggleType.Radio,
                IsChecked = profile.Id == _settings.ActiveProfileId
            };
            var profileId = profile.Id;
            item.Click += async (_, _) => await SwitchProfile(profileId);
            _profilesMenu.Items.Add(item);
        }
    }

    private async Task SwitchProfile(string profileId)
    {
        _settings.ActiveProfileId = profileId;
        var settingsService = _services.GetRequiredService<ISettingsService>();
        await settingsService.SaveAsync(_settings);
        RebuildProfileMenu();

        var profileName = _settings.Profiles.FirstOrDefault(p => p.Id == profileId)?.Name ?? "?";
        if (_trayIcon is not null)
            _trayIcon.ToolTipText = $"Text Rewriter - {profileName}";
    }

    private void OnHotkeyTriggered(object? sender, EventArgs e)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.HandleHotkeyAsync();
            }
            catch (Exception ex)
            {
                var logger = _services.GetRequiredService<ILogger<App>>();
                logger.LogError(ex, "Hotkey handler failed");
            }
        });
    }

    private void OnBusyStateChanged(object? sender, bool busy)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_trayIcon is not null)
                _trayIcon.ToolTipText = busy ? "Text Rewriter - Herschrijven..." : "Text Rewriter";
        });
    }

    private void OnStatusMessage(object? sender, string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_trayIcon is not null)
                _trayIcon.ToolTipText = $"Text Rewriter - {message}";
        });
    }

    private SettingsWindow? _settingsWindow;

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_settingsWindow is not null)
            {
                _settingsWindow.Activate();
                return;
            }

            var vm = new SettingsViewModel(
                _settings,
                _services.GetRequiredService<ISettingsService>(),
                _services.GetRequiredService<IRewriteService>());
            _settingsWindow = new SettingsWindow { DataContext = vm };
            _settingsWindow.Closed += async (_, _) =>
            {
                _settingsWindow = null;
                // Reload settings in case they changed
                var settingsService = _services.GetRequiredService<ISettingsService>();
                _settings = await settingsService.LoadAsync();
                RebuildProfileMenu();
                await _hotkeyService.UpdateBindingAsync(_settings.Hotkey);
                // Clear auth cache so new API key takes effect immediately
                _services.GetRequiredService<IAuthService>().ClearCache();
            };
            _settingsWindow.Show();
        });
    }


    private void OnQuitClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.TryShutdown();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        var platform = PlatformServiceFactory.Create();
        services.AddSingleton<IPlatformService>(platform);
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IClipboardService, TextClipboardService>();
        services.AddSingleton<IInputSimulator>(sp =>
            new InputSimulatorService(sp.GetRequiredService<IPlatformService>()));
        services.AddSingleton<IRewriteService, RewriteService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<RewriteOrchestrator>();

        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        return services.BuildServiceProvider();
    }
}
