using System.Text.Json.Serialization;

namespace translator_demo.Models
{
    public class DocumentResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("createdDateTimeUtc")]
        public DateTime CreatedDateTimeUtc { get; set; }

        [JsonPropertyName("lastActionDateTimeUtc")]
        public DateTime LastActionDateTimeUtc { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("error")]
        public Error Error { get; set; }

        [JsonPropertyName("summary")]
        public Summary Summary { get; set; }
    }

    public class Error
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("target")]
        public string Target { get; set; }

        [JsonPropertyName("innerError")]
        public InnerError InnerError { get; set; }
    }

    public class InnerError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class Summary
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }

        [JsonPropertyName("success")]
        public int Success { get; set; }

        [JsonPropertyName("inProgress")]
        public int InProgress { get; set; }

        [JsonPropertyName("notYetStarted")]
        public int NotYetStarted { get; set; }

        [JsonPropertyName("cancelled")]
        public int Cancelled { get; set; }

        [JsonPropertyName("totalCharacterCharged")]
        public int TotalCharacterCharged { get; set; }
    }

}
