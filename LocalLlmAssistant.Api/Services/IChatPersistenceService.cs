using LocalLlmAssistant.Api.Models;

namespace LocalLlmAssistant.Api.Services;

public interface IChatPersistenceService
{
    Task<Chat> CreateChatAsync(
        string title,
        CancellationToken cancellationToken = default);

    Task AddMessageAsync(
        Guid chatId,
        string role,
        string content,
        CancellationToken cancellationToken = default);

    Task<List<Chat>> GetChatsAsync(
        CancellationToken cancellationToken = default);

    Task<Chat?> GetChatAsync(
        Guid chatId,
        CancellationToken cancellationToken = default);

    Task DeleteChatAsync(
        Guid chatId,
        CancellationToken cancellationToken = default);
}