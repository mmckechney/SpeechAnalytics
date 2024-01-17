//using System.Collections.Generic;
//using System.Text.Json.Serialization;

////namespace SpeechAnalytics.Models
////{

////   public class TranscriptionRequest
////   {
////      [JsonPropertyName("contentUrls")]
////      public List<string> ContentUrls { get; set; }

////      [JsonPropertyName("locale")]
////      public string Locale { get; set; } = "en-us";

////      [JsonPropertyName("displayName")]
////      public string DisplayName { get; set; }

////      [JsonPropertyName("model")]
////      public object Model { get; set; }

////      [JsonPropertyName("properties")]
////      public RequestProperties Properties { get; set; } = new RequestProperties();

////      [JsonPropertyName("timeToLive")]
////      public string TimeToLive { get; set; } = "PT4H";

////      [JsonPropertyName("contentContainerUrl")]
////      public string ContentContainerUrl { get; set; }
////   }

////public class RequestProperties
////{
////   [JsonPropertyName("wordLevelTimestampsEnabled")]
////   public bool WordLevelTimestampsEnabled { get; set; } = false;

////   [JsonPropertyName("diarizationEnabled")]
////   public bool DiarizationEnabled { get; set; } = true;

////   [JsonPropertyName("punctuationMode")]
////   public string PunctuationMode { get; set; } = "DictatedAndAutomatic";

////   [JsonPropertyName("profanityFilterMode")]
////   public string ProfanityFilterMode { get; set; } = "Masked";

////   [JsonPropertyName("languageIdentification")]
////   public RequestLanguageIdentification LanguageIdentification { get; set; } = new RequestLanguageIdentification() { CandidateLocales = new List<string>() { "en-US", "es-ES" } };
////}

////public class RequestLanguageIdentification
////{
////   [JsonPropertyName("candidateLocales")]
////   public List<string> CandidateLocales { get; set; }
////}

////}
