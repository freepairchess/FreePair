namespace FreePair.Core.Publishing;

/// <summary>
/// Kind of published artefact. Values mirror the NAChessHub
/// <c>FileType</c> enum numerically, and member names match exactly
/// so the wire form (both the query-string int and the
/// <see cref="System.Text.Json.Serialization.JsonStringEnumConverter{T}"/>
/// round-trip) work without a custom mapper.
/// </summary>
/// <remarks>
/// We only need the subset that FreePair produces today. Values that
/// NAChessHub defines but we don't emit (e.g. <c>GamesPGN = 6</c>)
/// are intentionally included so a future caller can publish them
/// without another enum bump.
/// </remarks>
public enum FileType
{
    Flyer = 0,
    Pairing = 1,
    Standing = 2,
    WallChart = 3,
    Prize = 4,
    Regulation = 5,
    GamesPGN = 6,
    GamesCBV = 7,
    SwissSysJSON = 8,
    Other = 9,
    /// <summary>Full SwissSys 11 <c>.sjson</c> tournament file
    /// (pairings + results + overview). FreePair's default publish
    /// payload because it lets the hub re-derive every view.</summary>
    SwissSys11SJson = 10,
}
