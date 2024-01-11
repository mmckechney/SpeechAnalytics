using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SpeechAnalytics.Models
{
  
   public class TranscriptionOutput
   {
      [JsonPropertyName("source")]
      public string Source { get; set; }

      [JsonPropertyName("timestamp")]
      public DateTime Timestamp { get; set; }

      [JsonPropertyName("durationInTicks")]
      public long DurationInTicks { get; set; }

      [JsonPropertyName("duration")]
      public string Duration { get; set; }

      [JsonPropertyName("combinedRecognizedPhrases")]
      public List<CombinedRecognizedPhrase> CombinedRecognizedPhrases { get; set; }

      [JsonPropertyName("recognizedPhrases")]
      public List<RecognizedPhrase> RecognizedPhrases { get; set; }

      [JsonIgnore]
      public string TranscriptionText
      {
         get
         {
            return CombinedRecognizedPhrases?.FirstOrDefault()?.Display ?? string.Empty;
         }
      }
   }

     public class CombinedRecognizedPhrase
   {
      [JsonPropertyName("channel")]
      public int Channel { get; set; }

      [JsonPropertyName("lexical")]
      public string Lexical { get; set; }

      [JsonPropertyName("itn")]
      public string Itn { get; set; }

      [JsonPropertyName("maskedITN")]
      public string MaskedITN { get; set; }

      [JsonPropertyName("display")]
      public string Display { get; set; }
   }

   public class NBest
   {
      [JsonPropertyName("confidence")]
      public double Confidence { get; set; }

      [JsonPropertyName("lexical")]
      public string Lexical { get; set; }

      [JsonPropertyName("itn")]
      public string Itn { get; set; }

      [JsonPropertyName("maskedITN")]
      public string MaskedITN { get; set; }

      [JsonPropertyName("display")]
      public string Display { get; set; }
   }

   public class RecognizedPhrase
   {
      [JsonPropertyName("recognitionStatus")]
      public string RecognitionStatus { get; set; }

      [JsonPropertyName("channel")]
      public int Channel { get; set; }

      [JsonPropertyName("offset")]
      public string Offset { get; set; }

      [JsonPropertyName("duration")]
      public string Duration { get; set; }

      [JsonPropertyName("offsetInTicks")]
      public double OffsetInTicks { get; set; }

      [JsonPropertyName("durationInTicks")]
      public double DurationInTicks { get; set; }

      [JsonPropertyName("nBest")]
      public List<NBest> NBest { get; set; }

      [JsonPropertyName("locale")]
      public string Locale { get; set; }
   }

   


}
