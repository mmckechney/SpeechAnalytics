using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using SpeechAnalyticsLibrary;
using SpeechAnalyticsLibrary.Models;

namespace SpeechAnalyticsLibrary
{
#pragma warning disable SKEXP0052 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0021 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
   public class SemanticMemory
   {
      ISemanticTextMemory semanticMemory;
      ILogger<SemanticMemory> log;
      IConfiguration config;
      ILoggerFactory logFactory;
      bool usingVolatileMemory = false;
      AzureOpenAi aOAISettings;
      AiSearch searchSettings;
      public SemanticMemory(ILoggerFactory logFactory, AnalyticsSettings settings)
      {
         log = logFactory.CreateLogger<SemanticMemory>();
         aOAISettings = settings.AzureOpenAi;
         searchSettings = settings.AiSearch;
         this.logFactory = logFactory;
         InitMemory();
      }


      private void InitMemory()
      {

         var openAIEndpoint = aOAISettings.EndPoint ?? throw new ArgumentException("Missing AzureOpenAi.Endpoint in configuration.");
         var embeddingModel = aOAISettings.EmbeddingModel ?? throw new ArgumentException("Missing AzureOpenAi.EmbeddingModel in configuration.");
         var embeddingDeploymentName = aOAISettings.EmbeddingDeploymentName ?? throw new ArgumentException("Missing AzureOpenAi.EmbeddingDeploymentName in configuration.");
         var apiKey = aOAISettings.Key ?? throw new ArgumentException("Missing AzureOpenAi.Key in configuration.");
         var aiSearchEndpoint = searchSettings.Endpoint ?? throw new ArgumentException("Missing AzureOpenAi.Key in configuration.");
         var aiSearchAdminKey = searchSettings.Key ?? throw new ArgumentException("Missing AzureOpenAi.Key in configuration.");


         IMemoryStore store;
         store =
         store = new AzureAISearchMemoryStore(aiSearchEndpoint, aiSearchAdminKey);

         var memBuilder = new MemoryBuilder()
             .WithMemoryStore(store)
             .WithTextEmbeddingGeneration(new AzureOpenAITextEmbeddingGenerationService(deploymentName: embeddingDeploymentName, modelId: embeddingModel, endpoint: openAIEndpoint, apiKey: apiKey))
             .WithLoggerFactory(logFactory);

         semanticMemory = memBuilder.Build();

      }

      public async Task StoreMemoryAsync(string fileName, string transcription, string collectionName = "general")
      {

         log.LogInformation("Storing memory...");

         await semanticMemory.SaveReferenceAsync(
               collection: collectionName,
               externalSourceName: "CosmosDb",
               externalId: fileName,
               description: $"{fileName} {transcription}",
               text: transcription);

         log.LogInformation($"Saved.");
      }

      public async Task<IAsyncEnumerable<MemoryQueryResult>> SearchMemoryAsync(string query, string collectionName = "general")
      {

         log.LogInformation("\nQuery: " + query + "\n");

         var memoryResults = semanticMemory.SearchAsync(collectionName, query, limit: 30, minRelevanceScore: 0.5, withEmbeddings: true);

         int i = 0;
         await foreach (MemoryQueryResult memoryResult in memoryResults)
         {
            log.LogInformation($"Result {++i}:");
            log.LogInformation("  URL:     : " + memoryResult.Metadata.Id);
            log.LogInformation("  Text    : " + memoryResult.Metadata.Description);
            log.LogInformation("  Relevance: " + memoryResult.Relevance);


         }

         log.LogInformation("----------------------");

         return memoryResults;
      }


   }
}
