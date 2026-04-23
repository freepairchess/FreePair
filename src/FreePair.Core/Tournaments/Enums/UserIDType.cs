namespace FreePair.Core.Tournaments.Enums;

/// <summary>
/// Kind of external identifier stored on a user / organiser. Values
/// mirror NAChessHub's <c>UserIDType</c> enum numerically, and member
/// names match exactly so they round-trip through
/// <see cref="System.Text.Json.Serialization.JsonStringEnumConverter{T}"/>.
/// </summary>
public enum UserIDType
{
    USCFID = 0,
    FIDEID = 1,
    CFCID = 2,
    USCFAffiliateID = 3,
    FIDEOrganizerID = 4,
    CFCOrganizerID = 5,
    Local = 9,
    Other = 10,
}
