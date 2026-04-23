namespace FreePair.Core.Tournaments.Enums;

/// <summary>
/// Event-level pairing system. Values mirror NAChessHub's
/// <c>PairingRule</c> enum numerically. This is distinct from
/// <see cref="SectionKind"/> — the event holds a default; individual
/// sections retain their own <see cref="Section.Kind"/>.
/// </summary>
public enum PairingRule
{
    Swiss = 0,
    /// <summary>Double Swiss. NAChessHub name <c>DSwiss</c> preserved for JSON round-trip.</summary>
    DSwiss = 1,
    /// <summary>Round-robin. NAChessHub name <c>RR</c> preserved for JSON round-trip.</summary>
    RR = 2,
    Team = 3,
    Arena = 4,
    /// <summary>Double round-robin.</summary>
    DRR = 5,
    Quad = 6,
    Other = 9,
}
