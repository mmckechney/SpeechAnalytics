using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SpeechAnalyticsLibrary.Models;

namespace SpeechAnalyticsLibrary
{
   public class CosmosHelper
   {
      private ILogger<CosmosHelper> log;
      private CosmosClient client;
      CosmosDB settings;
      public CosmosHelper(ILogger<CosmosHelper> log, AnalyticsSettings settings)
      {
         this.log = log;
         this.settings = settings.CosmosDB;
         client = new CosmosClient(this.settings.ConnectionString);
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
   }
}
