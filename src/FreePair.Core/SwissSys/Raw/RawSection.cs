using System.Collections.Generic;
using System.Text.Json.Serialization;
using FreePair.Core.Tournaments.Enums;

namespace FreePair.Core.SwissSys.Raw;

/// <summary>
/// Verbatim mirror of one entry in the <c>Sections</c> array of a SwissSys
/// <c>.sjson</c> file.
/// </summary>
public sealed class RawSection
{
    [JsonPropertyName("Section name")]
    public string? SectionName { get; set; }

    [JsonPropertyName("Section title")]
    public string? SectionTitle { get; set; }

    [JsonPropertyName("Type")]
    public int Type { get; set; }

    [JsonPropertyName("Section time control")]
    public string? SectionTimeControl { get; set; }

    [JsonPropertyName("Number of players")]
    public int NumberOfPlayers { get; set; }

    [JsonPropertyName("Number of teams")]
    public int NumberOfTeams { get; set; }

    [JsonPropertyName("Rounds paired")]
    public int RoundsPaired { get; set; }

    [JsonPropertyName("Rounds played")]
    public int RoundsPlayed { get; set; }

    [JsonPropertyName("Rating to use")]
    public int RatingToUse { get; set; }

    [JsonPropertyName("Engine")]
    public int Engine { get; set; }

    [JsonPropertyName("Last unrestricted round")]
    public int LastUnrestrictedRound { get; set; }

    [JsonPropertyName("Need search options")]
    public bool NeedSearchOptions { get; set; }

    [JsonPropertyName("First board")]
    public int? FirstBoard { get; set; }

    [JsonPropertyName("Final round")]
    public int FinalRound { get; set; }

    [JsonPropertyName("Coin toss")]
    public int CoinToss { get; set; }

    [JsonPropertyName("Team cut")]
    public int TeamCut { get; set; }

    [JsonPropertyName("Acceleration")]
    public int Acceleration { get; set; }

    [JsonPropertyName("Blitz")]
    public bool Blitz { get; set; }

    [JsonPropertyName("Got logic")]
    public bool GotLogic { get; set; }

    [JsonPropertyName("Pair table items")]
    public int PairTableItems { get; set; }

    [JsonPropertyName("Team pair table items")]
    public int TeamPairTableItems { get; set; }

    [JsonPropertyName("Players")]
    public List<RawPlayer> Players { get; set; } = new();

    [JsonPropertyName("Teams")]
    public List<RawTeam> Teams { get; set; } = new();

    [JsonPropertyName("Prizes")]
    public RawPrizes? Prizes { get; set; }

    // ==== FreePair-specific extensions ====================================
    // SwissSys ignores unknown section keys, so this is harmless to
    // non-FreePair consumers while giving us per-section persistent
    // state.

    /// <summary>
    /// When <c>true</c>, the section is "soft-deleted" — FreePair
    /// blocks mutations against it (results, pairings, intervention
    /// swaps, etc.) and <c>SwissSysResultJsonBuilder</c> excludes it
    /// from the published results JSON. Clearing the flag via
    /// <see cref="FreePair.Core.Tournaments.TournamentMutations.UndeleteSection"/>
    /// restores normal behavior. Persisted as
    /// <c>"FreePair soft deleted"</c>.
    /// </summary>
    [JsonPropertyName("FreePair soft deleted")]
    public bool? FreePairSoftDeleted { get; set; }

    /// <summary>
    /// Per-section pairing engine override. <c>null</c> means
    /// "inherit from the tournament-level setting, which itself falls
    /// back to the rating-type-derived default". Persisted as the
    /// string form of <see cref="PairingEngineKind"/>.
    /// </summary>
    [JsonPropertyName("FreePair pairing engine")]
    [JsonConverter(typeof(JsonStringEnumConverter<PairingEngineKind>))]
    public PairingEngineKind? FreePairPairingEngine { get; set; }
}
