using FreePair.Core.Tournaments.Enums;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Helpers that turn a tournament's rating-type (and the
/// per-tournament / per-section overrides) into the concrete
/// <see cref="PairingEngineKind"/> FreePair should use to pair the
/// next round.
/// </summary>
/// <remarks>
/// <para><b>Default-derivation rule.</b> If the tournament's
/// <see cref="Tournament.RatingType"/> includes FIDE (any of the
/// FIDE / USCF_FIDE / CFC_FIDE / *_FIDE_NW combos), the default is
/// <see cref="PairingEngineKind.Bbp"/> — FIDE events should be paired
/// by the FIDE Dutch implementation. Otherwise the default is
/// <see cref="PairingEngineKind.Uscf"/>: USCF, CFC, online-only, and
/// unrated events all use FreePair's USCF Swiss engine.</para>
///
/// <para><b>Override cascade for a section's effective engine</b>
/// (highest precedence first):</para>
/// <list type="number">
///   <item><see cref="Section.PairingEngine"/> when set —
///         the TD pinned the section explicitly.</item>
///   <item><see cref="Tournament.PairingEngine"/> when set —
///         the TD pinned the whole event.</item>
///   <item><see cref="ForRatingType"/> applied to
///         <see cref="Tournament.RatingType"/> — derived default.</item>
/// </list>
/// </remarks>
public static class PairingEngineDefaults
{
    /// <summary>
    /// Default engine for a tournament with the given rating type.
    /// FIDE-inclusive types → <see cref="PairingEngineKind.Bbp"/>;
    /// everything else → <see cref="PairingEngineKind.Uscf"/>.
    /// </summary>
    public static PairingEngineKind ForRatingType(RatingType? ratingType) =>
        IsFideRated(ratingType) ? PairingEngineKind.Bbp : PairingEngineKind.Uscf;

    /// <summary>
    /// True when <paramref name="ratingType"/> includes FIDE in any
    /// combinatorial form. Used by <see cref="ForRatingType"/> and by
    /// UI logic that wants to flag "this event is FIDE-rated".
    /// </summary>
    public static bool IsFideRated(RatingType? ratingType) => ratingType switch
    {
        RatingType.FIDE                => true,
        RatingType.USCF_FIDE           => true,
        RatingType.CFC_FIDE            => true,
        RatingType.CFC_USCF_FIDE       => true,
        RatingType.USCF_FIDE_NW        => true,
        RatingType.CFC_FIDE_NW         => true,
        RatingType.USCF_CFC_FIDE_NW    => true,
        _                              => false,
    };

    /// <summary>
    /// Resolves the effective pairing engine for a section using the
    /// override cascade documented on <see cref="PairingEngineDefaults"/>.
    /// </summary>
    public static PairingEngineKind Resolve(Tournament tournament, Section section)
    {
        System.ArgumentNullException.ThrowIfNull(tournament);
        System.ArgumentNullException.ThrowIfNull(section);
        return section.PairingEngine
            ?? tournament.PairingEngine
            ?? ForRatingType(tournament.RatingType);
    }
}
