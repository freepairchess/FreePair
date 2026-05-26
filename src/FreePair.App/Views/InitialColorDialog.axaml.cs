using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.Core.Bbp;

namespace FreePair.App.Views;

/// <summary>
/// Result returned by <see cref="InitialColorDialog"/>: the chosen
/// initial colour <em>and</em> whether the TD toggled the avoid-same-team
/// constraint during the prompt.
/// </summary>
public sealed record InitialColorResult(InitialColor Color, bool AvoidSameTeam);

public partial class InitialColorDialog : Window
{
    private InitialColor _coinTossResult = InitialColor.White;

    /// <summary>
    /// Set before <see cref="ShowDialog{TResult}"/> to seed the
    /// checkbox state from the section's current value.
    /// </summary>
    public bool AvoidSameTeam { get; set; }

    public InitialColorDialog()
    {
        InitializeComponent();

        _coinTossResult = RollCoin();
        if (PreviewText is not null)
        {
            PreviewText.Text = FormatCoinTossMessage(_coinTossResult);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (AvoidSameTeamCheck is not null)
        {
            AvoidSameTeamCheck.IsChecked = AvoidSameTeam;
        }
    }

    private void OnSelectionChanged(object? sender, RoutedEventArgs e)
    {
        // IsCheckedChanged also fires when a radio is *un*-checked; only
        // act when the firing radio has transitioned to Checked.
        if (sender is RadioButton rb && rb.IsChecked != true)
        {
            return;
        }

        if (PreviewText is null)
        {
            return;
        }

        if (TopWhiteRadio.IsChecked == true)
        {
            PreviewText.Text = "The top-seeded player will be assigned White on board 1.";
        }
        else if (TopBlackRadio.IsChecked == true)
        {
            PreviewText.Text = "The top-seeded player will be assigned Black on board 1.";
        }
        else if (CoinTossRadio.IsChecked == true)
        {
            _coinTossResult = RollCoin();
            PreviewText.Text = FormatCoinTossMessage(_coinTossResult);
        }
    }

    private void OnPair(object? sender, RoutedEventArgs e)
    {
        InitialColor result;
        if (TopWhiteRadio.IsChecked == true)
        {
            result = InitialColor.White;
        }
        else if (TopBlackRadio.IsChecked == true)
        {
            result = InitialColor.Black;
        }
        else
        {
            result = _coinTossResult;
        }

        var avoidTeam = AvoidSameTeamCheck?.IsChecked == true;
        Close(new InitialColorResult(result, avoidTeam));
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close((InitialColorResult?)null);
    }

    private static InitialColor RollCoin() =>
        Random.Shared.Next(2) == 0 ? InitialColor.White : InitialColor.Black;

    private static string FormatCoinTossMessage(InitialColor result) =>
        $"Coin toss result: the top-seeded player will be assigned {result} on board 1. " +
        $"Select \"Coin toss\" again to re-roll.";
}
