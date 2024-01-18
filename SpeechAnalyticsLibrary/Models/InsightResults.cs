namespace SpeechAnalyticsLibrary.Models
{
   public class InsightResults
   {
      public string id { get; set; } = Guid.NewGuid().ToString();
      public string CallId { get; set; }
      public string Sentiment { get; set; }
      public string[] SentimentExamples { get; set; }
      public string[] FollowUpActions { get; set; }
      public string ProblemStatement { get; set; }

      public string RootCause { get; set; }
      public string Resolved { get; set; }
      public string TranscriptText { get; set; }
   }

}


