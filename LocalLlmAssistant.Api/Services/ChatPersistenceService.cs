using LocalLlmAssistant.Api.Data;
using LocalLlmAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalLlmAssistant.Api.Services;

public class ChatPersistenceService : IChatPersistenceService
{
    private readonly AppDbContext _dbContext;

    public ChatPersistenceService(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Chat> CreateChatAsync(
        string title,
        CancellationToken cancellationToken = default)
    {
        var chat = new Chat
        {
            Title = string.IsNullOrWhiteSpace(title)
                ? "New Chat"
                : title.Trim()
        };

        _dbContext.Chats.Add(chat);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return chat;
    }

    public async Task AddMessageAsync(
        Guid chatId,
        string role,
        string content,
        CancellationToken cancellationToken = default)
    {
        var chatExists =
            await _dbContext.Chats
                .AnyAsync(
                    x => x.Id == chatId,
                    cancellationToken);

        if (!chatExists)
        {
            throw new InvalidOperationException(
                $"Chat '{chatId}' was not found.");
        }

        var message = new ChatMessageEntity
        {
            ChatId = chatId,
            Role = role,
            Content = content
        };

        _dbContext.ChatMessages.Add(message);

        var chat =
            await _dbContext.Chats
                .FirstAsync(
                    x => x.Id == chatId,
                    cancellationToken);

        chat.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<List<Chat>> GetChatsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Chats
            .AsNoTracking()
            .OrderByDescending(
                x => x.UpdatedAt)
            .ToListAsync(
                cancellationToken);
    }

    public async Task<Chat?> GetChatAsync(
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Chats
            .AsNoTracking()
            .Include(x => x.Messages
                .OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(
                x => x.Id == chatId,
                cancellationToken);
    }

    public async Task DeleteChatAsync(
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        var chat =
            await _dbContext.Chats
                .FirstOrDefaultAsync(
                    x => x.Id == chatId,
                    cancellationToken);

        if (chat == null)
        {
            return;
        }

        _dbContext.Chats.Remove(chat);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}