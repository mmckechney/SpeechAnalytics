using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace translator_demo.Models
{
    public class DocumentRequest
    {
        [JsonPropertyName("inputs")]
        public List<Input> Inputs { get; set; }
    }

    public class Input
    {
        [JsonPropertyName("source")]
        public Source Source { get; set; }

        [JsonPropertyName("targets")]
        public List<Target> Targets { get; set; }

        [JsonPropertyName("storageType")]
        public string StorageType { get; set; } = "File";
    }

    public class Source
    {
        [JsonPropertyName("sourceUrl")]
        public string SourceUrl { get; set; }

        //[JsonPropertyName("storageSource")]
        //public string StorageSource { get; set; } = "AzureBlob";

        [JsonPropertyName("language")]
        public string Language { get; set; } = "en";
    }

    public class Target
    {
        [JsonPropertyName("targetUrl")]
        public string TargetUrl { get; set; }

        //[JsonPropertyName("storageSource")]
        //public string StorageSource { get; set; } = "AzureBlob";

        //[JsonPropertyName("category")]
        //public string Category { get; set; } = "general";

        [JsonPropertyName("language")]
        public string Language { get; set; }
    }

   

   


}
