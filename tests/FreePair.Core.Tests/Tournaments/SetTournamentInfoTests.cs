using System;
using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;

namespace FreePair.Core.Tests.Tournaments;

public class SetTournamentInfoTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(SampleFileName));
        return SwissSysMapper.Map(raw);
    }

    [Fact]
    public async Task SetTournamentInfo_updates_supplied_fields_only()
    {
        var t = await LoadAsync();

        var updated = TournamentMutations.SetTournamentInfo(t,
            title: "Renamed",
            location: "Palo Alto, CA",
            timeControl: "G/30;d5",
            defaultRatingType: "FIDE");

        Assert.Equal("Renamed",       updated.Title);
        Assert.Equal("Palo Alto, CA", updated.Location);
        Assert.Equal("G/30;d5",       updated.TimeControl);
        Assert.Equal("FIDE",          updated.DefaultRatingType);

        // Unspecified fields untouched.
        Assert.Equal(t.StartDate,        updated.StartDate);
        Assert.Equal(t.EndDate,          updated.EndDate);
        Assert.Equal(t.NachEventId,      updated.NachEventId);
        Assert.Equal(t.Sections.Count,   updated.Sections.Count);
        // DefaultPairingKind defaulted (SectionKind.Unknown sentinel) → unchanged.
        Assert.Equal(t.DefaultPairingKind, updated.DefaultPairingKind);
    }

    [Fact]
    public async Task SetTournamentInfo_writes_default_pairing_kind_when_specified()
    {
        var t = await LoadAsync();
        var updated = TournamentMutations.SetTournamentInfo(t,
            defaultPairingKind: SectionKind.RoundRobin);

        Assert.Equal(SectionKind.RoundRobin, updated.DefaultPairingKind);
        Assert.Equal(t.Title, updated.Title); // everything else unchanged
    }

    [Fact]
    public async Task SetTournamentInfo_overwrites_dates()
    {
        var t = await LoadAsync();
        var newStart = new DateOnly(2026, 5, 1);
        var newEnd   = new DateOnly(2026, 5, 2);
        var updated = TournamentMutations.SetTournamentInfo(t,
            startDate: newStart, endDate: newEnd);

        Assert.Equal(newStart, updated.StartDate);
        Assert.Equal(newEnd,   updated.EndDate);
    }

    [Fact]
    public void Tournament_defaults_new_metadata_fields_when_not_supplied()
    {
        // Legacy-shaped construction (positional) leaves the new
        // optional fields at their defaults.
        var t = new Tournament(
            Title: "X", StartDate: null, EndDate: null,
            TimeControl: null, NachEventId: null,
            Sections: Array.Empty<Section>());

        Assert.Null(t.Location);
        Assert.Null(t.DefaultRatingType);
        Assert.Equal(SectionKind.Swiss, t.DefaultPairingKind);
    }
}
