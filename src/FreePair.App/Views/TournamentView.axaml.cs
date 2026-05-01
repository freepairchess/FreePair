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
            vm.PromptStartingBoardAsync = PromptStartingBoardAsync;
            vm.PromptPairingPreviewAsync = PromptPairingPreviewAsync;
            vm.ShowPublishingDialogAsync = ShowPublishingDialogAsync;
            vm.ShowManageByesDialogAsync = ShowManageByesDialogAsync;
            vm.ShowPlayerFormDialogAsync = ShowPlayerFormDialogAsync;
            vm.ShowSectionFormDialogAsync = ShowSectionFormDialogAsync;
            vm.ShowNewEventDialogAsync = ShowNewEventDialogAsync;
            vm.PickNewEventSavePathAsync = PickNewEventSavePathAsync;
            vm.PickPlayerImportFileAsync = PickPlayerImportFileAsync;
            vm.ShowOpenFromRegistryDialogAsync = ShowOpenFromRegistryDialogAsync;
            vm.ShowBrowseRegistryEventsDialogAsync = ShowBrowseRegistryEventsDialogAsync;
            vm.ShowUscfExportDialogAsync = ShowUscfExportDialogAsync;
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

    private async Task<int?> PromptStartingBoardAsync(string sectionName, int roundNumber, int current, int recommended)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            // No owner window (designer / smoke-test) — pass through
            // the current value so pairing proceeds unchanged.
            return current;
        }

        var dialog = new StartingBoardDialog(sectionName, roundNumber, current, recommended);
        return await dialog.ShowDialog<int?>(owner);
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
    /// Open-file picker scoped to roster formats we can import.
    /// Returns the local path or null on cancel.
    /// </summary>
    private async Task<string?> PickPlayerImportFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = "Import players from...",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Roster files (CSV / TSV / Excel)")
                {
                    Patterns = new[] { "*.csv", "*.tsv", "*.txt", "*.xlsx" }
                },
                new FilePickerFileType("CSV (comma-separated)")  { Patterns = new[] { "*.csv" } },
                new FilePickerFileType("TSV (tab-separated)")    { Patterns = new[] { "*.tsv", "*.txt" } },
                new FilePickerFileType("Excel workbook")         { Patterns = new[] { "*.xlsx" } },
                FilePickerFileTypes.All
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    /// <summary>
    /// Shows the "Open from online registry (by event ID)" dialog.
    /// Returns the VM on OK, null on Cancel.
    /// </summary>
    private async Task<OpenFromRegistryViewModel?> ShowOpenFromRegistryDialogAsync(OpenFromRegistryViewModel vm)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return null;
        var dialog = new OpenFromRegistryDialog(vm);
        return await dialog.ShowDialog<OpenFromRegistryViewModel?>(owner);
    }

    /// <summary>
    /// Shows the "Browse online events" dialog. Returns the VM on
    /// OK (selected event + passcode), null on Cancel.
    /// </summary>
    private async Task<BrowseRegistryEventsViewModel?> ShowBrowseRegistryEventsDialogAsync(BrowseRegistryEventsViewModel vm)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return null;
        var dialog = new BrowseRegistryEventsDialog(vm);
        return await dialog.ShowDialog<BrowseRegistryEventsViewModel?>(owner);
    }

    /// <summary>Opens the "Export USCF report files" dialog.</summary>
    private async Task<UscfExportViewModel?> ShowUscfExportDialogAsync(UscfExportViewModel vm)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return null;
        var dialog = new UscfExportDialog(vm);
        return await dialog.ShowDialog<UscfExportViewModel?>(owner);
    }

    /// <summary>
    /// Save-file picker scoped to <c>.sjson</c> for the New-event
    /// flow. Returns the local path or null on cancel.
    /// </summary>
    private async Task<string?> PickNewEventSavePathAsync(string suggestedFolder, string suggestedName)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        IStorageFolder? startFolder = null;
        if (!string.IsNullOrWhiteSpace(suggestedFolder))
        {
            try
            {
                startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(suggestedFolder);
            }
            catch
            {
                // Folder may not exist yet on some platforms; fall
                // back to the OS default start location silently.
            }
        }

        var options = new FilePickerSaveOptions
        {
            Title = "Save new tournament as",
            SuggestedFileName = suggestedName,
            SuggestedStartLocation = startFolder,
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
