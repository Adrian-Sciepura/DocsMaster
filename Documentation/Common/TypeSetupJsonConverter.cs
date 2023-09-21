using Documentation.Models.CodeElements;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Documentation.Common
{
    internal class TypeSetupJsonConverter : JsonConverter<Dictionary<CodeElementType, bool>>
    {
        public override Dictionary<CodeElementType, bool> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();

            var result = new Dictionary<CodeElementType, bool>();
            var converter = (JsonConverter<bool>)options.GetConverter(typeof(bool));

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return result;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                string? propertyName = reader.GetString();

                if (!Enum.TryParse(propertyName, out CodeElementType key))
                    throw new JsonException($"Unable to convert \"{propertyName}\" to Enum.");


                reader.Read();
                var value = converter.Read(ref reader, typeof(bool), options)!;

                result.Add(key, value);
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<CodeElementType, bool> value, JsonSerializerOptions options)
        {
            foreach (var typeSetup in value)
            {
                writer.WritePropertyName(typeSetup.Key.ToString());
                writer.WriteStartObject();
                writer.WriteBooleanValue(typeSetup.Value);
                writer.WriteEndObject();
            }
        }
    }
}
