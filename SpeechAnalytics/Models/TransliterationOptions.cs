using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace translator_demo.Models
{
    public class ToScript
    {
        public string code { get; set; }
        public string name { get; set; }
        public string nativeName { get; set; }
        public string dir { get; set; }
    }

    public class Script
    {
        public string code { get; set; }
        public string name { get; set; }
        public string nativeName { get; set; }
        public string dir { get; set; }
        public List<ToScript> toScripts { get; set; }
    }

    public class LanguageData
    {
        public string name { get; set; }
        public string nativeName { get; set; }
        public List<Script> scripts { get; set; }
    }

    public class Transliteration
    {
        public Dictionary<string, LanguageData> Languages { get; set; }
    }

    public class Root
    {
        public Transliteration transliteration { get; set; }
    }
}
//static public async Task GetTransliterationOptions()
//{


//    using (var client = new HttpClient())
//    using (var request = new HttpRequestMessage())
//    {
//        // Build the request.
//        request.Method = HttpMethod.Get;
//        request.RequestUri = new Uri("https://api.cognitive.microsofttranslator.com/languages?api-version=3.0&scope=transliteration");

//        // Send the request and get response.
//        HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
//        // Read response as a string.
//        string result = await response.Content.ReadAsStringAsync();
//        //var deserializedOutput = JsonConvert.DeserializeObject<Root>(result);
//        var r = System.Text.Json.JsonSerializer.Deserialize<Transliteration>(result);
//        Root root = JsonConvert.DeserializeObject<Root>(result);

//        //var x = 



//        var t = true;

//    }
//}