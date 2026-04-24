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
    /// <summary>Half-point bye rounds only, e.g. <c>"2, 5"</c>. Null if none.</summary>
    string? HalfByeRounds,
    /// <summary>Zero-point bye rounds only, e.g. <c>"3, 4"</c>. Null if none.</summary>
    string? ZeroByeRounds,
    string? Email,
    string? Phone,
    /// <summary>True when the player is soft-deleted (pre-round-1 only).</summary>
    bool IsSoftDeleted,
    /// <summary>Soft-delete icon visibility: live AND section not yet paired.</summary>
    bool CanSoftDelete,
    /// <summary>Undelete icon visibility: player is currently soft-deleted.</summary>
    bool CanUndelete,
    /// <summary>Hard-delete icon visibility: soft-deleted AND section not yet paired.</summary>
    bool CanHardDelete,
    /// <summary>True when the player is withdrawn (mid-tournament state).</summary>
    bool IsWithdrawn,
    /// <summary>Withdraw icon visibility: live (non-soft-deleted, non-withdrawn) AND section has paired at least one round.</summary>
    bool CanWithdraw,
    /// <summary>Return-from-withdrawal icon visibility: currently withdrawn.</summary>
    bool CanUnwithdraw,
    /// <summary>
    /// Manage-byes icon visibility: player is live (not soft-deleted,
    /// not withdrawn) AND the section has at least one unpaired
    /// future round to assign a bye for.
    /// </summary>
    bool CanManageByes);

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
    private readonly Action<PairingRow, int /*pair*/, ByeKind>? _onConvertToBye;
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
        Action<PairingRow, PairingResult>? onResultChanged,
        Action<PairingRow, int, ByeKind>? onConvertToBye = null)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        _formatter = formatter;
        _onResultChanged = onResultChanged;
        _onConvertToBye = onConvertToBye;

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
    [NotifyPropertyChangedFor(nameof(FilteredPairings))]
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

    // =================== Pairings / Standings / Wall-Chart / Byes filters ===================

    /// <summary>
    /// Case-insensitive filter for the Pairings tab DataGrid. Matches
    /// against board number, white/black pair numbers and names. Empty
    /// shows every board for the selected round.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPairings))]
    private string _pairingFilter = string.Empty;

    /// <summary>Filtered projection of <see cref="CurrentRoundPairings"/>.</summary>
    public IReadOnlyList<PairingRow> FilteredPairings
    {
        get
        {
            var needle = (PairingFilter ?? string.Empty).Trim();
            var rows = CurrentRoundPairings;
            if (needle.Length == 0) return rows;
            return rows
                .Where(p => p.Board.ToString().Contains(needle, StringComparison.Ordinal)
                         || p.WhitePair.ToString().Contains(needle, StringComparison.Ordinal)
                         || p.BlackPair.ToString().Contains(needle, StringComparison.Ordinal)
                         || ContainsCI(p.WhiteName, needle)
                         || ContainsCI(p.BlackName, needle))
                .ToArray();
        }
    }

    /// <summary>Filter text for the Standings tab.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredStandings))]
    private string _standingsFilter = string.Empty;

    /// <summary>Filtered projection of <see cref="StandingsDisplay"/>.</summary>
    public IReadOnlyList<StandingsDisplayRow> FilteredStandings
    {
        get
        {
            var needle = (StandingsFilter ?? string.Empty).Trim();
            if (needle.Length == 0) return StandingsDisplay;
            return StandingsDisplay
                .Where(r => ContainsCI(r.Row.Name, needle)
                         || r.Row.PairNumber.ToString().Contains(needle, StringComparison.Ordinal)
                         || ContainsCI(r.Row.Place, needle)
                         || r.Row.Rating.ToString().Contains(needle, StringComparison.Ordinal))
                .ToArray();
        }
    }

    /// <summary>Filter text for the Wall Chart tab.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredWallChart))]
    private string _wallChartFilter = string.Empty;

    /// <summary>Filtered projection of <see cref="WallChartDisplay"/>.</summary>
    public IReadOnlyList<WallChartDisplayRow> FilteredWallChart
    {
        get
        {
            var needle = (WallChartFilter ?? string.Empty).Trim();
            if (needle.Length == 0) return WallChartDisplay;
            return WallChartDisplay
                .Where(r => ContainsCI(r.Row.Name, needle)
                         || r.Row.PairNumber.ToString().Contains(needle, StringComparison.Ordinal)
                         || ContainsCI(r.Row.Club, needle)
                         || ContainsCI(r.Row.State, needle)
                         || ContainsCI(r.Row.Team, needle))
                .ToArray();
        }
    }

    /// <summary>Filter text for the Byes &amp; Withdrawals tab.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredAllByes))]
    private string _byesFilter = string.Empty;

    /// <summary>Filtered projection of <see cref="AllByes"/>.</summary>
    public IReadOnlyList<ByeRow> FilteredAllByes
    {
        get
        {
            var needle = (ByesFilter ?? string.Empty).Trim();
            if (needle.Length == 0) return AllByes;
            return AllByes
                .Where(b => ContainsCI(b.Name, needle)
                         || b.PairNumber.ToString().Contains(needle, StringComparison.Ordinal)
                         || b.Round.ToString().Contains(needle, StringComparison.Ordinal)
                         || b.Kind.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }

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
            if (IsSoftDeleted)
            {
                return "This section is soft-deleted. Undelete it before making changes.";
            }

            if (IsPairingNextRound)
            {
                return "Pairing in progress...";
            }

            // Pre-round-1 block: if the TD has any soft-deleted players,
            // require them to restore or permanently delete before
            // pairing round 1. The domain mutation enforces the same
            // rule; surfacing here also disables the Pair button and
            // shows an inline explanation.
            if (Section.RoundsPaired == 0)
            {
                var softDeleted = Section.Players.Where(p => p.SoftDeleted).ToArray();
                if (softDeleted.Length > 0)
                {
                    return softDeleted.Length == 1
                        ? $"Player #{softDeleted[0].PairNumber} {softDeleted[0].Name} is soft-deleted. Restore or permanently delete before pairing round 1."
                        : $"{softDeleted.Length} players are soft-deleted. Restore or permanently delete them before pairing round 1.";
                }
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
    public bool CanDeleteLastRound => Section.Rounds.Count > 0 && !IsPairingNextRound && !IsSoftDeleted;

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

    // ================================================================
    // Soft-delete / undelete / hard-delete
    // ================================================================

    /// <summary>
    /// True when the section has been marked as soft-deleted. Used by
    /// the view to show the yellow "This section is soft-deleted…"
    /// banner and to disable every interactive control on the Pairings
    /// tab. The mutations layer ALSO refuses to touch soft-deleted
    /// sections, so the UI guard is defense-in-depth.
    /// </summary>
    public bool IsSoftDeleted => Section.SoftDeleted;

    /// <summary>
    /// Convenience inverse for binding <c>IsEnabled</c>-style
    /// properties without an explicit converter.
    /// </summary>
    public bool IsLive => !IsSoftDeleted;

    /// <summary>
    /// Suffix appended to the section's left-tab label when it's
    /// soft-deleted (empty string for live sections). Keeps the
    /// existing <see cref="Name"/> binding intact while letting the
    /// tab template show a " [deleted]" hint.
    /// </summary>
    public string TabLabelSuffix => IsSoftDeleted ? " [deleted]" : "";

    /// <summary>
    /// Raised when the TD clicks "🗑 Delete section…" on the Pairings
    /// tab. The parent <c>TournamentViewModel</c> handles the confirm
    /// prompt, the mutation, and the auto-save.
    /// </summary>
    public event Func<SectionViewModel, Task>? SoftDeleteRequested;

    /// <summary>
    /// Raised when the TD clicks "Undelete" on the soft-deleted
    /// banner. No confirm prompt — this is a reversible operation.
    /// </summary>
    public event Func<SectionViewModel, Task>? UndeleteRequested;

    /// <summary>
    /// Raised when the TD clicks "Permanently delete…" on the
    /// soft-deleted banner. Parent VM prompts with an extra-scary
    /// confirm before invoking <c>HardDeleteSection</c>.
    /// </summary>
    public event Func<SectionViewModel, Task>? HardDeleteRequested;

    [RelayCommand]
    private async Task SoftDeleteSectionAsync()
    {
        var handler = SoftDeleteRequested;
        if (handler is null) return;
        await handler(this).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task UndeleteSectionAsync()
    {
        var handler = UndeleteRequested;
        if (handler is null) return;
        await handler(this).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task HardDeleteSectionAsync()
    {
        var handler = HardDeleteRequested;
        if (handler is null) return;
        await handler(this).ConfigureAwait(true);
    }

    // ================================================================
    // Player lifecycle dispatch
    // ================================================================

    /// <summary>
    /// Raised when the TD clicks the green 🗑 icon on a player row.
    /// Parent VM runs the confirm and invokes
    /// <see cref="TournamentMutations.SoftDeletePlayer"/>.
    /// </summary>
    public event Func<SectionViewModel, int /*pairNumber*/, Task>? PlayerSoftDeleteRequested;

    /// <summary>
    /// Raised when the TD clicks the blue ↩ icon on a soft-deleted
    /// player row. No confirm — reversible.
    /// </summary>
    public event Func<SectionViewModel, int /*pairNumber*/, Task>? PlayerUndeleteRequested;

    /// <summary>
    /// Raised when the TD clicks the red 🗑 icon on a soft-deleted
    /// player row. Parent VM runs a scary confirm before invoking
    /// <see cref="TournamentMutations.HardDeletePlayer"/>.
    /// </summary>
    public event Func<SectionViewModel, int /*pairNumber*/, Task>? PlayerHardDeleteRequested;

    /// <summary>
    /// Invoked by the SectionView code-behind when the green 🗑 icon
    /// is clicked. Routes through the event to the parent VM so the
    /// confirm / mutate / persist flow runs there.
    /// </summary>
    public Task RequestPlayerSoftDeleteAsync(int pairNumber) =>
        PlayerSoftDeleteRequested?.Invoke(this, pairNumber) ?? Task.CompletedTask;

    public Task RequestPlayerUndeleteAsync(int pairNumber) =>
        PlayerUndeleteRequested?.Invoke(this, pairNumber) ?? Task.CompletedTask;

    public Task RequestPlayerHardDeleteAsync(int pairNumber) =>
        PlayerHardDeleteRequested?.Invoke(this, pairNumber) ?? Task.CompletedTask;

    /// <summary>
    /// Raised when the TD clicks the Withdraw icon on a post-round-1
    /// player row. Parent VM runs a confirm and invokes
    /// <see cref="TournamentMutations.SetPlayerWithdrawn"/> with
    /// <c>withdrawn=true</c>. Unlike soft-delete, this keeps the
    /// player's past results in place — they just aren't paired in
    /// future rounds and appear with a "Withdrawn" marker.
    /// </summary>
    public event Func<SectionViewModel, int /*pairNumber*/, Task>? PlayerWithdrawRequested;

    /// <summary>
    /// Raised when the TD clicks the "return from withdrawal" icon on
    /// a withdrawn player row. No confirm — reversible.
    /// </summary>
    public event Func<SectionViewModel, int /*pairNumber*/, Task>? PlayerUnwithdrawRequested;

    public Task RequestPlayerWithdrawAsync(int pairNumber) =>
        PlayerWithdrawRequested?.Invoke(this, pairNumber) ?? Task.CompletedTask;

    public Task RequestPlayerUnwithdrawAsync(int pairNumber) =>
        PlayerUnwithdrawRequested?.Invoke(this, pairNumber) ?? Task.CompletedTask;

    /// <summary>
    /// Raised when the TD clicks the ✎ byes icon on a player row.
    /// Parent VM opens the <see cref="Views.ManageByesDialog"/>,
    /// diffs the TD's selections against the current state, and
    /// applies the deltas via
    /// <see cref="TournamentMutations.AddRequestedBye"/> /
    /// <see cref="TournamentMutations.RemoveRequestedBye"/>.
    /// </summary>
    public event Func<SectionViewModel, int /*pairNumber*/, Task>? PlayerManageByesRequested;

    public Task RequestPlayerManageByesAsync(int pairNumber) =>
        PlayerManageByesRequested?.Invoke(this, pairNumber) ?? Task.CompletedTask;

    private static PlayerRow BuildPlayerRowStatic(Player player, Section section, IScoreFormatter formatter)
    {
        string status;
        if (player.SoftDeleted)
        {
            status = "Soft-deleted";
        }
        else if (section.IsWithdrawn(player))
        {
            status = "Withdrawn";
        }
        else if (player.RequestedByeRounds.Count > 0 || player.ZeroPointByeRoundsOrEmpty.Count > 0)
        {
            status = "Bye requested";
        }
        else
        {
            status = "Active";
        }

        // Concatenate half and zero point bye requests for the
        // "Requested byes" text column; "2H, 3H, 5U" reads naturally
        // (H = half-point, U = zero-point following SwissSys).
        var byeParts = new System.Collections.Generic.List<string>();
        foreach (var r in player.RequestedByeRounds)         byeParts.Add($"{r}H");
        foreach (var r in player.ZeroPointByeRoundsOrEmpty)  byeParts.Add($"{r}U");
        byeParts.Sort(); // simple lexical sort; round numbers are single-digit for typical events
        var requestedByes = byeParts.Count == 0 ? null : string.Join(", ", byeParts);

        // Separate display strings, one per kind, for the new
        // dedicated columns on the Players tab. Each is a simple
        // comma-separated list of round numbers; null when empty so
        // DataGrid sorting groups empty cells consistently.
        var halfByeRounds = player.RequestedByeRounds.Count == 0
            ? null
            : string.Join(", ", player.RequestedByeRounds.OrderBy(r => r));
        var zeroByeRounds = player.ZeroPointByeRoundsOrEmpty.Count == 0
            ? null
            : string.Join(", ", player.ZeroPointByeRoundsOrEmpty.OrderBy(r => r));

        // Delete / undelete icon visibility. Soft / hard are gated on
        // section.RoundsPaired == 0 (post-round-1 the TD must withdraw
        // instead); the mutations layer enforces the same guard so the
        // domain can't drift. Undelete is always available if the
        // player is soft-deleted.
        var preRoundOne = section.RoundsPaired == 0;
        var isWithdrawn = section.IsWithdrawn(player);
        // Target rounds = the larger of RoundsPaired, FinalRound,
        // RoundsPlayed (matches TargetRounds on the enclosing
        // SectionViewModel). Byes can be requested for any round
        // strictly after RoundsPaired.
        var target = System.Math.Max(System.Math.Max(section.RoundsPaired, section.FinalRound), section.RoundsPlayed);
        var hasFutureRounds = target > section.RoundsPaired;

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
            HalfByeRounds: halfByeRounds,
            ZeroByeRounds: zeroByeRounds,
            Email: player.Email,
            Phone: player.Phone,
            IsSoftDeleted: player.SoftDeleted,
            CanSoftDelete: !player.SoftDeleted && preRoundOne,
            CanUndelete:   player.SoftDeleted,
            CanHardDelete: player.SoftDeleted && preRoundOne,
            IsWithdrawn:   isWithdrawn,
            // Withdraw is the post-round-1 analogue of soft-delete:
            // it removes the player from future pairing but keeps
            // their past game results in place. Only offered when the
            // section has at least one paired round (pre-round-1 the
            // TD should just soft/hard-delete instead).
            CanWithdraw:   !player.SoftDeleted && !isWithdrawn && !preRoundOne,
            CanUnwithdraw: isWithdrawn,
            // Manage byes is offered for any live non-withdrawn player
            // as long as there's at least one unpaired future round
            // to assign a bye for. Pre-round-1 this is effectively
            // always available for active players.
            CanManageByes: !player.SoftDeleted && !isWithdrawn && hasFutureRounds);
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
        ByeKind.Unpaired => "Zero-point bye",
        _ => kind.ToString(),
    };
}
