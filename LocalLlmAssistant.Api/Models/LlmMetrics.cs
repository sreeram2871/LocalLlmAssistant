namespace LocalLlmAssistant.Api.Models;

public class LlmMetrics
{
    public string Model { get; set; } = string.Empty;

    public int PromptTokens { get; set; }

    public int OutputTokens { get; set; }

    public double TotalSeconds { get; set; }

    public double GenerationSeconds { get; set; }

    public double TokensPerSecond { get; set; }

    public double LoadSeconds { get; set; }
}