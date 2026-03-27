using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimplerJiangAiAgent.Api.Infrastructure.Serialization;

public static class ChinaTimeZone
{
    public static readonly TimeZoneInfo Info = Resolve();

    public static DateTime ToChina(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, Info);
    }

    public static DateTime? ToChina(DateTime? value)
    {
        return value.HasValue ? ToChina(value.Value) : null;
    }

    private static TimeZoneInfo Resolve()
    {
        var ids = new[] { "China Standard Time", "Asia/Shanghai" };
        foreach (var id in ids)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch
            {
                // continue
            }
        }

        return TimeZoneInfo.Utc;
    }
}

public sealed class ChinaDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss.fffffffK";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Invalid DateTime token.");
        }

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return default;
        }

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
        {
            return dto.UtcDateTime;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
        }

        throw new JsonException($"Invalid DateTime value: {raw}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var china = ChinaTimeZone.ToChina(value);
        var dto = new DateTimeOffset(china, ChinaTimeZone.Info.GetUtcOffset(china));
        writer.WriteStringValue(dto.ToString(Format, CultureInfo.InvariantCulture));
    }
}

public sealed class ChinaNullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private readonly ChinaDateTimeJsonConverter _inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return _inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        _inner.Write(writer, value.Value, options);
    }
}