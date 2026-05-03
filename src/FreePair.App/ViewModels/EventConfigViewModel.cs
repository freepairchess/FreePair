using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Enums;

namespace FreePair.App.ViewModels;

/// <summary>
/// One row in the "Section starting boards" panel on the Event
/// configuration tab — exposes a single section's name, the
/// recommended <see cref="Section.FirstBoard"/> derived from
/// player counts, and the editable value the TD has chosen.
/// Apply on the parent VM walks these rows and persists changes
/// via <see cref="TournamentMutations.SetSectionFirstBoard"/>.
/// </summary>
public sealed partial class SectionBoardRow : ObservableObject
{
    public string Name { get; }

    /// <summary>
    /// Recommended start board (from <see cref="BoardNumberRecommender"/>).
    /// Read-only; refreshes when the parent VM rebuilds the rows.
    /// </summary>
    public int Recommended { get; }

    /// <summary>
    /// Editable starting board for this section. Bound to a
    /// <c>NumericUpDown</c> in the view. Null means "use board 1
    /// (no offset)" — displayed as 1 in the spinner because
    /// NumericUpDown doesn't support null cleanly.
    /// </summary>
    [ObservableProperty] private int _firstBoard;

    public SectionBoardRow(string name, int? currentFirstBoard, int recommended)
    {
        Name = name;
        Recommended = recommended;
        _firstBoard = currentFirstBoard ?? 1;
    }

    /// <summary>True when the user changed the value vs the recommended start.</summary>
    public bool DiffersFromRecommended => FirstBoard != Recommended;
}

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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectivePairingEngineDisplay))]
    private RatingType? _ratingType;

    /// <summary>
    /// Tournament-level pairing engine override. Bound to a combobox
    /// on the Event-config form. <c>null</c> ("Default for rating
    /// type") means the engine is derived from <see cref="RatingType"/>
    /// at run-time — see
    /// <see cref="FreePair.Core.Tournaments.PairingEngineDefaults.ForRatingType"/>.
    /// Read-only after any section has paired a round
    /// (<see cref="CanChangePairingEngine"/>).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectivePairingEngineDisplay))]
    private PairingEngineChoice? _selectedEngineChoice;

    // ============ scheduling / counts ============
    [ObservableProperty] private int? _roundsPlanned;
    [ObservableProperty] private int? _halfPointByesAllowed;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UscfAffiliateUrl))]
    private string? _organizerId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UscfAffiliateUrl))]
    private FreePair.Core.Tournaments.Enums.UserIDType? _organizerIdType;
    [ObservableProperty] private string? _organizerName;
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

    public UserIDType?[] AvailableOrganizerIdTypes { get; } = new UserIDType?[]
    {
        null,
        FreePair.Core.Tournaments.Enums.UserIDType.USCFID,
        FreePair.Core.Tournaments.Enums.UserIDType.FIDEID,
        FreePair.Core.Tournaments.Enums.UserIDType.CFCID,
        FreePair.Core.Tournaments.Enums.UserIDType.USCFAffiliateID,
        FreePair.Core.Tournaments.Enums.UserIDType.FIDEOrganizerID,
        FreePair.Core.Tournaments.Enums.UserIDType.CFCOrganizerID,
        FreePair.Core.Tournaments.Enums.UserIDType.Local,
        FreePair.Core.Tournaments.Enums.UserIDType.Other,
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

    /// <summary>
    /// Choices shown in the tournament-level pairing-engine combobox.
    /// Same three items as the section-level combobox but with the
    /// "inherit" sentinel labelled differently ("Default for rating
    /// type" instead of "Inherit from event").
    /// </summary>
    public IReadOnlyList<PairingEngineChoice> AvailableEngineChoices =>
        PairingEngineChoice.TournamentChoices;

    /// <summary>
    /// True when the TD can edit
    /// <see cref="SelectedEngineChoice"/>: no section has paired a
    /// round yet. Once a round lands the combobox goes read-only —
    /// the underlying mutation also throws.
    /// </summary>
    public bool CanChangePairingEngine =>
        _getTournament().Sections.All(s => s.SoftDeleted || s.RoundsPaired == 0);

    /// <summary>
    /// Live-resolved label for the engine FreePair would dispatch
    /// today against the (potentially-edited) form values. Shows the
    /// effective engine after the inherit/default cascade so the TD
    /// can see what their override currently amounts to without
    /// having to Apply first.
    /// </summary>
    public string EffectivePairingEngineDisplay
    {
        get
        {
            var effective = SelectedEngineChoice?.Value
                ?? FreePair.Core.Tournaments.PairingEngineDefaults.ForRatingType(RatingType);
            return PairingEngineChoice.DisplayFor(effective);
        }
    }

    public string? NachEventId => _getTournament().NachEventId;

    /// <summary>
    /// Per-section starting-board rows shown on the Event
    /// configuration tab. One row per non-soft-deleted section,
    /// ordered the same as <see cref="Tournament.Sections"/>.
    /// Rebuilt on every <see cref="Reset"/>; persisted on
    /// <see cref="ApplyCommand"/>.
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<SectionBoardRow> SectionBoards { get; } = new();

    /// <summary>
    /// US Chess affiliate URL derived from <see cref="OrganizerId"/>
    /// when the id type is <see cref="FreePair.Core.Tournaments.Enums.UserIDType.USCFAffiliateID"/>,
    /// otherwise null. Bound in the view as a clickable link next to
    /// the Organizer ID textbox; auto-refreshes when either source
    /// property changes via <c>NotifyPropertyChangedFor</c>.
    /// </summary>
    public string? UscfAffiliateUrl =>
        OrganizerIdType == FreePair.Core.Tournaments.Enums.UserIDType.USCFAffiliateID
        && !string.IsNullOrWhiteSpace(OrganizerId)
            ? $"https://ratings.uschess.org/affiliate/{OrganizerId.Trim()}"
            : null;

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

        SelectedEngineChoice = PairingEngineChoice.TournamentChoices.FirstOrDefault(
            c => c.Value == t.PairingEngine)
            ?? PairingEngineChoice.TournamentChoices[0];
        OnPropertyChanged(nameof(CanChangePairingEngine));

        RoundsPlanned        = t.RoundsPlanned;
        HalfPointByesAllowed = t.HalfPointByesAllowed;
        OrganizerId          = t.OrganizerId;
        OrganizerIdType      = t.OrganizerIdType;
        OrganizerName        = t.OrganizerName;
        NachPasscode         = t.NachPasscode;
        ShowNachPasscode     = false;  // always reset to masked on reload
        TimeZone             = t.TimeZone;

        OnPropertyChanged(nameof(NachEventId));

        // Rebuild the per-section starting-board rows. Recommended
        // values come from the BoardNumberRecommender; current
        // values come from each section's FirstBoard. Soft-deleted
        // sections are skipped (they aren't in the recommender's
        // output either).
        SectionBoards.Clear();
        var recommended = BoardNumberRecommender.Recommend(t);
        foreach (var s in t.Sections)
        {
            if (s.SoftDeleted) continue;
            var rec = recommended.TryGetValue(s.Name, out var r) ? r : 1;
            SectionBoards.Add(new SectionBoardRow(s.Name, s.FirstBoard, rec));
        }
    }

    /// <summary>
    /// Copies the recommended starting board into every editable
    /// row, leaving Apply to actually persist. Used by the
    /// "Use recommended" button on the Section starting boards
    /// panel — the global toolbar "🔢 Renumber boards" button is a
    /// shortcut that does this AND applies in one click.
    /// </summary>
    [RelayCommand]
    private void UseRecommendedBoards()
    {
        foreach (var row in SectionBoards)
        {
            row.FirstBoard = row.Recommended;
        }
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

            organizerId:          OrganizerId,
            organizerIdType:      new Box<FreePair.Core.Tournaments.Enums.UserIDType?>(OrganizerIdType),
            organizerName:        OrganizerName,
            nachPasscode:         NachPasscode,
            timeZone:             TimeZone,
            roundsPlanned:        RoundsPlanned,
            halfPointByesAllowed: HalfPointByesAllowed);

        // Fold per-section FirstBoard edits into the same Apply
        // round-trip so both event-level and section-level edits
        // land as one auto-save.
        updated = ApplySectionBoardEdits(updated);

        // Apply the tournament-level pairing-engine override too.
        // Skipped silently when the value didn't change OR when any
        // section has already paired a round (the mutation would
        // throw — UI prevents the change but defensive in case the
        // form value is stale).
        try
        {
            var desiredEngine = SelectedEngineChoice?.Value;
            if (updated.PairingEngine != desiredEngine
                && updated.Sections.All(s => s.SoftDeleted || s.RoundsPaired == 0))
            {
                updated = TournamentMutations.SetTournamentPairingEngine(updated, desiredEngine);
            }
        }
        catch (System.InvalidOperationException) { /* lock — keep prior value */ }

        _setTournament(updated);
        Reset();
    }

    /// <summary>
    /// Persists pending edits to the per-section starting board
    /// rows back onto the tournament. Called from
    /// <see cref="ApplyCommand"/> after <see cref="SetTournamentInfo"/>
    /// so both event-level and per-section edits land in a single
    /// auto-save. Skipped rows (no change vs the section's current
    /// FirstBoard) are no-ops.
    /// </summary>
    private Tournament ApplySectionBoardEdits(Tournament t)
    {
        foreach (var row in SectionBoards)
        {
            var current = t.Sections.FirstOrDefault(s => s.Name == row.Name);
            if (current is null) continue;
            var newValue = row.FirstBoard <= 1 ? (int?)null : row.FirstBoard;
            if (current.FirstBoard == newValue) continue;
            t = TournamentMutations.SetSectionFirstBoard(t, row.Name, newValue);
        }
        return t;
    }

    [RelayCommand]
    private void Discard() => Reset();
}
