using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TextRewriter.Core.Interfaces;
using TextRewriter.Core.Models;

namespace TextRewriter.App.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly IRewriteService _rewriteService;
    private RewriteProfile? _selectedProfile;
    private bool _isRecordingHotkey;

    public event PropertyChangedEventHandler? PropertyChanged;

    private static readonly ModelInfo[] FallbackModels =
    [
        new() { Id = "claude-haiku-4-5-20251001", DisplayName = "Claude Haiku 4.5" },
        new() { Id = "claude-sonnet-4-20250514", DisplayName = "Claude Sonnet 4" },
        new() { Id = "claude-opus-4-20250514", DisplayName = "Claude Opus 4" },
    ];

    public SettingsViewModel(AppSettings settings, ISettingsService settingsService, IRewriteService rewriteService)
    {
        _settings = settings;
        _settingsService = settingsService;
        _rewriteService = rewriteService;
        Profiles = new ObservableCollection<RewriteProfile>(settings.Profiles);
        AvailableModels = new ObservableCollection<ModelInfo>(FallbackModels);
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == settings.ActiveProfileId)
                          ?? Profiles.FirstOrDefault();
    }

    public ObservableCollection<RewriteProfile> Profiles { get; }
    public ObservableCollection<ModelInfo> AvailableModels { get; }

    public RewriteProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            _selectedProfile = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(ProfileName));
            OnPropertyChanged(nameof(ProfilePrompt));
            OnPropertyChanged(nameof(SelectedModelOption));
        }
    }

    public bool HasSelection => _selectedProfile is not null;

    public string ProfileName
    {
        get => _selectedProfile?.Name ?? "";
        set
        {
            if (_selectedProfile is not null)
            {
                _selectedProfile.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public string ProfilePrompt
    {
        get => _selectedProfile?.SystemPrompt ?? "";
        set
        {
            if (_selectedProfile is not null)
            {
                _selectedProfile.SystemPrompt = value;
                OnPropertyChanged();
            }
        }
    }

    public ModelInfo? SelectedModelOption
    {
        get
        {
            var modelId = _selectedProfile?.ModelId ?? "claude-sonnet-4-20250514";
            return AvailableModels.FirstOrDefault(m => m.Id == modelId);
        }
        set
        {
            if (_selectedProfile is not null && value is not null)
            {
                _selectedProfile.ModelId = value.Id;
                OnPropertyChanged();
            }
        }
    }

    public string ApiKey
    {
        get => _settings.ApiKey ?? "";
        set
        {
            _settings.ApiKey = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            OnPropertyChanged();
            OnPropertyChanged(nameof(AuthStatus));
        }
    }

    public string AuthStatus
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
                return "API key ingesteld";
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
                return "Via ANTHROPIC_API_KEY omgevingsvariabele";
            return "Niet geconfigureerd";
        }
    }

    public bool IsRecordingHotkey
    {
        get => _isRecordingHotkey;
        set
        {
            _isRecordingHotkey = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HotkeyDisplay));
        }
    }

    public string HotkeyDisplay => _isRecordingHotkey
        ? "Druk op een toetscombinatie..."
        : _settings.Hotkey.DisplayName;

    public void UpdateHotkey(ushort keyCode, string keyName, bool ctrl, bool shift, bool alt, bool meta)
    {
        _settings.Hotkey.KeyCode = keyCode;
        _settings.Hotkey.KeyName = keyName;
        _settings.Hotkey.Ctrl = ctrl;
        _settings.Hotkey.Shift = shift;
        _settings.Hotkey.Alt = alt;
        _settings.Hotkey.Meta = meta;
        IsRecordingHotkey = false;
    }

    public async Task LoadModelsAsync()
    {
        var models = await _rewriteService.GetModelsAsync();
        if (models.Count == 0)
            return;

        // Ensure the current profile's model is in the list
        var currentModelId = _selectedProfile?.ModelId;
        if (!string.IsNullOrEmpty(currentModelId) && models.All(m => m.Id != currentModelId))
        {
            models = models.Prepend(new ModelInfo { Id = currentModelId, DisplayName = currentModelId }).ToList();
        }

        AvailableModels.Clear();
        foreach (var model in models)
            AvailableModels.Add(model);

        OnPropertyChanged(nameof(SelectedModelOption));
    }

    public void AddProfile()
    {
        var newProfile = new RewriteProfile
        {
            Name = "Nieuw profiel",
            SystemPrompt = "Herschrijf de volgende tekst. Geef ENKEL de herschreven tekst terug."
        };
        Profiles.Add(newProfile);
        SelectedProfile = newProfile;
    }

    public void RemoveProfile()
    {
        if (_selectedProfile is null || Profiles.Count <= 1) return;
        var idx = Profiles.IndexOf(_selectedProfile);
        Profiles.Remove(_selectedProfile);
        SelectedProfile = Profiles[Math.Min(idx, Profiles.Count - 1)];
    }

    public async Task SaveAsync()
    {
        _settings.Profiles = Profiles.ToList();
        if (_selectedProfile is not null)
            _settings.ActiveProfileId = _selectedProfile.Id;
        await _settingsService.SaveAsync(_settings);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
