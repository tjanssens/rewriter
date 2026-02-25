using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TextRewriter.Core.Interfaces;
using TextRewriter.Core.Models;

namespace TextRewriter.Services;

public class RewriteService : IRewriteService
{
    private readonly IAuthService _auth;
    private readonly ILogger<RewriteService> _logger;
    private readonly HttpClient _httpClient;
    private const string ApiBaseUrl = "https://api.anthropic.com/v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public RewriteService(IAuthService auth, ILogger<RewriteService> logger)
    {
        _auth = auth;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    public async Task<RewriteResult> RewriteAsync(string text, RewriteProfile profile, CancellationToken ct = default)
    {
        try
        {
            var token = await _auth.GetAccessTokenAsync();
            if (token is null)
            {
                return new RewriteResult
                {
                    Success = false,
                    ErrorMessage = "Niet geauthenticeerd. Start Claude Code ('claude' in terminal) om in te loggen."
                };
            }

            var requestBody = new
            {
                model = profile.ModelId,
                max_tokens = 4096,
                system = profile.SystemPrompt,
                messages = new[]
                {
                    new { role = "user", content = text }
                }
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Set auth header based on token type
            if (token.StartsWith("sk-ant-api"))
            {
                request.Headers.Add("x-api-key", token);
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("API error {StatusCode}: {Body}", response.StatusCode, errorBody);

                return new RewriteResult
                {
                    Success = false,
                    ErrorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized =>
                            "Authenticatie verlopen. Start Claude Code opnieuw ('claude' in terminal).",
                        System.Net.HttpStatusCode.TooManyRequests =>
                            "Rate limit bereikt. Wacht even en probeer opnieuw.",
                        _ => $"API fout ({response.StatusCode}): {errorBody}"
                    }
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseJson, JsonOptions);

            var rewrittenText = apiResponse?.Content?
                .Where(c => c.Type == "text")
                .Select(c => c.Text)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(rewrittenText))
            {
                return new RewriteResult
                {
                    Success = false,
                    ErrorMessage = "Geen tekst ontvangen van Claude."
                };
            }

            return new RewriteResult
            {
                Success = true,
                RewrittenText = rewrittenText
            };
        }
        catch (TaskCanceledException)
        {
            return new RewriteResult
            {
                Success = false,
                ErrorMessage = "Verzoek geannuleerd of timeout."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rewrite failed");
            return new RewriteResult
            {
                Success = false,
                ErrorMessage = $"Fout: {ex.Message}"
            };
        }
    }

    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken ct = default)
    {
        try
        {
            var token = await _auth.GetAccessTokenAsync();
            if (token is null)
                return [];

            var request = new HttpRequestMessage(HttpMethod.Get, "/v1/models?limit=100");

            if (token.StartsWith("sk-ant-api"))
                request.Headers.Add("x-api-key", token);
            else
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch models: {StatusCode}", response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<ModelsResponse>(json, JsonOptions);

            return result?.Data?
                .Select(m => new ModelInfo { Id = m.Id, DisplayName = m.DisplayName })
                .ToList()
                ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch models");
            return [];
        }
    }

    // API response models
    private sealed class ApiResponse
    {
        public List<ContentBlock>? Content { get; set; }
    }

    private sealed class ContentBlock
    {
        public string Type { get; set; } = "";
        public string? Text { get; set; }
    }

    private sealed class ModelsResponse
    {
        public List<ModelData>? Data { get; set; }
    }

    private sealed class ModelData
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
