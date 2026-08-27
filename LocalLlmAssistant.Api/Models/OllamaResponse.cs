using System.Text.Json.Serialization;

namespace LocalLlmAssistant.Api.Models;

public class OllamaResponse
{
    public string Response { get; set; } = string.Empty;
}