namespace FreePair.Core.Tournaments.Enums;

/// <summary>
/// Classification of an event — open / scholastic / invitational /
/// instructional etc. Values mirror NAChessHub's <c>EventType</c>
/// enum numerically so both systems can interoperate.
/// </summary>
public enum EventType
{
    Open = 0,
    Closed = 1,
    Scholastic = 2,
    /// <summary>Invitational. Name preserved from NAChessHub for JSON round-trip.</summary>
    Invit = 3,
    Lecture = 11,
    /// <summary>Simultaneous exhibition.</summary>
    Simul = 12,
    Camp = 13,
    GroupLesson = 14,
    League = 15,
    Other = 99,
}
