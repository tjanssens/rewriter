using System.Text.Json;
using Microsoft.Extensions.Logging;
using TextRewriter.Core.Interfaces;
using TextRewriter.Core.Models;

namespace TextRewriter.Services;

public class AuthService : IAuthService
{
    private readonly IPlatformService _platform;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<AuthService> _logger;
    private string? _cachedToken;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AuthService(IPlatformService platform, ISettingsService settingsService, ILogger<AuthService> logger)
    {
        _platform = platform;
        _settingsService = settingsService;
        _logger = logger;
    }

    public bool IsAuthenticated => _cachedToken is not null;

    public async Task<string?> GetAccessTokenAsync()
    {
        // Check cache
        if (_cachedToken is not null && DateTime.UtcNow < _cacheExpiry)
            return _cachedToken;

        // Priority 1: API key from settings
        var settings = await _settingsService.LoadAsync();
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            _cachedToken = settings.ApiKey;
            _cacheExpiry = DateTime.UtcNow.Add(CacheTtl);
            return _cachedToken;
        }

        // Priority 2: ANTHROPIC_API_KEY environment variable
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            _cachedToken = apiKey;
            _cacheExpiry = DateTime.UtcNow.Add(CacheTtl);
            return apiKey;
        }

        // Priority 3: CLAUDE_CODE_OAUTH_TOKEN environment variable
        var oauthEnv = Environment.GetEnvironmentVariable("CLAUDE_CODE_OAUTH_TOKEN");
        if (!string.IsNullOrEmpty(oauthEnv))
        {
            _cachedToken = oauthEnv;
            _cacheExpiry = DateTime.UtcNow.Add(CacheTtl);
            return oauthEnv;
        }

        // Priority 4: Claude Code credential file (OAuth)
        try
        {
            var credJson = _platform.ReadClaudeCredentials();
            if (credJson is not null)
            {
                var creds = JsonSerializer.Deserialize<OAuthCredentials>(credJson, JsonOptions);
                var token = creds?.ClaudeAiOauth?.AccessToken;
                if (!string.IsNullOrEmpty(token))
                {
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

    public void ClearCache()
    {
        _cachedToken = null;
        _cacheExpiry = DateTime.MinValue;
    }
}
