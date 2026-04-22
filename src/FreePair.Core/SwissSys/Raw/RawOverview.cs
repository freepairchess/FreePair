using System.Text.Json.Serialization;

namespace FreePair.Core.SwissSys.Raw;

/// <summary>
/// Tournament-level metadata block (<c>Overview</c>) in a SwissSys
/// <c>.sjson</c> file.
/// </summary>
public sealed class RawOverview
{
    [JsonPropertyName("Program")]
    public string? Program { get; set; }

    [JsonPropertyName("Version")]
    public double? Version { get; set; }

    [JsonPropertyName("Tournament title")]
    public string? TournamentTitle { get; set; }

    [JsonPropertyName("Tournament time controls")]
    public string? TournamentTimeControls { get; set; }

    [JsonPropertyName("Starting date")]
    public string? StartingDate { get; set; }

    [JsonPropertyName("Ending date")]
    public string? EndingDate { get; set; }

    [JsonPropertyName("NACH event ID")]
    public string? NachEventId { get; set; }

    [JsonPropertyName("NACH passcode")]
    public string? NachPasscode { get; set; }

    [JsonPropertyName("Trace")]
    public string? Trace { get; set; }
}
