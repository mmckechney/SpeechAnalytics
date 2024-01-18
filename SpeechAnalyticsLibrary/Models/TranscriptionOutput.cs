using System.Text;
using System.Text.Json.Serialization;

namespace SpeechAnalyticsLibrary.Models
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
      public string RawTranscriptionText
      {
         get
         {
            return CombinedRecognizedPhrases?.FirstOrDefault()?.Display ?? string.Empty;
         }
      }
      private string _speakerTranscriptionText = "";
      public string SpeakerTranscriptionText
      {
         get
         {
            if (_speakerTranscriptionText == "")
            {
               return CompileSpeakerTranscriptionText();
            }
            else
            {
               return _speakerTranscriptionText;
            }
         }
      }

      private string CompileSpeakerTranscriptionText()
      {
         StringBuilder sb = new();
         int lastSpeaker = -1;
         var ordered = RecognizedPhrases.Where(r => r.Speaker != 0).OrderBy(r => r.OffsetInTicks);
         foreach (var phrase in ordered)
         {
            if (phrase.Speaker != lastSpeaker)
            {
               sb.Append(Environment.NewLine);
               sb.Append($"SPEAKER {phrase.Speaker}: {phrase.NBest[0].Display}");
               lastSpeaker = phrase.Speaker;
            }
            else
            {
               sb.Append($" {phrase.NBest[0].Display}");
            }
         }
         sb.AppendLine();
         return sb.ToString();
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

         [JsonPropertyName("speaker")]
         public int Speaker { get; set; }
      }


   }

}
