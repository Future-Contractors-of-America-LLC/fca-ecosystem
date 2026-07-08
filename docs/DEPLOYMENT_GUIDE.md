# Auricrux Open-Source Backend - Deployment Guide

This is a complete, vendor-independent version of Auricrux that removes all Azure/Microsoft dependencies.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  Clients (Mobile, Desktop, Web)                         │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│  ASP.NET Core Backend (C#)                              │
│  - Chat API (/api/auricrux/chat)                        │
│  - TTS API (/api/auricrux/speak)                        │
│  - Health check (/api/auricrux/health)                  │
└─────────────────────────────────────────────────────────┘
         │                                      │
         ▼                                      ▼
    ┌─────────┐                           ┌─────────────┐
    │ LLM API │                           │ TTS Service │
    │ (OpenAI)│                           │ (Coqui)     │
    │ (Ollama)│                           │ (Python)    │
    └─────────┘                           └─────────────┘
```

## Quick Start (Docker)

### Prerequisites

- Docker & Docker Compose installed
- OpenAI API key (or local Ollama running)

### 1. Set Environment Variables

```bash
# Create .env file
cat > .env << EOF
LLM_API_KEY=sk-your-openai-key-here
LLM_PROVIDER=openai
LLM_MODEL=gpt-4
TTS_PROVIDER=coqui
DEBUG=false
EOF
```

### 2. Start Services

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Check health
curl http://localhost:5000/api/auricrux/health
```

### 3. Test Chat Endpoint

```bash
curl -X POST http://localhost:5000/api/auricrux/chat \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {"role": "user", "content": "How much concrete do I need for a 10x10 patio?"}
    ],
    "thinkingMode": "auto",
    "searchScope": "both",
    "source": "mobile"
  }'
```

### 4. Test TTS Endpoint

```bash
curl -X POST http://localhost:5001/synthesize \
  -H "Content-Type: application/json" \
  -d '{
    "text": "The concrete should be poured to a depth of 4 inches.",
    "voice": "en",
    "format": "wav"
  }' \
  --output response.wav
```

## Local Development (Without Docker)

### Prerequisites

- .NET 8 SDK
- Python 3.11+
- Node.js (for web frontend)

### 1. Backend Setup

```bash
cd backend

# Set environment variables
export LLM__ApiKey=sk-your-key-here
export LLM__Provider=openai
export TTS__ServiceUrl=http://localhost:5000

# Run
dotnet restore
dotnet run
```

Backend will be available at: `http://localhost:5000`

### 2. TTS Service Setup

```bash
cd ../tts-service

# Install dependencies
pip install -r requirements.txt

# Set environment variables
export TTS_MODEL=tts_models/en/ljspeech/tacotron2-DDC
export USE_GPU=false

# Run
python app.py
```

TTS will be available at: `http://localhost:5000`

### 3. Use with Local LLM (Ollama)

Instead of OpenAI API, you can run LLMs locally:

```bash
# Install Ollama: https://ollama.ai/

# Start Ollama
ollama serve

# In another terminal, pull a model
ollama pull mistral  # or llama2, neural-chat, etc.

# Configure backend to use Ollama
export LLM__Provider=ollama
export LLM__ApiUrl=http://localhost:11434
export LLM__Model=mistral

# Run backend
cd backend && dotnet run
```

## Configuration

### LLM Providers

Edit `backend/appsettings.json`:

```json
{
  "LLM": {
    "Provider": "openai|ollama|azure-openai",
    "Model": "gpt-4|mistral|llama2|etc",
    "ApiKey": "your-api-key",
    "ApiUrl": "http://localhost:11434"  // for Ollama
  }
}
```

**Supported Providers:**

- **OpenAI** (`provider: openai`)
  - Models: `gpt-4`, `gpt-3.5-turbo`, etc.
  - Requires: `LLM:ApiKey`
  - Cost: Pay-per-token

- **Ollama** (`provider: ollama`)
  - Models: `mistral`, `llama2`, `neural-chat`, etc.
  - Requires: Ollama running locally
  - Cost: Free (runs locally)
  - Installation: https://ollama.ai/

- **Azure OpenAI** (deprecated for OSS)
  - Not recommended for open-source deployments

### TTS Providers

Edit `backend/appsettings.json`:

```json
{
  "TTS": {
    "Provider": "coqui|elevenlabs|local",
    "ServiceUrl": "http://localhost:5000",
    "ApiKey": "optional-api-key"
  }
}
```

**Supported Providers:**

- **Coqui** (`provider: coqui`) - **RECOMMENDED**
  - Open-source, high-quality
  - Models: tacotron2, glow-tts, xtts-v2
  - Cost: Free
  - Runs locally or in Docker

- **ElevenLabs** (`provider: elevenlabs`)
  - High-quality voices
  - Requires: API key
  - Cost: Pay-per-character

- **Local Service** (`provider: local`)
  - Custom HTTP endpoint
  - Useful for self-hosted alternatives

## Deployment Options

### Option 1: Docker Compose (Recommended)

```bash
docker-compose up -d
```

**Pros:**
- Easy to deploy
- All services isolated
- Environment-ready

**Cons:**
- Requires Docker installed

### Option 2: Kubernetes

See `kubernetes/` directory for Helm charts (coming soon).

### Option 3: Self-Hosted Linux Server

```bash
# Install .NET 8 runtime
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --runtime aspnetcore --version 8.0

# Install Python
apt-get install python3.11 python3-pip

# Clone repo and run
git clone <repo>
cd auricrux-oss

# Start backend
cd backend
dotnet run -c Release &

# Start TTS
cd ../tts-service
python3 app.py &
```

### Option 4: Cloud Deployment

**AWS:**
- Backend: ECS/Fargate + Application Load Balancer
- TTS: ECS/Fargate (optional, can use serverless SageMaker)
- LLM: Use Bedrock API instead of OpenAI

**Google Cloud:**
- Backend: Cloud Run
- TTS: Vertex AI Text-to-Speech (or keep Coqui)
- LLM: Vertex AI Generative AI (or keep OpenAI)

**DigitalOcean:**
- Backend: App Platform
- TTS: Droplet (simple VM)
- LLM: Managed via environment variable to external API

## API Reference

### Chat Endpoint

```
POST /api/auricrux/chat
```

**Request:**
```json
{
  "messages": [
    {"role": "user", "content": "..."},
    {"role": "assistant", "content": "..."}
  ],
  "thinkingMode": "quick|auto|deep",
  "searchScope": "internal|public|both",
  "source": "mobile|desktop|web",
  "userId": "optional-user-id",
  "sessionId": "optional-session-id"
}
```

**Response:**
```json
{
  "sessionId": "uuid",
  "thinkingMode": "auto",
  "searchScope": "both",
  "source": "mobile",
  "response": "...",
  "timestamp": "2024-01-15T10:30:00Z",
  "metadata": {
    "tokensUsed": 1234,
    "modelUsed": "gpt-4",
    "processingTimeMs": 2500
  }
}
```

### TTS Endpoint

```
POST /api/auricrux/speak
```

**Request:**
```json
{
  "text": "The concrete should be poured to a depth of 4 inches.",
  "voice": "default|en|de|fr|etc",
  "format": "wav|mp3|ogg"
}
```

**Response:**
- Binary audio stream
- Content-Type: `audio/wav` (or appropriate format)

### Health Endpoint

```
GET /api/auricrux/health
```

**Response:**
```json
{
  "status": "healthy|degraded|error",
  "timestamp": "2024-01-15T10:30:00Z",
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

## Troubleshooting

### Backend won't start

```bash
# Check logs
docker-compose logs auricrux-api

# Verify environment variables
echo $LLM_API_KEY
echo $LLM_PROVIDER

# Test connectivity to LLM
curl https://api.openai.com/v1/models -H "Authorization: Bearer $LLM_API_KEY"
```

### TTS service not responding

```bash
# Check TTS logs
docker-compose logs auricrux-tts

# TTS models take time to download on first run
docker-compose logs auricrux-tts | grep -i "downloading\|model"

# Manually test
curl http://localhost:5001/health
```

### Ollama connection issues

```bash
# Verify Ollama is running
curl http://localhost:11434/api/tags

# If not running locally, update appsettings.json:
# "ApiUrl": "http://remote-ollama-server:11434"
```

### API returns 503

```bash
# Check health endpoint
curl http://localhost:5000/api/auricrux/health

# Services available?
docker-compose ps
```

## Migration from Azure Version

If you're upgrading from the Azure-based version:

### Code Changes

**Mobile App** (`apps/auricrux-mobile/App.tsx`):
```typescript
// OLD (Azure hardcoded)
const response = await fetch('https://auricrux-central.azurewebsites.net/api/auricrux/chat', ...);

// NEW (environment-based)
const apiUrl = process.env.REACT_APP_API_URL || 'http://localhost:5000';
const response = await fetch(`${apiUrl}/api/auricrux/chat`, ...);
```

**MAUI Desktop** (`src/FcaMobile/Pages/AuricruxPage.xaml.cs`):
```csharp
// OLD (Azure hardcoded)
var url = "https://auricrux-central.azurewebsites.net/api/auricrux/chat";

// NEW (environment-based)
var baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";
var url = $"{baseUrl}/api/auricrux/chat";
```

**Web App** (`src/components/AuricruxDock.jsx`):
```javascript
// OLD (Azure hardcoded)
const api = new ApiClient('https://auricrux-central.azurewebsites.net');

// NEW (environment-based)
const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';
const api = new ApiClient(apiUrl);
```

### Deployment

1. Stop Azure Function App
2. Update client apps to point to new backend URL
3. Deploy Docker containers or run locally
4. Test all platforms (mobile, desktop, web)

## Production Checklist

- [ ] Configure HTTPS/TLS certificates
- [ ] Set up rate limiting (already in backend)
- [ ] Configure CORS origins correctly
- [ ] Set up logging and monitoring
- [ ] Configure backup/persistence for TTS models
- [ ] Set up error alerting
- [ ] Configure automatic restarts
- [ ] Load testing completed
- [ ] Security audit passed
- [ ] Documentation updated

## License

This open-source version maintains the same license as the FCA Auricrux project.

## Support

For issues or questions:
1. Check logs: `docker-compose logs -f`
2. Review troubleshooting section above
3. Test with curl commands provided
4. Check GitHub issues in fca-ecosystem repo
