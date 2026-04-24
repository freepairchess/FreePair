using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FreePair.Core.Tournaments;

namespace FreePair.App.ViewModels;

/// <summary>
/// One row of the "Byes for past rounds" section on the Add-player
/// form. Each row is one already-paired round; the TD picks a
/// <see cref="ByeKind"/> for the new player.
/// </summary>
public partial class PastRoundByeRow : ObservableObject
{
    public int Round { get; }
    public string RoundLabel { get; }
    public IReadOnlyList<ByeChoiceOption> Options { get; }

    [ObservableProperty]
    private ByeChoiceOption _choice;

    public PastRoundByeRow(int round, IReadOnlyList<ByeChoiceOption> options, ByeChoiceOption initial)
    {
        Round = round;
        RoundLabel = $"Round {round}";
        Options = options;
        _choice = initial;
    }
}

/// <summary>
/// View-model for the player form dialog used by both
/// <see cref="Views.PlayerFormDialog"/> edit and add flows. Exposes
/// every editable identity / contact field as an
/// <see cref="ObservableProperty"/>; the parent view-model reads the
/// final values off on Save and feeds them through the matching
/// <see cref="TournamentMutations.UpdatePlayerInfo"/> /
/// <c>AddPlayer</c> mutation.
/// </summary>
public partial class PlayerFormViewModel : ObservableObject
{
    public string Title { get; }
    public string ConfirmLabel { get; }
    public string SectionName { get; }

    /// <summary>
    /// Pair number displayed in the header (for the edit case) and
    /// used by the calling handler to target the mutation. For a new
    /// player the caller typically seeds this with the next-available
    /// number or a placeholder string.
    /// </summary>
    public string HeaderLabel { get; }

    /// <summary>
    /// Per-past-round bye rows. Empty for the edit flow and for the
    /// add flow when the section has no paired rounds. When
    /// non-empty, the dialog surfaces a "Byes for past rounds"
    /// section below the identity fields.
    /// </summary>
    public IReadOnlyList<PastRoundByeRow> PastRoundByes { get; init; } = System.Array.Empty<PastRoundByeRow>();

    /// <summary>True when the past-round byes section should be visible.</summary>
    public bool HasPastRoundByes => PastRoundByes.Count > 0;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _uscfId;
    [ObservableProperty] private string _ratingText = "0";
    [ObservableProperty] private string? _secondaryRatingText;
    [ObservableProperty] private string? _membershipExpiration;
    [ObservableProperty] private string? _club;
    [ObservableProperty] private string? _state;
    [ObservableProperty] private string? _team;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _phone;

    [ObservableProperty] private string? _errorMessage;

    public PlayerFormViewModel(string title, string confirmLabel, string sectionName, string headerLabel)
    {
        Title = title;
        ConfirmLabel = confirmLabel;
        SectionName = sectionName;
        HeaderLabel = headerLabel;
    }

    /// <summary>
    /// Seeds the form fields from an existing <see cref="Player"/>.
    /// Used by the edit flow.
    /// </summary>
    public static PlayerFormViewModel ForEdit(string sectionName, Player player) =>
        new("Edit player", "Save", sectionName, $"#{player.PairNumber} {player.Name}")
        {
            Name = player.Name,
            UscfId = player.UscfId,
            RatingText = player.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SecondaryRatingText = player.SecondaryRating?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            MembershipExpiration = player.MembershipExpiration,
            Club = player.Club,
            State = player.State,
            Team = player.Team,
            Email = player.Email,
            Phone = player.Phone,
        };

    /// <summary>
    /// Builds a blank form for adding a new player to
    /// <paramref name="sectionName"/>. If the section has already
    /// paired rounds, one <see cref="PastRoundByeRow"/> per round is
    /// seeded with <see cref="ByeKind.Unpaired"/> (zero-point bye)
    /// as the default — safest for a late entry.
    /// </summary>
    public static PlayerFormViewModel ForAdd(string sectionName, int nextPairNumber, int roundsPaired)
    {
        var options = new[]
        {
            new ByeChoiceOption(ByeKind.Unpaired, "Zero-point bye (0)"),
            new ByeChoiceOption(ByeKind.Half,     "Half-point bye (½)"),
            new ByeChoiceOption(ByeKind.Full,     "Full-point bye (1)"),
        };
        var zeroPtDefault = options[0];

        var rows = new List<PastRoundByeRow>(roundsPaired);
        for (var r = 1; r <= roundsPaired; r++)
        {
            rows.Add(new PastRoundByeRow(r, options, zeroPtDefault));
        }

        return new PlayerFormViewModel(
            title: "Add player",
            confirmLabel: "Add",
            sectionName: sectionName,
            headerLabel: $"New player (will be assigned pair #{nextPairNumber})")
        {
            PastRoundByes = rows,
        };
    }

    /// <summary>
    /// Reads the TD's per-past-round bye selections back into a
    /// dictionary for the <see cref="TournamentMutations.AddPlayer"/>
    /// call. Empty when the form is in edit mode or the section had
    /// no paired rounds.
    /// </summary>
    public IReadOnlyDictionary<int, ByeKind> CollectByesForPastRounds()
    {
        var dict = new Dictionary<int, ByeKind>();
        foreach (var row in PastRoundByes)
        {
            if (row.Choice.Kind is ByeKind kind)
            {
                dict[row.Round] = kind;
            }
        }
        return dict;
    }

    /// <summary>
    /// Validates the required fields (Name non-empty; Rating parseable
    /// non-negative int; Secondary rating either null/empty or a
    /// parseable int). Sets <see cref="ErrorMessage"/> for the first
    /// failure and returns false; otherwise returns true.
    /// </summary>
    public bool TryValidate(out int rating, out int? secondaryRating)
    {
        rating = 0;
        secondaryRating = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Name is required.";
            return false;
        }

        if (!int.TryParse(RatingText, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out rating) || rating < 0)
        {
            ErrorMessage = "Rating must be a non-negative whole number.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SecondaryRatingText))
        {
            if (!int.TryParse(SecondaryRatingText, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out var sr) || sr < 0)
            {
                ErrorMessage = "Secondary rating must be a non-negative whole number or blank.";
                return false;
            }
            secondaryRating = sr;
        }

        ErrorMessage = null;
        return true;
    }
}
