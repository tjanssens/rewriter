namespace TextRewriter.Core.Models;

public sealed class OAuthCredentials
{
    public ClaudeAiOauth? ClaudeAiOauth { get; set; }
}

public sealed class ClaudeAiOauth
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public long? ExpiresAt { get; set; }
}
