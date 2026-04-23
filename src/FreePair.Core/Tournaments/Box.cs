namespace FreePair.Core.Tournaments;

/// <summary>
/// Tiny "optional" wrapper used by <see cref="TournamentMutations.SetTournamentInfo"/>
/// (and similar batched mutations) to distinguish between
/// "leave this field unchanged" (parameter omitted ⇒ <c>null</c>)
/// and "set this field to <see langword="null"/>" (caller passes a
/// non-null <see cref="Box{T}"/> whose <see cref="Value"/> is null).
/// </summary>
/// <remarks>
/// Needed for nullable value types and enum-typed fields where you
/// can't rely on a null sentinel, because null is itself a legal
/// target value. A <see cref="Box{T}"/> instance is cheap (reference
/// equality) and self-documenting at call sites.
/// </remarks>
public sealed record Box<T>(T? Value);
