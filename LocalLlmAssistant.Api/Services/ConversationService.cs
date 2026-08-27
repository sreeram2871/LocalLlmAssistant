using LocalLlmAssistant.Api.Models;

namespace LocalLlmAssistant.Api.Services;

public class ConversationService : IConversationService
{
    private readonly Conversation _conversation;

    public ConversationService()
    {
        _conversation = new Conversation();
    }

    public Conversation CreateConversation()
    {
        return _conversation;
    }

    public void AddMessage(
        Conversation conversation,
        ChatMessage message)
    {
        conversation.Messages.Add(message);
    }

    public Conversation GetConversation()
    {
        return _conversation;
    }
}