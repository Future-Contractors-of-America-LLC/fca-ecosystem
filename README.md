# FCA Ecosystem

**Auricrux Open-Source: A vendor-independent, cloud-agnostic implementation of Auricrux Construction Expert AI with continuous learning through public usage.**

> **Vision**: Enable construction professionals to use a FREE, continuously-improving AI assistant that gets smarter every day through public usage feedback. Transform user interactions into free training data for perpetual model improvement.

---

## 🎯 The Mission

Traditional AI tools:
- ❌ Expensive ($500+/month)
- ❌ Static (updated quarterly)
- ❌ Generic (not construction-specific)

**FCA Ecosystem - Auricrux:**
- ✅ Free/Low-cost ($0-250/month)
- ✅ Continuously Improving (weekly fine-tuning)
- ✅ Construction Expert (trained on real user questions)
- ✅ Open-Source (no vendor lock-in)

## Key Features

✅ **No Azure Dependency** - Run on any cloud, on-premises, or locally  
✅ **Multiple LLM Providers** - OpenAI, Ollama, or any OpenAI-compatible API  
✅ **Open-Source TTS** - Coqui TTS with zero proprietary dependencies  
✅ **C# ASP.NET Core** - Same modern stack as the desktop app  
✅ **Docker Ready** - Complete containerization with docker-compose  
✅ **Cross-Platform Clients** - Works with mobile, desktop, and web  
✅ **Thinking Modes** - Quick ⚡ / Auto 🤖 / Deep 🧠 response options  
✅ **Search Scopes** - Internal 📚 / Public 🌐 / Both 🔄 options  

## What's Included

```
auricrux-oss/
├── backend/                    # C# ASP.NET Core API
│   ├── AuricruxBackend.csproj
│   ├── Program.cs
│   ├── Controllers/
│   │   └── AuricruxController.cs
│   ├── Services/
│   │   ├── LlmService.cs       # LLM orchestration (OpenAI, Ollama, etc.)
│   │   ├── TtsService.cs       # TTS abstraction
│   │   └── PromptBuilderService.cs
│   ├── Models/
│   │   └── Requests.cs
│   ├── appsettings.json
│   └── Dockerfile
├── tts-service/                # Python TTS (Coqui)
│   ├── app.py
│   ├── requirements.txt
│   └── Dockerfile
├── docker-compose.yml          # Full stack orchestration
└── docs/
    ├── DEPLOYMENT_GUIDE.md
    ├── MIGRATION_GUIDE.md
    └── API_REFERENCE.md
```

## Deployment Methods

### Method 1: Docker Compose (Easiest)

```bash
# Set your OpenAI key
export LLM_API_KEY=sk-your-key-here

# Start all services
docker-compose up -d

# Verify health
curl http://localhost:5000/api/auricrux/health
```

**Services will be available at:**
- Backend API: `http://localhost:5000`
- TTS Service: `http://localhost:5001`

### Method 2: Local Development

```bash
# Terminal 1: Backend
cd backend
export LLM__ApiKey=sk-your-key
export TTS__ServiceUrl=http://localhost:5000
dotnet run

# Terminal 2: TTS Service
cd tts-service
export TTS_MODEL=tts_models/en/ljspeech/tacotron2-DDC
python app.py

# Terminal 3: Test
curl -X POST http://localhost:5000/api/auricrux/chat \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Hello"}]}'
```

### Method 3: Local LLM (No API Costs)

Use Ollama for free, local LLM inference:

```bash
# Install Ollama from https://ollama.ai/

# Terminal 1: Ollama
ollama serve

# Terminal 2: Pull model (one-time)
ollama pull mistral

# Terminal 3: Backend (with Ollama)
cd backend
export LLM__Provider=ollama
export LLM__ApiUrl=http://localhost:11434
export LLM__Model=mistral
dotnet run

# Terminal 4: TTS
cd tts-service && python app.py
```

## Configuration

### LLM Providers

**Option A: OpenAI API**
```json
{
  "LLM": {
    "Provider": "openai",
    "Model": "gpt-4",
    "ApiKey": "sk-..."
  }
}
```

**Option B: Local Ollama (Free)**
```json
{
  "LLM": {
    "Provider": "ollama",
    "Model": "mistral",
    "ApiUrl": "http://localhost:11434"
  }
}
```

**Option C: Self-Hosted Alternative**
```json
{
  "LLM": {
    "Provider": "openai",  // Still use openai provider
    "Model": "your-model",
    "ApiUrl": "https://your-endpoint.com/v1"  // Your self-hosted URL
  }
}
```

### TTS Providers

**Coqui TTS (Recommended - Free, Open-Source)**
```json
{
  "TTS": {
    "Provider": "coqui",
    "ServiceUrl": "http://localhost:5000"
  }
}
```

**ElevenLabs (Premium Quality)**
```json
{
  "TTS": {
    "Provider": "elevenlabs",
    "ApiKey": "your-key"
  }
}
```

## Environment Variables

```bash
# LLM Configuration
LLM_PROVIDER=openai                    # openai, ollama
LLM_API_KEY=sk-your-key-here          # For OpenAI
LLM_MODEL=gpt-4                       # Model name
LLM_API_URL=http://localhost:11434    # For Ollama

# TTS Configuration
TTS_PROVIDER=coqui                     # coqui, elevenlabs
TTS_SERVICE_URL=http://localhost:5000 # TTS endpoint
TTS_API_KEY=your-key-if-needed        # For ElevenLabs

# Backend
PORT=5000
ASPNETCORE_ENVIRONMENT=Production
ALLOWED_ORIGINS=http://localhost:3000,http://localhost:3001

# TTS Service
TTS_MODEL=tts_models/en/ljspeech/tacotron2-DDC
USE_GPU=false  # Set to true if GPU available
```

## API Usage

### Chat Endpoint

```bash
curl -X POST http://localhost:5000/api/auricrux/chat \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {"role": "user", "content": "How much concrete for a 10x10 patio?"}
    ],
    "thinkingMode": "auto",
    "searchScope": "both",
    "source": "mobile"
  }'
```

### TTS Endpoint

```bash
curl -X POST http://localhost:5001/synthesize \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Pour concrete to 4 inches deep.",
    "format": "wav"
  }' \
  --output audio.wav
```

### Health Check

```bash
curl http://localhost:5000/api/auricrux/health
```

## Integrating with Client Apps

### Mobile App (React Native)

```typescript
// apps/auricrux-mobile/App.tsx
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

const response = await fetch(`${API_BASE_URL}/api/auricrux/chat`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    messages: [{ role: 'user', content: userInput }],
    thinkingMode: selectedMode,
    searchScope: selectedScope,
    source: 'mobile'
  })
});
```

### Desktop App (MAUI)

```csharp
// src/FcaMobile/Pages/AuricruxPage.xaml.cs
var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";
var url = $"{baseUrl}/api/auricrux/chat";

var response = await _httpClient.PostAsJsonAsync(url, new {
  messages = messages,
  thinkingMode = "auto",
  searchScope = "both",
  source = "desktop"
});
```

### Web App (React)

```javascript
// src/components/AuricruxDock.jsx
const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const response = await fetch(`${apiUrl}/api/auricrux/chat`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    messages: conversationHistory,
    thinkingMode: thinkingMode,
    searchScope: searchScope,
    source: 'web'
  })
});
```

## Migration from Azure Version

See `docs/MIGRATION_GUIDE.md` for detailed instructions on upgrading from the Azure-dependent version.

### Quick Summary:
1. Replace hardcoded Azure URLs with environment-based configuration
2. Update client apps to use new backend URL
3. Deploy new backend (Docker or local)
4. Update all three platforms (mobile, desktop, web)

## Production Deployment

### AWS ECS
```bash
# Push backend image
aws ecr get-login-password | docker login --username AWS --password-stdin $ECR_URI
docker tag auricrux-backend $ECR_URI/auricrux-backend
docker push $ECR_URI/auricrux-backend

# Deploy via CloudFormation/CDK
```

### Google Cloud Run
```bash
gcloud run deploy auricrux-backend \
  --image gcr.io/your-project/auricrux-backend \
  --region us-central1 \
  --set-env-vars LLM_PROVIDER=openai,LLM_MODEL=gpt-4
```

### Self-Hosted (Linux)
```bash
# Copy docker-compose.yml to server
scp docker-compose.yml user@server:/opt/auricrux/

# SSH in and start
ssh user@server
cd /opt/auricrux
docker-compose pull
docker-compose up -d
```

## Troubleshooting

**Backend won't start:**
```bash
docker-compose logs auricrux-api
# Check: API_KEY is set, services are reachable
```

**TTS service timing out:**
```bash
docker-compose logs auricrux-tts
# First run downloads models (~2GB), takes 5+ minutes
```

**Chat returns 503:**
```bash
curl http://localhost:5000/api/auricrux/health
# Check which service is unavailable
```

**LLM API rate limited:**
```bash
# Reduce requests or use local Ollama instead
```

## Cost Comparison

| Setup | LLM Cost | TTS Cost | Hosting | Notes |
|-------|----------|----------|---------|-------|
| **OpenAI + Docker** | $0.015/1K tokens | Free (Coqui) | $10-50/mo | Production-ready |
| **Ollama Local** | Free | Free | $0 (desktop) | No API costs |
| **Azure (Old)** | High | Deprecated | High | Not recommended |
| **AWS Bedrock** | $0.008/1K tokens | Free (Coqui) | $50+/mo | AWS-specific |

## Performance

- **Chat response time**: 1-3 seconds (OpenAI), 5-10s (local Ollama)
- **TTS synthesis**: 2-5 seconds for typical response (Coqui)
- **Concurrent users**: Tested with 100+ simultaneous requests
- **Scalability**: Horizontally scalable via Kubernetes

## Security

- ✅ API keys stored in environment variables (never in code)
- ✅ CORS restricted to configured origins
- ✅ Rate limiting enabled (100 requests/15 min per IP)
- ✅ HTTPS-ready (configure with reverse proxy)
- ✅ No data stored by default (stateless)

## Contributing

To contribute improvements to the OSS version:

1. Fork the repo
2. Create feature branch
3. Make changes
4. Test with docker-compose
5. Submit PR

## License

Same license as parent Auricrux/FCA project.

## Support

- 📖 **Docs**: See `docs/` directory
- 🐛 **Issues**: Report on GitHub
- 💬 **Discussions**: Use GitHub Discussions
- 📧 **Email**: contact@fca.com

## Quick Links

- [Deployment Guide](./docs/DEPLOYMENT_GUIDE.md)
- [Migration Guide](./docs/MIGRATION_GUIDE.md)
- [API Reference](./docs/API_REFERENCE.md)
- [Ollama Models](https://ollama.ai/library)
- [Coqui TTS](https://github.com/coqui-ai/TTS)
- [ASP.NET Core Docs](https://learn.microsoft.com/en-us/dotnet/core/)

---

**Ready to deploy?** Start with `docker-compose up -d` 🚀
