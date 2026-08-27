using LocalLlmAssistant.Api.Exceptions;
using LocalLlmAssistant.Api.Models;
using LocalLlmAssistant.Api.Services;
using LocalLlmAssistant.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://localhost:4201")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Chat-Id");
    });
});

builder.Services.AddControllers();

builder.Services.Configure<LlmOptions>(
    builder.Configuration.GetSection("Llm"));

builder.Services.AddHttpClient<ILlmService, OllamaLlmService>();

builder.Services.AddSingleton<
    IConversationService,
    ConversationService>();

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddDbContext<AppDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection")));

builder.Services.AddScoped<
    IChatPersistenceService,
    ChatPersistenceService>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("Angular");

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();