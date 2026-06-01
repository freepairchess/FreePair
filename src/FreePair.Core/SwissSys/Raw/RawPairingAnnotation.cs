using System.Text.Json.Serialization;

namespace FreePair.Core.SwissSys.Raw;

public sealed class RawPairingAnnotation
{
    [JsonPropertyName("board")]
    public int Board { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}
