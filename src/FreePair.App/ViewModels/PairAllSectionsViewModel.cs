using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// One row in the Pair-all-sections dialog — one per non-soft-
/// deleted section. Status / icon / IsReady are computed from the
/// underlying <see cref="SectionPairingStatus"/> at construction.
/// </summary>
public sealed partial class PairAllSectionRow : ObservableObject
{
    public string Name { get; }
    public int ActivePlayers { get; }
    public int RoundsPaired { get; }
    public int FinalRound { get; }
    public int NextRound { get; }
    public int StartBoard { get; }
    public SectionPairingStatus Status { get; }

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

    public PairAllSectionRow(Section section)
    {
        Name           = section.Name;
        ActivePlayers  = SectionPairingReadiness.ActivePlayerCount(section);
        RoundsPaired   = section.Rounds.Count;
        FinalRound     = section.FinalRound;
        NextRound      = SectionPairingReadiness.NextRoundNumber(section);
        StartBoard     = section.FirstBoard ?? 1;
        Status         = SectionPairingReadiness.Classify(section);
    }
}

/// <summary>
/// Backs <c>PairAllSectionsDialog</c>: surfaces one row per
/// section with its readiness state + start board, plus a count
/// of how many sections will actually be paired by the
/// "Pair ready sections" button.
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

    public PairAllSectionsViewModel(Tournament tournament)
    {
        if (tournament is null) throw new ArgumentNullException(nameof(tournament));
        foreach (var s in tournament.Sections)
        {
            // Soft-deleted sections are hidden so the dialog stays
            // a clean operational view. SectionPairingReadiness
            // returns SoftDeleted, but listing them here would
            // just be noise.
            if (s.SoftDeleted) continue;
            Rows.Add(new PairAllSectionRow(s));
        }
    }

    /// <summary>
    /// Names of the sections the dialog believes can be paired in
    /// a batch run. Caller iterates and invokes its standard
    /// per-section pairing flow on each. Order matches the
    /// tournament's section order so multi-section pairing visits
    /// Open before U1700 etc.
    /// </summary>
    public IReadOnlyList<string> ReadySectionNames =>
        Rows.Where(r => r.IsReady).Select(r => r.Name).ToArray();
}
