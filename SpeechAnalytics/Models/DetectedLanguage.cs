// This sample requires C# 7.1 or later for async/await.

// Install Newtonsoft.Json with NuGet

namespace translator_demo.Models
{
    public class DetectedLanguage
    {
        public string Language { get; set; }
        public float Score { get; set; }
    }
}
