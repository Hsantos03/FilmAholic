using System.Text.Json;
using System.Text.Json.Serialization;

namespace FilmAholic.Server.Converters
{
    /// <summary>
    /// Representa um conversor de DateTime para UTC na aplicação.
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        /// <summary>
        /// Representa um conversor de DateTime para UTC na aplicação.
        /// </summary>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        /// <summary>
        /// Representa um conversor de DateTime para UTC na aplicação.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // If the DateTime is Unspecified, assume it's UTC and convert it so the 'Z' is appended
            if (value.Kind == DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }

    /// <summary>
    /// Representa um conversor de DateTime nulo para UTC na aplicação.
    /// </summary>
    public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
    {
        /// <summary>
        /// Representa um conversor de DateTime nulo para UTC na aplicação.
        /// </summary>
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        /// <summary>
        /// Representa um conversor de DateTime nulo para UTC na aplicação.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            var dt = value.Value;
            if (dt.Kind == DateTimeKind.Unspecified)
            {
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            writer.WriteStringValue(dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }
}
