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

    /// <summary>
    /// NAChessHub organiser identifier. Renamed from
    /// <c>"NACH organizer ID"</c> to <c>"Organizer ID"</c> in SwissSys
    /// 11.34 — the value is still the same kind of opaque id string.
    /// </summary>
    [JsonPropertyName("Organizer ID")]
    public string? OrganizerId { get; set; }

    /// <summary>Kind of id stored in <see cref="OrganizerId"/>.</summary>
    [JsonPropertyName("Organizer ID Type")]
    [JsonConverter(typeof(JsonStringEnumConverter<UserIDType>))]
    public UserIDType? OrganizerIdType { get; set; }

    /// <summary>Display name for the organiser.</summary>
    [JsonPropertyName("Organizer Name")]
    public string? OrganizerName { get; set; }

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

    /// <summary>
    /// Per-tournament delegations array — owner + tournament
    /// directors entered by the organiser at registration time.
    /// FreePair reads this to pre-fill the USCF export dialog's
    /// CTD / ATD fields.
    /// </summary>
    [JsonPropertyName("Delegations")]
    public System.Collections.Generic.List<RawDelegation>? Delegations { get; set; }

    // ==== FreePair-specific extensions ====================================
    // Harmless to SwissSys (it ignores unknown keys); persisted at the
    // top of the Overview block so the writer can pass them through
    // without inventing a new nested structure.

    /// <summary>
    /// When <c>true</c>, FreePair will auto-upload this tournament's
    /// <c>.sjson</c> after every pairing update. Round-trips through
    /// the Overview block so it's sticky per-tournament.
    /// </summary>
    [JsonPropertyName("FreePair auto publish pairings")]
    public bool? FreePairAutoPublishPairings { get; set; }

    /// <summary>
    /// When <c>true</c>, FreePair will auto-upload this tournament's
    /// <c>.sjson</c> after every individual result entry.
    /// </summary>
    [JsonPropertyName("FreePair auto publish results")]
    public bool? FreePairAutoPublishResults { get; set; }

    /// <summary>
    /// ISO-8601 UTC timestamp of the most recent successful publish to
    /// NA Chess Hub (or any configured publishing destination). Stamped
    /// after both the <c>.sjson</c> and the derived results JSON upload
    /// succeed. Present only when the tournament has been published at
    /// least once.
    /// </summary>
    [JsonPropertyName("FreePair last published at")]
    public string? FreePairLastPublishedAt { get; set; }

    // ==== FreePair USCF report preferences (per-tournament sticky) ====

    /// <summary>
    /// Affiliate ID override for the USCF export. When set, takes
    /// precedence over <see cref="OrganizerId"/>. Useful when the
    /// organiser ID isn't actually a USCF affiliate id (e.g. a FIDE
    /// id) but the TD still wants to submit to USCF.
    /// </summary>
    [JsonPropertyName("FreePair USCF affiliate ID")]
    public string? FreePairUscfAffiliateId { get; set; }

    [JsonPropertyName("FreePair USCF chief TD ID")]
    public string? FreePairUscfChiefTdId { get; set; }

    [JsonPropertyName("FreePair USCF assistant TD ID")]
    public string? FreePairUscfAssistantTdId { get; set; }

    [JsonPropertyName("FreePair USCF other TD notes")]
    public string? FreePairUscfOtherTdNotes { get; set; }

    /// <summary>One letter — R / Q / B / D.</summary>
    [JsonPropertyName("FreePair USCF rating system")]
    public string? FreePairUscfRatingSystem { get; set; }

    [JsonPropertyName("FreePair USCF send crosstable")]
    public bool? FreePairUscfSendCrossTable { get; set; }

    [JsonPropertyName("FreePair USCF grand prix")]
    public bool? FreePairUscfGrandPrix { get; set; }

    [JsonPropertyName("FreePair USCF FIDE rated")]
    public bool? FreePairUscfFideRated { get; set; }

    [JsonPropertyName("FreePair USCF include section dates")]
    public bool? FreePairUscfIncludeSectionDates { get; set; }
}
