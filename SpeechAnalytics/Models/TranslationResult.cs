// This sample requires C# 7.1 or later for async/await.

// Install Newtonsoft.Json with NuGet

namespace translator_demo.Models
{
    /// <summary>
    /// The C# classes that represents the JSON returned by the Translator Text API.
    /// </summary>
    public class TranslationResult
    {
        public DetectedLanguage DetectedLanguage { get; set; }
        public TextResult SourceText { get; set; }
        public Translation[] Translations { get; set; }

        public string MeteredUsage { get; set; }
    }
}
