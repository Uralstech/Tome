using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamebananaAPI.Data;

internal sealed class UnixDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long timestamp = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        throw new NotImplementedException();
}
