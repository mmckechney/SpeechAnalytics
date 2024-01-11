// This sample requires C# 7.1 or later for async/await.

// Install Newtonsoft.Json with NuGet

namespace translator_demo.Models
{
    public class SentenceLength
    {
        public int[] SrcSentLen { get; set; }
        public int[] TransSentLen { get; set; }
    }
}
