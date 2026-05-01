using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FreePair.App.Views;

/// <summary>
/// Pre-pairing prompt that lets the TD confirm or override the
/// physical starting board number for the next round of one
/// section. Pre-fills with the section's current
/// <c>FirstBoard</c>; offers a "Use recommended" button that
/// jumps to <see cref="BoardNumberRecommender"/>'s suggestion.
/// Returns the chosen value via <see cref="Window.Close(object?)"/>;
/// <c>null</c> on Cancel so the caller can abort pairing.
/// </summary>
public partial class StartingBoardDialog : Window
{
    private readonly int _recommended;

    public StartingBoardDialog()
    {
        InitializeComponent();
        _recommended = 1;
    }

    public StartingBoardDialog(string sectionName, int roundNumber, int current, int recommended) : this()
    {
        _recommended = recommended;
        HeaderText.Text       = $"Pair round {roundNumber} for \"{sectionName}\"";
        BoardSpinner.Value    = current;
        RecommendedText.Text  = recommended.ToString();
    }

    private void OnUseRecommended(object? sender, RoutedEventArgs e)
    {
        BoardSpinner.Value = _recommended;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        var v = BoardSpinner.Value;
        // NumericUpDown stores decimal; clamp to the spinner's
        // declared range so we never return junk.
        var asInt = v is null ? _recommended : (int)System.Math.Clamp((decimal)v, 1m, 9999m);
        Close((int?)asInt);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) =>
        Close((int?)null);
}
