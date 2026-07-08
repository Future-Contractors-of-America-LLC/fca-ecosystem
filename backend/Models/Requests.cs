namespace AuricruxBackend.Models;

/// <summary>
/// Chat request from any client (mobile, desktop, web)
/// </summary>
public class ChatRequest
{
    public List<ChatMessage> Messages { get; set; } = new();
    public string ThinkingMode { get; set; } = "auto"; // quick, auto, deep
    public string SearchScope { get; set; } = "internal"; // internal, public, both
    public string Source { get; set; } = "unknown"; // mobile, desktop, web
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = "user"; // user, assistant, system
    public string Content { get; set; } = "";
}

/// <summary>
/// Chat response returned to all clients
/// </summary>
public class ChatResponse
{
    public string SessionId { get; set; } = "";
    public string ThinkingMode { get; set; } = "";
    public string SearchScope { get; set; } = "";
    public string Source { get; set; } = "";
    public string Response { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public ChatMetadata Metadata { get; set; } = new();
}

public class ChatMetadata
{
    public int TokensUsed { get; set; }
    public string ModelUsed { get; set; } = "";
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// Text-to-speech request
/// </summary>
public class SpeakRequest
{
    public string Text { get; set; } = "";
    public string Voice { get; set; } = "default";
    public string Format { get; set; } = "wav"; // wav, mp3, ogg
}

/// <summary>
/// Feedback on responses
/// </summary>
public class FeedbackRequest
{
    public string? SessionId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public string Source { get; set; } = "";
}

/// <summary>
/// Generic error response
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; } = "";
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Health check response
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = "ok";
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Services { get; set; } = new();
    public Dictionary<string, string> Config { get; set; } = new();
}
