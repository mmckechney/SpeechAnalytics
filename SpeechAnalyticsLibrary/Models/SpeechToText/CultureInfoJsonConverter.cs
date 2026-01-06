using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeechAnalyticsLibrary.Models.SpeechToText
{
    public class CultureInfoJsonConverter : JsonConverter<CultureInfo>
    {
        public override CultureInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var cultureName = reader.GetString();
            return string.IsNullOrEmpty(cultureName) ? null : new CultureInfo(cultureName);
        }

        public override void Write(Utf8JsonWriter writer, CultureInfo value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Name);
            }
        }
    }
}
