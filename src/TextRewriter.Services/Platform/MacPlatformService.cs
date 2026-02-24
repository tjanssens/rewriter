using System.Diagnostics;
using TextRewriter.Core.Interfaces;

namespace TextRewriter.Services.Platform;

public class MacPlatformService : IPlatformService
{
    public bool IsMacOS => true;

    public string GetConfigDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "Library", "Application Support", "TextRewriter");
    }

    public string? ReadClaudeCredentials()
    {
        // Try macOS Keychain first
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "security",
                    Arguments = "find-generic-password -s \"Claude Code-credentials\" -w",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                return output;
        }
        catch
        {
            // Fall through to file-based approach
        }

        // Fallback: file-based credentials
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var credPath = Path.Combine(home, ".claude", ".credentials.json");
        return File.Exists(credPath) ? File.ReadAllText(credPath) : null;
    }
}
