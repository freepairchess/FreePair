using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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
            vm.PickTournamentsRootFolderAsync = PickTournamentsRootFolderAsync;
        }
    }

    private async Task<string?> PickPairingEngineBinaryAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = "Select pairing engine binary",
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
}
