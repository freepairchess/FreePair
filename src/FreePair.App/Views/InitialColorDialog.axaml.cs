using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FreePair.Core.Bbp;

namespace FreePair.App.Views;

public partial class InitialColorDialog : Window
{
    private InitialColor _coinTossResult = InitialColor.White;

    public InitialColorDialog()
    {
        InitializeComponent();

        // The XAML pre-checks the "Coin toss" radio, but the IsCheckedChanged
        // event that normally reveals the outcome fires before our field
        // handlers are hooked up. Do an explicit initial roll so the preview
        // panel always shows a valid result.
        _coinTossResult = RollCoin();
        if (PreviewText is not null)
        {
            PreviewText.Text = FormatCoinTossMessage(_coinTossResult);
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

        Close((InitialColor?)result);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close((InitialColor?)null);
    }

    private static InitialColor RollCoin() =>
        Random.Shared.Next(2) == 0 ? InitialColor.White : InitialColor.Black;

    private static string FormatCoinTossMessage(InitialColor result) =>
        $"Coin toss result: the top-seeded player will be assigned {result} on board 1. " +
        $"Select \"Coin toss\" again to re-roll.";
}
