using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SpeechAnalyticsLibrary;
using SpeechAnalyticsLibrary.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<AnalyticsSettings>(sp =>
{
    var settings = new AnalyticsSettings();
    builder.Configuration.Bind(settings);
    return settings;
});

builder.Services.AddSingleton<IdentityHelper>();
builder.Services.AddSingleton<FileHandling>();
builder.Services.AddSingleton<CosmosHelper>();
builder.Services.AddSingleton<FoundryAgentClient>();
builder.Services.AddSingleton<BatchTranscription>();
builder.Services.AddSingleton<SpeechDiarization>();
builder.Services.AddSingleton<TranscriptionProcessor>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapPost("/events", async (HttpRequest request, TranscriptionProcessor processor, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("TranscriptionService");
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(payload))
    {
        logger.LogWarning("Received empty payload from Event Grid.");
        return Results.BadRequest();
    }

    EventGridEvent[] events;
    try
    {
        events = EventGridEvent.ParseMany(BinaryData.FromString(payload));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to parse Event Grid events");
        return Results.BadRequest();
    }

    foreach (var evt in events)
    {
        if (evt.TryGetSystemEventData(out object? systemEvent))
        {
            switch (systemEvent)
            {
                case SubscriptionValidationEventData validation:
                    logger.LogInformation("Handling subscription validation event");
                    return Results.Ok(new { validationResponse = validation.ValidationCode });
                case StorageBlobCreatedEventData blobCreated:
                    await processor.ProcessBlobCreatedAsync(blobCreated, evt.Subject ?? string.Empty);
                    break;
                default:
                    logger.LogInformation("Ignoring event type {EventType}", evt.EventType);
                    break;
            }
        }
        else if (evt.EventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
        {
            var data = evt.Data.ToObjectFromJson<SubscriptionValidationEventData>();
            return Results.Ok(new { validationResponse = data.ValidationCode });
        }
        else
        {
            logger.LogInformation("Ignoring event type {EventType}", evt.EventType);
        }
    }

    return Results.Ok();
});

app.MapGet("/health", () => Results.Ok("healthy"));

await app.RunAsync();

internal class TranscriptionProcessor
{
    private readonly ILogger<TranscriptionProcessor> _logger;
    private readonly AnalyticsSettings _settings;
    private readonly BatchTranscription _batchTranscription;
    private readonly FileHandling _fileHandler;
    private readonly FoundryAgentClient _agentClient;
    private readonly CosmosHelper _cosmosHelper;

    public TranscriptionProcessor(
        ILogger<TranscriptionProcessor> logger,
        AnalyticsSettings settings,
        BatchTranscription batchTranscription,
        FileHandling fileHandler,
        FoundryAgentClient agentClient,
        CosmosHelper cosmosHelper)
    {
        _logger = logger;
        _settings = settings;
        _batchTranscription = batchTranscription;
        _fileHandler = fileHandler;
        _agentClient = agentClient;
        _cosmosHelper = cosmosHelper;
    }

    public async Task ProcessBlobCreatedAsync(StorageBlobCreatedEventData blobCreated, string subject)
    {
        if (blobCreated == null || string.IsNullOrWhiteSpace(blobCreated.Url))
        {
            _logger.LogWarning("Blob created event missing URL");
            return;
        }

        var blobUri = new Uri(blobCreated.Url);
        _logger.LogInformation("Processing transcription for blob {BlobUri}", blobUri);

        var containerClient = new BlobContainerClient(new Uri(_settings.Storage.SourceContainerUrl));
        var name = Path.GetFileName(blobUri.LocalPath.Trim('/'));
        var blobClient = containerClient.GetBlobClient(name);

        var aiSvcs = _settings.AiServices;

        var initialResponse = await _batchTranscription.StartBatchTranscription(aiSvcs.Endpoint, _settings.Storage.SourceContainerUrl, _settings.Storage.TargetContainerUrl, blobClient.Uri);

        if (initialResponse == null)
        {
            _logger.LogError("Failed to start batch transcription for {Blob}", blobUri);
            return;
        }

        _logger.LogDebug("Path to Transcription Job: {Path}", initialResponse.Self);
        var statusResponse = await _batchTranscription.CheckTranscriptionStatus(initialResponse.Self);

        if (statusResponse?.Links?.Files == null)
        {
            _logger.LogError("Failed to retrieve transcription links for {Blob}", blobUri);
            return;
        }

        var translationLinks = await _batchTranscription.GetTranslationOutputLinks(statusResponse.Links.Files);
        if (translationLinks == null)
        {
            _logger.LogError("No translation links returned for {Blob}", blobUri);
            return;
        }

        var transcriptions = await _batchTranscription.GetTranscriptionText(translationLinks);
        if (transcriptions == null)
        {
            _logger.LogWarning("No transcriptions produced for {Blob}", blobUri);
            return;
        }

        foreach (var transcription in transcriptions)
        {
            await _fileHandler.SaveTranscriptionFile(transcription.source, transcription.transcription, _settings.Storage.TargetContainerUrl);

            var existingInsightObj = await _cosmosHelper.GetAnalysis(transcription.source);
            string insights = await _agentClient.GetTranscriptionInsights(transcription.transcription, transcription.source);
            if (string.IsNullOrWhiteSpace(insights))
            {
                _logger.LogError("Failed to get insights from Azure OpenAI");
                continue;
            }
            var insightObj = JsonSerializer.Deserialize<InsightResults>(insights.ExtractJson());
            if (insightObj == null)
            {
                _logger.LogError("Failed to deserialize insight results for {Source}", transcription.source);
                continue;
            }
            insightObj.TranscriptText = transcription.transcription;
            if (existingInsightObj != null)
            {
                insightObj.id = existingInsightObj.id;
            }
            bool saved = await _cosmosHelper.SaveAnalysis(insightObj);
            if (saved)
            {
                _logger.LogInformation("Saved analysis to CosmosDB for {CallId}", insightObj.CallId);
            }
            else
            {
                _logger.LogWarning("Failed to save analysis to CosmosDB for {CallId}", insightObj.CallId);
            }
        }
    }
}
