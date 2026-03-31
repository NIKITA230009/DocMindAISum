using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DocMind.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocMind.Services.Services;

public class OpenRouterAiService : IOpenRouterAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenRouterAiService> _logger;
    private string? _apiKey;

    private const string BaseUrl = "https://openrouter.ai/api/v1";
    private const string DefaultModel = "deepseek/deepseek-v3.2";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public OpenRouterAiService(HttpClient httpClient, ILogger<OpenRouterAiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey.Trim();
        _logger.LogInformation("OpenRouter API key configured");
    }

    public async Task<string> ExecuteCommandAsync(string text, string command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("OpenRouter API key not configured. Enter your API key in the settings.");
        }

        string normalizedCommand = command.Trim().ToLowerInvariant();
        string action = normalizedCommand.Contains("суммар") || normalizedCommand.Contains("summar") ? "summarize"
                     : normalizedCommand.Contains("перефраз") || normalizedCommand.Contains("paraphr") ? "paraphrase"
                     : "summarize";

        string systemPrompt = @"You are DocMind AI, a helpful assistant for text summarization and paraphrasing.
You receive a document text and a command (summarize or paraphrase).
Output ONLY the result, without explanations, comments, or the original text.
Be concise and clear in your response.";

        string userMessage = $"Command: {action}\n\nDocument: {text}";

        var request = new OpenRouterRequest
        {
            Model = DefaultModel,
            Messages = new[]
            {
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = userMessage }
            },
            MaxTokens = 1024,
            Temperature = 0.3f
        };

        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
            requestMessage.Headers.Add("Authorization", $"Bearer {_apiKey}");
            requestMessage.Headers.Add("HTTP-Referer", "https://docmind.app");
            requestMessage.Headers.Add("X-Title", "DocMind");

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            requestMessage.Content = JsonContent.Create(request, options: jsonOptions);

            _logger.LogDebug("Sending request to OpenRouter API");

            var response = await _httpClient.SendAsync(requestMessage, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = ParseErrorMessage(responseBody) ?? response.StatusCode.ToString();
                _logger.LogError("OpenRouter API error: {StatusCode} - {Response}", response.StatusCode, responseBody);
                throw new Exception($"API error ({response.StatusCode}): {errorMessage}");
            }

            var responseOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var completion = JsonSerializer.Deserialize<OpenRouterResponse>(responseBody, responseOptions);
            var content = completion?.Choices?[0]?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                return "Empty response from API";
            }

            return content.Trim();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling OpenRouter API");
            throw new Exception($"Network error: {ex.Message}", ex);
        }
        catch (Exception ex) when (!ex.Message.StartsWith("API error"))
        {
            _logger.LogError(ex, "Failed to call OpenRouter API");
            throw new Exception($"OpenRouter API error: {ex.Message}", ex);
        }
    }

    private static string? ParseErrorMessage(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                    return message.GetString();
                if (error.TryGetProperty("code", out var code))
                    return code.GetString();
            }
        }
        catch { }
        return null;
    }

    private class OpenRouterRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }
    }

    private class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OpenRouterResponse
    {
        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; set; }

        [JsonPropertyName("error")]
        public OpenRouterError? Error { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }

    private class OpenRouterError
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}
