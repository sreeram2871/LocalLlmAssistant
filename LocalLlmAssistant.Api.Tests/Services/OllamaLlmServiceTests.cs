using System.Net;
using System.Text;
using System.Text.Json;

using LocalLlmAssistant.Api.Models;
using LocalLlmAssistant.Api.Services;

using Microsoft.Extensions.Options;

using NUnit.Framework;

namespace LocalLlmAssistant.Api.Tests.Services;

[TestFixture]
public class OllamaLlmServiceTests
{
    // =====================================================
    // 1. NORMAL RESPONSE
    // =====================================================

    [Test]
    public async Task GenerateAsync_ShouldReturnAssistantResponse()
    {
        // Arrange

        var ollamaResponse = """
        {
          "message": {
            "role": "assistant",
            "content": "C# is a programming language."
          },
          "done": true
        }
        """;

        var handler =
            new FakeHttpMessageHandler(
                ollamaResponse);

        var service =
            CreateService(handler);

        var messages =
            CreateMessages();


        // Act

        var result =
            await service.GenerateAsync(
                messages);


        // Assert

        Assert.That(
            result,
            Is.EqualTo(
                "C# is a programming language."));
    }


    // =====================================================
    // 2. HTTP ERROR
    // =====================================================

    [Test]
    public async Task GenerateAsync_WhenOllamaReturnsError_ShouldThrow()
    {
        // Arrange

        var ollamaResponse = """
        {
          "error": "model not found"
        }
        """;

        var handler =
            new FakeHttpMessageHandler(
                ollamaResponse,
                HttpStatusCode.NotFound);

        var service =
            CreateService(handler);

        var messages =
            CreateMessages();


        // Act

        HttpRequestException? exception = null;

        try
        {
            await service.GenerateAsync(
                messages);
        }
        catch (HttpRequestException ex)
        {
            exception = ex;
        }


        // Assert

        Assert.That(
            exception,
            Is.Not.Null);
    }


    // =====================================================
    // 3. STREAMING CHUNKS
    // =====================================================

    [Test]
    public async Task GenerateStreamAsync_ShouldReturnContentChunks()
    {
        // Arrange

        var streamResponse = """
        {"message":{"role":"assistant","content":"Hello"},"done":false}
        {"message":{"role":"assistant","content":" world"},"done":false}
        {"message":{"role":"assistant","content":""},"done":true,"prompt_eval_count":5,"eval_count":2,"total_duration":1000000000,"load_duration":10000000,"eval_duration":500000000}
        """;

        var handler =
            new FakeHttpMessageHandler(
                streamResponse);

        var service =
            CreateService(handler);

        var messages =
            CreateMessages();


        var chunks =
            new List<string>();


        // Act

        await foreach (
            var chunk in service.GenerateStreamAsync(
                messages))
        {
            chunks.Add(chunk);
        }


        // Assert

        Assert.That(
            chunks,
            Has.Some.EqualTo("Hello"));

        Assert.That(
            chunks,
            Has.Some.EqualTo(" world"));
    }


    // =====================================================
    // 4. METRICS
    // =====================================================

    [Test]
    public async Task GenerateStreamAsync_ShouldReturnMetrics()
    {
        // Arrange

        var streamResponse = """
        {"message":{"role":"assistant","content":"Hello"},"done":false}
        {"message":{"role":"assistant","content":""},"done":true,"prompt_eval_count":10,"eval_count":20,"total_duration":3000000000,"load_duration":500000000,"eval_duration":2000000000}
        """;

        var handler =
            new FakeHttpMessageHandler(
                streamResponse);

        var service =
            CreateService(handler);

        var messages =
            CreateMessages();


        var chunks =
            new List<string>();


        // Act

        await foreach (
            var chunk in service.GenerateStreamAsync(
                messages))
        {
            chunks.Add(chunk);
        }


        var metricsChunk =
            chunks.FirstOrDefault(
                x =>
                    x.StartsWith(
                        "[[LLM_METRICS]]"));


        // Assert

        Assert.That(
            metricsChunk,
            Is.Not.Null);


        var json =
            metricsChunk!
                .Replace(
                    "[[LLM_METRICS]]",
                    "");


        var metrics =
            JsonSerializer.Deserialize<LlmMetrics>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });


        Assert.That(
            metrics,
            Is.Not.Null);


        Assert.That(
            metrics!.PromptTokens,
            Is.EqualTo(10));


        Assert.That(
            metrics.OutputTokens,
            Is.EqualTo(20));


        Assert.That(
            metrics.TotalSeconds,
            Is.EqualTo(3.0).Within(0.01));


        Assert.That(
            metrics.GenerationSeconds,
            Is.EqualTo(2.0).Within(0.01));


        Assert.That(
            metrics.TokensPerSecond,
            Is.EqualTo(10.0).Within(0.01));
    }


    // =====================================================
    // 5. CUSTOM SETTINGS
    // =====================================================

    [Test]
    public async Task GenerateStreamAsync_ShouldUseProvidedSettings()
    {
        // Arrange

        var streamResponse = """
        {"message":{"role":"assistant","content":"Test"},"done":false}
        {"message":{"role":"assistant","content":""},"done":true,"prompt_eval_count":5,"eval_count":4,"total_duration":1000000000,"load_duration":10000000,"eval_duration":500000000}
        """;

        var handler =
            new FakeHttpMessageHandler(
                streamResponse);

        var service =
            CreateService(handler);

        var messages =
            CreateMessages();

        var options =
            new LlmGenerationOptions
            {
                Model =
                    "custom-model",

                Temperature =
                    0.2,

                TopK =
                    20,

                TopP =
                    0.8,

                MaxTokens =
                    500
            };


        // Act

        await foreach (
            var _ in service.GenerateStreamAsync(
                messages,
                options))
        {
            // Consume stream
        }


        // Assert

        Assert.That(
            handler.RequestBody,
            Is.Not.Null);


        using var json =
            JsonDocument.Parse(
                handler.RequestBody!);


        var root =
            json.RootElement;


        Assert.That(
            root.GetProperty("model").GetString(),
            Is.EqualTo("custom-model"));


        Assert.That(
            root.GetProperty("temperature").GetDouble(),
            Is.EqualTo(0.2).Within(0.001));


        Assert.That(
            root.GetProperty("top_k").GetInt32(),
            Is.EqualTo(20));


        Assert.That(
            root.GetProperty("top_p").GetDouble(),
            Is.EqualTo(0.8).Within(0.001));


        Assert.That(
            root.GetProperty("num_predict").GetInt32(),
            Is.EqualTo(500));


        Assert.That(
            root.GetProperty("stream").GetBoolean(),
            Is.True);
    }


    // =====================================================
    // 6. CANCELLATION
    // =====================================================

    [Test]
    public async Task GenerateStreamAsync_WhenCancelled_ShouldStop()
    {
        // Arrange

        var streamResponse = """
        {"message":{"role":"assistant","content":"Hello"},"done":false}
        {"message":{"role":"assistant","content":" world"},"done":false}
        {"message":{"role":"assistant","content":""},"done":true,"prompt_eval_count":5,"eval_count":2,"total_duration":1000000000,"load_duration":10000000,"eval_duration":500000000}
        """;

        var handler =
            new FakeHttpMessageHandler(
                streamResponse);

        var service =
            CreateService(handler);

        var messages =
            CreateMessages();


        using var cancellationTokenSource =
            new CancellationTokenSource();


        cancellationTokenSource.Cancel();


        // Act

        OperationCanceledException? exception = null;

        try
        {
            await foreach (
                var _ in service.GenerateStreamAsync(
                    messages,
                    cancellationToken:
                        cancellationTokenSource.Token))
            {
            }
        }
        catch (OperationCanceledException ex)
        {
            exception = ex;
        }


        // Assert

        Assert.That(
            exception,
            Is.Not.Null);
    }


    // =====================================================
    // TEST SERVICE FACTORY
    // =====================================================

    private static OllamaLlmService CreateService(
        FakeHttpMessageHandler handler)
    {
        var httpClient =
            new HttpClient(handler)
            {
                BaseAddress =
                    new Uri(
                        "http://localhost:11434/")
            };


        var options =
            Options.Create(
                new LlmOptions
                {
                    BaseUrl =
                        "http://localhost:11434",

                    Model =
                        "llama3.2",

                    SystemPrompt =
                        "You are a C# assistant.",

                    Temperature =
                        0.7,

                    TopK =
                        40,

                    TopP =
                        0.9,

                    MaxTokens =
                        1000
                });


        return new OllamaLlmService(
            httpClient,
            options);
    }


    // =====================================================
    // TEST MESSAGE FACTORY
    // =====================================================

    private static List<ChatMessage> CreateMessages()
    {
        return
        [
            new ChatMessage
            {
                Role = "user",
                Content = "What is C#?"
            }
        ];
    }


    // =====================================================
    // FAKE HTTP HANDLER
    // =====================================================

    private sealed class FakeHttpMessageHandler
        : HttpMessageHandler
    {
        private readonly string _response;

        private readonly HttpStatusCode _statusCode;


        public string? RequestBody { get; private set; }


        public FakeHttpMessageHandler(
            string response,
            HttpStatusCode statusCode =
                HttpStatusCode.OK)
        {
            _response =
                response;

            _statusCode =
                statusCode;
        }


        protected override async Task<HttpResponseMessage>
            SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
        {
            // Capture request body
            RequestBody =
                await request.Content!
                    .ReadAsStringAsync(
                        cancellationToken);


            // Respect cancellation
            cancellationToken.ThrowIfCancellationRequested();


            var response =
                new HttpResponseMessage(
                    _statusCode)
                {
                    Content =
                        new StringContent(
                            _response,
                            Encoding.UTF8,
                            "application/x-ndjson")
                };


            return response;
        }
    }
}