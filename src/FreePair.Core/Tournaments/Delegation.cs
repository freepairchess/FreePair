namespace FreePair.Core.Tournaments;

/// <summary>
/// Role a delegation entry confers on its holder. Mirrors the
/// SwissSys <c>"Delegation Level"</c> string values verbatim so
/// the JSON enum converter round-trips them.
/// </summary>
public enum DelegationLevel
{
    /// <summary>Catch-all for unknown / unmapped levels.</summary>
    Other = 0,

    /// <summary>Tournament owner / organiser.</summary>
    Owner,

    /// <summary>Tournament director (chief or assistant — order in the array decides).</summary>
    TournamentDirector,
}

/// <summary>
/// One person delegated rights on a tournament — typically the
/// owner and one or more TDs. Sourced from the SwissSys Overview's
/// <c>"Delegations"</c> array. Used to pre-fill the USCF export
/// dialog's CTD / ATD fields without forcing the TD to type IDs
/// they've already entered into the registration site.
/// </summary>
/// <param name="PlayerId">USCF (or other) member id of the delegate.</param>
/// <param name="PlayerName">Display name "Last, First".</param>
/// <param name="Email">Optional contact email.</param>
/// <param name="Phone">Optional contact phone.</param>
/// <param name="Level">Role conferred — Owner / TournamentDirector / Other.</param>
public sealed record Delegation(
    string PlayerId,
    string PlayerName,
    string? Email = null,
    string? Phone = null,
    DelegationLevel Level = DelegationLevel.Other);
