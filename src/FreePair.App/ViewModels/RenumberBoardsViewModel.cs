using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// One editable row in <see cref="RenumberBoardsViewModel"/> —
/// represents one section's prospective starting board, and the
/// derived end-board / conflict information the dialog uses to
/// warn the TD about overlapping board ranges.
/// </summary>
public sealed partial class RenumberBoardRow : ObservableObject
{
    /// <summary>Section name (read-only).</summary>
    public string Name { get; }

    /// <summary>Active (non-soft-deleted, non-withdrawn) player count.</summary>
    public int PlayerCount { get; }

    /// <summary>Worst-case board count needed = <c>ceil(activePlayers / 2)</c>.</summary>
    public int MaxBoards { get; }

    /// <summary>BoardNumberRecommender's suggestion if this section started after every prior section.</summary>
    public int Recommended { get; }

    /// <summary>The section's current persisted <see cref="Section.FirstBoard"/> (or 1 if unset).</summary>
    public int CurrentFirstBoard { get; }

    /// <summary>
    /// Editable starting board for this section. Bound to a
    /// NumericUpDown in the dialog. Changing this re-triggers
    /// conflict computation on the parent VM.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndBoard))]
    [NotifyPropertyChangedFor(nameof(BoardRangeText))]
    private int _startingBoard;

    /// <summary>Last board this section will occupy = <c>StartingBoard + MaxBoards - 1</c> (or <c>StartingBoard</c> when MaxBoards is 0).</summary>
    public int EndBoard => MaxBoards == 0 ? StartingBoard : StartingBoard + MaxBoards - 1;

    /// <summary>Display string for the boards column ("1 – 25" / "26 – 40").</summary>
    public string BoardRangeText => MaxBoards == 0
        ? "(no boards needed)"
        : $"{StartingBoard} – {EndBoard}";

    /// <summary>True when this section's range overlaps another section's range. Set by parent VM.</summary>
    [ObservableProperty] private bool _hasConflict;

    /// <summary>Tooltip / inline message describing what this row conflicts with. Empty when no conflict.</summary>
    [ObservableProperty] private string _conflictMessage = string.Empty;

    public RenumberBoardRow(
        string name,
        int playerCount,
        int maxBoards,
        int recommended,
        int currentFirstBoard,
        int initialStart)
    {
        Name              = name;
        PlayerCount       = playerCount;
        MaxBoards         = maxBoards;
        Recommended       = recommended;
        CurrentFirstBoard = currentFirstBoard;
        _startingBoard    = initialStart;
    }
}

/// <summary>
/// Backs <c>RenumberBoardsDialog</c>: an interactive review dialog
/// the TD opens via the 🔢 Renumber boards toolbar button. Each
/// section gets a row with player count, max boards needed,
/// editable starting-board field, end-of-range, and a non-blocking
/// conflict warning when ranges overlap (multi-room events
/// intentionally overlap so this is a warning, not an error).
/// </summary>
public sealed partial class RenumberBoardsViewModel : ViewModelBase
{
    public ObservableCollection<RenumberBoardRow> Rows { get; } = new();

    /// <summary>Becomes true when at least one row's range overlaps another. Drives the warning footer in the view.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConflictSummary))]
    private bool _hasAnyConflict;

    public string ConflictSummary => HasAnyConflict
        ? "⚠ Some sections' board ranges overlap. This is fine if those sections will play in different rooms; otherwise edit the starting numbers below."
        : "No board-range conflicts detected.";

    public RenumberBoardsViewModel(Tournament tournament)
    {
        if (tournament is null) throw new ArgumentNullException(nameof(tournament));

        var recommended = BoardNumberRecommender.Recommend(tournament);
        foreach (var section in tournament.Sections)
        {
            if (section.SoftDeleted) continue;
            var maxBoards = BoardNumberRecommender.MaxBoardsNeeded(section);
            var rec       = recommended.TryGetValue(section.Name, out var r) ? r : 1;
            var current   = section.FirstBoard ?? 1;
            // Pre-fill with the section's current FirstBoard so prior
            // TD edits aren't silently overwritten. The dialog's
            // "Use recommended" button cascades to the recommender if
            // they want the auto-layout.
            var initial = current;
            var active = section.Players.Count(p => !p.SoftDeleted && !p.Withdrawn);
            var row = new RenumberBoardRow(section.Name, active, maxBoards, rec, current, initial);
            row.PropertyChanged += OnRowChanged;
            Rows.Add(row);
        }
        RecomputeConflicts();
    }

    private void OnRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RenumberBoardRow.StartingBoard))
        {
            RecomputeConflicts();
        }
    }

    /// <summary>
    /// Rebuilds <see cref="RenumberBoardRow.HasConflict"/> /
    /// <see cref="RenumberBoardRow.ConflictMessage"/> across every
    /// row. Two rows conflict when their inclusive board ranges
    /// overlap. Sections with zero boards needed (empty / all-
    /// withdrawn) never participate in a conflict.
    /// </summary>
    public void RecomputeConflicts()
    {
        // Reset.
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
                // Inclusive-range overlap: aStart <= bEnd && bStart <= aEnd.
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
    /// Cascades each row's <see cref="RenumberBoardRow.StartingBoard"/>
    /// to the BoardNumberRecommender's suggestion, walking sections
    /// in order and stacking each one's range after the previous.
    /// Same effect as the legacy one-click "Renumber" button — but
    /// the TD can still tweak individual rows after.
    /// </summary>
    [RelayCommand]
    private void UseRecommended()
    {
        var nextBoard = 1;
        foreach (var row in Rows)
        {
            row.StartingBoard = nextBoard;
            // Sections with zero boards needed don't bump the next-
            // board cursor; the spinner just shows their idle start.
            if (row.MaxBoards > 0) nextBoard += row.MaxBoards;
        }
        RecomputeConflicts();
    }

    /// <summary>
    /// Captures the per-section StartingBoard values the TD chose,
    /// keyed by section name, so the caller can apply them via
    /// <see cref="TournamentMutations.SetSectionFirstBoard"/>.
    /// </summary>
    public IReadOnlyDictionary<string, int> SnapshotChosenStartingBoards()
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in Rows) dict[row.Name] = row.StartingBoard;
        return dict;
    }
}
