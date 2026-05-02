namespace FreePair.Core.Tournaments.Enums;

/// <summary>
/// Which pairing engine FreePair uses to generate the next round for an
/// event or section.
/// </summary>
/// <remarks>
/// <para>FreePair ships with two engines side-by-side; neither replaces
/// the other. The right choice depends on which rating federation is
/// going to publish the event:</para>
/// <list type="bullet">
///   <item><see cref="Bbp"/> drives <c>bbpPairings.exe</c>, the FIDE
///         Dutch system implementation. Use for FIDE-rated events (or
///         dual-rated events that include FIDE).</item>
///   <item><see cref="Uscf"/> drives FreePair's home-grown USCF Swiss
///         engine (<c>FreePair.UscfEngine.exe</c>) — see
///         <c>docs/USCF_ENGINE.md</c>. Use for purely USCF-rated events
///         (and other non-FIDE federations that follow USCF-style
///         Swiss rules).</item>
/// </list>
/// <para>Both engines speak identical TRF-in / BBP-pairings-out formats,
/// so the dispatch is a pure binary-path swap — the rest of FreePair's
/// pairing pipeline doesn't need to know which engine ran.</para>
/// </remarks>
public enum PairingEngineKind
{
    /// <summary>
    /// FIDE Dutch via bbpPairings (the long-time default, bundled with
    /// every FreePair release). Best for FIDE-rated events.
    /// </summary>
    Bbp = 0,

    /// <summary>
    /// FreePair's USCF Swiss engine (<c>FreePair.UscfEngine.exe</c>).
    /// Best for purely USCF-rated events.
    /// </summary>
    Uscf = 1,
}
