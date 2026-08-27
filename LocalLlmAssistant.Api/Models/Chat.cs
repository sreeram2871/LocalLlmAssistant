namespace LocalLlmAssistant.Api.Models;

public class Chat
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessageEntity> Messages { get; set; }
        = new List<ChatMessageEntity>();
}