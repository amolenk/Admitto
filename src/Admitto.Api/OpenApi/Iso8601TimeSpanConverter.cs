using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace Amolenk.Admitto.ApiService.OpenApi;

public sealed class Iso8601TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => XmlConvert.ToTimeSpan(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteStringValue(XmlConvert.ToString(value));
}