using TextRewriter.Core.Interfaces;

namespace TextRewriter.Services.Platform;

public class LinuxPlatformService : IPlatformService
{
    public bool IsMacOS => false;

    public string GetConfigDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config", "textrewriter");
    }

    public string? ReadClaudeCredentials()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var credPath = Path.Combine(home, ".claude", ".credentials.json");
        return File.Exists(credPath) ? File.ReadAllText(credPath) : null;
    }
}
