using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SpeechAnalyticsLibrary;
using SpeechAnalyticsLibrary.Models;
using System.IO;
using System.Text.Json;
namespace CallCenterFunction
{
   public class Transcription
   {
      private readonly ILogger<Transcription> log;
      private AnalyticsSettings settings;
      private BatchTranscription batchTranscription;
      private IdentityHelper identityHelper; FileHandling fileHandler;
      private FoundryAgentClient agentClient;
      private CosmosHelper cosmosHelper;
      private SpeechDiarization speechDiarization;

      public Transcription(ILogger<Transcription> logger, AnalyticsSettings settings, BatchTranscription batchTranscription, IdentityHelper identityHelper, FileHandling fileHandler, FoundryAgentClient agentClient, CosmosHelper cosmosHelper, SpeechDiarization speechDiarization)
      {
         log = logger;
         this.fileHandler = fileHandler;
         this.settings = settings;
         this.batchTranscription = batchTranscription;
         this.identityHelper = identityHelper;
         this.agentClient = agentClient;
         this.cosmosHelper = cosmosHelper;
         this.speechDiarization = speechDiarization;

      }

      [Function(nameof(Transcription))]
      public async Task RunAsync([EventGridTrigger] EventGridEvent eventGridEvent)
      {
         if (eventGridEvent == null)
         {
            log.LogWarning("Event Grid trigger invoked with null event.");
            return;
         }

         StorageBlobCreatedEventData blobCreatedData = null;
         try
         {
            blobCreatedData = eventGridEvent.Data.ToObjectFromJson<StorageBlobCreatedEventData>();
         }
         catch (Exception ex)
         {
            log.LogError(ex, "Failed to deserialize Event Grid data for event {EventId}.", eventGridEvent.Id);
            return;
         }

         if (blobCreatedData == null || string.IsNullOrWhiteSpace(blobCreatedData.Url))
         {
            log.LogWarning("Event Grid data missing blob Url. Event Id: {EventId}", eventGridEvent.Id);
            return;
         }

         var blobUri = new Uri(blobCreatedData.Url);
         var name = Path.GetFileName(blobUri.LocalPath.Trim('/'));

         log.LogInformation("Event Grid processing blob: {BlobUri}", blobUri);

         List<(string source, string transcription)> transcriptions = null;
         var aiSvcs = settings.AiServices;

         //Get the blob client to obtain it's URI
         var containerClient = new BlobContainerClient(new Uri(settings.Storage.SourceContainerUrl));
         var blobClient = containerClient.GetBlobClient(name);

         //Start
         var initialResponse = await batchTranscription.StartBatchTranscription(aiSvcs.Endpoint, settings.Storage.SourceContainerUrl, settings.Storage.TargetContainerUrl, blobClient.Uri);

         //Monitor
         TranscriptionResponse? statusResponse = null;
         if (initialResponse != null)
         {
            log.LogDebug($"Path to Transcription Job: {initialResponse.Self}");
            statusResponse = await batchTranscription.CheckTranscriptionStatus(initialResponse.Self);
         }

         //Get translation links
         List<string>? translationLinks = null;
         if (statusResponse != null && statusResponse.Links != null && statusResponse.Links.Files != null)
         {
            log.LogDebug($"Path to Transcription Files List: ${statusResponse.Links.Files}");
            translationLinks = await batchTranscription.GetTranslationOutputLinks(statusResponse.Links.Files);
         }
         else
         {
            log.LogError("Failed to transcribe files");
            return;
         }

         //Save transcriptions 
         if (translationLinks != null)
         {
            log.LogDebug($"Transcription File Links: {string.Join(Environment.NewLine, translationLinks.ToArray())}");
            transcriptions = await batchTranscription.GetTranscriptionText(translationLinks);
            foreach (var transcription in transcriptions)
            {
               await fileHandler.SaveTranscriptionFile(transcription.source, transcription.transcription, settings.Storage.TargetContainerUrl);
               log.LogDebug($"Transcription for {transcription.source}:{Environment.NewLine}{transcription.transcription}");
            }
         }


         //Get insights and Save to CosmosDB
         foreach (var transcription in transcriptions)
         {

            var existingInsightObj = await cosmosHelper.GetAnalysis(transcription.source);
            string insights = await agentClient.GetTranscriptionInsights(transcription.transcription, transcription.source);
            if (string.IsNullOrWhiteSpace(insights))
            {
               log.LogError("Failed to get insights from Azure OpenAI");
               continue;
            }
            log.LogDebug($"{insights.ExtractJson()}", ConsoleColor.DarkCyan);
            var insightObj = JsonSerializer.Deserialize<InsightResults>(insights.ExtractJson());
            insightObj.TranscriptText = transcription.transcription;
            if (existingInsightObj != null)
            {
               insightObj.id = existingInsightObj.id;
            }
            log.LogDebug(JsonSerializer.Serialize<InsightResults>(insightObj, new JsonSerializerOptions() { WriteIndented = true }));
            bool saved = await cosmosHelper.SaveAnalysis(insightObj);
            if (saved)
            {
               log.LogInformation("Saved analysis to CosmosDB", ConsoleColor.Green);
            }
            else
            {
               log.LogWarning("Failed to save analyis to CosmosDB", ConsoleColor.Red);
            }
         }

         return;
      }
   }
}
