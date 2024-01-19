using System.Text.Json.Serialization;

namespace SpeechAnalyticsLibrary.Models
{
   public class TranscriptionLinks
   {
      [JsonPropertyName("values")]
      public List<Value> Values { get; set; }
   }

   public class Transcription_Links
   {
      [JsonPropertyName("contentUrl")]
      public string ContentUrl { get; set; }
   }

   public class Transcription_Properties
   {
      [JsonPropertyName("size")]
      public int Size { get; set; }
   }

   public class Value
   {
      [JsonPropertyName("self")]
      public string Self { get; set; }

      [JsonPropertyName("name")]
      public string Name { get; set; }

      [JsonPropertyName("kind")]
      public string Kind { get; set; }

      [JsonPropertyName("properties")]
      public Transcription_Properties Properties { get; set; }

      [JsonPropertyName("createdDateTime")]
      public DateTime CreatedDateTime { get; set; }

      [JsonPropertyName("links")]
      public Transcription_Links Links { get; set; }
   }


}
