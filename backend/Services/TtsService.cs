namespace AuricruxBackend.Services;

/// <summary>
/// Vendor-independent TTS service supporting multiple providers
/// </summary>
public interface ITtsService
{
    Task<Stream> SynthesizeAsync(
        string text,
        string voice = "default",
        string format = "wav",
        CancellationToken cancellationToken = default);

    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

public class TtsService : ITtsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<TtsService> _logger;

    private readonly string _provider;
    private readonly string _serviceUrl;
    private readonly string? _apiKey;

    public TtsService(HttpClient httpClient, IConfiguration config, ILogger<TtsService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        _provider = config["TTS:Provider"] ?? "coqui";
        _serviceUrl = config["TTS:ServiceUrl"] ?? "http://localhost:5000";
        _apiKey = config["TTS:ApiKey"];

        _logger.LogInformation("TTS Service initialized with provider: {Provider}", _provider);
    }

    public async Task<Stream> SynthesizeAsync(
        string text,
        string voice = "default",
        string format = "wav",
        CancellationToken cancellationToken = default)
    {
        try
        {
            return _provider.ToLower() switch
            {
                "coqui" => await SynthesizeCoquiAsync(text, voice, format, cancellationToken),
                "elevenlabs" => await SynthesizeElevenLabsAsync(text, voice, format, cancellationToken),
                "azure" => await SynthesizeAzureAsync(text, voice, format, cancellationToken),
                "local" => await SynthesizeLocalAsync(text, voice, format, cancellationToken),
                _ => throw new NotSupportedException($"TTS provider '{_provider}' is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing speech");
            throw;
        }
    }

    private async Task<Stream> SynthesizeCoquiAsync(
        string text,
        string voice,
        string format,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            text,
            voice = voice == "default" ? "en" : voice,
            format
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            $"{_serviceUrl}/synthesize",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private async Task<Stream> SynthesizeElevenLabsAsync(
        string text,
        string voice,
        string format,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("ElevenLabs API key not configured");

        var voiceId = voice == "default" ? "21m00Tcm4TlvDq8ikWAM" : voice;

        var request = new
        {
            text,
            voice_settings = new { stability = 0.5, similarity_boost = 0.75 }
        };

        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}");

        httpRequest.Headers.Add("xi-api-key", _apiKey);
        httpRequest.Content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private async Task<Stream> SynthesizeAzureAsync(
        string text,
        string voice,
        string format,
        CancellationToken cancellationToken)
    {
        // Azure Speech TTS (kept for compatibility but not recommended for OSS)
        _logger.LogWarning("Azure TTS is not recommended for OSS deployments");
        throw new NotImplementedException("Use 'coqui' or 'elevenlabs' provider instead");
    }

    private async Task<Stream> SynthesizeLocalAsync(
        string text,
        string voice,
        string format,
        CancellationToken cancellationToken)
    {
        // Local TTS service via HTTP
        var request = new { text, voice, format };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            $"{_serviceUrl}/speak",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_serviceUrl}/health",
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TTS health check failed");
            return false;
        }
    }
}
