using LocalLlmAssistant.Api.Models;
using LocalLlmAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LocalLlmAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILlmService _llmService;
    private readonly IConversationService _conversationService;

    private readonly IChatPersistenceService
    _chatPersistenceService;
    public ChatController(ILlmService llmService, IConversationService conversationService, IChatPersistenceService chatPersistenceService)
    {
        _llmService = llmService;
        _conversationService = conversationService;
        _chatPersistenceService = chatPersistenceService;
    }


    [HttpPost]
    public async Task<IActionResult> Chat(ChatRequest request)
    {
        if (request.Messages == null || request.Messages.Count == 0)
        {
            return BadRequest(new
            {
                error = "At least one message is required."
            });
        }

        try
        {
            var response = new List<string>();

            await foreach (var chunk in
                _llmService.GenerateStreamAsync(request.Messages))
            {
                response.Add(chunk);
            }

            return Ok(new
            {
                response = string.Concat(response)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            return StatusCode(500, new
            {
                error = "An unexpected error occurred."
            });
        }
    }

    //[HttpPost("stream")]
    //public async Task Stream(
    //   ChatRequest request,
    //   CancellationToken cancellationToken)
    //{
    //    Response.StatusCode = 200;
    //    Response.ContentType = "text/plain; charset=utf-8";

    //    Response.Headers.CacheControl = "no-cache";

    //    await foreach (
    //        var chunk in _llmService.GenerateStreamAsync(
    //            request.Messages,
    //            cancellationToken))
    //    {
    //        Console.WriteLine(
    //            $"CONTROLLER CHUNK: [{chunk}]");

    //        await Response.WriteAsync(
    //            chunk,
    //            cancellationToken);

    //        await Response.Body.FlushAsync(
    //            cancellationToken);
    //    }
    //}


    [HttpPost("stream")]
    public async Task Stream(
    [FromBody] ChatRequest request,
    CancellationToken cancellationToken)
    {
        Response.ContentType =
            "text/plain; charset=utf-8";

        Response.Headers.CacheControl =
            "no-cache";


        // ==========================================
        // VALIDATE REQUEST
        // ==========================================

        if (request.Messages == null ||
            request.Messages.Count == 0)
        {
            await Response.WriteAsync(
                "[[ERROR]]Please provide at least one message.",
                cancellationToken);

            return;
        }


        try
        {
            // ======================================
            // FIND OR CREATE CHAT
            // ======================================

            Guid chatId;

            if (request.ChatId.HasValue)
            {
                chatId = request.ChatId.Value;
            }
            else
            {
                var firstUserMessage =
                    request.Messages
                        .FirstOrDefault(
                            x => x.Role == "user");

                var title =
                    CreateChatTitle(
                        firstUserMessage?.Content);

                var chat =
                    await _chatPersistenceService
                        .CreateChatAsync(
                            title,
                            cancellationToken);

                chatId = chat.Id;
            }


            // ======================================
            // SEND CHAT ID TO ANGULAR
            // ======================================

            Response.Headers["X-Chat-Id"] =
                chatId.ToString();


            // ======================================
            // GET CURRENT USER MESSAGE
            // ======================================

            var currentUserMessage =
                request.Messages
                    .LastOrDefault(
                        x => x.Role == "user");


            if (currentUserMessage != null)
            {
                await _chatPersistenceService
                    .AddMessageAsync(
                        chatId,
                        currentUserMessage.Role,
                        currentUserMessage.Content,
                        cancellationToken);
            }


            // ======================================
            // COLLECT ASSISTANT RESPONSE
            // ======================================

            var assistantResponse =
                new System.Text.StringBuilder();


            // ======================================
            // STREAM FROM OLLAMA
            // ======================================

            await foreach (
                var chunk in
                _llmService.GenerateStreamAsync(
                    request.Messages,
                    request.Options,
                    cancellationToken))
            {
                // Metrics should go to Angular,
                // but should NOT be saved as assistant text.

                if (chunk.StartsWith(
                    "[[LLM_METRICS]]"))
                {
                    await Response.WriteAsync(
                        chunk,
                        cancellationToken);

                    await Response.Body.FlushAsync(
                        cancellationToken);

                    continue;
                }


                // Errors should not become
                // normal assistant content.

                if (chunk.StartsWith(
                    "[[ERROR]]"))
                {
                    await Response.WriteAsync(
                        chunk,
                        cancellationToken);

                    await Response.Body.FlushAsync(
                        cancellationToken);

                    continue;
                }


                // Accumulate actual assistant text

                assistantResponse.Append(
                    chunk);


                // Send chunk immediately
                // to Angular

                await Response.WriteAsync(
                    chunk,
                    cancellationToken);

                await Response.Body.FlushAsync(
                    cancellationToken);
            }


            // ======================================
            // SAVE ASSISTANT RESPONSE
            // ======================================

            var finalAssistantResponse =
                assistantResponse
                    .ToString()
                    .Trim();

            if (!string.IsNullOrWhiteSpace(
                finalAssistantResponse))
            {
                await _chatPersistenceService
                    .AddMessageAsync(
                        chatId,
                        "assistant",
                        finalAssistantResponse,
                        cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                "LLM generation cancelled.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(
                $"Ollama connection error: {ex.Message}");

            try
            {
                await Response.WriteAsync(
                    "[[ERROR]]Ollama is not running. Please start Ollama and try again.",
                    cancellationToken);

                await Response.Body.FlushAsync(
                    cancellationToken);
            }
            catch
            {
                // Response may already be closed.
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Unexpected streaming error: {ex}");

            try
            {
                await Response.WriteAsync(
                    "[[ERROR]]An unexpected error occurred while generating the response.",
                    cancellationToken);

                await Response.Body.FlushAsync(
                    cancellationToken);
            }
            catch
            {
                // Response may already be closed.
            }
        }
    }

    [HttpPost("test-service")]
    public async Task<IActionResult> TestService(ChatRequest request)
    {
        Console.WriteLine("========== SERVICE TEST START ==========");

        var result = new StringBuilder();

        await foreach (var chunk in _llmService.GenerateStreamAsync(
            request.Messages))
        {
            Console.WriteLine($"SERVICE CHUNK: [{chunk}]");

            result.Append(chunk);
        }

        Console.WriteLine(
            $"SERVICE FINAL RESULT: [{result}]");

        Console.WriteLine("========== SERVICE TEST END ==========");

        return Ok(new
        {
            response = result.ToString()
        });
    }

    [HttpGet("test-ollama")]
    public async Task<IActionResult> TestOllama()
    {
        using var client = new HttpClient();

        var request = new
        {
            model = "llama3.2",
            messages = new[]
            {
            new
            {
                role = "user",
                content = "Say hello"
            }
        },
            stream = false
        };

        var response = await client.PostAsJsonAsync(
            "http://localhost:11434/api/chat",
            request);

        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine("OLLAMA STATUS: " + response.StatusCode);
        Console.WriteLine("OLLAMA BODY: " + body);

        return Content(body, "application/json");
    }

    [HttpGet("test-ollama-stream")]
    public async Task<IActionResult> TestOllamaStream()
    {
        using var client = new HttpClient();

        var request = new
        {
            model = "llama3.2",
            messages = new[]
            {
            new
            {
                role = "user",
                content = "Explain what C# is in two sentences."
            }
        },
            stream = true
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "http://localhost:11434/api/chat")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await client.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead);

        Console.WriteLine(
            $"OLLAMA STATUS: {response.StatusCode}");

        response.EnsureSuccessStatusCode();

        using var stream =
            await response.Content.ReadAsStreamAsync();

        using var reader =
            new StreamReader(stream);

        var result = new StringBuilder();

        while (await reader.ReadLineAsync() is string line)
        {
            Console.WriteLine(
                $"OLLAMA STREAM LINE: {line}");

            result.AppendLine(line);
        }

        return Content(
            result.ToString(),
            "text/plain");
    }



    private static string CreateChatTitle(
    string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return "New Chat";
        }

        var title =
            question.Trim();

        if (title.Length > 60)
        {
            title =
                title[..60] + "...";
        }

        return title;
    }
}