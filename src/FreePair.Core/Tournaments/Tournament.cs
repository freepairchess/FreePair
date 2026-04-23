using System;
using System.Collections.Generic;
using FreePair.Core.Tournaments.Enums;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Root of the FreePair domain model for a loaded tournament.
/// Most event-level metadata mirrors the NAChessHub top-level block
/// in a SwissSys <c>.sjson</c> file. All new fields are nullable so
/// legacy files that pre-date the extended metadata still load.
/// </summary>
public sealed record Tournament(
    string? Title,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? TimeControl,
    string? NachEventId,
    IReadOnlyList<Section> Sections,

    // ============= extended NAChessHub metadata =============

    /// <summary>
    /// Organiser identifier as recorded in the SwissSys Overview
    /// (renamed from the earlier <c>"NACH organizer ID"</c> to
    /// <c>"Organizer ID"</c> in SwissSys 11.34).
    /// </summary>
    string? OrganizerId = null,
    /// <summary>Kind of id stored in <see cref="OrganizerId"/>.</summary>
    Enums.UserIDType? OrganizerIdType = null,
    /// <summary>Organiser display name.</summary>
    string? OrganizerName = null,

    /// <summary>NAChessHub passcode (secret). Handled as a password
    /// field in the UI — round-tripped as-is through SwissSys.</summary>
    string? NachPasscode = null,

    /// <summary>Timed start, superset of <see cref="StartDate"/>.</summary>
    DateTimeOffset? StartDateTime = null,
    /// <summary>Timed end, superset of <see cref="EndDate"/>.</summary>
    DateTimeOffset? EndDateTime = null,
    /// <summary>Windows time-zone id, e.g. <c>"Pacific Standard Time"</c>.</summary>
    string? TimeZone = null,

    // --- address ---
    string? EventAddress = null,
    string? EventCity = null,
    string? EventState = null,
    string? EventZipCode = null,
    string? EventCountry = null,

    // --- classifications ---
    EventFormat? EventFormat = null,
    EventType? EventType = null,
    PairingRule? PairingRule = null,
    TimeControlType? TimeControlType = null,
    RatingType? RatingType = null,

    /// <summary>Total rounds planned at the event level (SwissSys <c>"Rounds"</c>).</summary>
    int? RoundsPlanned = null,
    /// <summary>Max half-point byes a player may request (SwissSys <c>"Half point byes"</c>).</summary>
    int? HalfPointByesAllowed = null,

    // ============ FreePair-specific persisted settings ============
    /// <summary>
    /// When <c>true</c>, FreePair auto-uploads the <c>.sjson</c> to
    /// the configured publishing destination after every pairing
    /// update. Persisted as <c>"FreePair auto publish pairings"</c>
    /// in the Overview block.
    /// </summary>
    bool? AutoPublishPairings = null,
    /// <summary>
    /// When <c>true</c>, FreePair auto-uploads after every individual
    /// result entry. Persisted as <c>"FreePair auto publish results"</c>.
    /// </summary>
    bool? AutoPublishResults = null,
    /// <summary>
    /// Wall-clock timestamp of the most recent successful publish to
    /// the configured online destination (NA Chess Hub etc.). Persisted
    /// as <c>"FreePair last published at"</c> (ISO-8601 UTC). Null when
    /// the tournament has never been published.
    /// </summary>
    DateTimeOffset? LastPublishedAt = null)
{
    /// <summary>
    /// Human-readable one-line location summary built from the
    /// address parts (City, State, Country — Country omitted for USA
    /// to keep the header tight). Empty when no address is set.
    /// </summary>
    public string LocationSummary
    {
        get
        {
            var parts = new List<string>(3);
            if (!string.IsNullOrWhiteSpace(EventCity))  parts.Add(EventCity!);
            if (!string.IsNullOrWhiteSpace(EventState)) parts.Add(EventState!);
            if (!string.IsNullOrWhiteSpace(EventCountry)
                && !string.Equals(EventCountry.Trim(), "USA", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(EventCountry!);
            }
            return string.Join(", ", parts);
        }
    }
}
