namespace FreePair.Core.Tournaments;

/// <summary>
/// Reason code describing why a pairing decision was made.
/// Multiple reasons can apply to a single pairing (e.g., transposition + color assignment).
/// </summary>
public enum PairingReason
{
    /// <summary>Natural SLIDE pairing — top half paired with bottom half in rating order.</summary>
    NaturalSlide,

    /// <summary>Round-1 seed-based pairing (top half vs bottom half by initial ranking).</summary>
    RoundOneSlide,

    /// <summary>Bottom-half transposition to avoid a rematch (USCF 28L1–L2).</summary>
    TranspositionAvoidRematch,

    /// <summary>Cross-half interchange — a top-half player swapped with a bottom-half player (USCF 28L3).</summary>
    CrossHalfInterchange,

    /// <summary>Color-conflict reduction — transposition reduced same-color-due clashes (USCF 29E).</summary>
    ColorConflictReduction,

    /// <summary>Color-optimized matching — full search for best color assignment in small pool.</summary>
    ColorOptimizedMatching,

    /// <summary>White assigned based on color equalization (player had more blacks).</summary>
    ColorEqualization,

    /// <summary>White assigned based on color alternation (player was black last round).</summary>
    ColorAlternation,

    /// <summary>White assigned by higher rating (when other color factors are equal).</summary>
    ColorByRating,

    /// <summary>White assigned by coin-flip / initial-color rule (first board of round).</summary>
    ColorByInitialRule,

    /// <summary>Player was floated down from a higher score group.</summary>
    FloatedDown,

    /// <summary>Player was dropped as the odd-man to float down (natural SLIDE drop).</summary>
    FloaterDropNatural,

    /// <summary>Player was dropped as the odd-man for better color balance.</summary>
    FloaterDropColorFriendly,

    /// <summary>Full-point bye assigned (odd player in lowest score group).</summary>
    ByeAssigned,

    /// <summary>Paired by FIDE Dutch engine (bbpPairings) — internal decisions opaque.</summary>
    FideEngine,

    /// <summary>Fallback: no non-rematch matching found; accepted least-bad rematch.</summary>
    FallbackRematchAccepted,
}

/// <summary>
/// A single annotation explaining one aspect of why a pairing was made.
/// A board may have multiple annotations (e.g., transposition + color reason).
/// </summary>
/// <param name="Board">Board number this annotation applies to (0 for round-level notes like bye).</param>
/// <param name="Reason">Machine-readable reason code.</param>
/// <param name="Detail">Human-readable explanation with specifics (player names, numbers, etc.).</param>
public sealed record PairingAnnotation(
    int Board,
    PairingReason Reason,
    string Detail);
