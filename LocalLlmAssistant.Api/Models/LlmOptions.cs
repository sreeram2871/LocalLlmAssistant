namespace LocalLlmAssistant.Api.Models;

public class LlmOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string SystemPrompt { get; set; } = string.Empty;

    public double Temperature { get; set; }


    public int TopK { get; set; }


    public double TopP { get; set; }

    public int MaxTokens { get; set; }
}