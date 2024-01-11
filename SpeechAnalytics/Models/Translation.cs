// This sample requires C# 7.1 or later for async/await.

// Install Newtonsoft.Json with NuGet

namespace translator_demo.Models
{
    public class Translation
    {
        public string Text { get; set; }
        public TextResult Transliteration { get; set; }
        public string To { get; set; }
        public Alignment Alignment { get; set; }
        public SentenceLength SentLen { get; set; }
    }
}
