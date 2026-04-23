using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FreePair.App.Views;

/// <summary>
/// Window that hosts the Publish-online form. The VM lives in
/// <see cref="ViewModels.PublishingDialogViewModel"/> and is wired by
/// the caller (typically <c>TournamentViewModel</c>) before
/// <c>ShowDialog</c> is invoked. Returns the VM on close so the
/// caller can inspect the chosen auto-flags and persist them on the
/// active <c>TournamentViewModel</c>.
/// </summary>
public partial class PublishingDialog : Window
{
    public PublishingDialog()
    {
        InitializeComponent();
    }

    private void OnClose(object? sender, RoutedEventArgs e)
        => Close(DataContext);
}
