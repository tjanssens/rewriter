namespace TextRewriter.Core.Interfaces;

public interface IPlatformService
{
    bool IsMacOS { get; }
    string GetConfigDirectory();
    string? ReadClaudeCredentials();
}
