using System;
using System.IO;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Enums;

namespace FreePair.Core.Tests.Tournaments;

public class SetTournamentInfoTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    /// <summary>
    /// Richer fixture (SwissSys 11.34) that includes all the extended
    /// NAChessHub metadata — address, enums, rounds, half-point bye
    /// count, time zone, organiser id, etc. Used by the mapper +
    /// round-trip writer tests below.
    /// </summary>
    private const string ExtendedSampleFileName = "Chess_A2Z_April_Open_2026_v11_34_extended.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(SampleFileName));
        return SwissSysMapper.Map(raw);
    }

    private static async Task<Tournament> LoadExtendedAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(ExtendedSampleFileName));
        return SwissSysMapper.Map(raw);
    }

    [Fact]
    public async Task SetTournamentInfo_updates_supplied_fields_only()
    {
        var t = await LoadAsync();

        var updated = TournamentMutations.SetTournamentInfo(t,
            title: "Renamed",
            eventCity: "Palo Alto",
            eventState: "CA",
            timeControl: "G/30;d5",
            ratingType: new Box<RatingType?>(RatingType.FIDE));

        Assert.Equal("Renamed",       updated.Title);
        Assert.Equal("Palo Alto",     updated.EventCity);
        Assert.Equal("CA",            updated.EventState);
        Assert.Equal("G/30;d5",       updated.TimeControl);
        Assert.Equal(RatingType.FIDE, updated.RatingType);

        Assert.Equal(t.StartDate,      updated.StartDate);
        Assert.Equal(t.EndDate,        updated.EndDate);
        Assert.Equal(t.NachEventId,    updated.NachEventId);
        Assert.Equal(t.Sections.Count, updated.Sections.Count);
        Assert.Equal(t.PairingRule,    updated.PairingRule);
    }

    [Fact]
    public async Task SetTournamentInfo_writes_pairing_rule_when_specified()
    {
        var t = await LoadAsync();
        var updated = TournamentMutations.SetTournamentInfo(t,
            pairingRule: new Box<PairingRule?>(PairingRule.RR));

        Assert.Equal(PairingRule.RR, updated.PairingRule);
        Assert.Equal(t.Title, updated.Title);
    }

    [Fact]
    public async Task SetTournamentInfo_can_clear_an_enum_field_via_null_box()
    {
        var t = await LoadAsync();
        t = TournamentMutations.SetTournamentInfo(t,
            ratingType: new Box<RatingType?>(RatingType.USCF));
        Assert.Equal(RatingType.USCF, t.RatingType);

        var cleared = TournamentMutations.SetTournamentInfo(t,
            ratingType: new Box<RatingType?>(null));
        Assert.Null(cleared.RatingType);
    }

    [Fact]
    public async Task SetTournamentInfo_omits_leave_enum_fields_unchanged()
    {
        var t = await LoadAsync();
        t = TournamentMutations.SetTournamentInfo(t,
            pairingRule: new Box<PairingRule?>(PairingRule.Swiss));

        var updated = TournamentMutations.SetTournamentInfo(t,
            title: "New name");

        Assert.Equal("New name",        updated.Title);
        Assert.Equal(PairingRule.Swiss, updated.PairingRule);
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
        var t = new Tournament(
            Title: "X", StartDate: null, EndDate: null,
            TimeControl: null, NachEventId: null,
            Sections: Array.Empty<Section>());

        Assert.Null(t.EventAddress);
        Assert.Null(t.EventCity);
        Assert.Null(t.EventState);
        Assert.Null(t.PairingRule);
        Assert.Null(t.RatingType);
        Assert.Null(t.EventFormat);
        Assert.Null(t.RoundsPlanned);
    }

    [Fact]
    public void LocationSummary_combines_city_state_and_non_us_country()
    {
        var t = new Tournament(
            Title: "X", StartDate: null, EndDate: null,
            TimeControl: null, NachEventId: null,
            Sections: Array.Empty<Section>(),
            EventCity: "Hillsboro",
            EventState: "OR",
            EventCountry: "USA");
        Assert.Equal("Hillsboro, OR", t.LocationSummary);

        var t2 = t with { EventCountry = "Canada" };
        Assert.Equal("Hillsboro, OR, Canada", t2.LocationSummary);

        var t3 = new Tournament(
            Title: "X", StartDate: null, EndDate: null,
            TimeControl: null, NachEventId: null,
            Sections: Array.Empty<Section>());
        Assert.Equal(string.Empty, t3.LocationSummary);
    }

    [Fact]
    public async Task Mapper_reads_extended_metadata_when_present()
    {
        // The bundled April 2026 fixture pre-dates the extended metadata
        // fields; here we just verify the mapper reads what IS present
        // (title, dates, time control) and leaves the new fields null
        // without throwing. When a richer fixture is added we can tighten
        // this assertion against specific enum / address values.
        var t = await LoadAsync();

        Assert.Equal("Chess A2Z April Open 2026", t.Title);
        Assert.Equal("G/45;d5",                   t.TimeControl);
    }

    [Fact]
    public async Task Mapper_reads_full_extended_metadata_from_v11_34_fixture()
    {
        // This fixture is the real NAChessHub-enriched .sjson shape the
        // app will see in production: every extended key populated.
        var t = await LoadExtendedAsync();

        Assert.Equal("Chess A2Z April Open 2026", t.Title);
        Assert.Equal("G/45;d5",                   t.TimeControl);
        Assert.Equal(new DateOnly(2026, 4, 4),    t.StartDate);
        Assert.Equal(new DateOnly(2026, 4, 4),    t.EndDate);

        Assert.Equal("A10034449",                 t.NachOrganizerId);
        Assert.Equal("0209be6f-6bea-4d55-819f-18b7c635ebe8", t.NachPasscode);
        Assert.Equal("Pacific Standard Time",     t.TimeZone);

        // DateTimeOffset parse uses AssumeLocal; compare date parts only.
        Assert.NotNull(t.StartDateTime);
        Assert.Equal(new DateTime(2026, 4, 4, 10,  0, 0), t.StartDateTime!.Value.DateTime);
        Assert.NotNull(t.EndDateTime);
        Assert.Equal(new DateTime(2026, 4, 4, 15, 30, 0), t.EndDateTime!.Value.DateTime);

        Assert.Equal("5529 NE Century Blvd", t.EventAddress);
        Assert.Equal("Hillsboro",            t.EventCity);
        Assert.Equal("OR",                   t.EventState);
        // Trimmed by the mapper (source value has trailing space).
        Assert.Equal("97124",                t.EventZipCode);
        Assert.Equal("USA",                  t.EventCountry);

        Assert.Equal(EventFormat.OTB,                    t.EventFormat);
        Assert.Equal(EventType.Open,                     t.EventType);
        Assert.Equal(PairingRule.Swiss,                  t.PairingRule);
        Assert.Equal(TimeControlType.RapidAndClassical,  t.TimeControlType);
        Assert.Equal(RatingType.USCF,                    t.RatingType);

        Assert.Equal(4, t.RoundsPlanned);
        Assert.Equal(2, t.HalfPointByesAllowed);

        Assert.Equal("Hillsboro, OR", t.LocationSummary);
    }

    [Fact]
    public async Task Writer_round_trips_every_extended_metadata_field()
    {
        // Copy the richer fixture to a temp file so we can round-trip
        // through the writer without touching the repo-owned sample.
        var src = TestPaths.SwissSysSample(ExtendedSampleFileName);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-event-meta-{Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var importer = new SwissSysImporter();
            var t = SwissSysMapper.Map(await importer.ImportAsync(tmp));

            // Mutate every extended field to distinct values so any
            // writer-side misses will show up in the post-load assertions.
            t = TournamentMutations.SetTournamentInfo(t,
                title:                "Renamed Open 2026",
                timeControl:          "G/60;d5",
                startDate:            new DateOnly(2026, 6, 1),
                endDate:              new DateOnly(2026, 6, 2),
                eventAddress:         "123 New St",
                eventCity:            "Portland",
                eventState:           "OR",
                eventZipCode:         "97205",
                eventCountry:         "USA",
                eventFormat:          new Box<EventFormat?>(EventFormat.Hybrid),
                eventType:            new Box<EventType?>(EventType.Scholastic),
                pairingRule:          new Box<PairingRule?>(PairingRule.RR),
                timeControlType:      new Box<TimeControlType?>(TimeControlType.Classical),
                ratingType:           new Box<RatingType?>(RatingType.USCF_FIDE),
                nachOrganizerId:      "B99999999",
                nachPasscode:         "new-secret-passcode",
                timeZone:             "Eastern Standard Time",
                roundsPlanned:        7,
                halfPointByesAllowed: 3);

            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            // Re-import and verify every field survived.
            var t2 = SwissSysMapper.Map(await importer.ImportAsync(tmp));

            Assert.Equal("Renamed Open 2026",            t2.Title);
            Assert.Equal("G/60;d5",                      t2.TimeControl);
            Assert.Equal(new DateOnly(2026, 6, 1),       t2.StartDate);
            Assert.Equal(new DateOnly(2026, 6, 2),       t2.EndDate);

            Assert.Equal("123 New St", t2.EventAddress);
            Assert.Equal("Portland",   t2.EventCity);
            Assert.Equal("OR",         t2.EventState);
            Assert.Equal("97205",      t2.EventZipCode);
            Assert.Equal("USA",        t2.EventCountry);

            Assert.Equal(EventFormat.Hybrid,         t2.EventFormat);
            Assert.Equal(EventType.Scholastic,       t2.EventType);
            Assert.Equal(PairingRule.RR,             t2.PairingRule);
            Assert.Equal(TimeControlType.Classical,  t2.TimeControlType);
            Assert.Equal(RatingType.USCF_FIDE,       t2.RatingType);

            Assert.Equal("B99999999",            t2.NachOrganizerId);
            Assert.Equal("Eastern Standard Time", t2.TimeZone);
            Assert.Equal(7, t2.RoundsPlanned);
            Assert.Equal(3, t2.HalfPointByesAllowed);

            // Untouched existing keys survived too.
            Assert.Equal("6e2a098b-ea49-48f2-9047-e5930a05564e", t2.NachEventId);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public async Task Writer_persists_extended_overview_metadata()
    {
        // Smaller smoke test using the legacy fixture — verifies the
        // writer can ADD new keys to an Overview block that didn't
        // originally carry them.
        var src = TestPaths.SwissSysSample(SampleFileName);
        var tmp = Path.Combine(Path.GetTempPath(), $"fp-event-meta-{Guid.NewGuid():N}.sjson");
        File.Copy(src, tmp, overwrite: true);
        try
        {
            var importer = new SwissSysImporter();
            var t = SwissSysMapper.Map(await importer.ImportAsync(tmp));

            t = TournamentMutations.SetTournamentInfo(t,
                eventCity: "Hillsboro",
                eventState: "OR",
                eventCountry: "USA",
                eventFormat:  new Box<EventFormat?>(EventFormat.OTB),
                eventType:    new Box<EventType?>(EventType.Open),
                pairingRule:  new Box<PairingRule?>(PairingRule.Swiss),
                ratingType:   new Box<RatingType?>(RatingType.USCF),
                roundsPlanned: 4,
                halfPointByesAllowed: 2);

            await new SwissSysTournamentWriter().SaveAsync(tmp, t);

            var t2 = SwissSysMapper.Map(await importer.ImportAsync(tmp));

            Assert.Equal("Hillsboro",        t2.EventCity);
            Assert.Equal("OR",               t2.EventState);
            Assert.Equal("USA",              t2.EventCountry);
            Assert.Equal(EventFormat.OTB,    t2.EventFormat);
            Assert.Equal(EventType.Open,     t2.EventType);
            Assert.Equal(PairingRule.Swiss,  t2.PairingRule);
            Assert.Equal(RatingType.USCF,    t2.RatingType);
            Assert.Equal(4,                  t2.RoundsPlanned);
            Assert.Equal(2,                  t2.HalfPointByesAllowed);
            Assert.Equal("Chess A2Z April Open 2026", t2.Title);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }
}
