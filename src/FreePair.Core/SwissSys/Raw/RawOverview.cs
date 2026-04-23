using System.Text.Json.Serialization;
using FreePair.Core.Tournaments.Enums;

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

    // ==== Extended NAChessHub metadata (added 2026) =====================

    [JsonPropertyName("NACH organizer ID")]
    public string? NachOrganizerId { get; set; }

    [JsonPropertyName("Starting date time")]
    public string? StartingDateTime { get; set; }

    [JsonPropertyName("Ending date time")]
    public string? EndingDateTime { get; set; }

    [JsonPropertyName("Time zone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("Event address")]
    public string? EventAddress { get; set; }

    [JsonPropertyName("Event city")]
    public string? EventCity { get; set; }

    [JsonPropertyName("Event state")]
    public string? EventState { get; set; }

    [JsonPropertyName("Event zip code")]
    public string? EventZipCode { get; set; }

    [JsonPropertyName("Event country")]
    public string? EventCountry { get; set; }

    [JsonPropertyName("Event format")]
    [JsonConverter(typeof(JsonStringEnumConverter<EventFormat>))]
    public EventFormat? EventFormat { get; set; }

    [JsonPropertyName("Event type")]
    [JsonConverter(typeof(JsonStringEnumConverter<EventType>))]
    public EventType? EventType { get; set; }

    [JsonPropertyName("Pairing rule")]
    [JsonConverter(typeof(JsonStringEnumConverter<PairingRule>))]
    public PairingRule? PairingRule { get; set; }

    [JsonPropertyName("Rounds")]
    public int? Rounds { get; set; }

    [JsonPropertyName("Half point byes")]
    public int? HalfPointByes { get; set; }

    [JsonPropertyName("Time control type")]
    [JsonConverter(typeof(JsonStringEnumConverter<TimeControlType>))]
    public TimeControlType? TimeControlType { get; set; }

    [JsonPropertyName("Rating type")]
    [JsonConverter(typeof(JsonStringEnumConverter<RatingType>))]
    public RatingType? RatingType { get; set; }
}
