using System;
using System.Threading.Tasks;
using Avalonia.Controls;
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
}
