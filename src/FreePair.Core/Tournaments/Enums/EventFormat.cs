namespace FreePair.Core.Tournaments.Enums;

/// <summary>
/// Shape of the event — over-the-board vs. online vs. hybrid.
/// Values match the NAChessHub <c>EventFormat</c> enum numerically
/// so they can interoperate when we eventually sync.
/// </summary>
public enum EventFormat
{
    /// <summary>Over-the-board (physical venue).</summary>
    OTB = 0,
    /// <summary>Online-only event.</summary>
    Online = 1,
    /// <summary>Mixed OTB + online.</summary>
    Hybrid = 2,
    /// <summary>Anything else — free-form venue description.</summary>
    Other = 9,
}
