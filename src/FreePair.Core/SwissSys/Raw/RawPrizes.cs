using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FreePair.Core.SwissSys.Raw;

/// <summary>
/// Verbatim mirror of a section's <c>Prizes</c> object in a SwissSys
/// <c>.sjson</c> file.
/// </summary>
public sealed class RawPrizes
{
    [JsonPropertyName("Place prizes")]
    public List<RawPrize> PlacePrizes { get; set; } = new();

    [JsonPropertyName("Class prizes")]
    public List<RawPrize> ClassPrizes { get; set; } = new();
}

/// <summary>
/// A single prize entry (place or class) as stored in a SwissSys
/// <c>.sjson</c> file.
/// </summary>
public sealed class RawPrize
{
    [JsonPropertyName("Value")]
    public decimal Value { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }
}
