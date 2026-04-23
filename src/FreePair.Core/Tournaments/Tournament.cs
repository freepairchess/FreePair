using System;
using System.Collections.Generic;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Root of the FreePair domain model for a loaded tournament.
/// </summary>
/// <param name="Location">
/// Free-form venue / city string (session-only for v1 — not yet
/// round-tripped through SwissSys save/load to avoid inventing
/// extra JSON fields). Editable via
/// <see cref="TournamentMutations.SetTournamentInfo"/>.
/// </param>
/// <param name="DefaultPairingKind">
/// Event-wide default pairing system. New sections the TD creates
/// inherit this value; existing sections retain whatever they
/// imported with. Inheritance propagation onto existing sections is
/// a later feature — for now this is just a recorded default.
/// </param>
/// <param name="DefaultRatingType">
/// Event-wide default rating federation ("USCF" / "FIDE" / "CFC" /
/// "NWSRS" / free-form). Same session-only / default-only semantics
/// as <see cref="DefaultPairingKind"/>.
/// </param>
public sealed record Tournament(
    string? Title,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? TimeControl,
    string? NachEventId,
    IReadOnlyList<Section> Sections,
    string? Location = null,
    SectionKind DefaultPairingKind = SectionKind.Swiss,
    string? DefaultRatingType = null);
