using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Enums;

namespace FreePair.Core.Tests.Tournaments;

/// <summary>
/// Tests the static derivation helpers used by the USCF export
/// dialog's pre-fill cascade — Time-control-type → rating-system
/// letter, and Rating-type → FIDE flag. Keeps these mappings
/// authoritative so a v12 SwissSys / NAChessHub enum revision
/// flushes regression here first.
/// </summary>
public class UscfReportPrefsDerivationTests
{
    [Theory]
    [InlineData(TimeControlType.Bullet,            'B')]
    [InlineData(TimeControlType.Blitz,             'B')]
    [InlineData(TimeControlType.Rapid,             'Q')]
    [InlineData(TimeControlType.Classical,         'R')]
    [InlineData(TimeControlType.RapidAndClassical, 'D')]
    public void RatingSystemFromTimeControl_maps_known_values(TimeControlType tct, char expected)
    {
        Assert.Equal(expected, UscfReportPrefs.RatingSystemFromTimeControl(tct));
    }

    [Fact]
    public void RatingSystemFromTimeControl_returns_null_for_other_or_missing()
    {
        Assert.Null(UscfReportPrefs.RatingSystemFromTimeControl(TimeControlType.Other));
        Assert.Null(UscfReportPrefs.RatingSystemFromTimeControl(null));
    }

    [Theory]
    [InlineData(RatingType.FIDE,             true)]
    [InlineData(RatingType.USCF_FIDE,        true)]
    [InlineData(RatingType.CFC_FIDE,         true)]
    [InlineData(RatingType.CFC_USCF_FIDE,    true)]
    [InlineData(RatingType.USCF_FIDE_NW,     true)]
    [InlineData(RatingType.CFC_FIDE_NW,      true)]
    [InlineData(RatingType.USCF_CFC_FIDE_NW, true)]
    [InlineData(RatingType.USCF,             false)]
    [InlineData(RatingType.CFC,              false)]
    [InlineData(RatingType.UnRated,          false)]
    [InlineData(RatingType.CHESSCOM,         false)]
    [InlineData(RatingType.LICHESS,          false)]
    [InlineData(RatingType.USCF_NW,          false)]
    [InlineData(RatingType.Other,            false)]
    public void IsFideRated_picks_up_every_FIDE_bearing_enum_value(RatingType rt, bool expected)
    {
        Assert.Equal(expected, UscfReportPrefs.IsFideRated(rt));
    }

    [Fact]
    public void IsFideRated_is_false_for_null()
    {
        Assert.False(UscfReportPrefs.IsFideRated(null));
    }
}
