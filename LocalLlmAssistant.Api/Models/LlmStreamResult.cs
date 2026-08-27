namespace LocalLlmAssistant.Api.Models;

public class LlmStreamResult
{
    public string Content { get; set; } = string.Empty;

    public LlmMetrics? Metrics { get; set; }
}