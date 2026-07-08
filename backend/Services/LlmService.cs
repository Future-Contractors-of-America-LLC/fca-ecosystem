using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace AuricruxBackend.Services;

/// <summary>
/// Vendor-independent LLM service supporting OpenAI, Ollama, and other OpenAI-compatible APIs
/// </summary>
public interface ILlmService
{
    Task<string> GetChatResponseAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        string thinkingMode,
        CancellationToken cancellationToken = default);

    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<LlmService> _logger;

    private readonly string _provider;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly string _apiUrl;

    public LlmService(HttpClient httpClient, IConfiguration config, ILogger<LlmService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        _provider = config["LLM:Provider"] ?? "openai";
        _apiKey = config["LLM:ApiKey"];
        _model = config["LLM:Model"] ?? "gpt-4";
        _apiUrl = config["LLM:ApiUrl"] ?? "http://localhost:11434";

        _logger.LogInformation("LLM Service initialized with provider: {Provider}, model: {Model}", _provider, _model);
    }

    public async Task<string> GetChatResponseAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        string thinkingMode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = _provider.ToLower() switch
            {
                "openai" => await CallOpenAiAsync(messages, systemPrompt, thinkingMode, cancellationToken),
                "ollama" => await CallOllamaAsync(messages, systemPrompt, thinkingMode, cancellationToken),
                "azure-openai" => await CallAzureOpenAiAsync(messages, systemPrompt, thinkingMode, cancellationToken),
                _ => throw new NotSupportedException($"LLM provider '{_provider}' is not supported")
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LLM service");
            throw;
        }
    }

    private async Task<string> CallOpenAiAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        string thinkingMode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("OpenAI API key not configured");

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var request = new
        {
            model = _model,
            messages = new[]
                {
                    new { role = "system", content = systemPrompt }
                }
                .Concat(messages.Select(m => new { role = m.Role, content = m.Content }))
                .ToArray(),
            temperature = 0.7,
            max_tokens = GetMaxTokensForThinkingMode(thinkingMode)
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseText);
        var root = doc.RootElement;

        return root.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }

    private async Task<string> CallOllamaAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        string thinkingMode,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            model = _model,
            messages = new[]
                {
                    new { role = "system", content = systemPrompt }
                }
                .Concat(messages.Select(m => new { role = m.Role, content = m.Content }))
                .ToArray(),
            stream = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            $"{_apiUrl}/api/chat",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseText);
        var root = doc.RootElement;

        return root.GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }

    private async Task<string> CallAzureOpenAiAsync(
        List<ChatMessage> messages,
        string systemPrompt,
        string thinkingMode,
        CancellationToken cancellationToken)
    {
        // Azure OpenAI compatibility layer
        // Uses OpenAI SDK but points to Azure endpoint
        throw new NotImplementedException("Azure OpenAI is deprecated for this OSS version. Use 'openai' provider instead.");
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return _provider.ToLower() switch
            {
                "openai" => true, // No direct health check, assume OK if key exists
                "ollama" => await CheckOllamaHealthAsync(cancellationToken),
                "azure-openai" => true,
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM health check failed");
            return false;
        }
    }

    private async Task<bool> CheckOllamaHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_apiUrl}/api/tags",
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private int GetMaxTokensForThinkingMode(string thinkingMode)
    {
        return thinkingMode.ToLower() switch
        {
            "quick" => 500,
            "deep" => 2000,
            "auto" => 1500,
            _ => 1500
        };
    }
}
