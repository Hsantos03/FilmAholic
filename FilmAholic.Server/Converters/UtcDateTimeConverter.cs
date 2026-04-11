using System.Text.Json;
using System.Text.Json.Serialization;

namespace FilmAholic.Server.Converters
{
    /// <summary>
    /// Conversor personalizado de JSON responsável por assegurar que propriedades do tipo <see cref="DateTime"/>
    /// são tratadas como UTC durante a serialização e desserialização.
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        /// <summary>
        /// Lê e converte uma string num formato JSON para um objeto <see cref="DateTime"/>.
        /// </summary>
        /// <param name="reader">Leitor de JSON nativo de leitura sequencial do payload.</param>
        /// <param name="typeToConvert">O tipo original que está a ser convertido.</param>
        /// <param name="options">As opções de serialização especificadas na configuração.</param>
        /// <returns>Objeto contendo a data lida da estrutura JSON.</returns>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        /// <summary>
        /// Escreve um objeto <see cref="DateTime"/> no formato JSON, garantindo que é tratado como UTC.
        /// </summary>
        /// <param name="writer">Escritor que escreve diretamente no fluxo de saída do JSON.</param>
        /// <param name="value">A data a ser serializada.</param>
        /// <param name="options">As opções de serialização atuais.</param>
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
    /// Conversor personalizado de JSON responsável por assegurar que propriedades do tipo <see cref="DateTime?"/>
    /// são tratadas como UTC durante a serialização e desserialização.
    /// </summary>
    public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
    {
        /// <summary>
        /// Lê e converte uma string num formato JSON para um objeto <see cref="DateTime?"/>.
        /// </summary>
        /// <param name="reader">Leitor assíncrono nativo de leitura JSON.</param>
        /// <param name="typeToConvert">Tipo de dados (DateTime?).</param>
        /// <param name="options">As opções correntes do JsonSerializer.</param>
        /// <returns>Retorna a data presente no JSON ou null caso esteja limpa.</returns>
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTime();
        }

        /// <summary>
        /// Escreve um objeto <see cref="DateTime?"/> no formato JSON, garantindo que é tratado como UTC.
        /// </summary>
        /// <param name="writer">Escritor responsável pela produção da saída final no JSON.</param>
        /// <param name="value">A data nula a submeter para processamento textual.</param>
        /// <param name="options">Configurações subjacentes da serialização em curso.</param>
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
