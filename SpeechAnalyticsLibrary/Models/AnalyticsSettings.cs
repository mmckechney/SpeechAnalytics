namespace SpeechAnalyticsLibrary.Models
{
   public class AnalyticsSettings
   {
      public AnalyticsSettings()
      { }
      public AiServices AiServices { get; set; }
      public AiSearch AiSearch { get; set; }
      public Storage Storage { get; set; }
      public AzureOpenAi AzureOpenAi { get; set; }
      public CosmosDB CosmosDB { get; set; }
   }

   public class AiSearch
   {
      public string Endpoint { get; set; }
      public string Key { get; set; }
   }

   public class AiServices
   {
      public string Endpoint { get; set; }
      public string Key { get; set; }

      public string Region { get; set; }
      public string ApiVersion { get; set; } = "v3.2-preview.1";
   }

   public class Storage
   {
      public string SourceContainerUrl { get; set; }
      public string TargetContainerUrl { get; set; }
   }

   public class AzureOpenAi
   {
      public string EndPoint { get; set; }
      public string Key { get; set; }
      public string ChatModel { get; set; }
      public string ChatDeploymentName { get; set; }
      public string EmbeddingModel { get; set; }
      public string EmbeddingDeploymentName { get; set; }
   }
   public class CosmosDB
   {
      public string ConnectionString { get; set; }
      public string ContainerName { get; set; }
      public string DatabaseName { get; set; }
   }


}
