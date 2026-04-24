using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// Selectable item in the section-form Kind combobox.
/// </summary>
public sealed record SectionKindOption(SectionKind Kind, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// View-model for the section form dialog. Minimal scope: just the
/// fields the TD needs to seed a new section (name, kind, final
/// round, optional time control + title). On Save, the caller reads
/// the validated fields and dispatches to
/// <see cref="TournamentMutations.AddSection"/>.
/// </summary>
public partial class SectionFormViewModel : ObservableObject
{
    public string Title { get; }
    public string ConfirmLabel { get; }
    public string HeaderLabel { get; }

    public IReadOnlyList<SectionKindOption> KindOptions { get; } = new[]
    {
        new SectionKindOption(SectionKind.Swiss,      "Swiss"),
        new SectionKindOption(SectionKind.RoundRobin, "Round robin"),
    };

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _sectionTitle;
    [ObservableProperty] private SectionKindOption _kind;
    [ObservableProperty] private string _finalRoundText = "5";
    [ObservableProperty] private string? _timeControl;
    [ObservableProperty] private string? _errorMessage;

    public SectionFormViewModel(string title, string confirmLabel, string headerLabel)
    {
        Title = title;
        ConfirmLabel = confirmLabel;
        HeaderLabel = headerLabel;
        _kind = KindOptions[0];
    }

    /// <summary>Seeds a blank form for the Add flow.</summary>
    public static SectionFormViewModel ForAdd(string tournamentTitle) =>
        new("Add section", "Add", $"New section — {tournamentTitle}");

    /// <summary>
    /// Validates <see cref="Name"/> non-empty and
    /// <see cref="FinalRoundText"/> parses to a positive int. Sets
    /// <see cref="ErrorMessage"/> on failure.
    /// </summary>
    public bool TryValidate(out int finalRound)
    {
        finalRound = 0;
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Section name is required.";
            return false;
        }
        if (!int.TryParse(FinalRoundText, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out finalRound) || finalRound < 1)
        {
            ErrorMessage = "Final round must be a positive whole number.";
            return false;
        }
        ErrorMessage = null;
        return true;
    }
}
