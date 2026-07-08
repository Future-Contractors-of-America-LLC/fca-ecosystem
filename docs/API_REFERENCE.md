# Auricrux API Reference

Complete API documentation for the Auricrux Open-Source Backend.

## Base URL

```
http://localhost:5000
https://api.your-domain.com  (production)
```

## Authentication

Currently, the API is **unauthenticated**. For production, consider adding:
- API key authentication
- JWT bearer tokens
- OAuth 2.0

## Rate Limiting

- **Default**: 100 requests per 15 minutes per IP
- **Rate limit headers**:
  - `X-RateLimit-Limit`: 100
  - `X-RateLimit-Remaining`: 99
  - `X-RateLimit-Reset`: Unix timestamp

## Endpoints

### 1. Chat Endpoint

**POST** `/api/auricrux/chat`

Main endpoint for conversational AI. Handles all thinking modes and search scopes.

#### Request

```json
{
  "messages": [
    {
      "role": "user",
      "content": "How much concrete do I need for a 10x10 patio?"
    }
  ],
  "thinkingMode": "auto",
  "searchScope": "both",
  "source": "mobile",
  "userId": "user123",
  "sessionId": "uuid-or-auto-generated"
}
```

##### Request Fields

| Field | Type | Required | Values | Description |
|-------|------|----------|--------|-------------|
| `messages` | Array | ✅ Yes | ChatMessage[] | Conversation history |
| `messages[].role` | String | ✅ Yes | `user`, `assistant` | Message sender |
| `messages[].content` | String | ✅ Yes | Text | Message content |
| `thinkingMode` | String | ❌ No | `quick`, `auto`, `deep` | Response complexity (default: `auto`) |
| `searchScope` | String | ❌ No | `internal`, `public`, `both` | Knowledge source (default: `internal`) |
| `source` | String | ❌ No | `mobile`, `desktop`, `web` | Client platform |
| `userId` | String | ❌ No | Any string | For analytics/logging |
| `sessionId` | String | ❌ No | UUID | Session tracking (auto-generated if omitted) |

#### Response

```json
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "thinkingMode": "auto",
  "searchScope": "both",
  "source": "mobile",
  "response": "For a 10x10 patio with 4 inches depth:\n\n1. Calculate volume: 10 ft × 10 ft × (4/12) ft = 33.33 cubic feet\n2. Convert to cubic yards: 33.33 ÷ 27 = 1.23 cubic yards\n3. Account for waste: 1.23 × 1.05 = 1.29 cubic yards\n4. Round up: Order 1.5 cubic yards\n\nCost estimate: ~$90-150 depending on your location.",
  "timestamp": "2024-01-15T10:30:45.123Z",
  "metadata": {
    "tokensUsed": 287,
    "modelUsed": "gpt-4",
    "processingTimeMs": 2456
  }
}
```

##### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `sessionId` | String | Unique identifier for this conversation |
| `thinkingMode` | String | Applied thinking mode |
| `searchScope` | String | Applied search scope |
| `source` | String | Client platform |
| `response` | String | AI response |
| `timestamp` | ISO 8601 | Response timestamp (UTC) |
| `metadata.tokensUsed` | Number | Tokens consumed by LLM |
| `metadata.modelUsed` | String | Model that generated response |
| `metadata.processingTimeMs` | Number | Total processing time in milliseconds |

#### Thinking Modes

- **`quick`** ⚡
  - 2-3 second response
  - Concise, direct answers
  - ~500 token limit
  - Use case: Mobile users, quick lookups

- **`auto`** 🤖
  - 5 second response (default)
  - Balanced depth and brevity
  - ~1500 token limit
  - Use case: General purpose

- **`deep`** 🧠
  - 10 second response
  - Comprehensive analysis
  - ~2000 token limit
  - Use case: Complex problems, decision making

#### Search Scopes

- **`internal`** 📚
  - Uses only internal FCA knowledge base
  - References established standards
  - Use case: FCA-specific queries, compliance

- **`public`** 🌐
  - Uses current market rates
  - Public regulations, industry data
  - Use case: Current pricing, market trends

- **`both`** 🔄
  - Combines internal + public (default)
  - Balanced approach
  - Use case: Most queries

#### Error Responses

**400 - Bad Request**
```json
{
  "error": "No messages provided",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

**500 - Internal Server Error**
```json
{
  "error": "LLM API request failed",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

#### Example Curl

```bash
curl -X POST http://localhost:5000/api/auricrux/chat \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {"role": "user", "content": "What is the cost per square foot for stamped concrete?"}
    ],
    "thinkingMode": "deep",
    "searchScope": "public",
    "source": "mobile"
  }'
```

---

### 2. Text-to-Speech Endpoint

**POST** `/api/auricrux/speak`

Convert response text to audio for accessibility.

#### Request

```json
{
  "text": "Pour the concrete to a depth of 4 inches for a standard patio.",
  "voice": "default",
  "format": "wav"
}
```

##### Request Fields

| Field | Type | Required | Values | Description |
|-------|------|----------|--------|-------------|
| `text` | String | ✅ Yes | 1-5000 chars | Text to synthesize |
| `voice` | String | ❌ No | `default`, `en`, `de`, `fr`, etc. | Voice/language (default: `default`) |
| `format` | String | ❌ No | `wav`, `mp3`, `ogg` | Audio format (default: `wav`) |

#### Response

Binary audio stream with appropriate content-type:
- `audio/wav` for WAV format
- `audio/mpeg` for MP3 format
- `audio/ogg` for OGG format

#### Error Responses

**400 - Bad Request**
```json
{
  "error": "No text provided",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

**413 - Payload Too Large**
```json
{
  "error": "Text too long (max 5000 characters)",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

#### Example Curl

```bash
# Get audio file
curl -X POST http://localhost:5000/api/auricrux/speak \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Concrete should cure for at least 7 days before heavy use.",
    "voice": "default",
    "format": "wav"
  }' \
  --output response.wav
```

---

### 3. Feedback Endpoint

**POST** `/api/auricrux/feedback`

Submit user feedback on responses.

#### Request

```json
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "rating": 5,
  "comment": "Very helpful and accurate cost estimate",
  "source": "mobile"
}
```

##### Request Fields

| Field | Type | Required | Values | Description |
|-------|------|----------|--------|-------------|
| `sessionId` | String | ✅ Yes | UUID | Session from chat response |
| `rating` | Number | ✅ Yes | 1-5 | User rating (1=poor, 5=excellent) |
| `comment` | String | ❌ No | Text | Optional feedback comment |
| `source` | String | ❌ No | `mobile`, `desktop`, `web` | Client platform |

#### Response

```json
{
  "success": true,
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

#### Example Curl

```bash
curl -X POST http://localhost:5000/api/auricrux/feedback \
  -H "Content-Type: application/json" \
  -d '{
    "sessionId": "550e8400-e29b-41d4-a716-446655440000",
    "rating": 5,
    "comment": "Exactly what I needed to know",
    "source": "web"
  }'
```

---

### 4. Health Check Endpoint

**GET** `/api/auricrux/health`

Check system status and service availability.

#### Response

```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:45.123Z",
  "services": {
    "api": "healthy",
    "llm": "healthy",
    "tts": "healthy"
  },
  "config": {
    "llm_provider": "openai",
    "tts_provider": "coqui",
    "model": "gpt-4"
  }
}
```

##### Status Values

- `healthy` - All services operational
- `degraded` - Some services unavailable
- `error` - Critical failure

##### HTTP Status Codes

- **200** - Healthy
- **503** - Service Unavailable (LLM or TTS down)

#### Example Curl

```bash
curl http://localhost:5000/api/auricrux/health

# With pretty print
curl -s http://localhost:5000/api/auricrux/health | jq .
```

---

## Client Implementation Examples

### JavaScript/TypeScript

```typescript
class AuricruxClient {
  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  async chat(
    message: string,
    options: {
      thinkingMode?: 'quick' | 'auto' | 'deep';
      searchScope?: 'internal' | 'public' | 'both';
      history?: any[];
    } = {}
  ) {
    const messages = [
      ...(options.history || []),
      { role: 'user', content: message }
    ];

    const response = await fetch(`${this.baseUrl}/api/auricrux/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        messages,
        thinkingMode: options.thinkingMode || 'auto',
        searchScope: options.searchScope || 'both',
        source: 'web'
      })
    });

    if (!response.ok) throw new Error(await response.text());
    return response.json();
  }

  async speak(text: string, format = 'wav') {
    const response = await fetch(`${this.baseUrl}/api/auricrux/speak`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ text, format })
    });

    if (!response.ok) throw new Error(await response.text());
    return response.blob();
  }
}

// Usage
const client = new AuricruxClient('http://localhost:5000');
const response = await client.chat('How much concrete?', {
  thinkingMode: 'auto',
  searchScope: 'both'
});
console.log(response.response);
```

### Python

```python
import requests
import json

class AuricruxClient:
    def __init__(self, base_url):
        self.base_url = base_url
    
    def chat(self, message, thinking_mode='auto', search_scope='both', history=None):
        messages = (history or []) + [{'role': 'user', 'content': message}]
        
        response = requests.post(
            f'{self.base_url}/api/auricrux/chat',
            json={
                'messages': messages,
                'thinkingMode': thinking_mode,
                'searchScope': search_scope,
                'source': 'python'
            },
            timeout=30
        )
        response.raise_for_status()
        return response.json()
    
    def speak(self, text, format='wav'):
        response = requests.post(
            f'{self.base_url}/api/auricrux/speak',
            json={'text': text, 'format': format},
            timeout=30
        )
        response.raise_for_status()
        return response.content

# Usage
client = AuricruxClient('http://localhost:5000')
result = client.chat('How much concrete?')
print(result['response'])
```

### C#

```csharp
public class AuricruxClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public AuricruxClient(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
    }

    public async Task<ChatResponse> ChatAsync(
        string message,
        string thinkingMode = "auto",
        string searchScope = "both")
    {
        var request = new ChatRequest
        {
            Messages = new[] { new ChatMessage { Role = "user", Content = message } },
            ThinkingMode = thinkingMode,
            SearchScope = searchScope,
            Source = "csharp"
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/auricrux/chat", request);
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsAsync<ChatResponse>();
    }

    public async Task<Stream> SpeakAsync(string text, string format = "wav")
    {
        var request = new { text, format };
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/auricrux/speak", request);
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }
}

// Usage
var client = new AuricruxClient("http://localhost:5000");
var response = await client.ChatAsync("How much concrete?");
Console.WriteLine(response.Response);
```

---

## Error Handling

All errors follow this structure:

```json
{
  "error": "Error message",
  "message": "Optional additional details",
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

### Common HTTP Status Codes

| Code | Meaning | Cause |
|------|---------|-------|
| 200 | OK | Request successful |
| 400 | Bad Request | Invalid input (no messages, etc.) |
| 413 | Payload Too Large | Text too long for TTS |
| 500 | Internal Error | Backend crash, LLM error |
| 503 | Service Unavailable | LLM or TTS service down |

---

## Best Practices

1. **Always include `source`** for analytics
2. **Store `sessionId`** for feedback correlation
3. **Implement retry logic** with exponential backoff
4. **Cache responses** when appropriate
5. **Monitor response times** (alert if > 10s)
6. **Batch requests** sparingly (rate limits apply)
7. **Test health endpoint** before chat requests
8. **Use appropriate thinking modes**:
   - Quick for mobile/UI responsiveness
   - Auto for general purpose
   - Deep for complex decisions

---

## Rate Limits & Quotas

- **API Requests**: 100 per 15 minutes per IP
- **Text Length**: Max 5000 characters per chat message
- **TTS**: Max 5000 characters per synthesize request
- **Concurrent**: Depends on backend resources

---

## Changelog

### v1.0.0 (Current)
- Initial release of OSS backend
- Removed Azure dependencies
- Added Ollama support
- Added Coqui TTS

---

For questions or issues, see the main [README.md](../README.md) or [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md).
