using System.Linq;
using System.Threading.Tasks;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Tiebreaks;

namespace FreePair.Core.Tests.SwissSys;

/// <summary>
/// Validates the tiebreak engine against oracle values emitted by SwissSys
/// into the companion <c>.json</c> export for the same tournament. Every
/// player in every played section is checked for all four USCF tiebreaks.
/// </summary>
public class TiebreakCalculatorTests
{
    private const string SampleFileName = "Chess_A2Z_April_Open_2026_SwissSys11.sjson";

    private static async Task<Tournament> LoadAsync()
    {
        var importer = new SwissSysImporter();
        var raw = await importer.ImportAsync(TestPaths.SwissSysSample(SampleFileName));
        return SwissSysMapper.Map(raw);
    }

    // Oracle format: { name, ModMed, Solkoff, Cumul, OpCumul } captured from
    // the SwissSys .json export for this tournament.

    public static readonly TheoryData<string, string, decimal, decimal, decimal, decimal>
        OpenIOracle = new()
        {
            { "Open I", "Maokhampio, Lucas",       3.5m, 4.5m, 6m,   10m   },
            { "Open I", "Pedram, Mehrdad",         3.5m, 4.5m, 4m,   11m   },
            { "Open I", "Badri, Sriram",           2m,   6m,   4m,   11.5m },
            { "Open I", "Shukla, Weg",             3m,   6m,   3m,   10.5m },
            { "Open I", "Liu, Merrick",            5m,   6m,   5m,   11m   },
            { "Open I", "Li, Bohan Jaden",         1.5m, 4m,   4m,   8m    },
            { "Open I", "Saripalli, Prahlada",     1.5m, 4m,   3m,   8.5m  },
            { "Open I", "Rahul, Rihaan",           3.5m, 4.5m, 3m,   10m   },
            { "Open I", "Randall, Dylan",          1m,   5m,   2m,   9.5m  },
            { "Open I", "Mallapu, Mokshith Kumar", 2.5m, 4.5m, 2m,   7m    },
            { "Open I", "Kesavan, Suhaan",         1.5m, 3m,   2.5m, 7m    },
            { "Open I", "Ho, William",             2m,   3.5m, 1.5m, 7m    },
            { "Open I", "Maokhampio, Olivia",      1m,   3m,   1m,   7m    },
            { "Open I", "Balasubramanian, Prakrithi", 2.5m, 4m, 0m,  7.5m  },
            { "Open I", "Ranjit, Nimay",           2.5m, 4.5m, 2m,   7.5m  },
            { "Open I", "Chappell, Robert Isaac",  3.5m, 5m,   5m,   11m   },
        };

    public static readonly TheoryData<string, string, decimal, decimal, decimal, decimal>
        OpenIIOracle = new()
        {
            { "Open II", "Cheng, Anthony S",           3.5m, 4.5m, 6m,   10.5m },
            { "Open II", "Prasanna Kumar, Ramswarup",  5m,   5.5m, 5m,   10m   },
            { "Open II", "Rahul, Vihaan",              2m,   4.5m, 1m,   9.5m  },
            { "Open II", "Adzhigirey, Philip",         3.5m, 4.5m, 4.5m, 7.5m  },
            { "Open II", "Daas, Aritra",               2m,   3.5m, 0m,   6.5m  },
            { "Open II", "Balasubramanian, Pranav",    2.5m, 5m,   2m,   8.5m  },
            { "Open II", "Daas, Adrito",               1.5m, 5m,   2.5m, 9.5m  },
            { "Open II", "Sathaye, Anay",              3m,   6m,   3m,   10.5m },
            { "Open II", "Dasari, Vasishta Sai",       1m,   3.5m, 1.5m, 7m    },
            { "Open II", "Pham, Liam",                 2m,   5m,   2m,   10m   },
            { "Open II", "Xie, Ryan",                  1m,   3m,   0m,   7m    },
            { "Open II", "Chen, Stephen",              4m,   5m,   5m,   9.5m  },
            { "Open II", "Teoh, Lucas",                3.5m, 4.5m, 4.5m, 9m    },
            { "Open II", "Chien, Evan",                1.5m, 4.5m, 3.5m, 8.5m  },
            { "Open II", "Hu, Tim",                    3m,   4m,   4m,   9m    },
            { "Open II", "Yedupati, Srihan",           3.5m, 4.5m, 6m,   10.5m },
            { "Open II", "Xie, Vincent",               1m,   3m,   2.5m, 6.5m  },
            { "Open II", "Kadali, Amartya Benegal",    1m,   2.5m, 0m,   5.5m  },
        };

    public static readonly TheoryData<string, string, decimal, decimal, decimal, decimal>
        Under1000Oracle = new()
        {
            { "Under_1000", "Saju, Angel Leann",        9.5m, 11.5m, 9.5m, 29.5m },
            { "Under_1000", "Maokhampio, Corina",       5m,   8.5m,  3m,   23.5m },
            { "Under_1000", "Isaacs, Rahul",            4m,   7m,    4m,   18m   },
            { "Under_1000", "Mishra, Sniksha",          7.5m, 9.5m,  8m,   24.5m },
            { "Under_1000", "Saju, Kester Anton",       7.5m, 8.5m,  9.5m, 20.5m },
            { "Under_1000", "Kanuparthi, Anirudh",      4m,   6m,    0m,   13m   },
            { "Under_1000", "Zhang, Jason",             7.5m, 8.5m,  7m,   22.5m },
            { "Under_1000", "Adzhigirey, Michael",      5m,   9.5m,  5m,   23.5m },
            { "Under_1000", "Javali, Suhas",            5m,   9m,    6m,   22m   },
            { "Under_1000", "Zalavadiya, Om",           5m,   9.5m,  5m,   22.5m },
            { "Under_1000", "Ramesh, Abhiram",          3m,   6m,    4m,   16m   },
            { "Under_1000", "Annamareddy, Akshara",     3m,   6.5m,  3m,   17.5m },
            { "Under_1000", "Yadlapalli, Vihaan",       3m,   5m,    4m,   10m   },
            { "Under_1000", "Dokania, Ishaan",          4m,   7m,    2m,   17m   },
        };

    public static readonly TheoryData<string, string, decimal, decimal, decimal, decimal>
        Under700Oracle = new()
        {
            { "Under_700", "Waldemer, Isaac",           8m,    9m,    10m,  26m   },
            { "Under_700", "Cattone, Jocelyn",          5.5m,  6.5m,  7m,   21m   },
            { "Under_700", "Farris, Oliad",             4m,    7m,    2m,   18m   },
            { "Under_700", "Hegde, Aarna",              4.5m,  5.5m,  6m,   15m   },
            { "Under_700", "Zalavadiya, Riya",          2.5m,  5.5m,  3m,   13m   },
            { "Under_700", "Hegde, Aarav",              7.5m,  8.5m,  9m,   22m   },
            { "Under_700", "Bahuguna, Akshaj",          5.5m, 11m,    7m,   27m   },
            { "Under_700", "Babian, Logan",             6.5m,  7m,    7m,   16.5m },
            { "Under_700", "Yadlapalli, Riyansh",       5m,    9m,    3m,   18.5m },
            { "Under_700", "Yeh, Evangeline",           5m,    8m,    3m,   16m   },
            { "Under_700", "Khandwe, Ansh",             6m,   10m,    7m,   24m   },
            { "Under_700", "Mannam, Yoganand",          6m,    9m,    4m,   22m   },
            { "Under_700", "Chou, Leo",                 6.5m,  9.5m,  4.5m, 22.5m },
            { "Under_700", "Thompson, Olivia",          5m,    8m,    1m,   20m   },
            { "Under_700", "Shekhar, Stavya",           5.5m,  6.5m,  5.5m, 17.5m },
            { "Under_700", "Saripalli, Meenakshi",      3m,    6m,    0m,   13m   },
        };

    [Theory]
    [MemberData(nameof(OpenIOracle))]
    [MemberData(nameof(OpenIIOracle))]
    [MemberData(nameof(Under1000Oracle))]
    [MemberData(nameof(Under700Oracle))]
    public async Task Tiebreaks_match_SwissSys_oracle(
        string sectionName,
        string playerName,
        decimal expectedModMed,
        decimal expectedSolkoff,
        decimal expectedCumul,
        decimal expectedOpCumul)
    {
        var t = await LoadAsync();
        var section = t.Sections.Single(s => s.Name == sectionName);
        var player = section.Players.Single(p => p.Name == playerName);

        var tb = TiebreakCalculator.ComputeFor(section, player);

        Assert.Equal(expectedModMed,   tb.ModifiedMedian);
        Assert.Equal(expectedSolkoff,  tb.Solkoff);
        Assert.Equal(expectedCumul,    tb.Cumulative);
        Assert.Equal(expectedOpCumul,  tb.OpponentCumulative);
    }

    [Fact]
    public async Task Compute_returns_values_keyed_by_pair_number()
    {
        var t = await LoadAsync();
        var openI = t.Sections.Single(s => s.Name == "Open I");

        var all = TiebreakCalculator.Compute(openI);

        Assert.Equal(openI.Players.Count, all.Count);
        Assert.All(openI.Players, p => Assert.True(all.ContainsKey(p.PairNumber)));

        // Cross-check: Pair 1 in Open I.
        var tb = all[1];
        Assert.Equal(3.5m, tb.ModifiedMedian);
        Assert.Equal(4.5m, tb.Solkoff);
    }

    [Fact]
    public async Task Empty_section_yields_zero_tiebreaks()
    {
        var t = await LoadAsync();
        var u400 = t.Sections.Single(s => s.Name == "Under_400");

        Assert.Empty(u400.Players);
        Assert.Empty(TiebreakCalculator.Compute(u400));
    }

    [Fact]
    public void Indexer_returns_requested_system_value()
    {
        var tb = new TiebreakValues(1m, 2m, 3m, 4m);

        Assert.Equal(1m, tb[TiebreakSystem.ModifiedMedian]);
        Assert.Equal(2m, tb[TiebreakSystem.Solkoff]);
        Assert.Equal(3m, tb[TiebreakSystem.Cumulative]);
        Assert.Equal(4m, tb[TiebreakSystem.OpponentCumulative]);
    }
}
