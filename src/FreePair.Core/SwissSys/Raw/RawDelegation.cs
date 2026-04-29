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
    [JsonConverter(typeof(JsonStringEnumConverter<DelegationLevel>))]
    public DelegationLevel? Level { get; set; }
}
