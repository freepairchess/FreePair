using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FreePair.Core.SwissSys.Raw;

/// <summary>
/// Verbatim mirror of one entry in a section's <c>Players</c> array in a
/// SwissSys <c>.sjson</c> file.
/// </summary>
public sealed class RawPlayer
{
    [JsonPropertyName("Pair number")]
    public int PairNumber { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("ID")]
    public string? Id { get; set; }

    [JsonPropertyName("Rating")]
    public int Rating { get; set; }

    [JsonPropertyName("Rating2")]
    public int? Rating2 { get; set; }

    [JsonPropertyName("Exp1")]
    public string? MembershipExpiration { get; set; }

    [JsonPropertyName("Club")]
    public string? Club { get; set; }

    [JsonPropertyName("Team")]
    public string? Team { get; set; }

    [JsonPropertyName("State")]
    public string? State { get; set; }

    [JsonPropertyName("Email")]
    public string? Email { get; set; }

    [JsonPropertyName("Phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("Reserved byes")]
    public string? ReservedByes { get; set; }

    [JsonPropertyName("Current result")]
    public string? CurrentResult { get; set; }

    [JsonPropertyName("Note")]
    public string? Note { get; set; }

    [JsonPropertyName("Results")]
    public List<string> Results { get; set; } = new();
}
