using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using SpeechAnalytics.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpeechAnalytics
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
      public SemanticMemory(ILoggerFactory logFactory, AzureOpenAi aOAISettings, AiSearch searchSettings)
      {
         log = logFactory.CreateLogger<SemanticMemory>();
         this.aOAISettings = aOAISettings;
         this.searchSettings = searchSettings;
         this.logFactory = logFactory;
      }


      public void InitMemory()
      {

         var openAIEndpoint = aOAISettings.EndPoint ?? throw new ArgumentException("Missing AzureOpenAi.Endpoint in configuration.");
         var embeddingModel = aOAISettings.EmbeddingModel ?? throw new ArgumentException("Missing AzureOpenAi.EmbeddingModel in configuration.");
         var embeddingDeploymentName = aOAISettings.EmbeddingDeploymentName ?? throw new ArgumentException("Missing AzureOpenAi.EmbeddingDeploymentName in configuration.");
         var apiKey = aOAISettings.Key ?? throw new ArgumentException("Missing AzureOpenAi.Key in configuration.");
         var cogSearchEndpoint = searchSettings.Endpoint ?? throw new ArgumentException("Missing AzureOpenAi.Key in configuration.");
         var cogSearchAdminKey = searchSettings.Key ?? throw new ArgumentException("Missing AzureOpenAi.Key in configuration.");

         IMemoryStore store;
         store = new AzureAISearchMemoryStore(cogSearchEndpoint, cogSearchAdminKey);
  
         var memBuilder = new MemoryBuilder()
             .WithMemoryStore(store)
             .WithAzureOpenAITextEmbeddingGeneration(deploymentName: embeddingDeploymentName, modelId: embeddingModel, endpoint: openAIEndpoint, apiKey: apiKey)
             .WithLoggerFactory(logFactory);

         semanticMemory = memBuilder.Build();

      }

      public async Task StoreMemoryAsync(string collectionName, string fileName, string transcription) 
      {

         log.LogInformation("Storing memory...");

         await semanticMemory.SaveReferenceAsync(
               collection: collectionName,
               externalSourceName: "BlobStorage",
               externalId: fileName,
               description: $"{fileName} {transcription}",
               text: transcription);

         log.LogInformation($"Saved.");
      }

      public async Task<IAsyncEnumerable<MemoryQueryResult>> SearchMemoryAsync(string collectionName, string query)
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
