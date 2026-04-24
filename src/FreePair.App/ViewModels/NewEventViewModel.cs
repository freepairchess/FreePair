using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// View-model for the "Create new event" dialog. Collects the bare
/// minimum a tournament needs to start: an event title and a single
/// first section (name, kind, round count). The TD can add more
/// sections / players / overview metadata afterward via the usual
/// Add-section / event-config flows.
/// </summary>
public partial class NewEventViewModel : ObservableObject
{
    public IReadOnlyList<SectionKindOption> KindOptions { get; } = new[]
    {
        new SectionKindOption(SectionKind.Swiss,      "Swiss"),
        new SectionKindOption(SectionKind.RoundRobin, "Round robin"),
    };

    [ObservableProperty] private string _eventTitle = string.Empty;
    [ObservableProperty] private string _firstSectionName = "Open";
    [ObservableProperty] private SectionKindOption _firstSectionKind;
    [ObservableProperty] private string _firstSectionRoundsText = "5";
    [ObservableProperty] private string? _firstSectionTimeControl;
    [ObservableProperty] private string? _errorMessage;

    public NewEventViewModel()
    {
        _firstSectionKind = KindOptions[0];
    }

    /// <summary>
    /// Validates title non-empty and the rounds text parses to a
    /// positive int. Section name is allowed blank — if blank we
    /// don't create a starter section, and the TD adds one via
    /// "➕ Add section" later.
    /// </summary>
    public bool TryValidate(out int firstSectionRounds)
    {
        firstSectionRounds = 0;
        if (string.IsNullOrWhiteSpace(EventTitle))
        {
            ErrorMessage = "Event title is required.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(FirstSectionName))
        {
            if (!int.TryParse(FirstSectionRoundsText, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out firstSectionRounds) || firstSectionRounds < 1)
            {
                ErrorMessage = "First section rounds must be a positive whole number.";
                return false;
            }
        }

        ErrorMessage = null;
        return true;
    }
}
