using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfAI.Services.Generation.Providers.FalAi.Models;

/// <summary>
/// fal.ai bazı alanları (örn. seed) modele göre JSON number, string veya decimal
/// olarak dönebilir. System.Text.Json default olarak number→string deserialize
/// edemediği için bu converter token tipi ne olursa olsun ham metni string'e çevirir.
/// Böylece Int64 sınırını aşan / scientific / decimal değerler de güvenle yakalanır.
/// </summary>
public class FlexibleStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                // Ham metni al — büyük sayılar ve format kaybı yaşanmasın.
                return reader.HasValueSequence
                    ? System.Text.Encoding.UTF8.GetString(reader.ValueSequence.ToArray())
                    : System.Text.Encoding.UTF8.GetString(reader.ValueSpan);
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            default:
                // Beklenmeyen token (object/array) — atla ve null dön.
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}
