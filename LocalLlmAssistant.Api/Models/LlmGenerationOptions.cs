namespace LocalLlmAssistant.Api.Models;

public class LlmGenerationOptions
{
    public string? Model { get; set; }

    public double? Temperature { get; set; }

    public int? TopK { get; set; }

    public double? TopP { get; set; }

    public int? MaxTokens { get; set; }
}