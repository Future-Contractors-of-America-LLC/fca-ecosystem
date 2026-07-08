using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AuricruxBackend.Controllers;

[ApiController]
[Route("api/auricrux")]
public class AuricruxController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly ITtsService _ttsService;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly ILogger<AuricruxController> _logger;

    public AuricruxController(
        ILlmService llmService,
        ITtsService ttsService,
        IPromptBuilderService promptBuilder,
        ILogger<AuricruxController> logger)
    {
        _llmService = llmService;
        _ttsService = ttsService;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Chat endpoint - compatible with mobile, desktop, and web clients
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validation
            if (request?.Messages == null || request.Messages.Count == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "No messages provided",
                    Timestamp = DateTime.UtcNow
                });
            }

            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

            _logger.LogInformation(
                "[{SessionId}] Chat request from {Source} - ThinkingMode: {ThinkingMode}, SearchScope: {SearchScope}",
                sessionId, request.Source, request.ThinkingMode, request.SearchScope);

            // Start timing
            var stopwatch = Stopwatch.StartNew();

            // Build system prompt
            var systemPrompt = _promptBuilder.BuildSystemPrompt(
                request.ThinkingMode,
                request.SearchScope);

            // Get LLM response
            var responseContent = await _llmService.GetChatResponseAsync(
                request.Messages,
                systemPrompt,
                request.ThinkingMode,
                cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "[{SessionId}] Response generated in {ElapsedMs}ms ({CharCount} chars)",
                sessionId, stopwatch.ElapsedMilliseconds, responseContent.Length);

            // Build response
            var response = new ChatResponse
            {
                SessionId = sessionId,
                ThinkingMode = request.ThinkingMode,
                SearchScope = request.SearchScope,
                Source = request.Source,
                Response = responseContent,
                Timestamp = DateTime.UtcNow,
                Metadata = new ChatMetadata
                {
                    ModelUsed = Environment.GetEnvironmentVariable("LLM:Model") ?? "unknown",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    TokensUsed = EstimateTokens(responseContent)
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat endpoint");
            return StatusCode(500, new ErrorResponse
            {
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Text-to-speech endpoint
    /// </summary>
    [HttpPost("speak")]
    public async Task<IActionResult> Speak(
        [FromBody] SpeakRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.Text))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "No text provided",
                    Timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("TTS request for {Format} format ({CharCount} chars)", 
                request.Format, request.Text.Length);

            var audioStream = await _ttsService.SynthesizeAsync(
                request.Text,
                request.Voice,
                request.Format,
                cancellationToken);

            var contentType = request.Format.ToLower() switch
            {
                "mp3" => "audio/mpeg",
                "ogg" => "audio/ogg",
                "wav" => "audio/wav",
                _ => "audio/wav"
            };

            return File(audioStream, contentType, inline: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in speak endpoint");
            return StatusCode(500, new ErrorResponse
            {
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Feedback endpoint
    /// </summary>
    [HttpPost("feedback")]
    public IActionResult Feedback([FromBody] FeedbackRequest request)
    {
        try
        {
            _logger.LogInformation(
                "[{SessionId}] Feedback received from {Source}: rating={Rating}",
                request.SessionId, request.Source, request.Rating);

            // TODO: Store feedback in database
            // For now, just acknowledge

            return Ok(new
            {
                success = true,
                sessionId = request.SessionId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in feedback endpoint");
            return StatusCode(500, new ErrorResponse
            {
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<HealthResponse>> Health(CancellationToken cancellationToken)
    {
        try
        {
            var llmHealthy = await _llmService.HealthCheckAsync(cancellationToken);
            var ttsHealthy = await _ttsService.HealthCheckAsync(cancellationToken);

            var response = new HealthResponse
            {
                Status = (llmHealthy && ttsHealthy) ? "healthy" : "degraded",
                Timestamp = DateTime.UtcNow,
                Services = new Dictionary<string, string>
                {
                    { "api", "healthy" },
                    { "llm", llmHealthy ? "healthy" : "unavailable" },
                    { "tts", ttsHealthy ? "healthy" : "unavailable" }
                },
                Config = new Dictionary<string, string>
                {
                    { "llm_provider", Environment.GetEnvironmentVariable("LLM:Provider") ?? "unknown" },
                    { "tts_provider", Environment.GetEnvironmentVariable("TTS:Provider") ?? "unknown" },
                    { "model", Environment.GetEnvironmentVariable("LLM:Model") ?? "unknown" }
                }
            };

            var statusCode = llmHealthy ? 200 : 503;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in health endpoint");
            return StatusCode(500, new HealthResponse
            {
                Status = "error",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Rough token estimation (1 token ≈ 4 characters)
    /// </summary>
    private int EstimateTokens(string text)
    {
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}
