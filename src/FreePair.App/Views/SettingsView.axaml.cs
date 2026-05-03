using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FreePair.App.ViewModels;

namespace FreePair.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.PickPairingEngineBinaryAsync = PickPairingEngineBinaryAsync;
            vm.PickUscfEngineBinaryAsync    = PickUscfEngineBinaryAsync;
            vm.PickTournamentsRootFolderAsync = PickTournamentsRootFolderAsync;
        }
    }

    private Task<string?> PickPairingEngineBinaryAsync() =>
        PickEngineBinaryAsync("Select BBP pairing engine binary");

    private Task<string?> PickUscfEngineBinaryAsync() =>
        PickEngineBinaryAsync("Select FreePair USCF engine binary");

    private async Task<string?> PickEngineBinaryAsync(string title)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Executable")
                {
                    Patterns = new[] { "*.exe", "*" }
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

    /// <summary>
    /// Folder picker for the tournaments-root setting. Returns the
    /// absolute path of the chosen folder, or null on cancel.
    /// </summary>
    private async Task<string?> PickTournamentsRootFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        var options = new FolderPickerOpenOptions
        {
            Title = "Select tournaments root folder",
            AllowMultiple = false,
        };

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }

    /// <summary>
    /// Re-opens the first-run pairing-engine disclosure dialog on
    /// demand from the Settings view. The TD's "Don't show this
    /// again" choice on close updates
    /// <c>AppSettings.HasAcknowledgedPairingEngineNotice</c> so
    /// they can also use this to UN-suppress the dialog (uncheck
    /// the box) and have it re-appear on the next launch.
    /// </summary>
    private async void OnShowPairingEngineNotice(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        if (DataContext is not SettingsViewModel vm) return;

        var dontShowAgain = await new PairingEngineNoticeDialog().ShowDialog<bool>(owner);
        await vm.SetPairingEngineNoticeAcknowledgedAsync(dontShowAgain);
    }
}
