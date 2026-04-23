using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Enums;

namespace FreePair.App.ViewModels;

/// <summary>
/// Backs the Event configuration tab: exposes editable copies of the
/// event-level metadata and an <see cref="ApplyCommand"/> that folds
/// the changes back into the parent <see cref="TournamentViewModel"/>
/// via <see cref="TournamentMutations.SetTournamentInfo"/>.
/// </summary>
public sealed partial class EventConfigViewModel : ViewModelBase
{
    private readonly Func<Tournament> _getTournament;
    private readonly Action<Tournament> _setTournament;

    // ============ basics ============
    [ObservableProperty] private string? _title;
    [ObservableProperty] private DateTimeOffset? _startDate;
    [ObservableProperty] private DateTimeOffset? _endDate;
    [ObservableProperty] private string? _timeControl;

    // ============ address ============
    [ObservableProperty] private string? _eventAddress;
    [ObservableProperty] private string? _eventCity;
    [ObservableProperty] private string? _eventState;
    [ObservableProperty] private string? _eventZipCode;
    [ObservableProperty] private string? _eventCountry;

    // ============ classifications (null = Unspecified) ============
    [ObservableProperty] private EventFormat? _eventFormat;
    [ObservableProperty] private EventType? _eventType;
    [ObservableProperty] private PairingRule? _pairingRule;
    [ObservableProperty] private TimeControlType? _timeControlType;
    [ObservableProperty] private RatingType? _ratingType;

    // ============ scheduling / counts ============
    [ObservableProperty] private int? _roundsPlanned;
    [ObservableProperty] private int? _halfPointByesAllowed;
    [ObservableProperty] private string? _nachOrganizerId;
    [ObservableProperty] private string? _nachPasscode;

    /// <summary>
    /// When <c>false</c>, the NACH passcode textbox renders as dots
    /// (UI toggle). Doesn't affect storage — the raw value stays in
    /// <see cref="NachPasscode"/>.
    /// </summary>
    [ObservableProperty] private bool _showNachPasscode;

    [ObservableProperty] private string? _timeZone;

    // ============ UI choice lists ============
    // Note: each enum type is fully-qualified below because the
    // generated observable property of the same name (PairingRule,
    // EventFormat, …) otherwise shadows the enum type in these
    // initializers.

    public PairingRule?[] AvailablePairingRules { get; } = new PairingRule?[]
    {
        null,
        FreePair.Core.Tournaments.Enums.PairingRule.Swiss, FreePair.Core.Tournaments.Enums.PairingRule.DSwiss,
        FreePair.Core.Tournaments.Enums.PairingRule.RR,    FreePair.Core.Tournaments.Enums.PairingRule.DRR,
        FreePair.Core.Tournaments.Enums.PairingRule.Quad,  FreePair.Core.Tournaments.Enums.PairingRule.Team, FreePair.Core.Tournaments.Enums.PairingRule.Arena,
        FreePair.Core.Tournaments.Enums.PairingRule.Other,
    };

    public EventFormat?[] AvailableEventFormats { get; } = new EventFormat?[]
    {
        null, FreePair.Core.Tournaments.Enums.EventFormat.OTB, FreePair.Core.Tournaments.Enums.EventFormat.Online, FreePair.Core.Tournaments.Enums.EventFormat.Hybrid, FreePair.Core.Tournaments.Enums.EventFormat.Other,
    };

    public EventType?[] AvailableEventTypes { get; } = new EventType?[]
    {
        null,
        FreePair.Core.Tournaments.Enums.EventType.Open, FreePair.Core.Tournaments.Enums.EventType.Closed, FreePair.Core.Tournaments.Enums.EventType.Scholastic, FreePair.Core.Tournaments.Enums.EventType.Invit,
        FreePair.Core.Tournaments.Enums.EventType.Lecture, FreePair.Core.Tournaments.Enums.EventType.Simul, FreePair.Core.Tournaments.Enums.EventType.Camp,
        FreePair.Core.Tournaments.Enums.EventType.GroupLesson, FreePair.Core.Tournaments.Enums.EventType.League, FreePair.Core.Tournaments.Enums.EventType.Other,
    };

    public TimeControlType?[] AvailableTimeControlTypes { get; } = new TimeControlType?[]
    {
        null,
        FreePair.Core.Tournaments.Enums.TimeControlType.Bullet, FreePair.Core.Tournaments.Enums.TimeControlType.Blitz,
        FreePair.Core.Tournaments.Enums.TimeControlType.Rapid,  FreePair.Core.Tournaments.Enums.TimeControlType.RapidAndClassical,
        FreePair.Core.Tournaments.Enums.TimeControlType.Classical, FreePair.Core.Tournaments.Enums.TimeControlType.Other,
    };

    public RatingType?[] AvailableRatingTypes { get; } = new RatingType?[]
    {
        null,
        FreePair.Core.Tournaments.Enums.RatingType.UnRated,
        FreePair.Core.Tournaments.Enums.RatingType.USCF, FreePair.Core.Tournaments.Enums.RatingType.FIDE, FreePair.Core.Tournaments.Enums.RatingType.CFC,
        FreePair.Core.Tournaments.Enums.RatingType.USCF_FIDE, FreePair.Core.Tournaments.Enums.RatingType.CFC_FIDE, FreePair.Core.Tournaments.Enums.RatingType.CFC_USCF,
        FreePair.Core.Tournaments.Enums.RatingType.CFC_USCF_FIDE,
        FreePair.Core.Tournaments.Enums.RatingType.USCF_NW, FreePair.Core.Tournaments.Enums.RatingType.USCF_FIDE_NW, FreePair.Core.Tournaments.Enums.RatingType.CFC_FIDE_NW,
        FreePair.Core.Tournaments.Enums.RatingType.USCF_CFC_FIDE_NW,
        FreePair.Core.Tournaments.Enums.RatingType.USCFONLINE, FreePair.Core.Tournaments.Enums.RatingType.CHESSCOM, FreePair.Core.Tournaments.Enums.RatingType.LICHESS, FreePair.Core.Tournaments.Enums.RatingType.CHESS24,
        FreePair.Core.Tournaments.Enums.RatingType.Other,
    };

    public string? NachEventId => _getTournament().NachEventId;

    public EventConfigViewModel(
        Func<Tournament> getTournament,
        Action<Tournament> setTournament)
    {
        _getTournament = getTournament ?? throw new ArgumentNullException(nameof(getTournament));
        _setTournament = setTournament ?? throw new ArgumentNullException(nameof(setTournament));
        Reset();
    }

    /// <summary>
    /// Reloads the form from the current tournament, discarding any
    /// unsaved edits. Called on construction, after every Apply, and
    /// whenever the underlying tournament identity changes.
    /// </summary>
    public void Reset()
    {
        var t = _getTournament();
        Title = t.Title;
        StartDate = t.StartDate.HasValue
            ? new DateTimeOffset(t.StartDate.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        EndDate = t.EndDate.HasValue
            ? new DateTimeOffset(t.EndDate.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        TimeControl = t.TimeControl;

        EventAddress = t.EventAddress;
        EventCity    = t.EventCity;
        EventState   = t.EventState;
        EventZipCode = t.EventZipCode;
        EventCountry = t.EventCountry;

        EventFormat     = t.EventFormat;
        EventType       = t.EventType;
        PairingRule     = t.PairingRule;
        TimeControlType = t.TimeControlType;
        RatingType      = t.RatingType;

        RoundsPlanned        = t.RoundsPlanned;
        HalfPointByesAllowed = t.HalfPointByesAllowed;
        NachOrganizerId      = t.NachOrganizerId;
        NachPasscode         = t.NachPasscode;
        ShowNachPasscode     = false;  // always reset to masked on reload
        TimeZone             = t.TimeZone;

        OnPropertyChanged(nameof(NachEventId));
    }

    [RelayCommand]
    private void Apply()
    {
        var current = _getTournament();
        var updated = TournamentMutations.SetTournamentInfo(
            current,
            title: Title,
            startDate: StartDate.HasValue ? DateOnly.FromDateTime(StartDate.Value.Date) : null,
            endDate:   EndDate.HasValue   ? DateOnly.FromDateTime(EndDate.Value.Date)   : null,
            timeControl: TimeControl,

            eventAddress: EventAddress,
            eventCity:    EventCity,
            eventState:   EventState,
            eventZipCode: EventZipCode,
            eventCountry: EventCountry,

            eventFormat:     new Box<EventFormat?>(EventFormat),
            eventType:       new Box<EventType?>(EventType),
            pairingRule:     new Box<PairingRule?>(PairingRule),
            timeControlType: new Box<TimeControlType?>(TimeControlType),
            ratingType:      new Box<RatingType?>(RatingType),

            nachOrganizerId:      NachOrganizerId,
            timeZone:             TimeZone,
            roundsPlanned:        RoundsPlanned,
            halfPointByesAllowed: HalfPointByesAllowed);

        _setTournament(updated);
        Reset();
    }

    [RelayCommand]
    private void Discard() => Reset();
}
