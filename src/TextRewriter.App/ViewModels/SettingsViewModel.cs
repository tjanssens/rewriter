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
    private RewriteProfile? _selectedProfile;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel(AppSettings settings, ISettingsService settingsService)
    {
        _settings = settings;
        _settingsService = settingsService;
        Profiles = new ObservableCollection<RewriteProfile>(settings.Profiles);
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == settings.ActiveProfileId)
                          ?? Profiles.FirstOrDefault();
    }

    public ObservableCollection<RewriteProfile> Profiles { get; }

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
            OnPropertyChanged(nameof(ProfileModel));
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

    public string ProfileModel
    {
        get => _selectedProfile?.ModelId ?? "claude-sonnet-4-20250514";
        set
        {
            if (_selectedProfile is not null)
            {
                _selectedProfile.ModelId = value;
                OnPropertyChanged();
            }
        }
    }

    public string ShortcutDisplay => _settings.Hotkey.DisplayName;

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
