using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// One row of the Manage-requested-byes dialog: a single unpaired
/// future round, with the current (if any) bye kind selected in the
/// <see cref="Choice"/> combobox. The dialog binds to an
/// <see cref="ManageByesViewModel.Rows"/> list of these.
/// </summary>
public partial class ManageByesRow : ObservableObject
{
    public int Round { get; }
    public string RoundLabel { get; }
    public IReadOnlyList<ByeChoiceOption> Options { get; }

    [ObservableProperty]
    private ByeChoiceOption _choice;

    public ManageByesRow(int round, ByeKind? current, IReadOnlyList<ByeChoiceOption> options)
    {
        Round = round;
        RoundLabel = $"Round {round}";
        Options = options;
        _choice = options.FirstOrDefault(o => o.Kind == current) ?? options[0];
    }
}

/// <summary>
/// Selectable item in the Manage-byes row combobox.
/// </summary>
/// <remarks>
/// <see cref="Kind"/> is <c>null</c> for the "No bye" option;
/// <see cref="ByeKind.Half"/> or <see cref="ByeKind.Unpaired"/>
/// otherwise.
/// </remarks>
public sealed record ByeChoiceOption(ByeKind? Kind, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// View-model for the Manage-requested-byes dialog. Seeded with the
/// player's current half-/zero-point bye round lists; the dialog
/// shows one row per unpaired future round, and <see cref="BuildDiffs"/>
/// returns the set of changes the TD picked for the parent view-model
/// to apply via the <c>AddRequestedBye</c> / <c>RemoveRequestedBye</c>
/// mutations.
/// </summary>
public sealed class ManageByesViewModel
{
    public string PlayerLabel { get; }
    public string SectionName { get; }
    public IReadOnlyList<ManageByesRow> Rows { get; }

    // Snapshot of the "before" state so BuildDiffs can compute the
    // minimal set of mutation calls.
    private readonly IReadOnlyDictionary<int, ByeKind?> _initial;

    private static readonly IReadOnlyList<ByeChoiceOption> Options = new[]
    {
        new ByeChoiceOption(null,                 "No bye"),
        new ByeChoiceOption(ByeKind.Half,         "Half-point bye (½)"),
        new ByeChoiceOption(ByeKind.Unpaired,     "Zero-point bye (0)"),
    };

    public ManageByesViewModel(string sectionName, Player player, int targetRounds, int roundsPaired)
    {
        SectionName = sectionName;
        PlayerLabel = $"#{player.PairNumber} {player.Name}";

        var half = new HashSet<int>(player.RequestedByeRounds);
        var zero = new HashSet<int>(player.ZeroPointByeRoundsOrEmpty);

        var rows = new List<ManageByesRow>();
        var initial = new Dictionary<int, ByeKind?>();
        // Offer one row per round that isn't yet paired. Rounds 1..roundsPaired
        // are done and off-limits; rounds roundsPaired+1..targetRounds are fair game.
        for (var r = roundsPaired + 1; r <= targetRounds; r++)
        {
            ByeKind? current =
                half.Contains(r) ? ByeKind.Half :
                zero.Contains(r) ? ByeKind.Unpaired :
                null;
            rows.Add(new ManageByesRow(r, current, Options));
            initial[r] = current;
        }

        Rows = rows;
        _initial = initial;
    }

    /// <summary>
    /// Returns the per-round delta between the dialog's seeded state
    /// and the current selections. <c>null</c> means "remove any
    /// existing bye request for this round"; otherwise the new kind
    /// to <see cref="TournamentMutations.AddRequestedBye"/>.
    /// </summary>
    public IReadOnlyList<(int Round, ByeKind? NewKind)> BuildDiffs()
    {
        var diffs = new List<(int, ByeKind?)>();
        foreach (var row in Rows)
        {
            var before = _initial[row.Round];
            var after  = row.Choice.Kind;
            if (before != after)
            {
                diffs.Add((row.Round, after));
            }
        }
        return diffs;
    }
}
