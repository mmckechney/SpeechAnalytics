using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpeechAnalyticsLibrary;
using SpeechAnalyticsLibrary.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<AnalyticsSettings>(sp =>
{
    var settings = new AnalyticsSettings();
    builder.Configuration.Bind(settings);
    return settings;
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<IdentityHelper>();
builder.Services.AddSingleton<FileHandling>();
builder.Services.AddSingleton<CosmosHelper>();
builder.Services.AddSingleton<FoundryAgentClient>();

builder.Services.AddHttpClient();

builder.Services.AddRouting();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AskInsightsService");
var agentClient = app.Services.GetRequiredService<FoundryAgentClient>();

app.MapPost("/ask", async (AskRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request?.Question))
    {
        return Results.BadRequest("Please provide a question in the request body.");
    }

    var answer = await agentClient.AskQuestions(request.Question);
    return Results.Ok(new AskResponse(answer));
});

app.MapGet("/ask", async (string? question) =>
{
    if (string.IsNullOrWhiteSpace(question))
    {
        return Results.BadRequest("Provide a question query parameter or POST body.");
    }

    var answer = await agentClient.AskQuestions(question);
    return Results.Ok(new AskResponse(answer));
});

app.MapGet("/health", () => Results.Ok("healthy"));

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "AskInsightsService terminated unexpectedly");
    throw;
}

internal record AskRequest(string Question);
internal record AskResponse(string Answer);
