using LocalLlmAssistant.Api.Models;

namespace LocalLlmAssistant.Api.Services;

public interface IConversationService
{
    Conversation CreateConversation();

    void AddMessage(
        Conversation conversation,
        ChatMessage message);

    Conversation GetConversation();
}