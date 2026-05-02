using System.Collections.Generic;
using FreePair.Core.Tournaments.Enums;

namespace FreePair.App.ViewModels;

/// <summary>
/// One row in the pairing-engine combobox shown on the Section header
/// and tournament Event-config form. Combines a nullable
/// <see cref="PairingEngineKind"/> value (where <c>null</c> means
/// "inherit from the parent: section inherits from tournament,
/// tournament inherits from rating-type-derived default") with a
/// human-readable display string.
/// </summary>
public sealed record PairingEngineChoice(PairingEngineKind? Value, string Display)
{
    public override string ToString() => Display;

    /// <summary>
    /// Choices for a section-level combobox: inherit-from-tournament
    /// or pin to a specific engine.
    /// </summary>
    public static readonly IReadOnlyList<PairingEngineChoice> SectionChoices = new[]
    {
        new PairingEngineChoice(null,                  "Inherit from event"),
        new PairingEngineChoice(PairingEngineKind.Bbp,  "BBP (FIDE Dutch)"),
        new PairingEngineChoice(PairingEngineKind.Uscf, "USCF Swiss"),
    };

    /// <summary>
    /// Choices for the tournament-level combobox on the Event-config
    /// form: inherit-from-rating-type or pin to a specific engine.
    /// </summary>
    public static readonly IReadOnlyList<PairingEngineChoice> TournamentChoices = new[]
    {
        new PairingEngineChoice(null,                  "Default for rating type"),
        new PairingEngineChoice(PairingEngineKind.Bbp,  "BBP (FIDE Dutch)"),
        new PairingEngineChoice(PairingEngineKind.Uscf, "USCF Swiss"),
    };

    /// <summary>
    /// Short label for the resolved (effective) engine — surfaced as
    /// a read-only badge next to the combobox so the TD can see what
    /// will actually run after inherit / default cascade.
    /// </summary>
    public static string DisplayFor(PairingEngineKind kind) => kind switch
    {
        PairingEngineKind.Bbp  => "BBP (FIDE Dutch)",
        PairingEngineKind.Uscf => "USCF Swiss",
        _                      => kind.ToString(),
    };
}
