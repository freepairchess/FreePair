using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FreePair.Core.SwissSys.Raw;

/// <summary>
/// Verbatim mirror of one entry in a section's <c>Teams</c> array in a
/// SwissSys <c>.sjson</c> file. Only populated in team-enabled sections.
/// </summary>
public sealed class RawTeam
{
    [JsonPropertyName("Pair number")]
    public int PairNumber { get; set; }

    [JsonPropertyName("Full name")]
    public string? FullName { get; set; }

    [JsonPropertyName("Team code")]
    public string? TeamCode { get; set; }

    [JsonPropertyName("Rating")]
    public int Rating { get; set; }

    [JsonPropertyName("Current result")]
    public string? CurrentResult { get; set; }

    [JsonPropertyName("Results")]
    public List<string> Results { get; set; } = new();
}
