using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FreePair.App.ViewModels;
using FreePair.Core.Bbp;

namespace FreePair.App.Views;

public partial class TournamentView : UserControl
{
    public TournamentView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is TournamentViewModel vm)
        {
            vm.PickTournamentFileAsync = PickTournamentFileAsync;
            vm.PickExportTrfPathAsync = PickExportTrfPathAsync;
            vm.PromptInitialColorAsync = PromptInitialColorAsync;
            vm.PromptConfirmAsync = PromptConfirmAsync;
            vm.PromptPairingPreviewAsync = PromptPairingPreviewAsync;
            vm.ShowPublishingDialogAsync = ShowPublishingDialogAsync;
            vm.ShowManageByesDialogAsync = ShowManageByesDialogAsync;
            vm.ShowPlayerFormDialogAsync = ShowPlayerFormDialogAsync;
            vm.ShowSectionFormDialogAsync = ShowSectionFormDialogAsync;
        }
    }

    private async Task<string?> PickTournamentFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = "Open SwissSys tournament",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("SwissSys tournament")
                {
                    Patterns = new[] { "*.sjson" }
                },
                FilePickerFileTypes.All
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            return null;
        }

        return files[0].TryGetLocalPath();
    }

    private async Task<string?> PickExportTrfPathAsync(string suggestedName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        var options = new FilePickerSaveOptions
        {
            Title = "Export section as TRF",
            SuggestedFileName = suggestedName,
            DefaultExtension = "trf",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("FIDE Tournament Report File (TRF)")
                {
                    Patterns = new[] { "*.trf" }
                },
                FilePickerFileTypes.All
            }
        };

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
        return file?.TryGetLocalPath();
    }

    private async Task<InitialColor?> PromptInitialColorAsync()
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            // No owner window (e.g. running in design-time) — default silently.
            return InitialColor.White;
        }

        var dialog = new InitialColorDialog();
        return await dialog.ShowDialog<InitialColor?>(owner);
    }

    private async Task<bool> PromptConfirmAsync(string title, string message, string confirmLabel)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return false;
        }

        var dialog = new ConfirmDialog();
        dialog.Configure(title, message, confirmLabel);
        var result = await dialog.ShowDialog<bool?>(owner);
        return result == true;
    }

    private async Task<FreePair.Core.Tournaments.Tournament?> PromptPairingPreviewAsync(
        FreePair.Core.Tournaments.Tournament tournament,
        string sectionName,
        int round,
        System.Collections.Generic.IReadOnlyList<string> conflicts)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            // No owner window (design-time): commit without preview.
            return tournament;
        }

        var dialog = new PairingPreviewDialog();
        dialog.Configure(new PairingPreviewViewModel(tournament, sectionName, round, conflicts));
        return await dialog.ShowDialog<FreePair.Core.Tournaments.Tournament?>(owner);
    }

    /// <summary>
    /// Shows the Publish-online dialog modally against the owning
    /// window. Returns the VM on close so the caller can read the
    /// edited URL + auto-flags + passcode, or null on a force-close.
    /// </summary>
    private async Task<PublishingDialogViewModel?> ShowPublishingDialogAsync(
        PublishingDialogViewModel vm)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return null;
        }

        var dialog = new PublishingDialog { DataContext = vm };
        var result = await dialog.ShowDialog<object?>(owner);
        return result as PublishingDialogViewModel;
    }

    /// <summary>
    /// Shows the Manage-requested-byes dialog modally. Returns the
    /// VM on Save (so the caller can read BuildDiffs), or null on
    /// Cancel / force-close.
    /// </summary>
    private async Task<ManageByesViewModel?> ShowManageByesDialogAsync(ManageByesViewModel vm)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return null;
        }

        var dialog = new ManageByesDialog(vm);
        return await dialog.ShowDialog<ManageByesViewModel?>(owner);
    }

    /// <summary>
    /// Shows the Player form dialog modally (edit or add). Returns
    /// the VM on Save, null on Cancel.
    /// </summary>
    private async Task<PlayerFormViewModel?> ShowPlayerFormDialogAsync(PlayerFormViewModel vm)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return null;
        }

        var dialog = new PlayerFormDialog(vm);
        return await dialog.ShowDialog<PlayerFormViewModel?>(owner);
    }

    /// <summary>
    /// Shows the Section form dialog modally (add flow). Returns the
    /// VM on Save, null on Cancel.
    /// </summary>
    private async Task<SectionFormViewModel?> ShowSectionFormDialogAsync(SectionFormViewModel vm)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return null;
        }

        var dialog = new SectionFormDialog(vm);
        return await dialog.ShowDialog<SectionFormViewModel?>(owner);
    }

    /// <summary>
    /// Shows the New-event dialog modally. Returns the VM on Create,
    /// null on Cancel.
    /// </summary>
    private async Task<NewEventViewModel?> ShowNewEventDialogAsync(NewEventViewModel vm)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return null;
        }

        var dialog = new NewEventDialog(vm);
        return await dialog.ShowDialog<NewEventViewModel?>(owner);
    }

    /// <summary>
    /// Save-file picker scoped to <c>.sjson</c> for the New-event
    /// flow. Returns the local path or null on cancel.
    /// </summary>
    private async Task<string?> PickNewEventSavePathAsync(string suggestedName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        var options = new FilePickerSaveOptions
        {
            Title = "Save new tournament as",
            SuggestedFileName = suggestedName,
            DefaultExtension = "sjson",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("SwissSys tournament")
                {
                    Patterns = new[] { "*.sjson" }
                },
                FilePickerFileTypes.All
            }
        };

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
        return file?.TryGetLocalPath();
    }

    /// <summary>
    /// Opens the hub's EventFiles page for the last successful
    /// publish in the default browser. URL is bound to the clicked
    /// button's <c>Tag</c> via <c>TournamentViewModel.LastPublishedUrl</c>.
    /// </summary>
    private void OnOpenPublishedUrl(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string url || string.IsNullOrWhiteSpace(url))
            return;
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { /* best-effort */ }
    }
}
