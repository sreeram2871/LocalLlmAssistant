using LocalLlmAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LocalLlmAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatsController : ControllerBase
{
    private readonly IChatPersistenceService _chatPersistenceService;

    public ChatsController(
        IChatPersistenceService chatPersistenceService)
    {
        _chatPersistenceService =
            chatPersistenceService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateChat(
        [FromBody] CreateChatRequest request,
        CancellationToken cancellationToken)
    {
        var chat =
            await _chatPersistenceService.CreateChatAsync(
                request.Title,
                cancellationToken);

        return Ok(chat);
    }

    [HttpGet]
    public async Task<IActionResult> GetChats(
        CancellationToken cancellationToken)
    {
        var chats =
            await _chatPersistenceService.GetChatsAsync(
                cancellationToken);

        return Ok(chats);
    }

    [HttpGet("{chatId:guid}")]
    public async Task<IActionResult> GetChat(
        Guid chatId,
        CancellationToken cancellationToken)
    {
        var chat =
            await _chatPersistenceService.GetChatAsync(
                chatId,
                cancellationToken);

        if (chat == null)
        {
            return NotFound();
        }

        return Ok(chat);
    }

    [HttpDelete("{chatId:guid}")]
    public async Task<IActionResult> DeleteChat(
        Guid chatId,
        CancellationToken cancellationToken)
    {
        await _chatPersistenceService.DeleteChatAsync(
            chatId,
            cancellationToken);

        return NoContent();
    }
}

public class CreateChatRequest
{
    public string Title { get; set; } = string.Empty;
}