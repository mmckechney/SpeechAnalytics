using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using SpeechAnalyticsLibrary.Models;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Azure.Identity;

namespace SpeechAnalyticsLibrary
{
   public class CosmosHelper
   {
      private ILogger<CosmosHelper> log;
      private CosmosClient client;
      CosmosDB settings;
      SemanticMemory skMemory;
      public CosmosHelper(ILogger<CosmosHelper> log, AnalyticsSettings settings, SemanticMemory skMemory)
      {
         this.log = log;
         this.settings = settings.CosmosDB;
         client = new CosmosClient(settings.CosmosDB.AccountEndpoint, new DefaultAzureCredential());
         this.skMemory = skMemory;
      }
      public async Task<bool> SaveAnalysis(InsightResults insights)
      {
         try
         {
            var container = client.GetContainer(settings.DatabaseName, settings.ContainerName);

            var response = await container.UpsertItemAsync<InsightResults>(insights);
            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Created)
            {
               log.LogDebug($"Saved analysis of {insights.CallId} to CosmosDB");

               var json = JsonSerializer.Serialize<InsightResults>(insights, new JsonSerializerOptions() { WriteIndented = true });
               await skMemory.StoreMemoryAsync(insights.id, json);
               return true;
            }
            else
            {
               log.LogError($"Failed to save analysis of {insights.CallId} to CosmosDB. {response.StatusCode}");
               return false;
            }
         }
         catch (Exception exe)
         {
            log.LogError($"Failed to save analysis to CosmosDB: {exe.Message}");
            return false;
         }
      }

      public async Task<InsightResults?> GetAnalysis(string callid)
      {
         try
         {
            var container = client.GetContainer(settings.DatabaseName, settings.ContainerName);

            var query = $"SELECT * FROM c WHERE c.CallId = '{callid}'";
            var queryDefinition = new QueryDefinition(query);
            var queryResultSetIterator = container.GetItemQueryIterator<InsightResults>(queryDefinition);
            var results = new List<InsightResults>();
            while (queryResultSetIterator.HasMoreResults)
            {
               var response = await queryResultSetIterator.ReadNextAsync();
               return response.FirstOrDefault();
            }
            log.LogDebug("No analysis found in CosmosDB for item " + callid + ".");
            return null;
         }
         catch (Exception exe)
         {
            log.LogError("Failed to retrieve analysis from CosmosDB: " + exe.Message);
            return null;
         }

      }

      public async Task<string> GetQueryResults(string cosmosQuery)
      {
         log.LogDebug($"Cosmos Query: {cosmosQuery}");
         StringBuilder sb = new();
         try
         {
            var container = client.GetContainer(settings.DatabaseName, settings.ContainerName);


            var queryDefinition = new QueryDefinition(cosmosQuery);
            var streamIterator = container.GetItemQueryStreamIterator(queryDefinition);
            while (streamIterator.HasMoreResults)
            {
               var response = await streamIterator.ReadNextAsync();
               sb.AppendLine(new StreamReader(response.Content).ReadToEnd());
            }
            if (sb.Length == 0)
            {
               log.LogDebug("No analysis found in CosmosDB for query " + cosmosQuery + ".");
            }
            return sb.ToString();
         }
         catch (Exception exe)
         {
            log.LogError("Failed to retrieve analysis from CosmosDB: " + exe.Message);
            return sb.ToString();
         }

      }
   }
}
