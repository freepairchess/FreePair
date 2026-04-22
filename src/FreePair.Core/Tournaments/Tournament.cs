using System;
using System.Collections.Generic;

namespace FreePair.Core.Tournaments;

/// <summary>
/// Root of the FreePair domain model for a loaded tournament.
/// </summary>
public sealed record Tournament(
    string? Title,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? TimeControl,
    string? NachEventId,
    IReadOnlyList<Section> Sections);
