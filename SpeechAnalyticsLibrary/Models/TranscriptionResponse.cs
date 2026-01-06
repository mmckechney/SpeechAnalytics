namespace SpeechAnalyticsLibrary.Models
{
  // using SpeechAnalyticsLibrary.Models.SpeechToText.API;
   using BatchClient;
   using System.Collections.Generic;
   using System.Text.Json.Serialization;

   public class TranscriptionResponse
   {

      [JsonPropertyName("links")]
      public Links Links { get; set; }

      [JsonPropertyName("properties")]
      public Properties Properties { get; set; }

      [JsonPropertyName("self")]
      public string Self { get; set; }

      [JsonPropertyName("model")]
      public Model Model { get; set; }

      [JsonPropertyName("project")]
      public Project Project { get; set; }

      [JsonPropertyName("dataset")]
      public Dataset Dataset { get; set; }

      [JsonPropertyName("contentUrls")]
      public List<string> ContentUrls { get; set; }

      [JsonPropertyName("contentContainerUrl")]
      public string ContentContainerUrl { get; set; }

      [JsonPropertyName("locale")]
      public string Locale { get; set; }

      [JsonPropertyName("displayName")]
      public string DisplayName { get; set; }

      [JsonPropertyName("description")]
      public string Description { get; set; }

      [JsonPropertyName("customProperties")]
      public Dictionary<string, object> CustomProperties { get; set; }

      [JsonPropertyName("lastActionDateTime")]
      public string LastActionDateTime { get; set; }

      [JsonPropertyName("status")]
      public string Status { get; set; }

      [JsonPropertyName("createdDateTime")]
      public string CreatedDateTime { get; set; }
   }

   public class Links
   {
      [JsonPropertyName("files")]
      public string Files { get; set; }
   }

   public class Properties
   {
      [JsonPropertyName("diarizationEnabled")]
      public bool DiarizationEnabled { get; set; }

      [JsonPropertyName("wordLevelTimestampsEnabled")]
      public bool WordLevelTimestampsEnabled { get; set; }

      [JsonPropertyName("displayFormWordLevelTimestampsEnabled")]
      public bool DisplayFormWordLevelTimestampsEnabled { get; set; }

      [JsonPropertyName("duration")]
      public string Duration { get; set; }

      [JsonPropertyName("channels")]
      public List<int> Channels { get; set; }

      [JsonPropertyName("destinationContainerUrl")]
      public string DestinationContainerUrl { get; set; }

      [JsonPropertyName("punctuationMode")]
      public string PunctuationMode { get; set; }

      [JsonPropertyName("profanityFilterMode")]
      public string ProfanityFilterMode { get; set; }

      [JsonPropertyName("timeToLive")]
      public string TimeToLive { get; set; }

      [JsonPropertyName("diarization")]
      public Diarization Diarization { get; set; }

      [JsonPropertyName("languageIdentification")]
      public LanguageIdentificationProperties LanguageIdentification { get; set; }

      [JsonPropertyName("email")]
      public string Email { get; set; }

      [JsonPropertyName("error")]
      public Error Error { get; set; }
   }

   public class Diarization
   {
      [JsonPropertyName("speakers")]
      public Speakers Speakers { get; set; }
   }

   public class Speakers
   {
      [JsonPropertyName("minCount")]
      public int MinCount { get; set; }

      [JsonPropertyName("maxCount")]
      public int MaxCount { get; set; }
   }

   public class LanguageIdentification
   {
      [JsonPropertyName("candidateLocales")]
      public List<string> CandidateLocales { get; set; }

      [JsonPropertyName("speechModelMapping")]
      public Dictionary<string, object> SpeechModelMapping { get; set; }
   }

   public class Error
   {
      [JsonPropertyName("code")]
      public string Code { get; set; }

      [JsonPropertyName("message")]
      public string Message { get; set; }
   }

   public class Model
   {
      [JsonPropertyName("self")]
      public string Self { get; set; }
   }

   public class Project
   {
      [JsonPropertyName("self")]
      public string Self { get; set; }
   }

   public class Dataset
   {
      [JsonPropertyName("self")]
      public string Self { get; set; }
   }
}
