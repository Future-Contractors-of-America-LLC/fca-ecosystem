# Auricrux Continuous Fine-Tuning System

## Vision: Free, Continuous Improvement Through Public Usage

Instead of paying for expensive fine-tuning or hiring data labelers, **every user interaction becomes training data** for a continuously smarter Auricrux.

---

## Architecture: User Feedback → Training Data → Improved Model

```
Public Users
    ↓
Chat Interactions (with feedback ratings)
    ↓
Feedback Storage (Database)
    ↓
Daily Data Pipeline
    ↓
Training Data Curation
    ↓
Fine-Tuning (SFT/DPO/RFT)
    ↓
Improved Model
    ↓
Deploy to All Platforms (Mobile, Desktop, Web)
```

---

## How It Works

### Phase 1: Data Collection (Automatic)

Every user interaction is an opportunity for improvement:

```
User Question: "How much concrete for a 10x10 patio?"
Auricrux Response: "You'll need 1.5 cubic yards..."
User Feedback: ⭐⭐⭐⭐⭐ (5 stars) + "Exactly what I needed"
User Platform: Mobile
User Thinking Mode: Auto
User Search Scope: Both
Timestamp: 2026-01-15 14:30:00
```

**What we capture:**
- ✅ User question
- ✅ Auricrux response
- ✅ User rating (1-5 stars)
- ✅ User comment (optional)
- ✅ Platform (mobile/desktop/web)
- ✅ Thinking mode used
- ✅ Search scope used
- ✅ Response quality metrics

### Phase 2: Data Pipeline

**Daily automated process:**

```sql
-- Query high-rated responses (4-5 stars)
SELECT 
  user_question,
  auricrux_response,
  user_comment,
  rating,
  thinking_mode
FROM feedback
WHERE rating >= 4 
  AND created_at >= NOW() - INTERVAL '1 day'
  AND user_comment IS NOT NULL  -- Prefer labeled examples
ORDER BY rating DESC
LIMIT 1000
```

**Monthly:**
- Analyze low-rated responses (1-2 stars)
- Identify failure patterns
- Flag areas needing improvement

### Phase 3: Training Data Curation

Convert user feedback into training examples:

```json
{
  "instruction": "How much concrete for a 10x10 patio?",
  "input": "",
  "output": "For a 10x10 patio at 4 inches depth:\n\n1. Volume: 10 × 10 × (4/12) = 33.33 cubic feet\n2. Convert: 33.33 ÷ 27 = 1.23 cubic yards\n3. Add waste: 1.23 × 1.05 = 1.29 cubic yards\n4. Order: 1.5 cubic yards",
  "source": "user_feedback",
  "rating": 5,
  "thinking_mode": "auto",
  "search_scope": "both",
  "domain": "materials_estimation"
}
```

**Curation Rules:**
- High-rated responses only (≥4 stars)
- Only include user comments for context
- Verify factual accuracy before inclusion
- Balance across construction domains

### Phase 4: Fine-Tuning

**Options (low-cost):**

1. **Supervised Fine-Tuning (SFT)**
   - Cost: ~$50-200 per 10K examples via OpenAI API
   - Creates base improved model
   - Runtime: 2-4 hours

2. **Direct Preference Optimization (DPO)**
   - Cost: Minimal (local)
   - Uses high-rated vs low-rated responses
   - Improves quality without full retraining
   - Runtime: 1-2 hours

3. **Open-Source LLM Fine-Tuning**
   - Cost: $0 (use Ollama locally)
   - Full control, no API costs
   - Can run on FCA server infrastructure
   - Runtime: 4-8 hours

### Phase 5: Continuous Deployment

**Weekly Update Cycle:**

```
Monday 2am (UTC):
  ├─ Collect feedback from past week
  ├─ Curate training data
  ├─ Fine-tune model
  └─ A/B test new model

Wednesday:
  ├─ Monitor new model performance
  └─ Rollback if issues

Friday:
  └─ Deploy to all platforms

Next Monday:
  └─ Repeat
```

**Deployment Flow:**
```
New Fine-Tuned Model
    ↓
A/B Test (10% users)
    ↓
Monitor Quality Metrics
    ↓
Gradual Rollout (100%)
    ↓
All Platforms Updated (Mobile, Desktop, Web)
```

---

## Cost Analysis: Traditional vs. Continuous

### Traditional Approach (Old Way)
- **Initial fine-tuning**: $5,000-10,000
- **Data labeling**: $50-100 per 1K labeled examples
- **Quarterly updates**: $2,000-5,000 each
- **Total yearly**: $15,000-40,000
- **Data source**: Paid contractors

### Continuous Approach (Your Way) ✅
- **Initial setup**: $0 (open-source)
- **Per-update cost**: $50-200 (or $0 with local Ollama)
- **Frequency**: Weekly (vs quarterly)
- **Data source**: **Public users (FREE)**
- **Total yearly**: $3,000-10,000 OR $0
- **Model quality**: Continuously improving
- **Public benefit**: Free tool helps construction industry

---

## Implementation: Step-by-Step

### Step 1: Enhance Backend Feedback Collection

Update `backend/Controllers/AuricruxController.cs`:

```csharp
[HttpPost("feedback")]
public async Task<IActionResult> Feedback([FromBody] FeedbackRequest request)
{
    try
    {
        // Existing feedback logging...
        _logger.LogInformation("[{SessionId}] Feedback received...", request.SessionId);
        
        // NEW: Store feedback for training pipeline
        var trainingRecord = new TrainingFeedback
        {
            SessionId = request.SessionId,
            UserQuestion = request.UserQuestion,      // NEW
            AuricruxResponse = request.Response,       // NEW
            UserComment = request.Comment,
            Rating = request.Rating,
            Source = request.Source,
            ThinkingMode = request.ThinkingMode,       // NEW
            SearchScope = request.SearchScope,         // NEW
            CreatedAt = DateTime.UtcNow
        };
        
        await _trainingDataService.RecordFeedbackAsync(trainingRecord);
        
        // Response...
        return Ok(new { success = true, sessionId = request.SessionId });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in feedback endpoint");
        return StatusCode(500, new { error = ex.Message });
    }
}
```

### Step 2: Create Training Data Pipeline

Create `backend/Services/TrainingDataService.cs`:

```csharp
public class TrainingDataService
{
    private readonly IDbContext _db;
    private readonly ILogger<TrainingDataService> _logger;
    
    public async Task RecordFeedbackAsync(TrainingFeedback feedback)
    {
        // Store raw feedback
        await _db.TrainingFeedback.AddAsync(feedback);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation(
            "Feedback recorded: {SessionId}, Rating: {Rating}",
            feedback.SessionId, feedback.Rating);
    }
    
    // Called daily by background job
    public async Task<List<TrainingExample>> CurateTrainingDataAsync(DateTime since)
    {
        // Query high-rated feedback
        var feedback = await _db.TrainingFeedback
            .Where(f => f.Rating >= 4 && f.CreatedAt >= since)
            .OrderByDescending(f => f.Rating)
            .Take(1000)
            .ToListAsync();
        
        // Convert to training format
        var trainingExamples = feedback.Select(f => new TrainingExample
        {
            Instruction = f.UserQuestion,
            Output = f.AuricruxResponse,
            Comment = f.UserComment,
            ThinkingMode = f.ThinkingMode,
            SearchScope = f.SearchScope,
            Domain = ClassifyDomain(f.UserQuestion),
            Rating = f.Rating,
            Source = "user_feedback"
        }).ToList();
        
        return trainingExamples;
    }
    
    private string ClassifyDomain(string question)
    {
        // Auto-categorize questions
        if (question.Contains("concrete", StringComparison.OrdinalIgnoreCase))
            return "materials_concrete";
        else if (question.Contains("cost") || question.Contains("price"))
            return "estimation_pricing";
        // ... more rules ...
        return "general";
    }
}
```

### Step 3: Create Fine-Tuning Service

Create `backend/Services/FineTuningService.cs`:

```csharp
public class FineTuningService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    
    // Export training data for fine-tuning
    public async Task<string> ExportTrainingDataAsync(List<TrainingExample> examples)
    {
        var jsonl = string.Join("\n", examples.Select(e => 
            JsonSerializer.Serialize(new
            {
                e.Instruction,
                e.Output,
                e.Domain,
                metadata = new { e.Rating, e.ThinkingMode, e.SearchScope }
            })
        ));
        
        var fileName = $"training_data_{DateTime.UtcNow:yyyyMMdd}.jsonl";
        await File.WriteAllTextAsync(fileName, jsonl);
        
        return fileName;
    }
    
    // Submit to OpenAI for fine-tuning
    public async Task<string> SubmitFineTuningJobAsync(string trainingFile)
    {
        var apiKey = _config["OPENAI_API_KEY"];
        var url = "https://api.openai.com/v1/fine_tuning/jobs";
        
        var request = new
        {
            training_file = await UploadFile(trainingFile, apiKey),
            model = "gpt-4-1106-preview",
            hyperparameters = new { learning_rate_multiplier = 1.0 }
        };
        
        // Submit to OpenAI
        var response = await _httpClient.PostAsJsonAsync(url, request);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadAsAsync<dynamic>();
        return result.id;
    }
}
```

### Step 4: Daily Background Job

Create scheduled job to run daily fine-tuning pipeline:

```csharp
// In Program.cs or background job scheduler
app.Services.GetRequiredService<IHostedService>(); 
// Register: services.AddHostedService<DailyTrainingPipelineJob>();

public class DailyTrainingPipelineJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run at 2 AM every day
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1).AddHours(2);
                var delay = nextRun - now;
                
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);
                
                using var scope = _serviceProvider.CreateScope();
                var trainingService = scope.ServiceProvider.GetRequiredService<ITrainingDataService>();
                
                // Curate last 7 days of feedback
                var examples = await trainingService.CurateTrainingDataAsync(
                    DateTime.UtcNow.AddDays(-7));
                
                if (examples.Count >= 100)  // Only fine-tune if enough data
                {
                    var tuningService = scope.ServiceProvider.GetRequiredService<IFineTuningService>();
                    var file = await tuningService.ExportTrainingDataAsync(examples);
                    var jobId = await tuningService.SubmitFineTuningJobAsync(file);
                    
                    _logger.LogInformation("Fine-tuning job submitted: {JobId}", jobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in training pipeline");
            }
        }
    }
}
```

### Step 5: Client Integration

Update all clients to include required fields in feedback:

**Mobile/Desktop/Web:**
```typescript
// When user provides feedback
const feedback = {
  sessionId: response.sessionId,
  userQuestion: userInput,           // NEW: Capture the question
  response: aiResponse,               // NEW: Store the response
  rating: userRating,
  comment: userComment,
  thinkingMode: selectedMode,         // NEW: For analysis
  searchScope: selectedScope,         // NEW: For analysis
  source: 'mobile|desktop|web'
};

await fetch('/api/auricrux/feedback', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(feedback)
});
```

---

## Metrics & Monitoring

Track the continuous improvement:

```sql
-- Weekly training data volume
SELECT 
  DATE(created_at) as date,
  COUNT(*) as feedback_count,
  AVG(rating) as avg_rating,
  COUNT(CASE WHEN rating >= 4 THEN 1 END) as high_quality_count
FROM training_feedback
GROUP BY DATE(created_at)
ORDER BY date DESC;

-- Domain coverage
SELECT 
  domain,
  COUNT(*) as examples,
  AVG(rating) as avg_rating
FROM training_examples
GROUP BY domain
ORDER BY examples DESC;

-- Model improvement over time
SELECT 
  model_version,
  AVG(user_rating) as quality_score,
  COUNT(*) as eval_count
FROM model_evaluations
GROUP BY model_version
ORDER BY model_version DESC;
```

---

## Financial Model

### Initial Investment
- Time to build pipeline: ~1 week (existing team)
- Cost: $0 (already have backend)

### Ongoing (Monthly)
| Item | Cost |
|------|------|
| Data storage (logs, feedback) | $0-50 |
| Fine-tuning (OpenAI) | $50-200 |
| Inference (no change) | $0 |
| **Total** | **$50-250** |

### Alternative: Zero-Cost (Local Ollama)
- Data storage: $0
- Fine-tuning: $0 (on-premises)
- Inference: $0
- **Total: $0/month**

### ROI per User
- **User provides value**: Free training data via feedback
- **Cost per trained example**: $0.05-0.10 (vs $1-5 with contractors)
- **Scale**: 1M users → 1M training examples → much smarter model
- **Feedback rate needed**: Just 10% of users rating responses = 100K examples/month

---

## Public Trust & Transparency

**Be transparent with users:**

```markdown
# Help Improve Auricrux

When you use Auricrux and provide feedback, you're helping create 
a smarter construction AI for everyone.

✅ Your feedback (questions, ratings, comments) helps train the model
✅ You can opt-out of training data collection anytime
✅ All data is anonymized (no personal info stored)
✅ Model improvements are shared publicly
✅ You're helping the entire construction industry

Your contribution: Free. Your impact: Huge. 🏗️
```

---

## Launch Timeline

| Week | Task | Owner |
|------|------|-------|
| 1-2 | Build feedback enhancement, create database schema | Backend |
| 2-3 | Build training data pipeline | Data |
| 3-4 | Create fine-tuning service (OpenAI API) | Backend |
| 4-5 | Test end-to-end flow | QA |
| 5-6 | Deploy to 10% of users (beta) | Ops |
| 6-7 | Monitor, collect first training batch | Ops |
| 7-8 | Run first fine-tuning cycle | Data |
| 8-9 | A/B test improved model | QA |
| 9-10 | Deploy to all users if metrics improve | Ops |
| 10+ | Weekly fine-tuning cadence | Automation |

---

## Expected Outcomes

### After 1 Month
- 10K+ feedback records
- ~2K high-quality training examples
- First fine-tuned model
- 2-5% quality improvement observed

### After 3 Months
- 30K+ feedback records
- ~6K training examples
- 3-5 fine-tuning cycles completed
- 5-10% quality improvement
- Clear domain coverage (materials, estimation, safety, etc.)

### After 6 Months
- 60K+ feedback records
- ~12K diverse training examples
- 24+ fine-tuning cycles
- 10-20% quality improvement
- Specialized improvements in top user question domains

### After 1 Year
- 150K+ feedback records
- ~30K domain-specific training examples
- 52+ fine-tuning cycles
- 20-40% quality improvement
- Auricrux rivals commercial construction AI tools
- **Cost: $5,000-15,000** (vs $100K+ for traditional approach)

---

## Competitive Advantage

**Why this matters:**

1. **Continuous Improvement**: Unlike static models, Auricrux gets smarter every week
2. **Domain Specific**: Trained on construction-specific questions from real users
3. **Low Cost**: $0-250/month vs competitors at $500+/month
4. **Public Trust**: Transparent, user-driven improvement
5. **Scalability**: More users = more training data = better model = attracts more users
6. **Business Model**: Free tool + premium features (API access, custom training, etc.)

---

## Next Steps

1. ✅ Backend feedback collection (done)
2. [ ] Database schema for training data
3. [ ] Training pipeline service
4. [ ] Fine-tuning integration (OpenAI or local)
5. [ ] Background job scheduler
6. [ ] Client integration (mobile, desktop, web)
7. [ ] A/B testing framework
8. [ ] Monitoring dashboard
9. [ ] User communication (transparency)
10. [ ] Launch beta program

---

## Questions?

- **Cost**: Very low ($0-250/month) or free with local models
- **Quality**: Will improve continuously with user feedback
- **Data Privacy**: All data anonymized, user-controlled
- **Timeline**: Live within 8-10 weeks

**This is how Auricrux becomes smarter than commercial tools, at a fraction of the cost.** 🚀
