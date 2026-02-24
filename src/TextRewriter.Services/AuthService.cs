using System.Text.Json;
using Microsoft.Extensions.Logging;
using TextRewriter.Core.Interfaces;
using TextRewriter.Core.Models;

namespace TextRewriter.Services;

public class AuthService : IAuthService
{
    private readonly IPlatformService _platform;
    private readonly ILogger<AuthService> _logger;
    private string? _cachedToken;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AuthService(IPlatformService platform, ILogger<AuthService> logger)
    {
        _platform = platform;
        _logger = logger;
    }

    public bool IsAuthenticated => _cachedToken is not null;

    public Task<string?> GetAccessTokenAsync()
    {
        return Task.FromResult(GetAccessTokenInternal());
    }

    private string? GetAccessTokenInternal()
    {
        // Check cache
        if (_cachedToken is not null && DateTime.UtcNow < _cacheExpiry)
            return _cachedToken;

        // Priority 1: ANTHROPIC_API_KEY environment variable
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            _cachedToken = apiKey;
            _cacheExpiry = DateTime.UtcNow.Add(CacheTtl);
            return apiKey;
        }

        // Priority 2: CLAUDE_CODE_OAUTH_TOKEN environment variable
        var oauthEnv = Environment.GetEnvironmentVariable("CLAUDE_CODE_OAUTH_TOKEN");
        if (!string.IsNullOrEmpty(oauthEnv))
        {
            _cachedToken = oauthEnv;
            _cacheExpiry = DateTime.UtcNow.Add(CacheTtl);
            return oauthEnv;
        }

        // Priority 3: Claude Code credential file
        try
        {
            var credJson = _platform.ReadClaudeCredentials();
            if (credJson is not null)
            {
                var creds = JsonSerializer.Deserialize<OAuthCredentials>(credJson, JsonOptions);
                var token = creds?.ClaudeAiOauth?.AccessToken;
                if (!string.IsNullOrEmpty(token))
                {
                    // Check if token is expired
                    if (creds!.ClaudeAiOauth!.ExpiresAt.HasValue)
                    {
                        var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(creds.ClaudeAiOauth.ExpiresAt.Value);
                        if (expiresAt < DateTimeOffset.UtcNow)
                        {
                            _logger.LogWarning("Claude OAuth token is expired. Please run 'claude' to re-authenticate.");
                            return null;
                        }
                    }

                    _cachedToken = token;
                    _cacheExpiry = DateTime.UtcNow.Add(CacheTtl);
                    return token;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Claude credentials");
        }

        return null;
    }
}
