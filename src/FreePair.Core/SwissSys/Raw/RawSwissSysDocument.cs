using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FreePair.Core.SwissSys.Raw;

/// <summary>
/// Verbatim mirror of the root object of a SwissSys <c>.sjson</c> file.
/// Preserves the on-disk shape without interpretation so that higher layers
/// can map it into a clean domain model.
/// </summary>
public sealed class RawSwissSysDocument
{
    [JsonPropertyName("Overview")]
    public RawOverview? Overview { get; set; }

    [JsonPropertyName("Sections")]
    public List<RawSection> Sections { get; set; } = new();
}
