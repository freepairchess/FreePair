using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// Backs the Event configuration tab: exposes editable copies of the
/// event-level metadata and an <see cref="ApplyCommand"/> that folds
/// the changes back into the parent <see cref="TournamentViewModel"/>
/// via <see cref="TournamentMutations.SetTournamentInfo"/>.
/// </summary>
/// <remarks>
/// The VM operates on a <em>snapshot</em> copy of the Tournament fields
/// rather than binding directly. This lets the TD edit several fields
/// and apply them as a single immutable step (and discard with Reset
/// if they change their mind). On apply the parent VM's
/// <see cref="TournamentViewModel.Tournament"/> property setter triggers
/// the normal rebuild + auto-save pipeline.
/// </remarks>
public sealed partial class EventConfigViewModel : ObservableObject
{
    private readonly Func<Tournament> _getTournament;
    private readonly Action<Tournament> _setTournament;

    // ============ edited snapshot ============

    [ObservableProperty] private string? _title;
    [ObservableProperty] private string? _location;
    [ObservableProperty] private DateTimeOffset? _startDate;
    [ObservableProperty] private DateTimeOffset? _endDate;
    [ObservableProperty] private string? _timeControl;
    [ObservableProperty] private SectionKind _defaultPairingKind;
    [ObservableProperty] private string? _defaultRatingType;

    // ============ UI helpers ============

    /// <summary>All values the default-pairing-kind dropdown offers.</summary>
    public SectionKind[] AvailablePairingKinds { get; } = new[]
    {
        SectionKind.Swiss,
        SectionKind.RoundRobin,
    };

    /// <summary>Rating-system suggestions the Combo shows (free-form editable).</summary>
    public string[] KnownRatingTypes { get; } = new[]
    {
        "USCF", "FIDE", "CFC", "NWSRS",
    };

    /// <summary>
    /// True when the NACH event ID has been assigned by NAChessHub and
    /// should be shown as read-only info in the form.
    /// </summary>
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
        Location = t.Location;
        StartDate = t.StartDate.HasValue
            ? new DateTimeOffset(t.StartDate.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        EndDate = t.EndDate.HasValue
            ? new DateTimeOffset(t.EndDate.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        TimeControl = t.TimeControl;
        DefaultPairingKind = t.DefaultPairingKind == SectionKind.Unknown
            ? SectionKind.Swiss
            : t.DefaultPairingKind;
        DefaultRatingType = t.DefaultRatingType;
        OnPropertyChanged(nameof(NachEventId));
    }

    [RelayCommand]
    private void Apply()
    {
        var current = _getTournament();
        var updated = TournamentMutations.SetTournamentInfo(
            current,
            title: Title,
            location: Location,
            startDate: StartDate.HasValue
                ? DateOnly.FromDateTime(StartDate.Value.Date)
                : null,
            endDate: EndDate.HasValue
                ? DateOnly.FromDateTime(EndDate.Value.Date)
                : null,
            timeControl: TimeControl,
            defaultPairingKind: DefaultPairingKind,
            defaultRatingType: DefaultRatingType);
        _setTournament(updated);
        Reset(); // pull fresh values so NachEventId updates, etc.
    }

    [RelayCommand]
    private void Discard() => Reset();
}
