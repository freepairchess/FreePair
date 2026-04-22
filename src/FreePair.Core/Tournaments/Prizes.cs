using System.Collections.Generic;

namespace FreePair.Core.Tournaments;

/// <summary>
/// A prize amount (place or class) configured for a section.
/// </summary>
public sealed record Prize(decimal Value, string? Description);

/// <summary>
/// The set of prizes configured for a section.
/// </summary>
public sealed record Prizes(
    IReadOnlyList<Prize> Place,
    IReadOnlyList<Prize> Class)
{
    /// <summary>Shared empty instance for sections that declare no prizes.</summary>
    public static Prizes Empty { get; } = new(
        System.Array.Empty<Prize>(),
        System.Array.Empty<Prize>());
}
