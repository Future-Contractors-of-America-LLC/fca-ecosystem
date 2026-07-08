# Migration Guide: Azure → Open-Source Auricrux

This guide walks you through migrating from the Azure-dependent version to the open-source, vendor-independent version.

## Overview

| Aspect | Azure Version | OSS Version |
|--------|---------------|------------|
| Backend | Azure Function App | ASP.NET Core (self-hosted) |
| LLM | Azure OpenAI | OpenAI API or Ollama |
| TTS | Azure Speech | Coqui TTS (open-source) |
| Deployment | Azure | Docker, any cloud, or on-premises |
| Cost | High | Low (free with Ollama) |
| Vendor Lock-in | High | None |

## Prerequisites

- Git
- Docker & Docker Compose (recommended) OR .NET 8 + Python 3.11+
- OpenAI API key (optional - can use free Ollama instead)

## Step 1: Deploy New Backend

### Option A: Docker (Recommended)

```bash
# Clone the OSS version into your FCA workspace
cd C:\Users\Auricrux\OneDrive\FCA\Auricrux\auricrux-oss

# Create .env file with your settings
cat > .env << EOF
LLM_API_KEY=sk-your-openai-key
LLM_PROVIDER=openai
LLM_MODEL=gpt-4
TTS_PROVIDER=coqui
ASPNETCORE_ENVIRONMENT=Production
EOF

# Start services
docker-compose up -d

# Verify health
curl http://localhost:5000/api/auricrux/health
```

### Option B: Local (Development)

```bash
# Terminal 1: Backend
cd backend
export LLM__Provider=openai
export LLM__ApiKey=sk-your-key
export TTS__ServiceUrl=http://localhost:5000
dotnet restore
dotnet run

# Terminal 2: TTS Service
cd ../tts-service
pip install -r requirements.txt
python app.py
```

### Option C: Local Ollama (Zero API Costs)

```bash
# Install Ollama: https://ollama.ai/

# Terminal 1: Ollama
ollama serve

# Terminal 2: Pull a model
ollama pull mistral

# Terminal 3: Backend with Ollama
cd backend
export LLM__Provider=ollama
export LLM__ApiUrl=http://localhost:11434
export LLM__Model=mistral
dotnet run

# Terminal 4: TTS
cd ../tts-service && python app.py
```

## Step 2: Update Mobile App

File: `apps/auricrux-mobile/App.tsx`

### Before (Azure hardcoded):
```typescript
const AZURE_ENDPOINT = 'https://auricrux-central.azurewebsites.net/api/auricrux';

async function sendMessage(userMessage) {
  const response = await fetch(`${AZURE_ENDPOINT}/chat`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      messages: [...],
      thinkingMode: 'auto',
      searchScope: 'both'
    })
  });
  // ...
}
```

### After (Environment-based):
```typescript
// Add at top of file
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';

async function sendMessage(userMessage) {
  const response = await fetch(`${API_BASE_URL}/api/auricrux/chat`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      messages: [...],
      thinkingMode: selectedThinkingMode,
      searchScope: selectedSearchScope,
      source: 'mobile'
    })
  });
  // ...
}
```

### Environment Setup

Create `.env.local` in project root:
```
REACT_APP_API_URL=http://localhost:5000
# or for production:
# REACT_APP_API_URL=https://api.your-domain.com
```

For Expo:
```bash
# app.json
{
  "expo": {
    "extra": {
      "apiUrl": "http://localhost:5000"
    }
  }
}
```

## Step 3: Update Desktop App (MAUI)

File: `src/FcaMobile/Pages/AuricruxPage.xaml.cs`

### Before (Azure hardcoded):
```csharp
private const string AzureEndpoint = "https://auricrux-central.azurewebsites.net/api/auricrux/chat";

private async Task SendMessage(string userMessage)
{
    using var httpClient = new HttpClient();
    var request = new { messages = new[] { ... } };
    
    var response = await httpClient.PostAsJsonAsync(AzureEndpoint, request);
    // ...
}
```

### After (Environment-based):
```csharp
private string GetApiBaseUrl()
{
    return Environment.GetEnvironmentVariable("API_BASE_URL") 
        ?? "http://localhost:5000";
}

private async Task SendMessage(string userMessage)
{
    var apiUrl = GetApiBaseUrl();
    var endpoint = $"{apiUrl}/api/auricrux/chat";
    
    using var httpClient = new HttpClient();
    var request = new 
    { 
        messages = new[] { ... },
        thinkingMode = _selectedThinkingMode,
        searchScope = _selectedSearchScope,
        source = "desktop"
    };
    
    var response = await httpClient.PostAsJsonAsync(endpoint, request);
    // ...
}
```

### Configuration Setup

Create `appsettings.json` in project root:
```json
{
  "ApiBaseUrl": "http://localhost:5000"
}
```

Or via environment variable (preferred):
```bash
# Windows
set API_BASE_URL=http://localhost:5000

# Linux/Mac
export API_BASE_URL=http://localhost:5000

# Docker
ENV API_BASE_URL=http://backend:8080
```

## Step 4: Update Web App

File: `src/components/AuricruxDock.jsx`

### Before (Azure hardcoded):
```javascript
const AZURE_API = 'https://auricrux-central.azurewebsites.net';

const send = async (userMessage) => {
  const response = await fetch(`${AZURE_API}/api/auricrux/chat`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ messages: [...] })
  });
  // ...
};
```

### After (Environment-based):
```javascript
const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const send = async (userMessage) => {
  const response = await fetch(`${apiUrl}/api/auricrux/chat`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ 
      messages: [...],
      thinkingMode: thinkingMode,
      searchScope: searchScope,
      source: 'web'
    })
  });
  // ...
};
```

### Vite Configuration

Create `.env.local` (development):
```
VITE_API_URL=http://localhost:5000
```

Create `.env.production` (production):
```
VITE_API_URL=https://api.your-domain.com
```

Or define in `vite.config.js`:
```javascript
export default {
  define: {
    __API_URL__: JSON.stringify(
      process.env.VITE_API_URL || 'http://localhost:5000'
    )
  }
}
```

## Step 5: Test All Platforms

### Mobile App Test

```bash
cd apps/auricrux-mobile

# Set environment
export REACT_APP_API_URL=http://localhost:5000

# Start dev server
npm start
# or
npx expo start

# Test on device/emulator
# Ask: "How much wire do I need for a 100 amp service?"
```

### Desktop App Test

```bash
cd fca-mobile-maui

# Set environment
set API_BASE_URL=http://localhost:5000

# Build and run
dotnet run

# Test chat
# Ask: "What's the cost per square foot for concrete in 2024?"
```

### Web App Test

```bash
cd fca-bid-tracker

# Set environment
export VITE_API_URL=http://localhost:5000

# Start dev server
npm run dev

# Test chat in AuricruxDock
# Ask: "Explain the difference between rebar sizes"
```

## Step 6: Verify Consistency

All three platforms should produce **identical responses** to the same question.

### Test Procedure

1. Ask all three platforms the same question:
   ```
   "How do I calculate the volume of concrete needed for a 10x10x4 inch patio?"
   ```

2. Compare responses:
   - Should be identical or nearly identical
   - If different, check:
     - Both pointing to same backend URL
     - Both using same thinkingMode/searchScope
     - TTS service is working

3. Test thinking modes:
   - Quick mode: Response in ~2 seconds
   - Auto mode: Response in ~5 seconds
   - Deep mode: Response in ~10 seconds

4. Test search scopes:
   - Internal: References FCA knowledge base
   - Public: Includes current market rates
   - Both: Combines both sources

## Step 7: Production Deployment

### Deploy Backend

**Option A: Docker on VPS**
```bash
# Copy docker-compose.yml to server
scp docker-compose.yml user@server:/opt/auricrux/

# SSH and start
ssh user@server
cd /opt/auricrux
docker-compose up -d -f docker-compose.yml

# Set up reverse proxy (Nginx)
# Point https://api.your-domain.com → http://localhost:5000
```

**Option B: Cloud Platform**
```bash
# AWS
aws ecr create-repository --repository-name auricrux-backend
docker tag auricrux-backend:latest $AWS_ACCOUNT.dkr.ecr.$REGION.amazonaws.com/auricrux-backend:latest
docker push $AWS_ACCOUNT.dkr.ecr.$REGION.amazonaws.com/auricrux-backend:latest

# Google Cloud
gcloud run deploy auricrux-backend \
  --image gcr.io/your-project/auricrux-backend \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated

# DigitalOcean App Platform
# Push to GitHub, connect to App Platform, auto-deploy
```

### Deploy Client Apps

**Mobile (EAS Build)**
```bash
cd apps/auricrux-mobile

# Update app.json with new API URL
{
  "extra": {
    "apiUrl": "https://api.your-domain.com"
  }
}

# Build and submit
eas build --platform android --non-interactive
eas submit -p android --latest
```

**Desktop (GitHub Releases)**
```bash
cd fca-mobile-maui

# Build and create release
dotnet publish -c Release

# Upload to GitHub Releases
gh release create v1.0.0-oss ./bin/Release/*/fca-mobile-maui.zip
```

**Web (CI/CD)**
```bash
cd fca-bid-tracker

# Update production environment variable
VITE_API_URL=https://api.your-domain.com npm run build

# Deploy static site
npm run deploy
# or
vercel deploy --prod
```

## Step 8: Monitor & Verify

```bash
# Check backend health
curl https://api.your-domain.com/api/auricrux/health

# View logs
docker-compose logs -f

# Monitor usage
# Check: number of requests, response times, errors

# Set up alerts for:
# - Backend down (HTTP 503)
# - High response time (> 10s)
# - TTS service unavailable
```

## Rollback Plan

If you need to revert to Azure:

```bash
# Revert app changes
git checkout main -- apps/auricrux-mobile/App.tsx src/FcaMobile/Pages/AuricruxPage.xaml.cs src/components/AuricruxDock.jsx

# Redeploy Azure backend
# (Keep old Azure Function App running)

# Rebuild and submit apps
```

## FAQ

**Q: Can I use both Azure and OSS versions at the same time?**
A: Yes! Have separate environment variables for each, let users choose in settings.

**Q: What if the TTS service goes down?**
A: Set `provideFallbackAudio: false` in client apps to gracefully disable TTS. Or route to alternative TTS provider.

**Q: Can I migrate data from Azure?**
A: The OSS version is stateless by default. If you stored chat history in Azure, export it and import into your new backend database.

**Q: How do I scale this?**
A: Use load balancer (Nginx, AWS ALB) in front of multiple backend instances. All are stateless.

**Q: What about security?**
A: All API keys stored in environment variables. Use HTTPS in production. Implement authentication/authorization in backend if needed.

**Q: Do I need to retrain the model?**
A: No! Use the same fine-tuned model via OpenAI API. Or use Ollama with pre-trained open-source models.

## Next Steps

1. ✅ Deploy OSS backend
2. ✅ Update all client apps
3. ✅ Test on all platforms
4. ✅ Set up monitoring
5. ✅ Plan cutover date
6. ✅ Migrate users to new backend
7. ✅ Decommission Azure resources

## Support

- Check logs: `docker-compose logs -f`
- Test endpoints: See API Reference in README
- Debug environment: Set `ASPNETCORE_ENVIRONMENT=Development`
- Get help: Create GitHub issue in fca-ecosystem repo

---

**Migration Complete!** Your Auricrux instance is now vendor-independent and ready for the future. 🚀
