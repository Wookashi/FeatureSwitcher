using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wookashi.FeatureSwitcher.Shared.Abstraction.Json;

/// <summary>
/// Serializes every <see cref="DateTime"/> as UTC with the "Z" suffix. FeatureSwitcher stores and
/// exchanges all timestamps in UTC; a value without a time zone (<see cref="DateTimeKind.Unspecified"/>,
/// e.g. read from SQLite or received from an older Node) is treated as UTC. Without the suffix a
/// browser parses the value as local time and every displayed time is shifted by the viewer's UTC offset.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ToUtc(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ToUtc(value));

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
