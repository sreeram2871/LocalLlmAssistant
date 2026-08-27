using LocalLlmAssistant.Api.Exceptions;
using LocalLlmAssistant.Api.Models;
using LocalLlmAssistant.Api.Services;
using LocalLlmAssistant.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://localhost:4201",
                "https://localllmassistant-web.onrender.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Chat-Id");
    });
});


// ============================================================
// Controllers
// ============================================================

builder.Services.AddControllers();


// ============================================================
// LLM configuration
// ============================================================

builder.Services.Configure<LlmOptions>(
    builder.Configuration.GetSection("Llm"));


// ============================================================
// Ollama HTTP client
// ============================================================

builder.Services.AddHttpClient<
    ILlmService,
    OllamaLlmService>();


// ============================================================
// Conversation service
// ============================================================

builder.Services.AddSingleton<
    IConversationService,
    ConversationService>();


// ============================================================
// Problem Details
// ============================================================

builder.Services.AddProblemDetails();


// ============================================================
// Global exception handler
// ============================================================

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();


// ============================================================
// Entity Framework Core / SQL Server
// ============================================================

builder.Services.AddDbContext<AppDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection")));


// ============================================================
// Chat persistence
// ============================================================

builder.Services.AddScoped<
    IChatPersistenceService,
    ChatPersistenceService>();


// ============================================================
// Swagger
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();


// ============================================================
// Build application
// ============================================================

var app = builder.Build();


// ============================================================
// CORS
// IMPORTANT: Must be before MapControllers()
// ============================================================

app.UseCors("Angular");


// ============================================================
// Global exception handler
// ============================================================

app.UseExceptionHandler();


// ============================================================
// Swagger
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// ============================================================
// NOTE:
//
// Do NOT use:
//
// app.UseHttpsRedirection();
//
// Render terminates HTTPS at its edge and forwards
// the request to your container over HTTP.
// ============================================================


// ============================================================
// Authorization
// ============================================================

app.UseAuthorization();


// ============================================================
// Health check
// ============================================================

app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        status = "Healthy"
    }));


// ============================================================
// Controllers
// ============================================================

app.MapControllers();


// ============================================================
// Run
// ============================================================

app.Run();