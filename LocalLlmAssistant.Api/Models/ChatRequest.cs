namespace LocalLlmAssistant.Api.Models;

public class ChatRequest
{
    public Guid? ChatId { get; set; }
    public List<ChatMessage> Messages { get; set; } = [];

    public LlmGenerationOptions? Options { get; set; }
}