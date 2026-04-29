using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Enums;

namespace FreePair.Core.Tests.SwissSys;

/// <summary>
/// Tests for the May 2026 SwissSys 11.34 fixture — exercises the
/// fields the USCF export pre-fill cascade reads: Organizer ID
/// (USCFAffiliateID type), Event city/state/zip/country, and the
/// Delegations array (owner + tournament director).
/// </summary>
public class SwissSysMapperDelegationsTests
{
    private static readonly string s_inlineSjson = """
    {
      "Overview": {
        "Program": "SwissSys",
        "Version": 11.34,
        "Tournament title": "Chess A2Z May Open 2026",
        "Tournament time controls": "G/45;d5",
        "Starting date": "2026-05-16",
        "Ending date": "2026-05-16",
        "Organizer ID": "A10034449",
        "Organizer ID Type": "USCFAffiliateID",
        "Organizer Name": "Chess A2z",
        "Event city": "Hillsboro",
        "Event state": "OR",
        "Event zip code": "97124 ",
        "Event country": "USA",
        "Delegations": [
          {
            "Player ID": "16400590",
            "Player Name": "Zhang, Guiying",
            "Email": "lillyzh100@gmail.com",
            "Phone": "9714097621",
            "Delegation Level": "Owner"
          },
          {
            "Player ID": "16400584",
            "Player Name": "Tang, Maolong",
            "Email": "tang.93@gmail.com",
            "Phone": "6268077474",
            "Delegation Level": "TournamentDirector"
          }
        ]
      },
      "Sections": []
    }
    """;

    private static async Task<Tournament> LoadInlineAsync(string json)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fp-mapper-{System.Guid.NewGuid():N}.sjson");
        await System.IO.File.WriteAllTextAsync(path, json);
        try
        {
            var raw = await new SwissSysImporter().ImportAsync(path);
            return SwissSysMapper.Map(raw);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Fact]
    public async Task Maps_organizer_id_and_event_address_from_overview()
    {
        var t = await LoadInlineAsync(s_inlineSjson);

        Assert.Equal("A10034449", t.OrganizerId);
        Assert.Equal(UserIDType.USCFAffiliateID, t.OrganizerIdType);
        Assert.Equal("Chess A2z", t.OrganizerName);
        Assert.Equal("Hillsboro", t.EventCity);
        Assert.Equal("OR", t.EventState);
        // Trailing space in the on-disk value is normalised by the mapper.
        Assert.Equal("97124", t.EventZipCode);
        Assert.Equal("USA", t.EventCountry);
    }

    [Fact]
    public async Task Maps_delegations_with_levels_and_contact_info()
    {
        var t = await LoadInlineAsync(s_inlineSjson);

        Assert.NotNull(t.Delegations);
        Assert.Equal(2, t.Delegations!.Count);

        var owner = t.Delegations[0];
        Assert.Equal("16400590",  owner.PlayerId);
        Assert.Equal("Zhang, Guiying", owner.PlayerName);
        Assert.Equal(DelegationLevel.Owner, owner.Level);
        Assert.Equal("lillyzh100@gmail.com", owner.Email);

        var td = t.Delegations[1];
        Assert.Equal("16400584", td.PlayerId);
        Assert.Equal(DelegationLevel.TournamentDirector, td.Level);
    }

    [Fact]
    public async Task Maps_delegations_to_null_when_array_missing()
    {
        var t = await LoadInlineAsync("""
        {"Overview":{"Program":"SwissSys","Version":11.34,"Tournament title":"Empty","Starting date":"2026-01-01","Ending date":"2026-01-01"},"Sections":[]}
        """);

        Assert.Null(t.Delegations);
    }

    [Fact]
    public async Task Roundtrips_uscf_report_prefs_through_overview()
    {
        var t = await LoadInlineAsync("""
        {
          "Overview": {
            "Program": "SwissSys", "Version": 11.34,
            "Tournament title": "Prefs Test",
            "Starting date": "2026-01-01", "Ending date": "2026-01-01",
            "FreePair USCF affiliate ID":  "A1234567",
            "FreePair USCF chief TD ID":   "16000001",
            "FreePair USCF assistant TD ID":"16000002",
            "FreePair USCF rating system": "Q",
            "FreePair USCF send crosstable": true,
            "FreePair USCF grand prix":    false,
            "FreePair USCF FIDE rated":    true,
            "FreePair USCF include section dates": true
          },
          "Sections": []
        }
        """);

        Assert.NotNull(t.UscfReportPrefs);
        var p = t.UscfReportPrefs!;
        Assert.Equal("A1234567", p.AffiliateId);
        Assert.Equal("16000001", p.ChiefTdId);
        Assert.Equal("16000002", p.AssistantTdId);
        Assert.Equal('Q', p.RatingSystem);
        Assert.True(p.SendCrossTable);
        Assert.False(p.GrandPrix);
        Assert.True(p.FideRated);
        Assert.True(p.IncludeSectionDates);
    }
}
