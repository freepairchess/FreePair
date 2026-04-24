using System.Collections.Generic;

namespace FreePair.Core.Importers;

/// <summary>
/// One imported row ready to be added to a section via
/// <see cref="Tournaments.TournamentMutations.AddPlayer"/>. The
/// importer is responsible for header → field mapping, whitespace
/// trimming, rating parsing. Validation (non-empty name, etc.) is
/// left to the mutation layer so the same rules apply regardless
/// of how the player was created.
/// </summary>
public sealed record PlayerImportDraft(
    string Name,
    int Rating,
    string? UscfId = null,
    int? SecondaryRating = null,
    string? MembershipExpiration = null,
    string? Club = null,
    string? State = null,
    string? Team = null,
    string? Email = null,
    string? Phone = null);

/// <summary>
/// The result of a player-import operation. <see cref="Players"/>
/// holds the successfully parsed rows; <see cref="Warnings"/>
/// collects per-row soft failures (missing name, un-parseable
/// rating, unknown column, etc.) so the UI can surface them in an
/// error banner without aborting the whole import.
/// </summary>
public sealed record PlayerImportResult(
    IReadOnlyList<PlayerImportDraft> Players,
    IReadOnlyList<string> Warnings);
