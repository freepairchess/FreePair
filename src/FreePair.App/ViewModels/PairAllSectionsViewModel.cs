using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// One row in the Pair-all-sections dialog — one per non-soft-
/// deleted section. Status / icon / IsReady are computed from the
/// underlying <see cref="SectionPairingStatus"/> at construction.
/// Start board is editable (NumericUpDown) so the TD can adjust
/// board layout in the same dashboard they kick off batch pairing
/// from; the parent VM recomputes overlap warnings on every edit.
/// </summary>
public sealed partial class PairAllSectionRow : ObservableObject
{
    public string Name { get; }
    public int ActivePlayers { get; }
    public int RoundsPaired { get; }
    public int FinalRound { get; }
    public int NextRound { get; }
    public int MaxBoards { get; }
    public int Recommended { get; }

    /// <summary>The section's persisted <see cref="Section.FirstBoard"/> at dialog open (or 1 if unset).</summary>
    public int CurrentFirstBoard { get; }

    public SectionPairingStatus Status { get; }

    /// <summary>
    /// Editable starting board for this section. Bound to a
    /// NumericUpDown in the dialog; changes re-trigger conflict
    /// computation on the parent VM.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndBoard))]
    [NotifyPropertyChangedFor(nameof(BoardRangeText))]
    private int _startingBoard;

    /// <summary>Last board this section will occupy = <c>StartingBoard + MaxBoards - 1</c> (or <c>StartingBoard</c> when MaxBoards is 0).</summary>
    public int EndBoard => MaxBoards == 0 ? StartingBoard : StartingBoard + MaxBoards - 1;

    /// <summary>Display string for the boards column ("1 – 25").</summary>
    public string BoardRangeText => MaxBoards == 0
        ? "—"
        : $"{StartingBoard} – {EndBoard}";

    /// <summary>True when this row's range overlaps another row's range. Set by parent VM.</summary>
    [ObservableProperty] private bool _hasConflict;

    /// <summary>Tooltip / inline message describing what this row conflicts with. Empty when no conflict.</summary>
    [ObservableProperty] private string _conflictMessage = string.Empty;

    /// <summary>
    /// True when this section will be paired by Pair ready sections.
    /// Maps from <see cref="SectionPairingStatus.ReadyToPair"/> only.
    /// </summary>
    public bool IsReady => Status == SectionPairingStatus.ReadyToPair;

    /// <summary>
    /// "1 / 4" style progress label. Final round defaults to "?"
    /// when the source data has no planned round count.
    /// </summary>
    public string ProgressText =>
        FinalRound > 0 ? $"{RoundsPaired} / {FinalRound}" : $"{RoundsPaired} / ?";

    /// <summary>Single-glyph status icon shown in the dialog row.</summary>
    public string StatusIcon => Status switch
    {
        SectionPairingStatus.ReadyToPair         => "✓",
        SectionPairingStatus.WaitingForResults   => "⏳",
        SectionPairingStatus.AllRoundsPaired     => "🏁",
        SectionPairingStatus.InsufficientPlayers => "⚠",
        _                                        => "•",
    };

    /// <summary>Friendly status sentence for the dialog row.</summary>
    public string StatusText => Status switch
    {
        SectionPairingStatus.ReadyToPair when RoundsPaired == 0
            => $"Ready to pair round 1",
        SectionPairingStatus.ReadyToPair
            => $"Ready to pair round {NextRound}",
        SectionPairingStatus.WaitingForResults
            => $"Waiting for round {RoundsPaired} results",
        SectionPairingStatus.AllRoundsPaired
            => "All rounds paired",
        SectionPairingStatus.InsufficientPlayers when ActivePlayers == 0
            => "No active players",
        SectionPairingStatus.InsufficientPlayers
            => "Need at least 2 active players",
        _ => "—",
    };

    public PairAllSectionRow(Section section, int recommended)
    {
        Name              = section.Name;
        ActivePlayers     = SectionPairingReadiness.ActivePlayerCount(section);
        RoundsPaired      = section.Rounds.Count;
        FinalRound        = section.FinalRound;
        NextRound         = SectionPairingReadiness.NextRoundNumber(section);
        MaxBoards         = BoardNumberRecommender.MaxBoardsNeeded(section);
        Recommended       = recommended;
        CurrentFirstBoard = section.FirstBoard ?? 1;
        Status            = SectionPairingReadiness.Classify(section);
        _startingBoard    = CurrentFirstBoard;
    }
}

/// <summary>
/// Backs <c>PairAllSectionsDialog</c>: surfaces one row per
/// section with its readiness state + editable start board, plus a
/// count of how many sections will actually be paired by the
/// "Pair ready sections" button. Conflict detection mirrors the
/// Renumber-boards dialog so TDs can adjust layout right here
/// before kicking off batch pairing.
/// </summary>
public sealed partial class PairAllSectionsViewModel : ViewModelBase
{
    public ObservableCollection<PairAllSectionRow> Rows { get; } = new();

    public int ReadyCount => Rows.Count(r => r.IsReady);
    public int TotalCount => Rows.Count;

    public string Summary =>
        ReadyCount == 0
            ? "No sections are ready to pair right now."
            : ReadyCount == 1
                ? "1 section is ready to pair."
                : $"{ReadyCount} sections are ready to pair.";

    /// <summary>True when at least one row's range overlaps another. Drives the warning footer in the view.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConflictSummary))]
    private bool _hasAnyConflict;

    public string ConflictSummary => HasAnyConflict
        ? "⚠ Some sections' board ranges overlap. This is fine if those sections will play in different rooms; otherwise edit the starting numbers below."
        : "No board-range conflicts detected.";

    public PairAllSectionsViewModel(Tournament tournament)
    {
        if (tournament is null) throw new ArgumentNullException(nameof(tournament));
        var recommended = BoardNumberRecommender.Recommend(tournament);
        foreach (var s in tournament.Sections)
        {
            // Soft-deleted sections are hidden so the dialog stays
            // a clean operational view.
            if (s.SoftDeleted) continue;
            var rec = recommended.TryGetValue(s.Name, out var r) ? r : 1;
            var row = new PairAllSectionRow(s, rec);
            row.PropertyChanged += OnRowChanged;
            Rows.Add(row);
        }
        RecomputeConflicts();
    }

    private void OnRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PairAllSectionRow.StartingBoard))
        {
            RecomputeConflicts();
        }
    }

    /// <summary>
    /// Rebuilds <see cref="PairAllSectionRow.HasConflict"/> /
    /// <see cref="PairAllSectionRow.ConflictMessage"/> across every
    /// row. Two rows conflict when their inclusive board ranges
    /// overlap. Sections with zero boards needed never participate.
    /// </summary>
    public void RecomputeConflicts()
    {
        foreach (var row in Rows)
        {
            row.HasConflict = false;
            row.ConflictMessage = string.Empty;
        }

        var any = false;
        for (var i = 0; i < Rows.Count; i++)
        {
            var a = Rows[i];
            if (a.MaxBoards == 0) continue;
            var conflicts = new List<string>();
            for (var j = 0; j < Rows.Count; j++)
            {
                if (i == j) continue;
                var b = Rows[j];
                if (b.MaxBoards == 0) continue;
                if (a.StartingBoard <= b.EndBoard && b.StartingBoard <= a.EndBoard)
                {
                    conflicts.Add($"{b.Name} ({b.StartingBoard}–{b.EndBoard})");
                }
            }
            if (conflicts.Count > 0)
            {
                a.HasConflict = true;
                a.ConflictMessage = "Overlaps with: " + string.Join(", ", conflicts);
                any = true;
            }
        }
        HasAnyConflict = any;
    }

    /// <summary>
    /// Cascades each row's <see cref="PairAllSectionRow.StartingBoard"/>
    /// to the BoardNumberRecommender's suggestion (sections stack
    /// with no overlap). TD can still tweak individual rows after.
    /// </summary>
    [RelayCommand]
    private void UseRecommended()
    {
        var nextBoard = 1;
        foreach (var row in Rows)
        {
            row.StartingBoard = nextBoard;
            if (row.MaxBoards > 0) nextBoard += row.MaxBoards;
        }
        RecomputeConflicts();
    }

    /// <summary>
    /// Names of the sections the dialog believes can be paired in
    /// a batch run. Order matches the tournament's section order.
    /// </summary>
    public IReadOnlyList<string> ReadySectionNames =>
        Rows.Where(r => r.IsReady).Select(r => r.Name).ToArray();

    /// <summary>
    /// Per-section StartingBoard values the TD chose, keyed by
    /// section name. Caller applies via
    /// <see cref="TournamentMutations.SetSectionFirstBoard"/>
    /// before kicking off batch pairing so each section's pairings
    /// land at the chosen offset.
    /// </summary>
    public IReadOnlyDictionary<string, int> SnapshotChosenStartingBoards()
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in Rows) dict[row.Name] = row.StartingBoard;
        return dict;
    }
}
