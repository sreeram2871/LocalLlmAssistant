namespace LocalLlmAssistant.Api.Models;

public class Conversation
{
    public List<ChatMessage> Messages { get; set; } = new();
}