using System.Text.Json;
using TextRewriter.Core.Interfaces;
using TextRewriter.Core.Models;

namespace TextRewriter.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SettingsService(IPlatformService platform)
    {
        var dir = platform.GetConfigDirectory();
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = CreateDefaultSettings();
            await SaveAsync(defaults);
            return defaults;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                   ?? CreateDefaultSettings();
        }
        catch (JsonException)
        {
            return CreateDefaultSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    private static AppSettings CreateDefaultSettings()
    {
        var profiles = new List<RewriteProfile>
        {
            new()
            {
                Name = "Herschrijf (NL)",
                SystemPrompt = "Herschrijf de volgende tekst in helder, professioneel Nederlands. " +
                               "Behoud de originele betekenis en toon. " +
                               "Geef ENKEL de herschreven tekst terug, zonder uitleg."
            },
            new()
            {
                Name = "Rewrite (EN)",
                SystemPrompt = "Rewrite the following text in clear, professional English. " +
                               "Maintain the original meaning and tone. " +
                               "Return ONLY the rewritten text with no explanation."
            },
            new()
            {
                Name = "Formeel",
                SystemPrompt = "Herschrijf de volgende tekst in een formele, zakelijke stijl. " +
                               "Gebruik de u-vorm. Behoud de originele betekenis. " +
                               "Geef ENKEL de herschreven tekst terug."
            },
            new()
            {
                Name = "Casual",
                SystemPrompt = "Herschrijf de volgende tekst in een informele, vriendelijke toon. " +
                               "Gebruik de je-vorm. Behoud de originele betekenis. " +
                               "Geef ENKEL de herschreven tekst terug."
            },
            new()
            {
                Name = "Beknopt",
                SystemPrompt = "Maak de volgende tekst korter en bondiger zonder informatie te verliezen. " +
                               "Geef ENKEL de herschreven tekst terug."
            }
        };

        return new AppSettings
        {
            Profiles = profiles,
            ActiveProfileId = profiles[0].Id,
            Hotkey = new HotkeyBinding
            {
                KeyCode = 0x0013, // R key
                Ctrl = true,
                Shift = true
            }
        };
    }
}
