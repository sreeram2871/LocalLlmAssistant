namespace LocalLlmAssistant.Api.Models;

public class ChatMessageEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChatId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Chat Chat { get; set; } = null!;
}