using LocalLlmAssistant.Api.Models;

namespace LocalLlmAssistant.Api.Services;

public interface ILlmService
{
    Task<string> GenerateAsync(
        List<ChatMessage> messages,
        LlmGenerationOptions? options = null);

    IAsyncEnumerable<string> GenerateStreamAsync(
        List<ChatMessage> messages,
        LlmGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}