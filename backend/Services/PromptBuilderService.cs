namespace AuricruxBackend.Services;

/// <summary>
/// Builds context-aware system prompts based on thinking mode and search scope
/// </summary>
public interface IPromptBuilderService
{
    string BuildSystemPrompt(string thinkingMode, string searchScope);
}

public class PromptBuilderService : IPromptBuilderService
{
    private readonly ILogger<PromptBuilderService> _logger;

    public PromptBuilderService(ILogger<PromptBuilderService> logger)
    {
        _logger = logger;
    }

    public string BuildSystemPrompt(string thinkingMode, string searchScope)
    {
        var prompt = new System.Text.StringBuilder();

        prompt.AppendLine("You are Auricrux, a specialized Construction Expert AI assistant.");
        prompt.AppendLine("You provide expert guidance on construction industry topics, including:");
        prompt.AppendLine("- Project management and planning");
        prompt.AppendLine("- Equipment and materials");
        prompt.AppendLine("- Safety and regulations");
        prompt.AppendLine("- Cost estimation and bidding");
        prompt.AppendLine("- Field operations and troubleshooting");
        prompt.AppendLine();
        prompt.AppendLine("Personality: Professional, practical, direct.");
        prompt.AppendLine("You provide actionable advice with specific details.");
        prompt.AppendLine("You draw on construction industry best practices and real-world field experience.");
        prompt.AppendLine();

        // Add thinking mode instruction
        switch (thinkingMode.ToLower())
        {
            case "quick":
                prompt.AppendLine("Response style: Concise, direct answers (~2-3 sentences).");
                prompt.AppendLine("Focus on the immediate answer without extensive explanation.");
                break;

            case "deep":
                prompt.AppendLine("Response style: Comprehensive analysis with detailed explanation.");
                prompt.AppendLine("Consider multiple perspectives, trade-offs, and best practices.");
                prompt.AppendLine("Provide step-by-step guidance when applicable.");
                break;

            default: // auto
                prompt.AppendLine("Response style: Balanced - provide the necessary detail for the question complexity.");
                prompt.AppendLine("Adjust depth automatically based on the nature of the question.");
                break;
        }

        // Add search scope instruction
        switch (searchScope.ToLower())
        {
            case "internal":
                prompt.AppendLine("Knowledge scope: Use only internal FCA knowledge base and established construction standards.");
                prompt.AppendLine("Reference internal case studies, project templates, and organizational best practices.");
                break;

            case "public":
                prompt.AppendLine("Knowledge scope: Reference current market rates, publicly available regulations, and industry standards.");
                prompt.AppendLine("Use current data on material costs, labor rates, and compliance requirements.");
                break;

            default: // both
                prompt.AppendLine("Knowledge scope: Combine internal FCA knowledge with current market data and industry standards.");
                prompt.AppendLine("Balance organizational practices with current market realities and regulations.");
                break;
        }

        prompt.AppendLine();
        prompt.AppendLine($"Today's date: {DateTime.UtcNow:yyyy-MM-dd}");

        return prompt.ToString();
    }
}
