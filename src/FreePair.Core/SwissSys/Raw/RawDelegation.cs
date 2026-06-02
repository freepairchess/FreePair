using System.Text.Json;
using System.Text.Json.Serialization;
using FreePair.Core.Tournaments;

namespace FreePair.Core.SwissSys.Raw;

/// <summary>
/// Raw shape of one entry in the <c>Delegations</c> array under
/// the SwissSys Overview block. Field names match the on-disk
/// keys verbatim; <see cref="SwissSysMapper"/> projects this onto
/// the domain <see cref="Delegation"/> record.
/// </summary>
public sealed class RawDelegation
{
    [JsonPropertyName("Player ID")]
    public string? PlayerId { get; set; }

    [JsonPropertyName("Player Name")]
    public string? PlayerName { get; set; }

    [JsonPropertyName("Email")]
    public string? Email { get; set; }

    [JsonPropertyName("Phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("Delegation Level")]
    [JsonConverter(typeof(LenientDelegationLevelConverter))]
    public DelegationLevel? Level { get; set; }
}

/// <summary>
/// Deserializes <see cref="DelegationLevel"/> leniently: unknown or
/// empty strings map to <see cref="DelegationLevel.Other"/> instead
/// of throwing.
/// </summary>
internal sealed class LenientDelegationLevelConverter : JsonConverter<DelegationLevel?>
{
    public override DelegationLevel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return DelegationLevel.Other;

        if (Enum.TryParse<DelegationLevel>(value.Replace(" ", ""), ignoreCase: true, out var level))
            return level;

        return DelegationLevel.Other;
    }

    public override void Write(Utf8JsonWriter writer, DelegationLevel? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString());
    }
}
