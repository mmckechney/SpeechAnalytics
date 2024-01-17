using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SpeechAnalytics.Models.SpeechToText
{

   public class SpeechModels
   {
      [JsonPropertyName("values")]
      public List<Value> Values { get; set; }

      [JsonPropertyName("@nextLink")]
      public string NextLink { get; set; }
   }

   // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
   public class DeprecationDates
   {
      [JsonPropertyName("adaptationDateTime")]
      public DateTime AdaptationDateTime { get; set; }

      [JsonPropertyName("transcriptionDateTime")]
      public DateTime TranscriptionDateTime { get; set; }
   }

   public class Features
   {
      [JsonPropertyName("supportsTranscriptions")]
      public bool SupportsTranscriptions { get; set; }

      [JsonPropertyName("supportsEndpoints")]
      public bool SupportsEndpoints { get; set; }

      [JsonPropertyName("supportsTranscriptionsOnSpeechContainers")]
      public bool SupportsTranscriptionsOnSpeechContainers { get; set; }

      [JsonPropertyName("supportsAdaptationsWith")]
      public List<string> SupportsAdaptationsWith { get; set; }

      [JsonPropertyName("supportedOutputFormats")]
      public List<string> SupportedOutputFormats { get; set; }
   }

   public class Links
   {
      [JsonPropertyName("manifest")]
      public string Manifest { get; set; }
   }

   public class Properties
   {
      [JsonPropertyName("deprecationDates")]
      public DeprecationDates DeprecationDates { get; set; }

      [JsonPropertyName("features")]
      public Features Features { get; set; }

      [JsonPropertyName("chargeForAdaptation")]
      public bool ChargeForAdaptation { get; set; }
   }

   

   public class Value
   {
      [JsonPropertyName("self")]
      public string Self { get; set; }

      [JsonPropertyName("links")]
      public Links Links { get; set; }

      [JsonPropertyName("properties")]
      public Properties Properties { get; set; }

      [JsonPropertyName("lastActionDateTime")]
      public DateTime LastActionDateTime { get; set; }

      [JsonPropertyName("status")]
      public string Status { get; set; }

      [JsonPropertyName("createdDateTime")]
      public DateTime CreatedDateTime { get; set; }

      [JsonPropertyName("locale")]
      public string Locale { get; set; }

      [JsonPropertyName("displayName")]
      public string DisplayName { get; set; }

      [JsonPropertyName("description")]
      public string Description { get; set; }
   }


}
