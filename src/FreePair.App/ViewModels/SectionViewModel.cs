using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Formatting;
using FreePair.Core.SwissSys;
using FreePair.Core.Tournaments;
using FreePair.Core.Tournaments.Standings;
using FreePair.Core.Tournaments.WallCharts;

namespace FreePair.App.ViewModels;

/// <summary>
/// Pre-formatted row for the <b>Players</b> tab.
/// </summary>
public sealed record PlayerRow(
    int PairNumber,
    string Name,
    string? UscfId,
    int Rating,
    string? Club,
    string? State,
    string? Team,
    decimal Score,
    string ScoreText,
    string Status,
    string? RequestedByes,
    string? Email,
    string? Phone);

/// <summary>
/// Editable pairing row for the <b>Pairings</b> tab. Binds to a result
/// combo-box and propagates changes back to the enclosing
/// <see cref="SectionViewModel"/> so tournament state is mutated
/// consistently.
/// </summary>
public partial class PairingRow : ObservableObject
{
    private readonly IScoreFormatter _formatter;
    private readonly Action<PairingRow, PairingResult>? _onResultChanged;
    private bool _suppressCallback;

    [ObservableProperty]
    private PairingResultOption _selectedResult;

    [ObservableProperty]
    private string _resultText;

    public PairingRow(
        int board,
        int whitePair,
        string whiteName,
        int whiteRating,
        int blackPair,
        string blackName,
        int blackRating,
        PairingResult initialResult,
        IScoreFormatter formatter,
        Action<PairingRow, PairingResult>? onResultChanged)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        _formatter = formatter;
        _onResultChanged = onResultChanged;

        Board = board;
        WhitePair = whitePair;
        WhiteName = whiteName;
        WhiteRating = whiteRating;
        BlackPair = blackPair;
        BlackName = blackName;
        BlackRating = blackRating;

        AvailableResults = new[]
        {
            new PairingResultOption(PairingResult.Unplayed,  "-"),
            new PairingResultOption(PairingResult.WhiteWins, "1-0"),
            new PairingResultOption(PairingResult.Draw,      formatter.PairingResult(PairingResult.Draw)),
            new PairingResultOption(PairingResult.BlackWins, "0-1"),
        };

        _selectedResult = AvailableResults.First(o => o.Value == initialResult);
        _resultText = _selectedResult.Text;
    }

    public int Board { get; }
    public int WhitePair { get; }
    public string WhiteName { get; }
    public int WhiteRating { get; }
    public int BlackPair { get; }
    public string BlackName { get; }
    public int BlackRating { get; }

    public IReadOnlyList<PairingResultOption> AvailableResults { get; }

    public PairingResult Result => SelectedResult.Value;

    partial void OnSelectedResultChanged(PairingResultOption value)
    {
        ResultText = value.Text;
        if (!_suppressCallback)
        {
            _onResultChanged?.Invoke(this, value.Value);
        }
    }

    /// <summary>
    /// Updates <see cref="SelectedResult"/> without firing the
    /// result-changed callback. Used by the hosting view model when
    /// rebuilding after a mutation.
    /// </summary>
    internal void SetResultSilently(PairingResult result)
    {
        var target = AvailableResults.FirstOrDefault(o => o.Value == result)
                     ?? AvailableResults[0];
        _suppressCallback = true;
        try
        {
            SelectedResult = target;
        }
        finally
        {
            _suppressCallback = false;
        }
    }
}

/// <summary>
/// Pre-formatted row for the <b>Byes &amp; Withdrawals</b> tab and for
/// per-round bye lists.
/// </summary>
public sealed record ByeRow(
    int Round,
    int PairNumber,
    string Name,
    string Kind);

/// <summary>
/// Lightweight round-selector entry bound to the Pairings tab's combo.
/// </summary>
public sealed record RoundOption(int Number, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// View model for a single <see cref="Section"/>. Precomputes every per-tab
/// projection up front so the UI can bind directly without any further
/// transforms.
/// </summary>
public partial class SectionViewModel : ViewModelBase
{
    private readonly IReadOnlyDictionary<int, Player> _byPair;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentRoundPairings))]
    [NotifyPropertyChangedFor(nameof(CurrentRoundByes))]
    [NotifyPropertyChangedFor(nameof(HasSelectedRound))]
    [NotifyPropertyChangedFor(nameof(CanPairNextRound))]
    [NotifyPropertyChangedFor(nameof(PairNextRoundBlockReason))]
    private RoundOption? _selectedRound;

    /// <summary>
    /// Index of the currently active tab in the section's <c>TabControl</c>.
    /// Persisted across VM rebuilds so result-entry mutations don't bounce
    /// the user back to the Summary tab.
    /// </summary>
    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>
    /// Fired when the user picks a different result in one of the pairing
    /// rows. The host (<see cref="TournamentViewModel"/>) subscribes to
    /// apply the mutation against the live <see cref="Tournament"/>.
    /// </summary>
    public event Action<SectionViewModel, int /*round*/, PairingRow, PairingResult>? ResultChanged;

    /// <summary>
    /// Fired when the user clicks "Pair Next Round". The host performs
    /// the BBP invocation + mutation; the VM only surfaces status.
    /// </summary>
    public event Func<SectionViewModel, Task>? PairNextRoundRequested;

    /// <summary>
    /// Fired when the user clicks "Delete round N". The host performs the
    /// confirmation prompt + mutation.
    /// </summary>
    public event Func<SectionViewModel, Task>? DeleteLastRoundRequested;

    public SectionViewModel(Section section)
        : this(section, new ScoreFormatter())
    {
    }

    public SectionViewModel(Section section, IScoreFormatter formatter)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
        Formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _byPair = section.Players.ToDictionary(p => p.PairNumber);

        Standings = StandingsBuilder.Build(section);
        WallChart = WallChartBuilder.Build(section);

        Players = section.Players
            .OrderBy(p => p.PairNumber)
            .Select(p => BuildPlayerRow(p, section))
            .ToArray();

        StandingsDisplay = Standings
            .Select(r => new StandingsDisplayRow(
                r,
                formatter.Score(r.Score),
                formatter.Score(r.Tiebreaks.ModifiedMedian),
                formatter.Score(r.Tiebreaks.Solkoff),
                formatter.Score(r.Tiebreaks.Cumulative),
                formatter.Score(r.Tiebreaks.OpponentCumulative)))
            .ToArray();

        WallChartDisplay = WallChart
            .Select(w => new WallChartDisplayRow(
                w,
                formatter.Score(w.Score),
                formatter.Score(w.Tiebreaks.ModifiedMedian),
                formatter.Score(w.Tiebreaks.Solkoff),
                formatter.Score(w.Tiebreaks.Cumulative),
                formatter.Score(w.Tiebreaks.OpponentCumulative)))
            .ToArray();

        AllByes = section.Rounds
            .SelectMany(r => r.Byes.Select(b => BuildByeRow(r.Number, b)))
            .ToArray();
        AvailableRounds = section.Rounds
            .Select(r => new RoundOption(r.Number, $"Round {r.Number}"))
            .ToArray();

        SelectedRound = AvailableRounds.LastOrDefault();
    }

    /// <summary>Underlying domain section.</summary>
    public Section Section { get; }

    public IScoreFormatter Formatter { get; }

    /// <summary>
    /// Reference back to the hosting <see cref="TournamentViewModel"/>
    /// so code-behind for SectionView can reach the tournament snapshot
    /// (e.g. PDF export of reports that span the whole event header).
    /// Set by <see cref="TournamentViewModel.AttachSectionEvents"/>;
    /// null in isolated tests / design-time.
    /// </summary>
    public TournamentViewModel? ParentTournamentVm { get; internal set; }

    public string Name => Section.Name;

    public string DisplayName =>
        $"{Section.Name}  ·  {Section.Players.Count}p  ·  R{Section.RoundsPlayed}/{TargetRounds}";

    public int RoundsPlayed => Section.RoundsPlayed;

    public int RoundsPaired => Section.RoundsPaired;

    public int TargetRounds =>
        Math.Max(Math.Max(Section.RoundsPaired, Section.FinalRound), Section.RoundsPlayed);

    public bool HasTeams => Section.HasTeams;

    public string? TimeControl => Section.TimeControl;

    public int PlayerCount => Section.Players.Count;

    public int TeamCount => Section.Teams.Count;

    public int WithdrawnCount => Section.WithdrawnPlayers.Count();

    public int RequestedByeCount =>
        Section.Players.Count(p => p.RequestedByeRounds.Count > 0);

    public IReadOnlyList<StandingsRow> Standings { get; }

    public IReadOnlyList<WallChartRow> WallChart { get; }

    public IReadOnlyList<StandingsDisplayRow> StandingsDisplay { get; }

    public IReadOnlyList<WallChartDisplayRow> WallChartDisplay { get; }

    public IReadOnlyList<PlayerRow> Players { get; }

    /// <summary>
    /// Case-insensitive search filter applied to <see cref="Players"/>
    /// to populate <see cref="FilteredPlayers"/>. Matches against
    /// name, USCF id, club, state, team, email and phone. An empty
    /// string shows every player.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPlayers))]
    [NotifyPropertyChangedFor(nameof(FilteredPlayerCountText))]
    private string _playerFilter = string.Empty;

    /// <summary>
    /// <see cref="Players"/> projected through the current
    /// <see cref="PlayerFilter"/>. DataGrids on the Players tab bind
    /// to this collection. Computed on every access, which is fine
    /// for typical section sizes (tens to a few hundred players).
    /// </summary>
    public IReadOnlyList<PlayerRow> FilteredPlayers
    {
        get
        {
            var needle = (PlayerFilter ?? string.Empty).Trim();
            if (needle.Length == 0) return Players;

            return Players
                .Where(p => ContainsCI(p.Name, needle)
                         || ContainsCI(p.UscfId, needle)
                         || ContainsCI(p.Club, needle)
                         || ContainsCI(p.State, needle)
                         || ContainsCI(p.Team, needle)
                         || ContainsCI(p.Email, needle)
                         || ContainsCI(p.Phone, needle)
                         || p.PairNumber.ToString().Contains(needle, StringComparison.Ordinal))
                .ToArray();
        }
    }

    /// <summary>
    /// TD-readable "X of Y players" summary shown next to the filter
    /// textbox. When no filter is active, just renders "Y players".
    /// </summary>
    public string FilteredPlayerCountText => string.IsNullOrWhiteSpace(PlayerFilter)
        ? $"{Players.Count} players"
        : $"{FilteredPlayers.Count} of {Players.Count} players";

    private static bool ContainsCI(string? haystack, string needle) =>
        !string.IsNullOrEmpty(haystack)
        && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<ByeRow> AllByes { get; }

    public IReadOnlyList<RoundOption> AvailableRounds { get; }

    public bool HasSelectedRound => SelectedRound is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPairNextRound))]
    [NotifyPropertyChangedFor(nameof(PairNextRoundBlockReason))]
    private bool _isPairingNextRound;

    /// <summary>
    /// True when all scheduled rounds are complete and none of the gates
    /// (busy, missing results, no more rounds) blocks next-round pairing.
    /// </summary>
    public bool CanPairNextRound => PairNextRoundBlockReason is null;

    /// <summary>
    /// A human-readable explanation of why <see cref="CanPairNextRound"/>
    /// is false, or <c>null</c> if pairing is permitted.
    /// </summary>
    public string? PairNextRoundBlockReason
    {
        get
        {
            if (IsPairingNextRound)
            {
                return "Pairing in progress...";
            }

            if (TargetRounds <= 0)
            {
                return "This section has no scheduled rounds.";
            }

            if (Section.RoundsPlayed >= TargetRounds)
            {
                return $"All {TargetRounds} rounds are complete.";
            }

            if (Section.Rounds.Count > Section.RoundsPlayed)
            {
                var inProgress = Section.Rounds.Count;
                return $"Enter all results for round {inProgress} before pairing the next round.";
            }

            return null;
        }
    }

    public IReadOnlyList<PairingRow> CurrentRoundPairings =>
        SelectedRound is null
            ? Array.Empty<PairingRow>()
            : Section.Rounds
                .First(r => r.Number == SelectedRound.Number)
                .Pairings
                .Select(BuildPairingRow)
                .ToArray();

    public IReadOnlyList<ByeRow> CurrentRoundByes =>
        SelectedRound is null
            ? Array.Empty<ByeRow>()
            : Section.Rounds
                .First(r => r.Number == SelectedRound.Number)
                .Byes
                .Select(b => BuildByeRow(SelectedRound.Number, b))
                .ToArray();

    [RelayCommand]
    private async Task PairNextRoundAsync()
    {
        var handler = PairNextRoundRequested;
        if (handler is null)
        {
            return;
        }

        await handler(this).ConfigureAwait(true);
    }

    /// <summary>
    /// True when the section has at least one paired round that can be
    /// rolled back via <see cref="DeleteLastRoundCommand"/>.
    /// </summary>
    public bool CanDeleteLastRound => Section.Rounds.Count > 0 && !IsPairingNextRound;

    /// <summary>
    /// Label for the delete-round button, e.g. "Delete round 3".
    /// </summary>
    public string DeleteLastRoundLabel =>
        Section.Rounds.Count == 0
            ? "Delete round"
            : $"Delete round {Section.Rounds.Count}";

    [RelayCommand]
    private async Task DeleteLastRoundAsync()
    {
        var handler = DeleteLastRoundRequested;
        if (handler is null)
        {
            return;
        }

        await handler(this).ConfigureAwait(true);
    }

    private static PlayerRow BuildPlayerRowStatic(Player player, Section section, IScoreFormatter formatter)
    {
        string status;
        if (section.IsWithdrawn(player))
        {
            status = "Withdrawn";
        }
        else if (player.RequestedByeRounds.Count > 0)
        {
            status = "Bye requested";
        }
        else
        {
            status = "Active";
        }

        var requestedByes = player.RequestedByeRounds.Count == 0
            ? null
            : string.Join(", ", player.RequestedByeRounds);

        return new PlayerRow(
            PairNumber: player.PairNumber,
            Name: player.Name,
            UscfId: player.UscfId,
            Rating: player.Rating,
            Club: player.Club,
            State: player.State,
            Team: player.Team,
            Score: player.Score,
            ScoreText: formatter.Score(player.Score),
            Status: status,
            RequestedByes: requestedByes,
            Email: player.Email,
            Phone: player.Phone);
    }

    private PlayerRow BuildPlayerRow(Player player, Section section) =>
        BuildPlayerRowStatic(player, section, Formatter);

    private PairingRow BuildPairingRow(Pairing p)
    {
        var white = _byPair.TryGetValue(p.WhitePair, out var w) ? w : null;
        var black = _byPair.TryGetValue(p.BlackPair, out var b) ? b : null;
        var roundNumber = SelectedRound?.Number ?? 0;

        return new PairingRow(
            board: p.Board,
            whitePair: p.WhitePair,
            whiteName: white?.Name ?? $"Pair {p.WhitePair}",
            whiteRating: white?.Rating ?? 0,
            blackPair: p.BlackPair,
            blackName: black?.Name ?? $"Pair {p.BlackPair}",
            blackRating: black?.Rating ?? 0,
            initialResult: p.Result,
            formatter: Formatter,
            onResultChanged: (row, newResult) =>
                ResultChanged?.Invoke(this, roundNumber, row, newResult));
    }

    private ByeRow BuildByeRow(int round, ByeAssignment bye)
    {
        var name = _byPair.TryGetValue(bye.PlayerPair, out var player)
            ? player.Name
            : $"Pair {bye.PlayerPair}";

        return new ByeRow(round, bye.PlayerPair, name, FormatByeKind(bye.Kind));
    }

    private static string FormatByeKind(ByeKind kind) => kind switch
    {
        ByeKind.Full => "Full-point bye",
        ByeKind.Half => "Half-point bye",
        ByeKind.Unpaired => "Unpaired",
        _ => kind.ToString(),
    };
}
