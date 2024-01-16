using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechAnalytics.Models
{
   public class InsightResults
   {
      public string id { get; set; } = Guid.NewGuid().ToString();
      public string CallId { get; set; }
      public string Sentiment { get; set; }
      public string[] SentimentExamples { get; set; }
      public string[] FollowUpActions { get; set; }
      public string RootCause { get; set; }
   }
   
}


