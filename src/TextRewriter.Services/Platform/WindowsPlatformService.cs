using TextRewriter.Core.Interfaces;

namespace TextRewriter.Services.Platform;

public class WindowsPlatformService : IPlatformService
{
    public bool IsMacOS => false;

    public string GetConfigDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "TextRewriter");
    }

    public string? ReadClaudeCredentials()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var credPath = Path.Combine(home, ".claude", ".credentials.json");
        return File.Exists(credPath) ? File.ReadAllText(credPath) : null;
    }
}
