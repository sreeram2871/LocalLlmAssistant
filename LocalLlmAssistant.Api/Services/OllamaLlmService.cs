using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

using LocalLlmAssistant.Api.Models;
using Microsoft.Extensions.Options;

namespace LocalLlmAssistant.Api.Services;

public class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;

    private readonly string _model;

    private readonly string _systemPrompt;

    private readonly double _temperature;

    private readonly int _topK;

    private readonly double _topP;

    private readonly int _maxTokens;


    public OllamaLlmService(
        HttpClient httpClient,
        IOptions<LlmOptions> options)
    {
        var settings = options.Value;


        // ==========================================
        // VALIDATE CONFIGURATION
        // ==========================================

        if (string.IsNullOrWhiteSpace(
            settings.BaseUrl))
        {
            throw new InvalidOperationException(
                "Llm:BaseUrl is missing.");
        }

        if (string.IsNullOrWhiteSpace(
            settings.Model))
        {
            throw new InvalidOperationException(
                "Llm:Model is missing.");
        }

        if (string.IsNullOrWhiteSpace(
            settings.SystemPrompt))
        {
            throw new InvalidOperationException(
                "Llm:SystemPrompt is missing.");
        }

        if (settings.Temperature < 0)
        {
            throw new InvalidOperationException(
                "Llm:Temperature cannot be negative.");
        }

        if (settings.TopK <= 0)
        {
            throw new InvalidOperationException(
                "Llm:TopK must be greater than zero.");
        }

        if (settings.TopP <= 0 ||
            settings.TopP > 1)
        {
            throw new InvalidOperationException(
                "Llm:TopP must be between 0 and 1.");
        }

        if (settings.MaxTokens <= 0)
        {
            throw new InvalidOperationException(
                "Llm:MaxTokens must be greater than zero.");
        }


        // ==========================================
        // STORE CONFIGURATION
        // ==========================================

        _httpClient = httpClient;

        _httpClient.BaseAddress =
            new Uri(settings.BaseUrl);

        _model =
            settings.Model;

        _systemPrompt =
            settings.SystemPrompt;

        _temperature =
            settings.Temperature;

        _topK =
            settings.TopK;

        _topP =
            settings.TopP;

        _maxTokens =
            settings.MaxTokens;
    }


    // =====================================================
    // NORMAL NON-STREAMING GENERATION
    // =====================================================

    public async Task<string> GenerateAsync(
        List<ChatMessage> messages,
        LlmGenerationOptions? options = null)
    {
        // ==========================================
        // SYSTEM MESSAGE
        // ==========================================

        var systemMessage = new ChatMessage
        {
            Role = "system",
            Content = _systemPrompt
        };


        // ==========================================
        // COMPLETE CONVERSATION
        // ==========================================

        var allMessages =
            new List<ChatMessage>
            {
                systemMessage
            };

        allMessages.AddRange(messages);


        // ==========================================
        // REQUEST
        // ==========================================

        var request = new
        {
            model =
                options?.Model ?? _model,

            messages =
                allMessages,

            temperature =
                options?.Temperature
                ?? _temperature,

            top_k =
                options?.TopK
                ?? _topK,

            top_p =
                options?.TopP
                ?? _topP,

            num_predict =
                options?.MaxTokens
                ?? _maxTokens,

            stream = false
        };


        // ==========================================
        // CALL OLLAMA
        // ==========================================

        using var response =
            await _httpClient.PostAsJsonAsync(
                "api/chat",
                request);


        response.EnsureSuccessStatusCode();


        // ==========================================
        // READ RESPONSE
        // ==========================================

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    OllamaChatResponse>();


        return
            result?.Message?.Content
            ?? string.Empty;
    }


    // =====================================================
    // STREAMING GENERATION
    // =====================================================

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        List<ChatMessage> messages,
        LlmGenerationOptions? options = null,
        [EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        // ==========================================
        // SYSTEM MESSAGE
        // ==========================================

        var systemMessage = new ChatMessage
        {
            Role = "system",
            Content = _systemPrompt
        };


        // ==========================================
        // COMPLETE CONVERSATION
        // ==========================================

        var allMessages =
            new List<ChatMessage>
            {
                systemMessage
            };

        allMessages.AddRange(messages);


        // ==========================================
        // RESOLVE SETTINGS
        // ==========================================

        var selectedModel =
            options?.Model ?? _model;

        var selectedTemperature =
            options?.Temperature
            ?? _temperature;

        var selectedTopK =
            options?.TopK
            ?? _topK;

        var selectedTopP =
            options?.TopP
            ?? _topP;

        var selectedMaxTokens =
            options?.MaxTokens
            ?? _maxTokens;


        // ==========================================
        // OLLAMA REQUEST
        // ==========================================

        var request = new
        {
            model =
                selectedModel,

            messages =
                allMessages,

            temperature =
                selectedTemperature,

            top_k =
                selectedTopK,

            top_p =
                selectedTopP,

            num_predict =
                selectedMaxTokens,

            stream = true
        };


        // ==========================================
        // HTTP REQUEST
        // ==========================================

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                "api/chat")
            {
                Content =
                    JsonContent.Create(request)
            };


        Console.WriteLine(
            "=== STREAM SERVICE START ===");


        // ==========================================
        // SEND REQUEST
        // ==========================================

        using var response =
            await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption
                    .ResponseHeadersRead,
                cancellationToken);


        Console.WriteLine(
            $"OLLAMA STATUS: {response.StatusCode}");


        response.EnsureSuccessStatusCode();


        // ==========================================
        // READ RESPONSE STREAM
        // ==========================================

        await using var stream =
            await response.Content
                .ReadAsStreamAsync(
                    cancellationToken);

        using var reader =
            new StreamReader(stream);


        // ==========================================
        // FINAL CHUNK
        // ==========================================

        OllamaChatResponse?
            finalChunk = null;


        // ==========================================
        // READ OLLAMA STREAM
        // ==========================================

        while (!reader.EndOfStream)
        {
            cancellationToken
                .ThrowIfCancellationRequested();


            var line =
                await reader.ReadLineAsync(
                    cancellationToken);


            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }


            Console.WriteLine(
                $"OLLAMA RAW: {line}");


            // ======================================
            // DESERIALIZE
            // ======================================

            OllamaChatResponse? chunk;

            try
            {
                chunk =
                    JsonSerializer
                        .Deserialize<
                            OllamaChatResponse>(
                            line,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive =
                                    true
                            });
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"JSON DESERIALIZATION ERROR: {ex.Message}");

                continue;
            }


            if (chunk == null)
            {
                continue;
            }


            // ======================================
            // SAVE FINAL CHUNK
            // ======================================

            if (chunk.Done)
            {
                finalChunk = chunk;
            }


            // ======================================
            // GET GENERATED CONTENT
            // ======================================

            var content =
                chunk.Message?.Content;


            Console.WriteLine(
                $"CONTENT: [{content}]");


            // ======================================
            // SEND CHUNK TO CONTROLLER
            // ======================================

            if (!string.IsNullOrEmpty(
                content))
            {
                Console.WriteLine(
                    $"YIELDING: [{content}]");

                yield return content;
            }


            // ======================================
            // GENERATION COMPLETE
            // ======================================

            if (chunk.Done)
            {
                Console.WriteLine(
                    "=== OLLAMA DONE ===");

                break;
            }
        }


        // ==========================================
        // SEND METRICS
        // ==========================================

        if (finalChunk != null)
        {
            yield return BuildMetricsMessage(
                finalChunk,
                selectedModel);
        }


        Console.WriteLine(
            "=== STREAM SERVICE END ===");
    }


    // =====================================================
    // BUILD METRICS MESSAGE
    // =====================================================

    private string BuildMetricsMessage(
        OllamaChatResponse finalChunk,
        string model)
    {
        // ==========================================
        // TOTAL TIME
        // ==========================================

        var totalSeconds =
            finalChunk.TotalDuration
            / 1_000_000_000.0;


        // ==========================================
        // LOAD TIME
        // ==========================================

        var loadSeconds =
            finalChunk.LoadDuration
            / 1_000_000_000.0;


        // ==========================================
        // GENERATION TIME
        // ==========================================

        var generationSeconds =
            finalChunk.EvalDuration
            / 1_000_000_000.0;


        // ==========================================
        // TOKENS / SECOND
        // ==========================================

        var tokensPerSecond =
            generationSeconds > 0
                ? finalChunk.EvalCount
                    / generationSeconds
                : 0;


        // ==========================================
        // CREATE METRICS OBJECT
        // ==========================================

        var metrics =
            new LlmMetrics
            {
                Model =
                    model,

                PromptTokens =
                    finalChunk.PromptEvalCount,

                OutputTokens =
                    finalChunk.EvalCount,

                TotalSeconds =
                    totalSeconds,

                LoadSeconds =
                    loadSeconds,

                GenerationSeconds =
                    generationSeconds,

                TokensPerSecond =
                    tokensPerSecond
            };


        // ==========================================
        // JSON OPTIONS
        // ==========================================

        var jsonOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase
            };


        // ==========================================
        // RETURN SPECIAL STREAM MESSAGE
        // ==========================================

        return
            "[[LLM_METRICS]]" +
            JsonSerializer.Serialize(
                metrics,
                jsonOptions);
    }
}