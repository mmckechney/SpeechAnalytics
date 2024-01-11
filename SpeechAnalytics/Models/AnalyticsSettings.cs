using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechAnalytics.Models
{
    public class AnalyticsSettings
   {
      public AiServices AiServices { get; set; }
      public AiSearch AiSearch { get; set; }
      public Storage Storage { get; set; }
      public AzureOpenAi AzureOpenAi { get; set; }
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

}
